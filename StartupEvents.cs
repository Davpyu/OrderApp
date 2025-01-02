using DotNetService.Domain.Order.Listeners;

namespace DotNetService
{
    public partial class Startup
    {
        public void Events(IServiceCollection services)
        {
            services.AddScoped<CheckInventoryEvent>();
        }
    }
}