using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Services;
using System.Text;

namespace Intallk.Modules;

public class SimpleOneBotController : IOneBotController
{
    public static ILogger<SimpleOneBotController>? Logger;
    public static ICommandService? Service;
    public static ModuleInformation? Info;

    public SimpleOneBotController(ICommandService commandService, ILogger<SimpleOneBotController> logger)
    {
        Service = commandService;
        Logger = logger;
        Info = Initialize();
    }

    public virtual ModuleInformation Initialize()
    {
        throw new NotImplementedException();
    }
}
