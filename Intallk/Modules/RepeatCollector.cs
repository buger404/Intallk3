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

public static class DownloadString
{
    public static void DownLoad(this string url, string path)
    {
        byte[]? data = new RestClient().DownloadDataAsync(new RestRequest(url, Method.Get)).Result;
        if (data != null) File.WriteAllBytes(path, data!); else throw new Exception("下载失败。");
    }
}
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
                if (s.Content!.EndsWith(".image"))
                {
                    DownloadMessageImage(s);
                }  
                body.Add(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\" + s.Content));
                Console.WriteLine("Image prepared: " + s.Content);
            }
            else
            {
                body.Add(SoraSegment.Text(s.Content));
                Console.WriteLine("Text prepared: " + s.Content);
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
        }

        for (int i = 0; i < heats.Count; i++)
        {
            if (i >= heats.Count) break;
            MessageHeat heat = heats[i];
            //Console.WriteLine(MBS(e.Message.MessageBody) + " <compared to> " + MBS(heat.Message!));
            if (heat.Heat >= HeatLimit)
            {
                // Record
                if (!heat.Repeated && e.SourceGroup.Id == heat.Group) 
                {
                    if (heat.Message.FindIndex(x => x.Content?.ToLower().StartsWith("dy") ?? false) != -1)
                    {
                        e.Reply("我怀疑你想利用我作弊dy，但我没有证据。");
                        heat.Repeated = true;
                    }
                    else
                    {
                        Console.WriteLine("Start recording...");
                        List<MessageHeat> heats = new List<MessageHeat>();
                        foreach (MessageHeat he in messagepond[g].pond)
                        {
                            MessageHeat heat2 = new MessageHeat { QQ = he.QQ, SendTime = he.SendTime, Message = CopyMsgSegments(he.Message) };
                            heats.Add(heat2);
                            foreach (MessageSegment se in heat2.Message)
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
                                ".re <内容>：查看包含指定内容的语录\n" +
                                ".re bycontext <内容>：查看上文包含指定内容的语录\n" +
                                ".re byqq <QQ>：随机抽一条黑嘴收集过的某人的复读语录\n" +
                                ".re <QQ> info：看看黑嘴收集某个人的复读语录的情况\n" +
                                ".re <QQ> <id/内容>：看看某个人指定序号的语录/包含这个内容的语录\n" +
                                ".re <QQ> <id/内容> info：看看某个人指定序号的语录/包含这个内容的语录的情况\n" +
                                ".re context <id>：查看复读语录的上文\n" +
                                ".re byid <id>：查看指定序号对应的复读语录\n" +
                                ".re：随机抽一条语录");
    }
    [Command("re remove bygroup <id>")]
    public void RepeatRemoveGroup(GroupMessageEventArgs e, int id)
    {
        if (e.Sender.Id != 1361778219) return;
        e.Reply("好的，移除语录总数：" + collection.messages.FindAll(m => m.Group == id).Count);
        collection.messages.RemoveAll(m => m.Group == id);
        Dump(null);
    }
    [Command("re remove <id>")]
    public void RepeatRemove(GroupMessageEventArgs e, int id)
    {
        if (e.Sender.Id != 1361778219) return;
        e.Reply("好的，已经帮您移除语录" + id + "\n" + ToMessageBody(collection.messages[id].Message));
        collection.messages.RemoveAt(id);
        Dump(null);
    }
    [Command("re clean")]
    public void RepeatClean(GroupMessageEventArgs e)
    {
        if (e.Sender.Id != 1361778219) return;
        e.Reply("正在清理语录库...");
        int c = 0;
        for(int i = 0; i < collection.messages.Count; i++)
        {
            if (i >= collection.messages.Count) break;
            if(collection.messages[i].Message.FindIndex(y => y.Content?.ToLower().StartsWith("dy") ?? false) != -1)
            {
                collection.messages.RemoveAt(i);
                i--; c++;
            }
        }
        Dump(null);
        e.Reply("已成功清理" + c + "条语录！");
    }
    [Command("re dy_cheat_report")]
    public void RepeatSearchAll(GroupMessageEventArgs e)
    {
        if (e.Sender.Id != 1361778219) return;
        e.Reply("正在执行滥用报告和分析，请稍后...");
        List<MessageHeat> c = collection.messages.FindAll(x => x.Message.FindIndex(y => y.Content?.ToLower().StartsWith("dy") ?? false) != -1);
        StringBuilder report = new StringBuilder();
        Dictionary<string, List<MessageHeat>> op = new Dictionary<string, List<MessageHeat>>();
        Dictionary<long, List<MessageHeat>> target = new Dictionary<long, List<MessageHeat>>();
        foreach (MessageHeat mh in c)
        {
            string text = "";
            foreach (MessageSegment ms in mh.Message) text += ms.Content;
            string[] t = text.Split(' ');
            if (!op.ContainsKey(t[1].ToLower()))
            {
                op.Add(t[1].ToLower(), new List<MessageHeat>());
            }
            op[t[1].ToLower()].Add(mh);
        }
        report.AppendLine("滥用黑嘴执行OP指令报告(黑嘴机器人自动生成)：");
        int cnt = 0;
        foreach(string cmd in op.Keys)
        {
            cnt++;
            Console.WriteLine("1/2 " + cnt + "/" + op.Keys.Count);
            report.AppendLine("指令'" + cmd + "'的滥用情况(总计" + op[cmd].Count + "次)：");
            for(int i = 0; i < op[cmd].Count; i++)
            {
                MessageHeat mh = op[cmd][i];
                string text = "";
                foreach (MessageSegment ms in mh.Message) text += ms.Content;
                string[] t = text.Split(' ');
                if(t.Length >= 2)
                {
                    long qq = 0;
                    if (t.Length >= 3) long.TryParse(t[2], out qq);
                    report.Append((i + 1) + "." + text);
                    if (qq == 0)
                    {
                        report.AppendLine(" - 执行对象:未知");
                    }
                    else
                    {
                        report.AppendLine(" - 执行对象:" + MainModule.GetCacheQQName(e, qq) + "(" + qq + ")");
                        if (!target.ContainsKey(qq))
                        {
                            target.Add(qq, new List<MessageHeat>());
                        }
                        target[qq].Add(mh);
                    }
                    string repeat = "";
                    foreach (long q in mh.Repeaters)
                        repeat += MainModule.GetCacheQQName(e, q) + "(" + q + ")，";
                    report.AppendLine("  参与执行的所有人：" + repeat);
                    report.AppendLine("  发自群：" + mh.Group + "，时间：" + mh.SendTime.ToString("yy.MM.dd HH:mm:ss"));
                }
            }
            report.AppendLine("-----------以上为按指令分类的滥用报告情况。-------------");
        }
        report.AppendLine("总计滥用指令目标人数：" + target.Count);
        cnt = 0;
        List<long> back = new List<long>();
        foreach (long q in target.Keys)
        {
            cnt++;
            Console.WriteLine("2/2 " + cnt + "/" + target.Keys.Count);
            report.AppendLine("玩家'" + MainModule.GetCacheQQName(e, q) + "(" + q + ")" + "'的滥用情况(总计" + target[q].Count + "次)：");
            for (int i = 0; i < target[q].Count; i++)
            {
                MessageHeat mh = target[q][i];
                string text = "";
                foreach (MessageSegment ms in mh.Message) text += ms.Content;
                string[] t = text.Split(' ');
                if(t.Length >= 2)
                {
                    if (t[1].ToLower() == "/addeqi" || t[1].ToLower() == "/addvip" || t[1].ToLower() == "/addcoin" || t[1].ToLower() == "/checktradecd"
                        || t[1].ToLower() == "/addpet" || t[1].ToLower() == "/addtoptitle")
                    {
                        if(!back.Contains(q))
                            back.Add(q);
                    }
                    long qq = 0;
                    if (t.Length >= 3) long.TryParse(t[2], out qq);
                    report.Append((i + 1) + "." + text);
                    if (qq == 0)
                    {
                        report.AppendLine(" - 执行对象:未知");
                    }
                    else
                    {
                        report.AppendLine(" - 执行对象:" + MainModule.GetCacheQQName(e, qq) + "(" + qq + ")");
                    }
                    string repeat = "";
                    foreach (long sq in mh.Repeaters)
                        repeat += MainModule.GetCacheQQName(e, sq) + "(" + sq + ")，";
                    report.AppendLine("  参与执行的所有人：" + repeat);
                    report.AppendLine("  发自群：" + mh.Group + "，时间：" + mh.SendTime.ToString("yy.MM.dd HH:mm:ss"));
                }
            }
            report.AppendLine("-----------以上为按玩家分类的滥用报告情况。-------------");
        }
        string qqstr = string.Join(",", target.Keys.ToList());
        report.AppendLine("报告生成完毕，滥用总次数：" + c.Count + "次，涉及的所有玩家QQ(" + target.Count + "个)：\n" + qqstr + "\n" + 
            "涉及敏感操作的所有玩家QQ(" + back.Count + "个)：\n" + String.Join(",", back));
        File.WriteAllText("黑嘴滥用报告.txt", report.ToString());
        e.Reply("滥用报告已分析并生成，请查阅。");
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
            e.Reply("很抱歉，复读语录功能在本群不可用。");
            return;
        }
        if (id < 0 || id >= collection.messages.Count)
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
        if(heats.Count > 40)
        {
            heats.RemoveRange(40, heats.Count - 40);
            Dump(null);
        }
        foreach(MessageHeat message in heats)
        {
            string name = "";
            if(message.Repeaters.Count > 0)
            {
                name = MainModule.GetQQName(e, message.QQ) + "等共" + (message.Repeaters.Count + 1) + "人";
            }
            else
            {
                name = MainModule.GetQQName(e, message.QQ);
            }
            body += name + "：" + ToMessageBody(message.Message) + "\n";
        }
        Console.WriteLine(body.ToString());
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
            e.Reply("很抱歉，复读语录功能在本群不可用。");
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
                    e.Reply(e.Sender.At() + "找不到这样的语录捏。");
                    return;
                }
                i = si[random.Next(0, si.Count)];
            }
        }
        if (i < 0 || i >= c.Count)
        {
            e.Reply(e.Sender.At() + "咦，这个id好像不太对呢。");
            return;
        }
        if (i == -1 || c.Count == 0)
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
            e.Reply("(" + m.Index + ")" + MainModule.GetQQName(e, m.QQ) + "：" + ToMessageBody(m.Message));
        }
    }
}
