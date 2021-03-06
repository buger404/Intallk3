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
    public class RepeatCollection
    {
        public List<MessageHeat> messages = new List<MessageHeat>();
    }
    [Serializable]
    public class MessageSegment
    {
        public string? Content { get; set; }
        public string? Url { get; set; }
        public bool isImage;
    }
    [Serializable]
    public class MessageHeat
    {
        public bool Repeated = false;
        public int Index = 0;
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
    public class MessagePond
    {
        public long group;
        public List<MessageHeat>? pond;
    }
    const float HeatLimit = 2f;
    List<MessageHeat> heats = new List<MessageHeat>();
    List<MessagePond> messagepond = new List<MessagePond>();
    public static RepeatCollection collection = new RepeatCollection();
    public static DateTime DumpTime;
    Timer dumpTimer = new Timer(Dump, null, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0));
    readonly Random random = new(Guid.NewGuid().GetHashCode());
    readonly ILogger<RepeatCollector> _logger;
    public static void Dump(object? state)
    {
        DumpTime = DateTime.Now;
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
        for (int i = 0; i < collection.messages.Count; i++)
        {
            collection.messages[i].Index = i;
        }
        foreach (MessageHeat heat in collection.messages)
        {
            switch (heat.ForwardMessages)
            {
                case JArray jarray:
                    JsonSerializer serializer = new();
                    heats = (List<MessageHeat>)serializer.Deserialize(jarray.CreateReader(), typeof(List<MessageHeat>))!;
                    heat.ForwardMessages = heats;
                    break;
            }
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
                messages.Add(new MessageSegment { Content = "[??????:" + redbag.Title + "]", isImage = false });
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
        Console.WriteLine("Succeed in downloading(" + (DateTime.Now - time).TotalMilliseconds + "ms???");
    }
    static async void DownLoad(string url, string path)
    {
        byte[]? data = await new RestClient().DownloadDataAsync(new RestRequest(url, Method.Get));
        if (data != null) File.WriteAllBytes(path, data!); else throw new Exception("???????????????");
    }
    private static MessageBody ToMessageBody(List<MessageSegment> seg)
    {
        MessageBody body = new MessageBody();
        foreach(MessageSegment s in seg)
        {
            if (s.isImage)
            {
                // ????????????
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
        if (e.SourceGroup.Id == 1078432121) return 0;
        if (g == -1)
        {
            messagepond.Add(new MessagePond { group = e.SourceGroup.Id, pond = new List<MessageHeat>() });
            g = messagepond.Count - 1;
        }
        messagepond[g].pond.Add(new MessageHeat
        {
            Message = CopyMsgSegments(seg),
            QQ = e.Sender.Id,
            Group = e.SourceGroup.Id,
            SendTime = DateTime.Now
        });
        if (messagepond[g].pond.Count > 15) messagepond[g].pond.RemoveAt(0);

        if (f == -1)
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
        }
        else
        {
            if (heats[f].Repeaters.Contains(e.Sender.Id))
            {
                // ???????????????????????????????????????
                heats[f].Heat -= 1f;
                /**heats[f].RepeatCount++;
                if (heats[f].Heat <= -2 && heats[f].RepeatCount >= 3)
                {
                    e.Reply(e.Sender.At() + "?????????~???????????????????????????~");
                    e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
                }**/
                Console.WriteLine("Cool record " + f + " due to duplicate sending. (" + heats[f].Heat + "???");
            }
            else
            {
                heats[f].Hot();
                heats[f].Repeaters.Add(e.Sender.Id);
                Console.WriteLine("Heat record " + f + " by " + e.Sender.Id + " (" + heats[f].Heat + ")");
            }
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
                    heat.Index = collection.messages.Count;
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
            e.Reply("?????????????????????");
            return;
        }
        MessageBody body = new MessageBody();
        foreach (MessageHeat message in messagepond[g].pond)
        {
            body += MainModule.GetQQName(e, message.QQ) + "???" + ToMessageBody(message.Message) + "\n";
        }
        e.Reply(body);
    }
    [Command("re help")]
    public void RepeatHelp(GroupMessageEventArgs e)
    {
        e.Reply(e.Sender.At() + "??????????????????????????????~??????????????????????????????" + collection.messages.Count.ToString() +
                                "\n???????????????????????????????????????????????????~\n?????????\n" +
                                ".re <??????>????????????????????????????????????\n" +
                                ".re bycontext <??????>??????????????????????????????????????????\n" +
                                ".re byqq <QQ>?????????????????????????????????????????????????????????\n" +
                                ".re <QQ> info??????????????????????????????????????????????????????\n" +
                                ".re <QQ> <id/??????>???????????????????????????????????????/???????????????????????????\n" +
                                ".re <QQ> <id/??????> info???????????????????????????????????????/????????????????????????????????????\n" +
                                ".re context <id>??????????????????????????????\n" +
                                ".re byid <id>??????????????????????????????????????????\n" +
                                ".re????????????????????????");
    }
    [Command("re remove bygroup <id>")]
    public void RepeatRemoveGroup(GroupMessageEventArgs e, int id)
    {
        if (e.Sender.Id != 1361778219) return;
        e.Reply("??????????????????????????????" + collection.messages.FindAll(m => m.Group == id).Count);
        collection.messages.RemoveAll(m => m.Group == id);
        Dump(null);
    }
    [Command("re remove <id>")]
    public void RepeatRemove(GroupMessageEventArgs e, int id)
    {
        if (e.Sender.Id != 1361778219) return;
        e.Reply("?????????????????????????????????" + id + "\n" + ToMessageBody(collection.messages[id].Message));
        collection.messages.RemoveAt(id);
        Dump(null);
    }
    [Command("re byid <id>")]
    public void Repeat(GroupMessageEventArgs e, int id) => GeneralRepeat(e, null!, id.ToString(), false);
    [Command("re")]
    public void Repeat(GroupMessageEventArgs e) => GeneralRepeat(e, null!, "", false);
    [Command("re bycontext <content>")]
    public void RepeatContext(GroupMessageEventArgs e, string content) => GeneralRepeat(e, m => ((List<MessageHeat>)m.ForwardMessages!).FindIndex(n => n.Message.FindIndex(o => o.Content!.Contains(content)) != -1) != -1, "", false);
    [Command("re <content>")]
    public void Repeat(GroupMessageEventArgs e, string content) => GeneralRepeat(e, m => m.Message.FindIndex(n => n.Content!.Contains(content)) != -1, "", false);
    [Command("re byqq <QQ>")]
    public void Repeat(GroupMessageEventArgs e, User QQ) => GeneralRepeat(e, m => m.QQ == QQ.Id, "", false);
    [Command("re context <id>")]
    public void RepeatContext(GroupMessageEventArgs e, User QQ, int id)
    {
        if (e.SourceGroup.Id == 1078432121)
        {
            e.Reply("???????????????????????????????????????????????????");
            return;
        }
        if (id < 0 || id >= collection.messages.Count)
        {
            e.Reply("?????????????????????id???????????????????????????????????????????????????");
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
            string name = "";
            if(message.Repeaters.Count > 0)
            {
                name = MainModule.GetQQName(e, message.QQ) + "??????" + (message.Repeaters.Count + 1) + "???";
            }
            else
            {
                name = MainModule.GetQQName(e, message.QQ);
            }
            body += name + "???" + ToMessageBody(message.Message) + "\n";
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
        if (e.SourceGroup.Id == 1078432121)
        {
            e.Reply("???????????????????????????????????????????????????");
            return;
        }
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
                List<int> si = new List<int>();
                for(int j = 0;j < c.Count; j++)
                {
                    if (c[j].Message.FindIndex(n => n.Content!.Contains(key)) != -1)
                        si.Add(j);
                }
                if(si.Count == 0)
                {
                    e.Reply(e.Sender.At() + "??????????????????????????????");
                    return;
                }
                i = si[random.Next(0, si.Count)];
            }
        }
        if (i < 0 || i >= c.Count)
        {
            e.Reply(e.Sender.At() + "????????????id?????????????????????");
            return;
        }
        if (i == -1 || c.Count == 0)
        {
            e.Reply(e.Sender.At() + "?????????????????????????????????");
            return;
        }
        if (infoOnly)
        {
            e.Reply(e.Sender.At() + "??????" + c.Count.ToString() + "?????????");
        }
        else
        {
            MessageHeat m = c[i];
            e.Reply("(" + m.Index + ")" + MainModule.GetQQName(e, m.QQ) + "???" + ToMessageBody(m.Message));
        }
    }
}
