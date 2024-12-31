using DotNetService.Domain.Auth.Repositories;
using DotNetService.Domain.Notification.Repositories;
using DotNetService.Domain.Order.Repositories;
using DotNetService.Domain.Permission.Repositories;
using DotNetService.Domain.Role.Repositories;
using DotNetService.Domain.User.Repositories;

namespace DotNetService
{
    public partial class Startup
    {
        public void Repositories(IServiceCollection services)
        {
            services.AddScoped<AuthStoreRepository>();
            services.AddScoped<AuthQueryRepository>();
            services.AddScoped<UserQueryRepository>();
            services.AddScoped<UserStoreRepository>();
            services.AddScoped<RoleQueryRepository>();
            services.AddScoped<RoleStoreRepository>();
            services.AddScoped<PermissionQueryRepository>();
            services.AddScoped<PermissionStoreRepository>();
            services.AddScoped<NotificationQueryRepository>();
            services.AddScoped<NotificationStoreRepository>();
            services.AddScoped<OrderQueryRepository>();
            services.AddScoped<OrderStoreRepository>();
        }
    }
}
