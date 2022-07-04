using Intallk.Config;

using JiebaNet.Analyser;
using Newtonsoft.Json;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities.Base;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using System.Text;

namespace Intallk.Modules;

class Keyword : IOneBotController
{
    [Serializable]
    struct MessageRecord
    {
        public StringBuilder str = new StringBuilder();
        public long group;

        public MessageRecord(long id)
        {
            group = id; 
        }
    }
    [Serializable]
    struct MessageRecordFile
    {
        public List<MessageRecord> messages;
        public Dictionary<long, bool> switches;
    }
    readonly ILogger<Keyword> _logger;
    Timer announceTimer = new Timer(Announce, null, new TimeSpan(0, 0, 5), new TimeSpan(0, 0, 5));
    Timer dumpTimer = new Timer(Dump, null, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0));
    static DateTime announceTime = DateTime.MinValue;
    public static DateTime DumpTime;
    static Dictionary<long, bool> switches = new Dictionary<long, bool>();
    static List<MessageRecord> messages = new List<MessageRecord>();
    public static SoraApi sora = null!;
    public Keyword(ICommandService commandService, ILogger<Keyword> logger)
    {
        _logger = logger;
        JiebaNet.Segmenter.ConfigManager.ConfigFileBaseDir = @"C:\jiebanet\config";
        string file = IntallkConfig.DataPath + "\\keyword.json";
        if (File.Exists(file))
        {
            JsonSerializer serializer = new();
            MessageRecordFile mrf = new MessageRecordFile();
            mrf = (MessageRecordFile)serializer.Deserialize(new StringReader(File.ReadAllText(file)), typeof(MessageRecordFile))!;
            messages = mrf.messages;
            switches = mrf.switches;
            if (mrf.switches != null) switches = mrf.switches;
        }
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
    }
    public static void Dump(object? state)
    {
        DumpTime = DateTime.Now;
        string file = IntallkConfig.DataPath + "\\keyword.json";
        MessageRecordFile mrf = new MessageRecordFile();
        mrf.messages = messages;
        mrf.switches = switches;
        JsonSerializer serializer = new();
        var sb = new StringBuilder();
        serializer.Serialize(new StringWriter(sb), mrf);
        File.WriteAllText(file, sb.ToString());
    }
    [Command("keyword switch off")]
    public void KeywordOff(GroupMessageEventArgs e)
    {
        if (!switches.ContainsKey(e.SourceGroup.Id))
        {
            switches.Add(e.SourceGroup.Id, false);
        }
        else
        {
            switches[e.SourceGroup.Id] = false;
        }
        e.Reply("已在你群关闭关键词统计。");
        Dump(null);
    }
    [Command("keyword switch on")]
    public void KeywordOn(GroupMessageEventArgs e)
    {
        if (!switches.ContainsKey(e.SourceGroup.Id))
        {
            switches.Add(e.SourceGroup.Id, true);
        }
        else
        {
            switches[e.SourceGroup.Id] = true;
        }
        e.Reply("已在你群开启关键词统计。");
        Dump(null);
    }
    [Command("keyword switch")]
    public void KeywordSwitch(GroupMessageEventArgs e)
    {
        if (!switches.ContainsKey(e.SourceGroup.Id))
        {
            e.Reply("你群关键词统计：关闭。");
        }
        else
        {
            e.Reply("你群关键词统计：" + (switches[e.SourceGroup.Id] ? "开启" : "关闭") + "。");
        }
    }
    [Command("keyword clear")]
    public void KeywordClear(GroupMessageEventArgs e)
    {
        if (e.Sender.Id != 1361778219) return;
        int i = messages.FindIndex(m => m.group == e.SourceGroup);
        if (i == -1) return;
        messages[i].str.Clear();
        e.Reply("清除成功。");
    }
    [Command("keyword [count]")]
    public void KeywordToday(GroupMessageEventArgs e,int count = 5)
    {
        int i = messages.FindIndex(m => m.group == e.SourceGroup);
        if (i == -1)
        {
            e.Reply("你群暂无记录。");
            return;
        }
        MessageRecord r = messages[i];
        string text = r.str.ToString(), hlist = "";
        TfidfExtractor tfidfExtractor = new TfidfExtractor();
        List<WordWeightPair> key = tfidfExtractor.ExtractTagsWithWeight(text, count, null).ToList();
        for (int s = 0; s < key.Count; s++)
        {
            hlist += $"{s+1}.{key[s].Word}（{Math.Floor(key[i].Weight * 1000) / 10}%）\n";
        }
        e.Reply("今日截至现在你群最热聊天话题：\n" + hlist + "~来自黑嘴窥屏统计~");
    }
    private int Event_OnGroupMessage(OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if (!switches.ContainsKey(e.SourceGroup.Id)) return 0;
        if (!switches[e.SourceGroup.Id]) return 0;
        if (sora == null) sora = e.SoraApi;
        int i = messages.FindIndex(m => m.group == e.SourceGroup);
        if (i == -1)
        {
            messages.Add(new MessageRecord(e!.SourceGroup));
            i = messages.Count - 1;
        }
        string msg = "";
        foreach(SoraSegment se in e.Message.MessageBody)
        {
            if (se.MessageType == SegmentType.Text)
            {
                string text = ((TextSegment)se.Data).Content;
                if(!text.StartsWith("http")) msg += text;
            }
        }
        messages[i].str.AppendLine(msg);
        return 0;
    }
    private static void Announce(object? state)
    {
        if (!(DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && (DateTime.Now - announceTime).TotalMinutes > 2)) return;
        if (sora == null) return;
        announceTime = DateTime.Now;
        foreach(MessageRecord r in messages)
        {
            if (switches.ContainsKey(r.group))
            {
                if (switches[r.group])
                {
                    string text = r.str.ToString(), hlist = "";
                    TfidfExtractor tfidfExtractor = new TfidfExtractor();
                    List<WordWeightPair> key = tfidfExtractor.ExtractTagsWithWeight(text, 5, null).ToList();
                    for (int i = 0; i < key.Count; i++)
                    {
                        hlist += $"{i + 1}.{key[i].Word}（{Math.Floor(key[i].Weight * 1000) / 10}%）\n";
                    }
                    sora.GetGroup(r.group).SendGroupMessage("今日你群最热聊天话题：\n" + hlist + "~来自黑嘴窥屏统计~");
                    r.str.Clear();
                }
            }
        }

    }
}
