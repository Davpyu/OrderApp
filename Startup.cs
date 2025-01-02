
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Extensions.Http;
using FluentValidation.AspNetCore;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using DotNetService.Infrastructure.Integrations.Http;
using DotNetService.Constants.Logger;
using Microsoft.AspNetCore.DataProtection;
using DotNetService.Infrastructure.Middlewares;
using DotNetService.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc;
using NATS.Client.Hosting;
using NATS.Client.Core;
using DotNetService.Infrastructure.Queues;
using DotNetService.Infrastructure.BackgroundHosted;
using System.Net;
using DotNetService.Infrastructure.Exceptions;
using DotNetService.Infrastructure.Databases;
using DotNetService.Infrastructure.ModelBinder;
using Quartz;
using DotNetService.Infrastructure.Jobs;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace DotNetService
{
    public partial class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration { get; } = configuration;

        public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            var cooldownBreak = int.Parse(Configuration["CircuitBreaker:External:Cooldown"] ?? "5");
            var AllowedBroken = int.Parse(Configuration["CircuitBreaker:External:AllowedBroken"] ?? "5");

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(AllowedBroken, TimeSpan.FromMinutes(cooldownBreak));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        [Obsolete]
        public void ConfigureServices(IServiceCollection services)
        {
            if (!Directory.Exists("Storage")) Directory.CreateDirectory("Storage");

            var hostName = Dns.GetHostName();

            AddLogging(services);

            AddNats(services);

            Services(services);

            Repositories(services);

            Authentications(services);

            Integrations(services);

            Commands(services);

            Listeners(services);

            Jobs(services);

            Events(services);

            // Queue Servicee
            services.AddHostedService<QueuedHostedService>();
            services.AddHostedService<ConnectionPoolCheckerService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ConnectionPoolCheckerService>>();
                var connectionString = Configuration["ConnectionString:DefaultConnection1"];
                var interval = Configuration["ConnectionPoolCheckerInterval"] != null ? int.Parse(Configuration["ConnectionPoolCheckerInterval"]) : 60;
                return new ConnectionPoolCheckerService(logger, connectionString, interval);
            });
            services.AddSingleton(ctx =>
            {
                if (!int.TryParse(Configuration["Queue:Capacity"], out var queueCapacity)) queueCapacity = 100;
                return new BackgroundTaskQueue(queueCapacity);
            });

            // Hosted Background Service
            services.AddHostedService<NATsListener>();

            services.AddHealthChecks();

            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: "AllowOrigin",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                    });
            });

            services.AddFluentValidation(fvc => fvc.RegisterValidatorsFromAssemblyContaining<Startup>());

            var poolSize = Configuration["ConnectionPoolSize:DefaultConnection1"] != null ? int.Parse(Configuration["ConnectionPoolSize:DefaultConnection1"]) : 50;

            services.AddDbContextPool<IamDBContext>(
                options => options.UseSqlServer(Configuration["ConnectionString:DefaultConnection1"] ?? "").UseSnakeCaseNamingConvention(),
                poolSize
            );

            services.AddHttpContextAccessor();

            services.Configure<ApiBehaviorOptions>(opt =>
            {
                opt.SuppressModelStateInvalidFilter = true;
            });

            services.AddControllers(
                options =>
                {
                    options.Filters.Add<ValidatorAttribute>();
                    // Handling global [FromQuery] dto
                    options.ModelBinderProviders.Insert(0, new SnakeCaseQueryModelBinderProvider());
                }
            ).AddNewtonsoftJson(
                options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };
                }
            );

            // services.AddOpenApi();

            var circuitBreakerPolicy = GetCircuitBreakerPolicy();

            services.AddHttpClient<HttpIntegration>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(2)) // Circuit Breaker Cooldown time
                .AddPolicyHandler(circuitBreakerPolicy);

            var IsRedisEnable = bool.Parse(Configuration["Redis:IsEnable"]);
            var redisConnectionString = Configuration["Redis:Host"] + ':' + Configuration["Redis:Port"] + ",password=" + Configuration["Redis:Password"];
            if (IsRedisEnable)
            {
                services.AddDataProtection()
                    .SetApplicationName(Configuration["App:Name"] ?? "DotNetService")
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(60))
                    .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redisConnectionString), Configuration["App:DataProtectionKey"]);

                services.AddStackExchangeRedisCache(options => options.ConfigurationOptions = new ConfigurationOptions
                {
                    AllowAdmin = true,
                    Password = Configuration["Redis:Password"],
                    EndPoints = { Configuration["Redis:Host"] + ':' + Configuration["Redis:Port"] },
                    Ssl = false
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            // Quartz Scheduler
            services.AddQuartz(q =>
            {
                // base Quartz scheduler, job and trigger configuration
                var jobKey = new JobKey("NotificationHouseKeeping");
                q.AddJob<NotificationHouseKeepingJob>(opts => opts.WithIdentity(jobKey));

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("NotificationHouseKeepingTrigger")
                    // Schedule every first day of the month at 00:00
                    .WithCronSchedule("0 0 0 1 * ?")
                    )
                    ;
            });

            // ASP.NET Core hosting
            services.AddQuartzHostedService(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;
            });

            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Order Web API",
                    Description = ".NET Web API for Order App",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "John Doe",
                        Email = string.Empty,
                        Url = new Uri("https://google.com/"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Use under LICX",
                        Url = new Uri("https://example.com/license"),
                    }
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // DONT CHANGE THIS ORDERED MIDDLEWARE
            app.UseMiddleware<HandlerException>();
            app.UseMiddleware<CircuitBreakerMiddleware>();
            // ------

            app.UseMiddleware<HandlerException>();

            app.UseHttpsRedirection();

            app.UseCors("AllowOrigin");

            app.UseRouting();

            app.UseMiddleware<AuthorizationMiddleware>();

            app.UseResponseCaching();

            app.UseSwagger();
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Web API for Order App");
            });

            app.UseEndpoints(x =>
            {
                x.MapControllers();
                x.MapHealthChecks("/health").AllowAnonymous();
            });

            // app.MapOpenApi();
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
            {
                var appName = SanitizeFileName(Configuration["App:Name"]);
                var hostName = SanitizeFileName(Dns.GetHostName());
                var currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                loggingBuilder.AddFile($"Log/{appName}-{hostName}-{currentDate}-default.log",
                    fileLoggerOpts =>
                    {
                        fileLoggerOpts.Append = true;
                        fileLoggerOpts.FilterLogEntry = (msg) =>
                        {
                            return msg.LogLevel == LogLevel.Information
                                && !(
                                    msg.LogName == LoggerConstant.NATS ||
                                    msg.LogName == LoggerConstant.INTEGRATION ||
                                    msg.LogName == LoggerConstant.ACTIVITY
                                );
                        };
                    }
                );
                loggingBuilder.AddFile($"Log/{appName}-{hostName}-{currentDate}-integration.log",
                    fileLoggerOpts =>
                    {
                        fileLoggerOpts.Append = true;
                        fileLoggerOpts.FilterLogEntry = (msg) =>
                        {
                            return msg.LogName == LoggerConstant.INTEGRATION;
                        };
                    }
                );
                loggingBuilder.AddFile($"Log/{appName}-{hostName}-{currentDate}-nats.log",
                    fileLoggerOpts =>
                    {
                        fileLoggerOpts.Append = true;
                        fileLoggerOpts.FilterLogEntry = (msg) =>
                        {
                            return msg.LogName == LoggerConstant.NATS;
                        };
                    }
                );
                loggingBuilder.AddFile($"Log/{appName}-{hostName}-{currentDate}-activity.log",
                    fileLoggerOpts =>
                    {
                        fileLoggerOpts.Append = true;
                        fileLoggerOpts.FilterLogEntry = (msg) =>
                        {
                            return msg.LogName == LoggerConstant.ACTIVITY;
                        };
                    }
                );
                loggingBuilder.AddFile($"Log/{appName}-{hostName}-{currentDate}-error.log",
                    fileLoggerOpts =>
                    {
                        fileLoggerOpts.Append = true;
                        fileLoggerOpts.FilterLogEntry = (msg) =>
                        {
                            return msg.LogLevel == LogLevel.Error;
                        };
                    }
                );
            });
        }

        private void AddNats(IServiceCollection services)
        {
            services.AddNats(1000, options =>
            {

                var opts = new NatsOpts
                {
                    Url = Configuration["Nats:Url"],
                    AuthOpts = new NatsAuthOpts
                    {
                        Username = Configuration["Nats:Username"],
                        Password = Configuration["Nats:Password"],
                    },
                    Name = Configuration["Nats:Server"]
                };

                return opts;
            });
        }
    }
}
