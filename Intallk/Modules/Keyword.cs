using Intallk.Config;

using JiebaNet.Analyser;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;

using RestSharp;

using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using System.Text;

namespace Intallk.Modules;

class Keyword : IOneBotController
{
    struct MessageRecord
    {
        public StringBuilder str = new StringBuilder();
        public long group;
        public GroupMessageEventArgs e;

        public MessageRecord(long id, GroupMessageEventArgs e)
        {
            group = id; this.e = e;
        }
    }
    readonly ILogger<Keyword> _logger;
    Timer announceTimer = new Timer(Announce, null, new TimeSpan(0, 1, 0), new TimeSpan(0, 1, 0));
    static DateTime announceTime = DateTime.MinValue;
    static List<MessageRecord> messages = new List<MessageRecord>();
    public Keyword(ICommandService commandService, ILogger<Keyword> logger)
    {
        _logger = logger;
        JiebaNet.Segmenter.ConfigManager.ConfigFileBaseDir = @"C:\jiebanet\config";
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
    }
    private int Event_OnGroupMessage(OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        int i = messages.FindIndex(m => m.group == e.SourceGroup);
        if (i == -1)
        {
            messages.Add(new MessageRecord(e.SourceGroup, e));
            i = messages.Count - 1;
        }
        string msg = "";
        foreach(SoraSegment se in e.Message.MessageBody)
        {
            if(se.MessageType == SegmentType.Text) msg += ((TextSegment)se.Data).Content;
        }
        messages[i].str.AppendLine(msg);
        return 0;
    }
    private static void Announce(object? state)
    {
        if (!(DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && (DateTime.Now - announceTime).TotalMinutes > 2)) return;
        announceTime = DateTime.Now;
        foreach(MessageRecord r in messages)
        {
            string text = r.str.ToString(), hlist = "";
            TfidfExtractor tfidfExtractor = new TfidfExtractor();
            List<string> key = tfidfExtractor.ExtractTags(text, 5, null).ToList();
            for(int i = 0;i < key.Count;i++)    
            {
                hlist += $"{i}.{key[i]}\n";
            }
            r.e.Reply("今日你群最热聊天话题：\n" + hlist + "-来自黑嘴窥屏统计~");
            r.str.Clear();
        }

    }
}
