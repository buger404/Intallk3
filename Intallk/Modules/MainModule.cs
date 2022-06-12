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
    public delegate bool GroupMessageHookCallback(GroupMessageEventArgs e, GroupMessageHook hook);
    public class GroupMessageHook
    {
        public long Group { get; set; }
        public long QQ { get; set; }
        public GroupMessageHookCallback? Callback { get; set; }
        public object? Data { get; set; }
    }
    public static List<GroupMessageHook> hooks = new();
    public static List<PrivateMessageHook> hooks2 = new();
    readonly Random random = new(Guid.NewGuid().GetHashCode());
    readonly ILogger<MainModule> _logger;
    public static string GetQQName(object? e, long qqid)
    {
        string ret = "";
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
                break;
            case PrivateMessageEventArgs qq:
                UserInfo user = qq.SoraApi.GetUserInfo(qqid).Result.userInfo;
                ret = user.Nick;
                break;
        }
        return ret;
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
            logger.LogError(exception.Message + "\n" + exception.StackTrace);
            switch (context.SoraEventArgs)
            {
                case GroupMessageEventArgs group:
                    group.Reply("我...我才不是为了气死你才出错的呢！");
                    group.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                    break;
                case PrivateMessageEventArgs qq:
                    qq.Reply("我...我才不是为了气死你才出错的呢！");
                    qq.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                    break;
            }
            // 记小本本
            File.AppendAllText(IntallkConfig.DataPath + "\\Logs\\error_" + DateTime.Now.ToString("yy_MM_dd") + ".txt", DateTime.Now.ToString() + "\n" + exception.Message + "\n" + exception.StackTrace + "\n");
        };
        commandService.Event.OnGroupMessage += (context) =>
        {
            var e = (GroupMessageEventArgs)context.SoraEventArgs;
            bool needClear = false;
            foreach (var hook in hooks)
            {
                if (hook.QQ == e.Sender.Id && hook.Group == e.SourceGroup.Id)
                {
                    try
                    {
                        if (hook.Callback!(e, hook))
                        {
                            hook.QQ = 0;
                            needClear = true;
                        }
                    }
                    catch
                    {
                        e.Reply(e.Sender.At() + "本次会话出错啦，黑嘴被迫掐断了本次会话，给您带来不便敬请见谅嗷~");
                        hook.QQ = 0;
                        needClear = true;
                    }
                }
            }
            if (needClear) hooks.RemoveAll(m => m.QQ == 0);
            return 0;
        };
        commandService.Event.OnPrivateMessage += (context) =>
        {
            var e = (PrivateMessageEventArgs)context.SoraEventArgs;
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
                        catch
                        {
                            e.Reply("本次会话出错啦，黑嘴被迫掐断了本次会话，给您带来不便敬请见谅嗷~");
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
        e.Reply("欢迎玩...嗯？(/ω＼*)玩，玩黑嘴...！现在就让黑嘴教教你怎么玩她吧！\n" +
                "黑嘴：不要叫我，黑嘴超级忙，我不在！！！听见没！！\n" +
                ".test <次数>：让我骂我自己神经病...欸？（我要把404杀掉）\n" + 
                ".sx <中文缩写>：让黑嘴帮你搜一下这个缩写的意思\n" + 
                ".bug <内容>：欸？这是什么我也不知道呢。\n" +
                ".gifextract：请黑嘴帮你展开GIF。\n" +
                ".draw list：列出制图库的第一页。\n" +
                ".draw list <页数>：导航到制图库的第几页。\n" +
                ".draw help <模板>：让黑嘴教你指定模板的使用方法。\n" +
                ".draw <模板> (因模板而异)：请本小姐给你画画~\n" + 
                "（私聊）.draw build <模板> <模板脚本>：把你的绘图模板送给黑嘴~\n" +
                "（私聊）.draw edit <模板> <模板脚本>：修改你送给黑嘴的绘图模板~\n" +
                "（私聊）.draw remove <模板>：把你送给黑嘴的绘图模板拿回去呜呜呜。\n" +
                ".re <QQ>：随机抽一条黑嘴收集过的某人的复读语录\n" +
                ".re <QQ> info：看看黑嘴收集某个人的复读语录的情况\n" +
                ".re <QQ> <id/内容>：看看某个人指定序号的语录/包含这个内容的语录\n" +
                ".re <QQ> <id/内容> info：看看某个人指定序号的语录/包含这个内容的语录的情况\n" +
                ".re context <id>：查看复读语录的上下文\n" +
                ".re：随机抽一条语录\n" +
                ".keyword：查看你群今日截至现在最热话题");

}
