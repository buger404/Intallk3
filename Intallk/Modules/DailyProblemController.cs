using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

public class DailyProblemController : SimpleOneBotController
{
    private readonly DailyProblemService DailyProblemService;
    public DailyProblemController(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService permissionService, DailyProblemService dailyProblemService) : base(commandService, logger, permissionService)
    {
        DailyProblemService = dailyProblemService;
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        { 
            HelpCmd = "leetcode", ModuleName = "力扣每日一问", ModuleUsage = "为群里推送每日力扣问题。（感谢TLMegalovania的贡献！！）",
            RootPermission = "LEETCODETODAY",
            RegisteredPermission = new()
            {
                ["FETCH"] = ("手动取得题目权限", PermissionPolicy.AcceptedIfGroupAccepted),
                ["PUSH"] = ("群每日自动推送权限（群权限）", PermissionPolicy.RequireAccepted)
            }
        };

    [Command("leetcode today")]
    [CmdHelp("立即获取今日力扣问题")]
    public async void FetchImmidiate(GroupMessageEventArgs e)
    {
        if (!PermissionService.Judge(e, Info, "FETCH"))
            return;
        string? msg = await DailyProblemService.FetchDailyMessage();
        if (msg == null)
        {
            await e.Reply("获取失败。");
            return;
        }
        await e.Reply(msg);
    }
}
