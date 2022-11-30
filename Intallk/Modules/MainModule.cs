using Intallk.Config;
using Intallk.Models;
using Newtonsoft.Json;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Configuration;
using OneBot.CommandRoute.Services;
using OneBot.CommandRoute.Services.Implements;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Intallk.Modules;

public class MainModule : IOneBotController
{
    public static IServiceProvider? Services { get; set; }
    public static IntallkConfig? Config { get; set; }
    public delegate Task<bool> PrivateMessageHookCallback(PrivateMessageEventArgs e, PrivateMessageHook hook);
    public class PrivateMessageHook
    {
        public long QQ { get; set; }
        public PrivateMessageHookCallback? Callback { get; set; }
        public object? Data { get; set; }
    }
    public delegate Task<bool> GroupMessageHookCallback(GroupMessageEventArgs e, GroupMessageHook hook);
    public class GroupMessageHook
    {
        public long Group { get; set; }
        public long QQ { get; set; }
        public GroupMessageHookCallback? Callback { get; set; }
        public object? Data { get; set; }
    }
    public static List<GroupMessageHook> hooks = new();
    public static List<PrivateMessageHook> hooks2 = new();
    public static int ExceptionCount = 0;
    public static Dictionary<long, DateTime> replyTime = new Dictionary<long, DateTime>();
    readonly System.Random random = new(Guid.NewGuid().GetHashCode());
    readonly ILogger<MainModule> _logger;
    readonly ICommandService _commandService;
    public static Dictionary<long, string> nicks = new Dictionary<long, string>();
    public static string GetCacheQQName(object? e, long qqid)
    {
        //Console.WriteLine("Cache fetching nick: " + qqid);
        string ret = "";
        if(nicks.ContainsKey(qqid)) return nicks[qqid];
        ret = GetQQName(e, qqid);
        nicks.Add(qqid, ret);
        return ret;
    }
    public static string GetQQName(object? e, long qqid)
    {
        Console.WriteLine("Fetching nick: " + qqid);
        string ret = "";
        try
        {
            switch (e)
            {
                case GroupMessageEventArgs group:
                    GroupMemberInfo info = group.SourceGroup.GetGroupMemberInfo(qqid).Result.memberInfo;
                    ret = info.Card;
                    if (ret == "" || ret == null) ret = info.Nick;
                    if (ret == "" || ret == null)
                    {
                        UserInfo userinfo = group.SoraApi.GetUserInfo(qqid).Result.userInfo;
                        ret = userinfo.Nick;
                    }
                    if (ret == "" || ret == null) ret = qqid.ToString();
                    break;
                case PrivateMessageEventArgs qq:
                    UserInfo user = qq.SoraApi.GetUserInfo(qqid).Result.userInfo;
                    ret = user.Nick;
                    break;
            }
        }
        catch
        {
            ret = qqid.ToString() + "(异常账号)";
        }
        return ret;
    }
    public void LogError(Exception exception)
    {
        File.AppendAllText(IntallkConfig.DataPath + "\\Logs\\error_" + DateTime.Now.ToString("yy_MM_dd") + ".txt", DateTime.Now.ToString() + "\n" + exception.Message + "\n" + exception.StackTrace + "\n");
    }
    public MainModule(ICommandService commandService, ILogger<MainModule> logger)
    {
        _logger = logger;
        _commandService = commandService;
        foreach (string file in Directory.GetFiles(IntallkConfig.DataPath + "\\DrawingScript"))
        {
            string code = File.ReadAllText(file);
            JsonSerializer serializer = new();
            PaintFile paintfile = (PaintFile)serializer.Deserialize(new StringReader(code), typeof(PaintFile))!;
            Painting.paints.Add(new PaintingProcessing(paintfile));
        }
        logger.LogInformation("已读入" + Painting.paints.Count + "个绘图模板。");
        commandService.Event.OnException += (context, exception) =>
        {
            ExceptionCount++;
            logger.LogError(exception.Message + "\n" + exception.StackTrace);
            switch (context.SoraEventArgs)
            {
                case GroupMessageEventArgs group:
                    group.Reply("出错啦");
                    group.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                    break;
                case PrivateMessageEventArgs qq:
                    qq.Reply("出错啦");
                    qq.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                    break;
            }
            // 记小本本
            LogError(exception);
        };
        commandService.Event.OnGroupMessage += (context) =>
        {
            var e = (GroupMessageEventArgs)context.SoraEventArgs;
            // Debug
            /**
            if (e.Sender.Id != 1361778219 && e.Message.RawText.StartsWith('.'))
            {
                e.Reply("非常抱歉，现在黑嘴正在被404调整改造中，暂时无法使用呢qwq");
                return 1;
            }**/
            bool needClear = false;
            foreach (var hook in hooks)
            {
                if (hook.QQ == e.Sender.Id && hook.Group == e.SourceGroup.Id)
                {
                    try
                    {
                        if (hook.Callback!(e, hook).Result)
                        {
                            hook.QQ = 0;
                            needClear = true;
                        }
                    }
                    catch(Exception err)
                    {
                        LogError(err);
                        e.Reply(e.Sender.At() + "出了些问题，黑嘴无法继续会话。\n" + err.Message);
                        hook.QQ = 0;
                        needClear = true;
                    }
                }
            }
            if (needClear) hooks.RemoveAll(m => m.QQ == 0);
            return 0;
        };
        commandService.Event.OnFriendRequest += (context) =>
        {
            var e = (FriendRequestEventArgs)context.SoraEventArgs;
            e.Accept();
            e.Sender.SendPrivateMessage("您已成功与黑嘴添加好友，感谢您对黑嘴的支持。😘");
            e.Sender.SendPrivateMessage(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
            return 1;
        };
        commandService.Event.OnGroupRequest += (context) =>
        {
            var e = (AddGroupRequestEventArgs)context.SoraEventArgs;
            e.Accept();
            e.SourceGroup.SendGroupMessage("大家好呀，我是机器人黑嘴~发送'.help'可以查看说明书哦~");
            e.SourceGroup.SendGroupMessage(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
            return 1;
        };
        commandService.Event.OnPrivateMessage += (context) =>
        {
            var e = (PrivateMessageEventArgs)context.SoraEventArgs;
            bool sendBio = false;
            if (!replyTime.ContainsKey(e.Sender.Id))
            {
                replyTime.Add(e.Sender.Id, DateTime.Now);
                sendBio = true;
            }
            else
            {
                if ((DateTime.Now - replyTime[e.Sender.Id]).TotalMinutes > 20)
                {
                    sendBio = true;
                    replyTime[e.Sender.Id] = DateTime.Now;
                }
            }
            if (sendBio)
            {
                e.Reply("😊您好呀，我是404的机器人黑嘴，您可以在群里发送'.help'查看我的指令说明书噢~\n" +
                        "如果您要联系404，也可以：QQ1361778219。\n黑嘴将自动处理消息，因此404很少查看黑嘴的消息，有事请联系404，谢谢ヾ(≧▽≦*)o");
            }

            bool needClear = false;
            try
            {
                foreach (var hook2 in hooks2)
                {
                    if (hook2.QQ == e.Sender.Id)
                    {
                        try
                        {
                            if (hook2.Callback!(e, hook2).Result)
                            {
                                hook2.QQ = 0;
                                needClear = true;
                            }
                        }
                        catch(Exception err)
                        {
                            LogError(err);
                            e.Reply("出了些问题，黑嘴无法继续会话。\n" + err.Message);
                            hook2.QQ = 0;
                            needClear = true;
                        }
                    }
                }
            }
            catch
            {

            }
            if (needClear) hooks2.RemoveAll(m => m.QQ == 0);
            return 0;
        };
    }

    public static void RegisterHook(long QQ, PrivateMessageHookCallback Callback, object Data = null!)
    {
        hooks2.Add(new PrivateMessageHook
        {
            QQ = QQ,
            Callback = Callback,
            Data = Data
        });
    }

    public static void RegisterHook(long QQ, long Group, GroupMessageHookCallback Callback, object Data = null!)
    {
        hooks.Add(new GroupMessageHook
        {
            QQ = QQ,
            Group = Group,
            Callback = Callback,
            Data = Data
        });
    }

    [Command("黑嘴")]
    public void Bark(GroupMessageEventArgs e)
    {
        string[] eg = { "爬", "才...才不告诉你我在呢", "干嘛啦", "老娘活着", "我不在" };
        e.Reply(eg[random.Next(0, eg.Length)]);
    }

    [Command("黑嘴！")]
    public void Bark2(GroupMessageEventArgs e)
    {
        string[] eg = { "爬！", "老娘忙着！", "？什么事", "？", "我不在！不在！不在！不在！" };
        e.Reply(eg[random.Next(0, eg.Length)]);
    }
    [Command("黑嘴？")]
    public void Bark3(GroupMessageEventArgs e)
    {
        string[] eg = { "😅", "🤔", "😕", "？", "咋？" };
        e.Reply(eg[random.Next(0, eg.Length)]);
    }
    [Command("黑嘴晚安")]
    public void Bark4(GroupMessageEventArgs e)
    {
        string[] eg = { "嗯嗯，晚安哦", "晚安~", "嗯嗯，早点休息~", "快睡吧，一天下来也累了吧" };
        e.Reply(eg[random.Next(0, eg.Length)]);
    }
    [Command("黑嘴爱你")]
    public void Bark5(GroupMessageEventArgs e)
    {
        string[] eg = { "？？？？？？？？？？", "你不对劲你不对劲？？", "？？？不要这样，很突然，我很害怕", "？？嗯，，，嗯，。。。我，我也。。。爱   你！",
                        "（怎么办怎么办有人和我告白呜哇哇哇哇）","谢谢你，但是。。。我已经有喜欢的狗了。","！！！黑嘴很感动，但是...人和狗是...不可以的",
                        "你xp有点怪嗷","别，别，别。。。我没有经验的。","！！！对不起！现在才意识到！谢谢你，但是。不行","我知道，其实...但是...真的不可以",
                        "老娘下班了","不可以~现在黑嘴还在工作呢~谢谢你的心意。"};
        e.Reply(eg[random.Next(0, eg.Length)]);
    }

    [Command("help")]
    public void Help(GroupMessageEventArgs e)
    {
        string prefix = Config!.CommandPrefix[0];
        StringBuilder sb = new StringBuilder();
        foreach (IOneBotController controller in Services!.GetServices<IOneBotController>())
        {
            if (controller is SimpleOneBotController module)
            {
                ModuleInformation? info = module.Info;
                if (info != null)
                {
                    if (info.HelpCmd != null)
                    {
                        sb.AppendLine(prefix + "help " + info.HelpCmd + " ：" + info.ModuleName + " 使用指南");
                    }
                }
            }
        }
        e.Reply("🌈欢迎查看黑嘴使用说明！\n" +
                "目前支持的功能：\n" + sb.ToString());
    }
        

    [Command("help <moduleName>")]
    public void ModuleHelp(GroupMessageEventArgs e, string moduleName)
    {
        string prefix = Config!.CommandPrefix[0];
        foreach (IOneBotController controller in Services!.GetServices<IOneBotController>())
        {
            if (controller is SimpleOneBotController module)
            {
                ModuleInformation? info = module.Info;
                if (info != null)
                {
                    if (info.HelpCmd == moduleName)
                    {
                        StringBuilder sb = new StringBuilder(), pub = new StringBuilder(), pri = new StringBuilder();
                        sb.AppendLine("欢迎使用功能'" + info.ModuleName + "'！\n" + info.ModuleUsage);
                        foreach(MethodInfo minfo in module.GetType().GetMethods())
                        {
                            CmdHelpAttribute? help = minfo.GetCustomAttribute<CmdHelpAttribute>();
                            if (help != null)
                            {
                                CommandAttribute? cmd = minfo.GetCustomAttribute<CommandAttribute>();
                                if (cmd == null) 
                                    continue;
                                string cmdDes = prefix;
                                if (help.ArgDescription == "")
                                {
                                    cmdDes += cmd.Pattern;
                                }
                                else
                                {
                                    string[] des = help.ArgDescription.Split(' ');
                                    int j = 0; bool flag = false;
                                    for(int i = 0;i < des.Length; i++)
                                    {
                                        for(;j < cmd.Pattern.Length; j++)
                                        {
                                            if (cmd.Pattern[j] == '<' || cmd.Pattern[j] == '[')
                                            {
                                                cmdDes += cmd.Pattern[j];
                                                flag = true;
                                            }
                                            else if (cmd.Pattern[j] == '>' || cmd.Pattern[j] == ']')
                                            {
                                                cmdDes += des[i] + cmd.Pattern[j];
                                                flag = false;
                                                j++;
                                                break;
                                            }
                                            if (!flag) cmdDes += cmd.Pattern[j];
                                        }
                                    }
                                }
                                cmdDes += "：" + help.UsageDescription;
                                if (cmd.EventType == OneBot.CommandRoute.Models.Enumeration.EventType.GroupMessage)
                                {
                                    pub.AppendLine(cmdDes);
                                }
                                else
                                {
                                    pri.AppendLine(cmdDes);
                                }
                            }
                        }
                        if (pub.Length > 0)
                        {
                            sb.AppendLine("🌈群指令指南：");
                            sb.Append(pub);
                        }
                        if (pri.Length > 0)
                        {
                            sb.AppendLine("🌈私聊指令指南：");
                            sb.Append(pri);
                        }
                        sb.AppendLine("💡<>内的表示必须填写，[]内的表示可不填写。");
                        e.Reply(sb.ToString());
                        return;
                    }
                }
            }
        }
    }

    [Command("status")]
    public void Status(GroupMessageEventArgs e)
    {

    }
}
