using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

public class MainModule : IOneBotController
{
    public delegate bool GroupMessageHookCallback(GroupMessageEventArgs e, GroupMessageHook hook);
    public class GroupMessageHook
    {
        public long Group { get; set; }
        public long QQ { get; set; }
        public GroupMessageHookCallback Callback { get; set; }
        public object Data;
    }
    public static List<GroupMessageHook> hooks = new();
    readonly Random random = new(Guid.NewGuid().GetHashCode());
    readonly ILogger<MainModule> _logger;
    public MainModule(ICommandService commandService, ILogger<MainModule> logger)
    {
        _logger = logger;
        foreach (string file in Directory.GetFiles(IntallkConfig.DataPath + "\\DrawingScript"))
        {
            ScriptDrawer.drawTemplates.Add(file.Split('\\')[^1].Split('.')[0], System.IO.File.ReadAllText(file));
        }
        logger.LogInformation("已读入" + ScriptDrawer.drawTemplates.Count + "个绘图模板。");
        commandService.Event.OnException += (context, exception) =>
        {
            logger.LogError(exception.Message + "\n" + exception.StackTrace);
            switch (context.SoraEventArgs)
            {
                case GroupMessageEventArgs group:
                    group.Reply(SoraSegment.Reply(group.Message.MessageId) + "我...我才不是为了气死你才出错的呢！");
                    group.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                    break;
                case PrivateMessageEventArgs qq:
                    qq.Reply(SoraSegment.Reply(qq.Message.MessageId) + "我...我才不是为了气死你才出错的呢！");
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
                    if (hook.Callback(e, hook))
                    {
                        hook.QQ = 0;
                        needClear = true;
                    }
                }
            }
            if (needClear) hooks.RemoveAll(m => m.QQ == 0);
            return 0;
        };
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

    [Command("help")]
    public void Help(GroupMessageEventArgs e) => e.Reply("没写");

}
