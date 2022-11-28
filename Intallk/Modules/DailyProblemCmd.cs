using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

public class DailyProblemCmd : SimpleOneBotController
{
    public DailyProblemCmd(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService permissionService) : base(commandService, logger, permissionService)
    {
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation { HelpCmd = "dailyproblem", ModuleName = "力扣每日一问", ModuleUsage = "为群里推送每日力扣问题。（感谢TLMegalovania的贡献！！）",
                                RootPermission = "LEETCODETODAY"
        };

    [Command("dailyproblem fetch")]
    [CmdHelp("立即获取今日力扣问题")]
    public async void FetchImmidiate(GroupMessageEventArgs e)
    {
        if (!PermissionService.Judge(e, Info, "PUSH", Models.PermissionPolicy.AcceptedIfGroupAccepted))
            return;
        DailyProblem? instance = DailyProblem.Instance;
        if (instance == null)
        {
            await e.Reply("无法获取。");
            return;
        }
        string? msg = await instance.FetchDailyMessage();
        if (msg == null)
        {
            await e.Reply("获取失败。");
            return;
        }
        await e.Reply(msg);
    }
}
