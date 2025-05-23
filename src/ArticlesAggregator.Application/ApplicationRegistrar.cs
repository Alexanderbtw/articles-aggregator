using Microsoft.Extensions.DependencyInjection;

namespace ArticlesAggregator.Application;

public static class ApplicationRegistrar
{
    public static void Configure(IServiceCollection services)
    {
        services.AddMediatR(conf =>
        {
            conf.RegisterServicesFromAssemblyContaining(typeof(ApplicationRegistrar));
        });
    }
}
