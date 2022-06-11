using Intallk.Config;
using Intallk.Modules;

using OneBot.CommandRoute.Configuration;
using OneBot.CommandRoute.Mixin;
using OneBot.CommandRoute.Models.VO;
using OneBot.CommandRoute.Services;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureOneBotHost();

builder.ConfigureServices((context, services) =>
{
    IConfiguration configuration = context.Configuration;

    // 配置机器人核心
    // 设置 OneBot 配置
    services.Configure<CQHttpServerConfigModel>(configuration.GetSection("CQHttpConfig"));
    services.ConfigureOneBot();

    // 添加指令 / 事件
    // 推荐使用单例模式（而实际上框架代码也是当单例模式使用的）
    services.AddSingleton<IOneBotController, MainModule>()
        .AddSingleton<IOneBotController, GIFProcess>()
        .AddSingleton<IOneBotController, Testing>()
        .AddSingleton<IOneBotController, BugLanguage>()
        .AddSingleton<IOneBotController, Nbnhhsh>()
        .AddSingleton<IOneBotController, RepeatCollector>()
        .AddSingleton<IOneBotController, Painting>()
        .AddSingleton<IOneBotController, Keyword>()
        .AddSingleton<IOneBotCommandRouteConfiguration, IntallkConfig>();

    foreach (string childPath in new string[] { "", "\\Images", "\\Cache", "\\Resources", "\\Logs", "\\FileDetection" })
    {
        if (!Directory.Exists(IntallkConfig.DataPath + childPath))
            Directory.CreateDirectory(IntallkConfig.DataPath + childPath);
    }


});


var app = builder.Build();
app.Run();