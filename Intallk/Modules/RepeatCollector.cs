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
using Newtonsoft.Json.Linq;

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
        public string? Url { get; set; }
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
            if (Heat < 0) Heat = 0.5f;
            Heat *= 2f;
            RepeatCount++;
        }
    }
    class MessagePond
    {
        public long group;
        public List<MessageHeat> pond;
    }
    const float HeatLimit = 2f;
    List<MessageHeat> heats = new List<MessageHeat>();
    List<MessagePond> messagepond = new List<MessagePond>();
    static RepeatCollection collection = new RepeatCollection();
    Timer dumpTimer = new Timer(Dump, null, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0));
    readonly Random random = new(Guid.NewGuid().GetHashCode());
    readonly ILogger<RepeatCollector> _logger;
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
    public RepeatCollector(ICommandService commandService, ILogger<RepeatCollector> logger)
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
    private static List<MessageSegment> CopyMsgSegments(List<MessageSegment> seg)
    {
        List<MessageSegment> segs = new List<MessageSegment>();
        foreach(MessageSegment se in seg)
        {
            segs.Add(new MessageSegment { Content = se.Content, isImage = se.isImage, Url = se.Url });
        }
        return segs;
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
                messages.Add(new MessageSegment { Content = ((ImageSegment)m.Data).ImgFile, isImage = true, Url = ((ImageSegment)m.Data).Url });
            }else if(m.MessageType == Sora.Enumeration.SegmentType.RedBag)
            {
                RedbagSegment redbag = (RedbagSegment)m.Data;
                messages.Add(new MessageSegment { Content = "[红包:" + redbag.Title + "]", isImage = false });
            }
        }
        return messages;
    }
    static void DownloadMessageImage(MessageSegment se)
    {
        if (!se.Content!.EndsWith(".image")) return;
        string file = "context_" + DateTime.Now.ToString("yy.MM.dd.HH.mm.ss.") + se.GetHashCode() + ".jpg";
        Console.WriteLine("Downloading " + file);
        DownLoad(se.Url!, IntallkConfig.DataPath + "\\Resources\\" + file);
        DateTime time = DateTime.Now;
        se.Content = file;
        while (!File.Exists(IntallkConfig.DataPath + "\\Resources\\" + file))
        {
            if ((DateTime.Now - time).TotalSeconds > 5)
            {
                se.Content = "oh.png";
                break;
            }
            Thread.Sleep(100);
        }
        Console.WriteLine("Succeed in downloading(" + (DateTime.Now - time).TotalMilliseconds + "ms）");
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
                // 漏网之鱼
                if (s.Content!.EndsWith(".image")) DownloadMessageImage(s);
                body.Add(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\" + s.Content));
            }
            else
            {
                body.Add(SoraSegment.Text(s.Content));
            }
        }
        return body;
    }
    private static bool CompareMessageSegment(List<MessageSegment> seg1, List<MessageSegment> seg2)
    {
        if (seg1.Count != seg2.Count) return false;
        for(int i = 0; i < seg1.Count; i++)
        {
            if (seg1[i].isImage != seg2[i].isImage || seg1[i].Content != seg2[i].Content) return false;
        }
        return true;
    }
    private int Event_OnGroupMessage(OneBotContext scope)
    {
        GroupMessageEventArgs e = (GroupMessageEventArgs)scope.SoraEventArgs;
        List<MessageSegment> seg = GetMessageSegments(e.Message.MessageBody);
        if (e.Message.RawText.StartsWith("dy") || e.Message.RawText.StartsWith(".")) return 0;
        int f = heats.FindIndex(m => (m.Group == e.SourceGroup.Id && CompareMessageSegment(m.Message,seg)));
        int g = messagepond.FindIndex(m => m.group == e.SourceGroup.Id);
        if (g == -1)
        {
            messagepond.Add(new MessagePond { group = e.SourceGroup.Id, pond = new List<MessageHeat>() });
            g = messagepond.Count - 1;
        }
        if(f == -1)
        {
            //Console.WriteLine("New message.");
            MessageHeat h = new MessageHeat
            {
                Message = seg,
                QQ = e.Sender.Id,
                Group = e.SourceGroup.Id,
                SendTime = DateTime.Now
            };
            h.Repeaters.Add(e.Sender.Id);
            heats.Add(h);
            messagepond[g].pond.Add(h);
            if (messagepond[g].pond.Count > 10) messagepond[g].pond.RemoveAt(0);
        }
        else
        {
            if (heats[f].Repeaters.Contains(e.Sender.Id) && false)
            {
                // 复读自己，这人多半有点无聊
                heats[f].Heat -= 1f;
                /**heats[f].RepeatCount++;
                if (heats[f].Heat <= -2 && heats[f].RepeatCount >= 3)
                {
                    e.Reply(e.Sender.At() + "别刷啦~小心黑嘴把你吃掉哦~");
                    e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
                }**/
                Console.WriteLine("Cool record " + f + " due to duplicate sending. (" + heats[f].Heat + "）");
            }
            else
            {
                heats[f].Hot();
                heats[f].Repeaters.Add(e.Sender.Id);
                Console.WriteLine("Heat record " + f + " by " + e.Sender.Id + " (" + heats[f].Heat + ")");
            }
            messagepond[g].pond.Add(heats[f]);
            if (messagepond[g].pond.Count > 10) messagepond[g].pond.RemoveAt(0);
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
                    Console.WriteLine("Start recording...");
                    List<MessageHeat> heats = new List<MessageHeat>();
                    foreach (MessageHeat he in messagepond[g].pond)
                    {
                        MessageHeat heat2 = new MessageHeat { QQ = he.QQ, SendTime = he.SendTime, Message = CopyMsgSegments(he.Message) };
                        heats.Add(heat2);
                        foreach(MessageSegment se in heat2.Message)
                        {
                            if (se.isImage) DownloadMessageImage(se);
                        }
                    }
                    heat.ForwardMessages = heats;
                    foreach (MessageSegment se in heat.Message)
                    {
                        if (se.isImage) DownloadMessageImage(se);
                    }
                    collection.messages.Add(heat);
                    e.Reply(ToMessageBody(heat.Message));
                    Console.WriteLine("Sent.");
                    heat.Repeated = true;
                }
            }
            if (!heat.Repeated && heat.Group == e.SourceGroup.Id) heat.Cool();
        }
        heats.RemoveAll(m => m.Heat <= -2);
        return 0;
    }
    [Command("t")]
    public void ForwardMessages(GroupMessageEventArgs e)
    {
        int g = messagepond.FindIndex(m => m.group == e.SourceGroup.Id);
        if (g == -1)
        {
            e.Reply("你群暂无记录。");
            return;
        }
        MessageBody body = new MessageBody();
        foreach (MessageHeat message in messagepond[g].pond)
        {
            body += MainModule.GetQQName(e, message.QQ) + "：" + ToMessageBody(message.Message) + "\n";
        }
        e.Reply(body);
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
                                ".re context <id>：查看复读语录的上文\n" +
                                ".re：随机抽一条语录");
    }
    [Command("re")]
    public void Repeat(GroupMessageEventArgs e) => GeneralRepeat(e, null!, "", false);
    [Command("re <QQ>")]
    public void Repeat(GroupMessageEventArgs e, User QQ) => GeneralRepeat(e, m => m.QQ == QQ.Id, "", false);
    [Command("re context <id>")]
    public void RepeatContext(GroupMessageEventArgs e, User QQ, int id)
    {
        if(id < 0 || id >= collection.messages.Count)
        {
            e.Reply("呜呜，这是什么id呢，请按照黑嘴教你的发送指令好嘛？");
            return;
        }
        MessageHeat heat = collection.messages[id];
        MessageBody body = new MessageBody();
        List<MessageHeat> heats = new List<MessageHeat>();
        switch (heat.ForwardMessages)
        {
            case JArray jarray:
                JsonSerializer serializer = new();
                heats = (List<MessageHeat>)serializer.Deserialize(jarray.CreateReader(), typeof(List<MessageHeat>))!;
                heat.ForwardMessages = heats;
                break;
            case List<MessageHeat> list:
                heats = list;
                break;
        }
        foreach(MessageHeat message in heats)
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
            }else if(m.Heat >= 0)
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
                                     "人复读过，复读热度：" + heatName + "\n发送“.re context " + i + "”查看上文。"));
            e.Reply(MainModule.GetQQName(e, m.QQ) + "：" + ToMessageBody(m.Message));
        }
    }
}
