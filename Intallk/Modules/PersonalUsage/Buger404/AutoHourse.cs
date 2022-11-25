using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using OneBot.CommandRoute.Models;

namespace Intallk.Modules;

// 404私用功能
class AutoHorse : IOneBotController
{
    readonly Random random = new(Guid.NewGuid().GetHashCode());

    public AutoHorse(ICommandService commandService, ILogger<Keyword> logger)
    {
        commandService.Event.OnGroupMessage += Event_OnGroupMessage; ;
    }

    private int Event_OnGroupMessage(OneBotContext scope)
    {
        GroupMessageEventArgs g = (scope.SoraEventArgs as GroupMessageEventArgs)!;
        if (g.Message.RawText.Contains("比赛即将开始，请于30秒内助力"))
        {
            g.Reply("自动赛马捏");
            int num = random.Next(1, 5);
            if (num == 4) num = 2;
            g.Reply("助力" + num + "-" + random.Next(10,40));
        }
        if (g.Message.RawText.Contains("积分不足"))
        {
            g.Reply(@"积分😔҈我҈的҈天҈🥺҈我҈的҈心҈❤҈️҈澎҈湃҈💓҈💓҈ ҈飙҈快҈的҈心҈脏҈跳҈💗҈怦҈💗҈怦҈💗҈ ҈🥵҈我҈神҈魂҈颠҈倒҈🥵҈ ҈躁҈动҈的҈心҈❤҈️҈在҈放҈鞭҈炮҈🎉҈ ҈🥵҈神҈魂҈颠҈倒҈🥵҈ ҈🥵҈迷҈恋҈着҈你҈😍҈神҈魂҈颠҈倒҈🥵҈ ҈是҈你҈踩҈碎҈💘҈我҈的҈解҈药҈💊҈ ҈😭҈全҈都҈没҈关҈系҈😭҈ ҈ ҈别҈管҈我҈啦҈别҈管҈我҈啦҈😭҈");
            g.Reply(@"为了积分，本机器人不惜胡言乱语...");
            g.Reply(@"积分🥺，我的积分🥺");
        }
        return 0;
    }
}
