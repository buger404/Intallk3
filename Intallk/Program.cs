using Intallk.Config;
using Intallk.Modules;

using OneBot.CommandRoute.Configuration;
using OneBot.CommandRoute.Mixin;
using OneBot.CommandRoute.Models.VO;
using OneBot.CommandRoute.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
IConfiguration configuration = builder.Configuration;

#region ConfigureServices
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
    .AddSingleton<IOneBotController, ScriptDrawer>()
    .AddSingleton<IOneBotController, Painting>()
    .AddSingleton<IOneBotCommandRouteConfiguration, IntallkConfig>();
// 一行一行地将指令模块加进去

foreach (string childPath in new string[] { "", "\\Images", "\\Cache", "\\Resources", "\\Logs" })
{
    if (!Directory.Exists(IntallkConfig.DataPath + childPath))
        Directory.CreateDirectory(IntallkConfig.DataPath + childPath);
}
#endregion

var app = builder.Build();

#region Configure
#endregion

app.Run();