using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;
using OneBot.CommandRoute.Services.Implements;
using Sora.EventArgs.SoraEvent;
using System.Reflection;
using System.Text;

namespace Intallk.Modules;

public class CommandCD : ArchiveOneBotController<CmdCDModel>
{
    public static bool Paused = false;

    private string prefix;
    private IServiceProvider Services {
        get {
            return MainModule.Services!;
        }
    }
    private Dictionary<(long, string), DateTime> useTime = new Dictionary<(long, string), DateTime>();

    public override ModuleInformation Initialize() =>
    new ModuleInformation
    {
        DataFile = "cd", ModuleName = "指令冷却", RootPermission = "CD",
        HelpCmd = "cd", ModuleUsage = "机器人指令的使用冷却，防止部分指令被滥用于刷屏的情况。",
        RegisteredPermission = new()
        {
            ["SET"] = ("设置指令CD", PermissionPolicy.AcceptedAdminAsDefault)
        }
    };

    public CommandCD(ICommandService commandService, ILogger<ArchiveOneBotController<CmdCDModel>> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
        commandService.Event.OnGroupMessageReceived += Event_OnGroupMessageReceived;
        prefix = MainModule.Config!.CommandPrefix[0];
    }

    public override void OnDataNull() =>
        Data = new CmdCDModel();

    private int Event_OnGroupMessageReceived(OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if (e == null)
            return 0;
        if ((!PermissionService.JudgeGroup(e, "USE", PermissionPolicy.RequireAccepted) || Paused)
            && !PermissionService.Judge(e, "ANYTHING", PermissionPolicy.RequireAccepted, true)
            && !e.Message.RawText.StartsWith(".hack"))
            return 1;
        if (Data == null)
            return 0;
        if (Data.CD.ContainsKey(e.SourceGroup.Id))
        {
            foreach (CDInfo cd in Data.CD[e.SourceGroup.Id]!)
            {
                if (e.Message.RawText.ToLower().StartsWith(prefix + cd.CmdHead!))
                {
                    if (!PermissionService.Judge(e, Info.RootPermission + "_NOCD", PermissionPolicy.AcceptedAdminAsDefault , true))
                    {
                        (long, string) pair = (e.Sender.Id, cd.CmdHead!);
                        if (!useTime.ContainsKey(pair))
                            useTime.Add(pair, DateTime.Now);
                        TimeSpan span = DateTime.Now - useTime[pair];
                        if (span.TotalSeconds < cd.Duration)
                        {
                            span = TimeSpan.FromSeconds(cd.Duration) - span;
                            e.Reply(e.Sender.At() + " 指令冷却尚未结束(" + Math.Ceiling(span.TotalMinutes).ToString() + ":" + Math.Ceiling(span.TotalSeconds % 60).ToString("00") + ")");
                            return 1;
                        }
                        else
                        {
                            useTime[pair] = DateTime.Now;
                        }
                    }
                    break;
                }
            }
        }
        return 0;
    }

    ModuleInformation? SeekInfoByCmdHead(string head)
    {
        foreach (IOneBotController controller in Services!.GetServices<IOneBotController>())
        {
            if (controller is SimpleOneBotController module)
            {
                ModuleInformation? info = module.Info;
                if (info != null)
                {
                    foreach (MethodInfo minfo in module.GetType().GetMethods())
                    {
                        CommandAttribute? cmd = minfo.GetCustomAttribute<CommandAttribute>();
                        if (cmd != null)
                        {
                            if (cmd.Pattern.ToLower().StartsWith(head))
                                return info;
                        }
                    }
                }
            }
        }
        return null;
    }

    [Command("cd set <cmdhead> <duration>")]
    [CmdHelp("内容 时长", "设置以'内容'开头的指令的冷却时长，时长表示方法：2s、1m5s等")]
    public void CDSet(GroupMessageEventArgs e, string cmdhead, Duration duration)
    {
        if (!PermissionService.Judge(e, Info, "SET"))
            return;
        if (Data == null)
            return;
        if (!Data.CD.ContainsKey(e.SourceGroup.Id))
            Data.CD.Add(e.SourceGroup.Id, new List<CDInfo>());
        int i = Data.CD[e.SourceGroup.Id].FindIndex(x => x.CmdHead == cmdhead.ToLower());
        if (duration.Seconds < 0)
        {
            e.Reply("请输入正确的时长！");
            return;
        }
        if (duration.Seconds == 0)
        {
            if (i == -1)
                e.Reply("无需删除冷却，因为先前未设置过。");
            else
            {
                Data.CD[e.SourceGroup.Id].RemoveAt(i);
                e.Reply("已删除对指令'" + cmdhead + "'的冷却设定。");
            }
        }
        else
        {
            if (i == -1)
            {
                ModuleInformation? info = SeekInfoByCmdHead(cmdhead.ToLower());
                if (info == null)
                {
                    e.Reply("机器人似乎并没有这样的指令，无法设定冷却。");
                    return;
                }
                Data.CD[e.SourceGroup.Id].Add(new CDInfo
                {
                    CmdHead = cmdhead.ToLower(), Duration = duration.Seconds,
                    RootPermission = info.RootPermission
                });
            }
            else
                Data.CD[e.SourceGroup.Id][i].Duration = duration.Seconds;
            e.Reply("已设置对指令'" + cmdhead + "'的冷却设定：" + duration.Seconds + "秒");
        }
        Save();
    }

    [Command("cd remove <cmdhead>")]
    [CmdHelp("内容", "取消以'内容'开头的指令的冷却")]
    public void CDRemove(GroupMessageEventArgs e, string cmdhead)
        => CDSet(e, cmdhead, new Duration(0));
}
