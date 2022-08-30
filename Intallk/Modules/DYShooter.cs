using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using System.Drawing;
using System.Drawing.Imaging;

namespace Intallk.Modules;

class DYShooter : IOneBotController
{
    private Timer? signupTimer, beatTimer;
    readonly ILogger<RepeatCollector> _logger;
    readonly Random random = new(Guid.NewGuid().GetHashCode());
    // 与DreamY梦幻联动（？？？）
    [Command("dyshoot")]
    public void DyShoot(GroupMessageEventArgs e)
    {
        if(Keyword.sora == null)
        {
            e.Reply("不听");
            return;
        }
        e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\elegant.jpg"));
        e.Reply("好嘞，黑嘴要开始自动玩dy辣！");
        signupTimer = new Timer((s) =>
        {
            Group g = Keyword.sora.GetGroup(1078432121);
            g.SendGroupMessage(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\elegant.jpg"));
            g.SendGroupMessage("dy 签到");
        }, null, new TimeSpan(0, 0, 5), new TimeSpan(24, 0, 5));
        beatTimer = new Timer((s) =>
        {
            Group g = Keyword.sora.GetGroup(1078432121);
            g.SendGroupMessage("dy 神殿5");
            beatTimer!.Change(new TimeSpan(0, random.Next(8,20), 0), new TimeSpan(0, random.Next(8, 20), 0));
        }, null, new TimeSpan(0, 0, 10), new TimeSpan(0, 10, 0));
    }

    public DYShooter(ICommandService commandService, ILogger<RepeatCollector> logger)
    {
        _logger = logger;
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
        commandService.Event.OnGroupCardUpdate += Event_OnGroupCardUpdate;
    }

    private int Event_OnGroupCardUpdate(OneBot.CommandRoute.Models.OneBotContext scope)
    {
        GroupCardUpdateEventArgs? e = scope.SoraEventArgs as GroupCardUpdateEventArgs;
        if (e == null) return 0;
        if (e.User.Id != 1361778219) return 0;
        if (e.SourceGroup.Id != 665763261) return 0;
        if (e.NewCard == "") return 0;
        e.SourceGroup.SendGroupMessage($"乱改nm呢乱改");
        return 0;
    }

    private int Event_OnGroupMessage(OneBot.CommandRoute.Models.OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if(e.SourceGroup.Id == 1078432121 && e.Sender.Id != 2487411076)
        {
            if ((e.Message.RawText.Contains("404") || e.Message.RawText.Contains("4O4") || e.Message.RawText.Contains("4零4")
                || e.Message.RawText.Contains("四零四")) &&
            (e.Message.RawText.Contains("狗") || e.Message.RawText.Contains("🐶") || e.Message.RawText.Contains("🐕") || e.Message.RawText.ToLower().Replace(" ", "").Replace("\n", "").Contains("dog")
            || e.Message.RawText.Replace(" ", "").Contains("犭句")))
            {
                e.Message.RecallMessage();
                //e.Reply(e.Sender.At() + "已自动踢出群聊（无慈悲）。");
                e.SourceGroup.EnableGroupMemberMute(e.Sender.Id, 600);
                return 1;
            }
        }

        if (e!.Sender.Id != 2487411076) return 0;
        string msg = e!.Message.RawText;
        if (msg.Contains("黑嘴原路滚回去了。"))
        {
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\sad.jpg"));
        }
        if (msg.Contains("偷偷从你口袋里抓走了硬币") || (msg.Contains("你被Intallk") && msg.Contains("击败")))
        {
            int i = random.Next(0, 6);
            if(i == 0)
                e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\excited.jpg"));
            else
                e.Reply(SoraSegment.Record(IntallkConfig.DataPath + "\\Resources\\dogsong" + i + ".mp3"));
        }
        return 0;
    }

    [Command("dyrecovery")]
    public void DyRecovery(GroupMessageEventArgs e)
    {
        if (Keyword.sora == null)
        {
            e.Reply("不听");
            return;
        }
    }
}
