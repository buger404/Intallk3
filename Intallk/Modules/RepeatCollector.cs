using Intallk.Config;
using Intallk.Models;
using Newtonsoft.Json;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.EventArgs.SoraEvent;
using System.Text;
using Sora.Util;
using RestSharp;

namespace Intallk.Modules;

public class RepeatCollector : IOneBotController
{
    [Serializable]
    class RepeatCollection
    {
        public List<MessageHeat> messages = new List<MessageHeat>();
    }
    [Serializable]
    class MessageSegment
    {
        public string? Content { get; set; }
        public bool isImage;
    }
    [Serializable]
    class MessageHeat
    {
        public bool Repeated = false;
        public object? ForwardMessages;
        public List<MessageSegment> Message = new List<MessageSegment>();
        public DateTime SendTime;
        public long QQ { get; set; }
        public List<long> Repeaters = new List<long>();
        public long Group { get; set; }
        public int RepeatCount = 0;
        public float Heat = 1.0f;
        public void Cool() => Heat -= 0.1f;
        public void Hot()
        {
            Heat *= 2f;
            RepeatCount++;
        }
    }

    const float HeatLimit = 2f;
    List<MessageHeat> heats = new List<MessageHeat>();
    List<MessageHeat> messagepond = new List<MessageHeat>();
    static RepeatCollection collection = new RepeatCollection();
    Timer dumpTimer = new Timer(Dump, null, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0));
    readonly Random random = new(Guid.NewGuid().GetHashCode());
    readonly ILogger<MainModule> _logger;
    public static void Dump(object? state)
    {
        string file = IntallkConfig.DataPath + "\\collection.json",
        file_backup = IntallkConfig.DataPath + "\\collection_backup.json";
        if(File.Exists(file)) File.Copy(file, file_backup, true);
        try
        {
            JsonSerializer serializer = new();
            var sb = new StringBuilder();
            serializer.Serialize(new StringWriter(sb), collection);
            File.WriteAllText(file, sb.ToString());
        }
        catch
        {
            File.Copy(file_backup, file, true);
        }
    }
    public RepeatCollector(ICommandService commandService, ILogger<MainModule> logger)
    {
        _logger = logger;
        try
        {
            string file = IntallkConfig.DataPath + "\\collection.json";
            if (File.Exists(file))
            {
                JsonSerializer serializer = new();
                collection = (RepeatCollection)serializer.Deserialize(new StringReader(File.ReadAllText(file)), typeof(RepeatCollection))!;
            }
        }
        catch
        {
            File.Copy(IntallkConfig.DataPath + "\\collection.json", 
                IntallkConfig.DataPath + "\\collection_restore_" + DateTime.Now.ToString("yy.MM.dd.HH.mm") + ".json");
        }

        
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
    }
    private static List<MessageSegment> GetMessageSegments(MessageBody mb)
    {
        List<MessageSegment> messages = new List<MessageSegment>();
        foreach(SoraSegment m in mb)
        {
            if(m.MessageType == Sora.Enumeration.SegmentType.Text)
            {
                messages.Add(new MessageSegment { Content = ((TextSegment)m.Data).Content, isImage = false });
            }else if(m.MessageType == Sora.Enumeration.SegmentType.Image)
            {
                messages.Add(new MessageSegment { Content = ((ImageSegment)m.Data).Url, isImage = true });
            }else if(m.MessageType == Sora.Enumeration.SegmentType.RedBag)
            {
                RedbagSegment redbag = (RedbagSegment)m.Data;
                messages.Add(new MessageSegment { Content = "[红包:" + redbag.Title + "]", isImage = false });
            }
        }
        return messages;
    }
    static async void DownLoad(string url, string path)
    {
        byte[]? data = await new RestClient().DownloadDataAsync(new RestRequest(url, Method.Get));
        if (data != null) File.WriteAllBytes(path, data!); else throw new Exception("下载失败。");
    }
    private static MessageBody ToMessageBody(List<MessageSegment> seg)
    {
        MessageBody body = new MessageBody();
        foreach(MessageSegment s in seg)
        {
            if (s.isImage)
            {
                body.Add(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\" + s.Content));
            }
            else
            {
                body.Add(SoraSegment.Text(s.Content));
            }
        }
        return body;
    }
    private int Event_OnGroupMessage(OneBotContext scope)
    {
        GroupMessageEventArgs e = (GroupMessageEventArgs)scope.SoraEventArgs;
        List<MessageSegment> seg = GetMessageSegments(e.Message.MessageBody);
        if (e.Message.RawText.StartsWith("dy") || e.Message.RawText.StartsWith(".")) return 0;
        int f = heats.FindIndex(m => m.Group == e.SourceGroup.Id && m.Message.SequenceEqual(seg));
        if(f == -1)
        {
            MessageHeat h = new MessageHeat
            {
                Message = seg,
                QQ = e.Sender.Id,
                Group = e.SourceGroup.Id,
                SendTime = DateTime.Now
            };
            h.Repeaters.Add(e.Sender.Id);
            heats.Add(h);
            messagepond.Add(h);
            if (messagepond.Count > 10) messagepond.RemoveAt(0);
        }
        else
        {
            if (heats[f].Repeaters.Contains(e.Sender.Id))
            {
                // 复读自己，这人多半有点无聊
                heats[f].Cool();
            }
            else
            {
                heats[f].Hot();
                heats[f].Repeaters.Add(e.Sender.Id);
            }
            messagepond.Add(heats[f]);
            if (messagepond.Count > 10) messagepond.RemoveAt(0);
        }

        for (int i = 0; i < heats.Count; i++)
        {
            if (i >= heats.Count) break;
            MessageHeat heat = heats[i];
            //Console.WriteLine(MBS(e.Message.MessageBody) + " <compared to> " + MBS(heat.Message!));
            if (heat.Heat >= HeatLimit)
            {
                // Record
                if (!heat.Repeated) 
                {
                    foreach(MessageHeat he in messagepond)
                    {
                        foreach(MessageSegment se in he.Message)
                        {
                            if (se.isImage)
                            {
                                string file = "context_" + DateTime.Now.ToString("yy.MM.dd.HH.mm.ss.") + se.GetHashCode() + ".jpg";
                                DownLoad(se.Content!, IntallkConfig.DataPath + "\\Resources\\" + file);
                                se.Content = file;
                            }
                        }
                    }
                    heat.ForwardMessages = messagepond;
                    collection.messages.Add(heat);
                    e.Reply(ToMessageBody(heat.Message));
                    heat.Repeated = true;
                }
            }
            if (!heat.Repeated && heat.Group == e.SourceGroup.Id) heat.Cool();
        }
        heats.RemoveAll(m => m.Heat <= 0);
        return 0;
    }

    [Command("re help")]
    public void RepeatHelp(GroupMessageEventArgs e)
    {
        e.Reply(e.Sender.At() + "黑嘴珍藏的复读语录集~目前收集语录总条数：" + collection.messages.Count.ToString() + 
                                "\n嗯，你想看的话，黑嘴也可以给你看哦~\n指令：\n" +
                                ".re <QQ>：随机抽一条黑嘴收集过的某人的复读语录\n" +
                                ".re <QQ> info：看看黑嘴收集某个人的复读语录的情况\n" +
                                ".re <QQ> <id/内容>：看看某个人指定序号的语录/包含这个内容的语录\n" +
                                ".re <QQ> <id/内容> info：看看某个人指定序号的语录/包含这个内容的语录的情况\n" +
                                ".re context <id>：查看复读语录的上下文\n" +
                                ".re：随机抽一条语录");
    }
    [Command("re")]
    public void Repeat(GroupMessageEventArgs e) => GeneralRepeat(e, null!, "", false);
    [Command("re <QQ>")]
    public void Repeat(GroupMessageEventArgs e, User QQ) => GeneralRepeat(e, m => m.QQ == QQ.Id, "", false);
    [Command("re context <id>")]
    public void RepeatContext(GroupMessageEventArgs e, User QQ, int id)
    {
        if(id < 0 || id >= collection.messages.Count - 1)
        {
            e.Reply("呜呜，这是什么id呢，请按照黑嘴教你的发送指令好嘛？");
            return;
        }
        MessageHeat heat = collection.messages[id];
        MessageBody body = new MessageBody();
        foreach(MessageHeat message in (List<MessageHeat>)heat.ForwardMessages!)
        {
            body += MainModule.GetQQName(e, message.QQ) + "：" + ToMessageBody(message.Message) + "\n";
        }
        e.Reply(body);
    }
    [Command("re <QQ> info")]
    public void RepeatI(GroupMessageEventArgs e, User QQ) => GeneralRepeat(e, m => m.QQ == QQ.Id, "", true);
    [Command("re <QQ> <key>")]
    public void Repeat(GroupMessageEventArgs e, User QQ, string key) => GeneralRepeat(e, m => m.QQ == QQ.Id, key, false);
    [Command("re <QQ> <key> info")]
    public void RepeatI(GroupMessageEventArgs e, User QQ, string key) => GeneralRepeat(e, m => m.QQ == QQ.Id, key, true);
    private void GeneralRepeat(GroupMessageEventArgs e, Predicate<MessageHeat> p, string key, bool infoOnly)
    {
        List<MessageHeat> c = collection.messages;
        if (p != null) c = c.FindAll(p);
        int i = -1;
        if (key == "")
        {
            i = random.Next(0, c.Count);
        }
        else 
        {
            if (!int.TryParse(key, out i))
            {
                c = c.FindAll(m => m.Message.FindIndex(n => n.Content!.Contains(key)) != -1);
                i = random.Next(0, c.Count);
            }
        }
        if(i == -1 || c.Count == 0)
        {
            e.Reply(e.Sender.At() + "没有找到这样的语录呢。");
            return;
        }
        if (infoOnly)
        {
            e.Reply(e.Sender.At() + "共有" + c.Count.ToString() + "条语录");
        }
        else
        {
            MessageHeat m = c[i];
            string heatName = "";
            if(m.Heat < 0)
            {
                heatName = "过气";
            }else if(m.Heat > 5)
            {
                heatName = "一般";
            }else if(m.Heat > 10)
            {
                heatName = "狂热";
            }else if(m.Heat > 15)
            {
                heatName = "冰棍会融化的程度";
            }
            e.Reply(e.Sender.At() + ("发送自" + ((m.Group == e.SourceGroup.Id) ? "你群" : "群") + 
                                     m.Group.ToString() + "（" + m.SendTime.ToString() + "），总计被" + m.RepeatCount +
                                     "人复读过，复读热度：" + heatName));
            e.Reply(MainModule.GetQQName(e, m.QQ) + "：" + ToMessageBody(m.Message));
        }
    }
}
