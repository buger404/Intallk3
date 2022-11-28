using Intallk.Config;
using Intallk.Models;
using Newtonsoft.Json;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

public class MainModule : IOneBotController
{
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
    public void Help(GroupMessageEventArgs e) => 
        e.Reply("欢迎查看黑嘴使用说明\n" +
                "黑嘴：不要叫我，黑嘴超级忙，我不在！！！听见没！！\n" +
                "<>表示必填，[]表示可不填，/表示前后两个皆可，填写的时候不必抄写括号。\n" +
                ".test <次数>：测试\n" + 
                ".sx <中文缩写>：让黑嘴帮你搜一下这个缩写的意思\n" + 
                ".bug <内容>：欸？这是什么我也不知道呢。\n" +
                ".gifextract：请黑嘴帮你把一张动态图片拆成好几张静态图片。\n" +
                ".draw help：查看制图相关指定说明。\n" +
                ".re help：查看语录库的使用帮助。\n" +
                ".random <最小数> <最大数>：随机抽取一个数。\n" +
                ".random <数量>：随机抽取群内几位成员。\n" +
                ".keyword [列出项数]：查看你群今日截至现在的词云\n" +
                ".keyword switch on/off：开启或关闭你群词云统计（开启后才能使用词云）。\n" +
                ".t：回溯最近的10条消息。（防撤回）");

    [Command("status")]
    public void Status(GroupMessageEventArgs e) =>
        e.Reply("黑嘴黑嘴运转良好。\n" +
                "语录库总收录：" + RepeatCollector.Instance!.Data?.messages.Count ?? 0 + "\n" +
                "语录库备份时间：" + RepeatCollector.DumpTime.ToString() + "\n" +
                "关键词备份时间：" + MsgWordCloud.DumpTime.ToString() + "\n" +
                "绘图模板总收录：" + Painting.paints.Count + "\n" +
                "总计异常抛出数量：" + ExceptionCount.ToString());

}
