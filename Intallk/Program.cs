using Intallk.Config;
using Intallk.Models;
using Intallk.Modules;
using Microsoft.Net.Http.Headers;
using OneBot.CommandRoute.Configuration;
using OneBot.CommandRoute.Mixin;
using OneBot.CommandRoute.Models.VO;
using OneBot.CommandRoute.Services;
using static Intallk.Models.DictionaryReplyModel;

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
            .AddSingleton<IOneBotController, MsgWordCloud>()
            .AddSingleton<IOneBotController, UrlPreview>()
            .AddSingleton<IOneBotController, IntallkRandom>()
            .AddSingleton<IOneBotController, TTS>()
            .AddSingleton<IOneBotController, DictionaryReply>()
            .AddSingleton<IOneBotController, Permission>()
            .AddSingleton<IOneBotController, RhythmGameSong>()
            .AddSingleton<IOneBotController, DailyProblemCmd>()
            .AddSingleton<IOneBotController, Welcome>()
            .AddSingleton<IOneBotCommandRouteConfiguration, IntallkConfig>()
            .AddSingleton<PermissionService>()
            .AddHostedService<DailyProblem>()
            .AddHttpClient("leetcode", client =>
            {
                client.BaseAddress = new("https://leetcode.cn/graphql");
                client.DefaultRequestHeaders.Remove(HeaderNames.Cookie);
                client.DefaultRequestHeaders.Add(HeaderNames.Origin, "https://leetcode.cn");
            });

    foreach (string childPath in new string[] { "", "\\Images", "\\Cache", "\\Resources", "\\Logs", "\\FileDetection" })
    {
        if (!Directory.Exists(IntallkConfig.DataPath + childPath))
            Directory.CreateDirectory(IntallkConfig.DataPath + childPath);
    }


});


var app = builder.Build();
MainModule.Services = app.Services;
MainModule.Config = new IntallkConfig();
app.Run();