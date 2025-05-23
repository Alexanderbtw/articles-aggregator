using ArticlesAggregator.Infrastructure;
using ArticlesAggregator.Infrastructure.Abstractions;
using ArticlesAggregator.Worker.Options;
using ArticlesAggregator.Worker.Routers;
using ArticlesAggregator.Worker.Routers.Abstractions;
using ArticlesAggregator.Worker.Workers;

using Microsoft.Extensions.Options;

using Telegram.Bot;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("Bot"));

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    BotOptions opt = sp.GetRequiredService<IOptions<BotOptions>>().Value;

    return new TelegramBotClient(opt.Token);
});

InfrastructureRegistrar.Congigure(builder.Services);

builder.Services.AddScoped<IUpdateRouter, UpdateRouter>();
builder.Services.AddHostedService<UpdateWorker>();
builder.Services.AddMemoryCache();

await builder.Build().RunAsync();
