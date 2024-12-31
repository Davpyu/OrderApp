using DotNetService.Domain.Auth.Services;
using DotNetService.Domain.File.Services;
using DotNetService.Domain.Logging.Services;
using DotNetService.Domain.Notification.Services;
using DotNetService.Domain.Order.Services;
using DotNetService.Domain.Permission.Services;
using DotNetService.Domain.Role.Services;
using DotNetService.Domain.User.Services;
using DotNetService.Infrastructure.Shareds;

namespace DotNetService
{
    public partial class Startup
    {
        public void Services(IServiceCollection services)
        {
            services.AddScoped<AuthService>();
            services.AddScoped<UserService>();
            services.AddScoped<PermissionService>();
            services.AddScoped<RoleService>();

            services.AddScoped<LoggingService>();
            services.AddScoped<StorageService>();
            services.AddScoped<FileService>();

            services.AddScoped<NotificationService>();

            services.AddScoped<OrderService>();
        }
    }
}
