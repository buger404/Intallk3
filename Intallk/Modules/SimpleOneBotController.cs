using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Services;
using System.Text;

namespace Intallk.Modules;

public class SimpleOneBotController : IOneBotController
{
    public ILogger<SimpleOneBotController> Logger;
    public ICommandService Service;
    public ModuleInformation Info;
    public PermissionService PermissionService;

    public SimpleOneBotController(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService permissionService)
    {
        Service = commandService;
        Logger = logger;
        PermissionService = permissionService;
        Info = Initialize();
    }

    public virtual ModuleInformation Initialize()
    {
        throw new NotImplementedException();
    }
}
