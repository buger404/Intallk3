using Intallk.Config;
using Intallk.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OneBot.CommandRoute.Configuration;
using OneBot.CommandRoute.Mixin;
using OneBot.CommandRoute.Models.VO;
using OneBot.CommandRoute.Services;

using System.IO;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
IConfiguration configuration = builder.Configuration;

#region ConfigureServices
// ���û����˺���
// ���� OneBot ����
services.Configure<CQHttpServerConfigModel>(configuration.GetSection("CQHttpConfig"));
services.ConfigureOneBot();

// ���ָ�� / �¼�
// �Ƽ�ʹ�õ���ģʽ����ʵ���Ͽ�ܴ���Ҳ�ǵ�����ģʽʹ�õģ�
services.AddSingleton<IOneBotController, MainModule>()
    .AddSingleton<IOneBotController, GIFProcess>()
    .AddSingleton<IOneBotController, Testing>()
    .AddSingleton<IOneBotController, BugLanguage>()
    .AddSingleton<IOneBotController, Nbnhhsh>()
    .AddSingleton<IOneBotController, ScriptDrawer>()
    .AddSingleton<IOneBotCommandRouteConfiguration, IntallkConfig>();
// һ��һ�еؽ�ָ��ģ��ӽ�ȥ

foreach (string childPath in new[] { "", "\\Images", "\\Cache", "\\Resources", "\\Logs" })
{
    Directory.CreateDirectory(IntallkConfig.DataPath + childPath);
}
#endregion

var app = builder.Build();

app.Run();