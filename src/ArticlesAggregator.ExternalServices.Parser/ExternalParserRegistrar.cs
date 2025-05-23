using ArticlesAggregator.ExternalServices.Parser;
using ArticlesAggregator.ExternalServices.Parser.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.Retry;

namespace ArticlesAggregator.ExternalServices.WikiApi;

public static class ExternalParserRegistrar
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExternalParserOptions>(
            configuration.GetSection(ExternalParserOptions.SectionName));

        services
            .AddHttpClient<ExternalParserHttpClient>()
            .AddResilienceHandler("ExternalParser", static builder =>
            {
                builder
                    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        BackoffType   = DelayBackoffType.Exponential,
                        Delay         = TimeSpan.FromSeconds(1),
                        MaxRetryAttempts = 3
                    })
                    .AddTimeout(TimeSpan.FromSeconds(10));
            });

        services.AddScoped<IExternalParserClient, ExternalParserClient>();
    }
}
