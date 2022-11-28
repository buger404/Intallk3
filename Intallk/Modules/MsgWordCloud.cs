using Intallk.Config;
using Intallk.Models;
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
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using WordCloudSharp;

namespace Intallk.Modules;

class MsgWordCloud : ArchiveOneBotController<MessageRecordFile>
{
    public static MsgWordCloud? Instance { get; private set; }

    public static DateTime DumpTime;
    public static SoraApi sora = null!;

    readonly Timer pushTimer, dumpTimer;
    static DateTime pushTime = DateTime.MinValue;

    public override ModuleInformation Initialize() =>
        new ModuleInformation { DataFile = "wordcloud", ModuleName = "消息词云", RootPermission = "WORDCLOUD" };

    public MsgWordCloud(ICommandService commandService, ILogger<ArchiveOneBotController<MessageRecordFile>> logger) : base(commandService, logger)
    {
        JiebaNet.Segmenter.ConfigManager.ConfigFileBaseDir = @"C:\jiebanet\config";
        pushTimer = new Timer(SubscribePush, null, new TimeSpan(0, 0, 5), new TimeSpan(0, 0, 5));
        dumpTimer = new Timer((_) =>
        {
            Dump();
        }, null, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0));
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
        Instance = this;
    }
    public override void OnDataNull() =>
        Data = new MessageRecordFile();

    [Command("wordcloud clear")]
    public void WordCloudClear(GroupMessageEventArgs e)
    {
        if (!Permission.Judge(e, Info, "EDIT", PermissionPolicy.RequireAccepted)) 
            return;
        int i = Data!.Msg.FindIndex(m => m.GroupID == e.SourceGroup);
        if (i == -1) return;
        Data.Msg[i].StrBuilder.Clear();
        e.Reply("清除成功。");
    }
    [Command("wordcloud [count]")]
    public void WordCloudToday(GroupMessageEventArgs e,int count = 5)
    {
        if (!Permission.Judge(e, Info, "RECORD", PermissionPolicy.AcceptedIfGroupAccepted))
            return;
        int i = Data!.Msg.FindIndex(m => m.GroupID == e.SourceGroup);
        if (i == -1)
        {
            e.Reply("今日截至现在暂无记录。");
            return;
        }
        e.Reply("今日截至现在，从已记录的消息中分析的群词云：\n" + SoraSegment.Image(GenerateWordCloud(Data.Msg[i]), false));
    }
    private int Event_OnGroupMessage(OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if (e == null)
            return 0;
        if (!Permission.JudgeGroup(e, Info, "RECORD", PermissionPolicy.RequireAccepted))
            return 0;
        if (sora == null) sora = e.SoraApi;
        int i = Data!.Msg.FindIndex(m => m.GroupID == e.SourceGroup);
        if (i == -1)
        {
            Data.Msg.Add(new MessageRecord(e!.SourceGroup));
            i = Data.Msg.Count - 1;
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
        Data.Msg[i].StrBuilder.AppendLine(msg);
        return 0;
    }
    private static void SubscribePush(object? state)
    {
        if (!(DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && (DateTime.Now - pushTime).TotalMinutes > 2)) return;
        if (sora == null) return;
        pushTime = DateTime.Now;
        foreach(MessageRecord r in Instance!.Data!.Msg)
        {
            if (Permission.JudgeGroup(r.GroupID, Instance!.Info!.RootPermission + "_SUBSCRIBE", PermissionPolicy.RequireAccepted))
            {
                sora.GetGroup(r.GroupID).SendGroupMessage("今日群词云：\n" + SoraSegment.Image(GenerateWordCloud(r), false));
                r.StrBuilder.Clear();
            }
        }

    }
    public static string GenerateWordCloud(MessageRecord record)
    {
        string text = record.StrBuilder.ToString();
        TfidfExtractor tfidfExtractor = new TfidfExtractor();
        List<WordWeightPair> key = tfidfExtractor.ExtractTagsWithWeight(text, 100, null).ToList();
        WordCloud wc = new WordCloud(2560, 1440, fontname: "HarmonyOS Sans SC Medium", allowVerical: true);
        List<int> freqs = new List<int>();
        foreach (WordWeightPair wp in key) freqs.Add((int)(wp.Weight * 1000));
        List<string> words = key.Select(it => it.Word).ToList();
        if (words.Count == 0)
        {
            words.Add("今天群里什么也没有qwq");
            freqs.Add(100);
        }
        Image wi = wc.Draw(words, freqs);
        string file = "\\Resources\\wordcloud" + record.GroupID + ".jpg";
        wi.Save(IntallkConfig.DataPath + file, ImageFormat.Jpeg);
        wi.Dispose();
        return IntallkConfig.DataPath + file;
    }
}
