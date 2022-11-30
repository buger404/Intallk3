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
using System.Reflection;

namespace Intallk.Modules;

public class RepeatCollector : ArchiveOneBotController<RepeatCollection>
{
    // 设置屏蔽记录的语录
    public static RepeatCollector? Instance { get; private set; }
    readonly static Predicate<string> _filter = x => x.ToLower().StartsWith("dy") || x.StartsWith(".");
    readonly static Random _random = new(Guid.NewGuid().GetHashCode());
    public const float HeatLimit = 2f;
    public static DateTime DumpTime;

    List<MessageCollection> MsgBuffer = new List<MessageCollection>();
    readonly Timer DumpTimer;

    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            DataFile = "collection", ModuleName = "复读语录收集", RootPermission = "REPEATCOLLECTOR",
            HelpCmd = "re", ModuleUsage = "自动参与复读，并收集多人复读的语录。\n" +
                                          "例如，当群里不同的人发送相同的消息时，机器人将参与复读，同时记录该消息。\n" +
                                          "由于启用了消息记录，因此顺便支持了查看最近10条消息的功能。",
            RegisteredPermission = new ()
            {
                ["RECORD"] = ("记录群消息权限（群权限）", PermissionPolicy.RequireAccepted),
                ["VIEWFORWARDMSG"] = ("查看最近十条消息权限", PermissionPolicy.AcceptedIfGroupAccepted)
            }
        };

    public RepeatCollector(ICommandService commandService, ILogger<ArchiveOneBotController<RepeatCollection>> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
        if (Data != null)
        {
            List<SingleRecordingMsg> SingleMsg;
            foreach (SingleRecordingMsg msg in Data.messages)
            {
                switch (msg.ForwardMessages)
                {
                    case JArray jarray:
                        JsonSerializer serializer = new();
                        SingleMsg = (List<SingleRecordingMsg>)serializer.Deserialize(jarray.CreateReader(), typeof(List<SingleRecordingMsg>))!;
                        msg.ForwardMessages = SingleMsg;
                        break;
                }
            }
        }
        DumpTimer = new Timer((_) =>
        {
            DumpTime = DateTime.Now;
            string file = DataPath, file_backup = RepeatCollector.Instance!.DataPath + ".bak";
            if (File.Exists(file)) File.Copy(file, file_backup, true);
            Save();
        }, null, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0));
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
        Instance = this;
    }

    public override void OnDataNull()
    {
        Data = new RepeatCollection();
    }

    public override string? GetStatus() =>
        $"语录库收集总数：{Data!.messages.Count}\n消息池记录条数：{MsgBuffer.Count}\n上次语录库备份时间：{DumpTime.ToString("yy/MM/dd HH:mm")}";

    private int Event_OnGroupMessage(OneBotContext scope)
    {
        if (Data == null)
            return 0;
        GroupMessageEventArgs e = (GroupMessageEventArgs)scope.SoraEventArgs;
        if (!PermissionService.JudgeGroup(e, Info, "RECORD"))
            return 0;
        List<MessageSegment> seg = MessageSegment.Parse(e.Message.MessageBody);
        if (_filter.Invoke(e.Message.RawText))
            return 0;
        int f = Data.messages.FindIndex(m => (m.Group == e.SourceGroup.Id && MessageSegment.Compare(m.Message, seg)));
        int g = MsgBuffer.FindIndex(m => m.GroupID == e.SourceGroup.Id);
        if (g == -1)
        {
            MsgBuffer.Add(new MessageCollection { GroupID = e.SourceGroup.Id, Collection = new List<SingleRecordingMsg>() });
            g = MsgBuffer.Count - 1;
        }
        MsgBuffer[g].Collection.Add(new SingleRecordingMsg
        {
            Message = MessageSegment.Copy(seg),
            QQ = e.Sender.Id,
            Group = e.SourceGroup.Id,
            SendTime = DateTime.Now
        });
        if (MsgBuffer[g].Collection.Count > 15) MsgBuffer[g].Collection.RemoveAt(0);

        if (f == -1)
        {
            //Console.WriteLine("New message.");
            SingleRecordingMsg h = new SingleRecordingMsg
            {
                Message = seg,
                QQ = e.Sender.Id,
                Group = e.SourceGroup.Id,
                SendTime = DateTime.Now
            };
            h.Repeaters.Add(e.Sender.Id);
            Data.messages.Add(h);
        }
        else
        {
            if (Data.messages[f].Repeaters.Contains(e.Sender.Id))
            {
                // 复读自己，这人多半有点无聊
                Data.messages[f].Heat -= 1f;
                /**heats[f].RepeatCount++;
                if (heats[f].Heat <= -2 && heats[f].RepeatCount >= 3)
                {
                    e.Reply(e.Sender.At() + "别刷啦~小心黑嘴把你吃掉哦~");
                    e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
                }**/
                //Console.WriteLine("Cool record " + f + " due to duplicate sending. (" + SingleMsg[f].Heat + "）");
            }
            else
            {
                Data.messages[f].Hot();
                Data.messages[f].Repeaters.Add(e.Sender.Id);
                //Console.WriteLine("Heat record " + f + " by " + e.Sender.Id + " (" + SingleMsg[f].Heat + ")");
            }
        }

        for (int i = 0; i < Data.messages.Count; i++)
        {
            if (i >= Data.messages.Count) break;
            SingleRecordingMsg heat = Data.messages[i];
            //Console.WriteLine(MBS(e.Message.MessageBody) + " <compared to> " + MBS(heat.Message!));
            if (heat.Heat >= HeatLimit)
            {
                // Record
                if (!heat.Repeated && e.SourceGroup.Id == heat.Group && PermissionService.JudgeGroup(e, "INTER.NET_" + Info.RootPermission + "_COLLECT", PermissionPolicy.RequireAccepted))
                {
                    //Console.WriteLine("Start recording...");
                    List<SingleRecordingMsg> heats = new List<SingleRecordingMsg>();
                    foreach (SingleRecordingMsg he in MsgBuffer[g].Collection)
                    {
                        SingleRecordingMsg heat2 = new SingleRecordingMsg { QQ = he.QQ, SendTime = he.SendTime, Message = MessageSegment.Copy(he.Message) };
                        heats.Add(heat2);
                        foreach (MessageSegment se in heat2.Message)
                        {
                            if (se.isImage)
                                se.DownloadImage();
                        }
                    }
                    heat.ForwardMessages = heats;
                    foreach (MessageSegment se in heat.Message)
                    {
                        if (se.isImage)
                            se.DownloadImage();
                    }
                    heat.Index = Data.messages.Count;
                    Data.messages.Add(heat);
                    e.Reply(heat.Message.ToMessageBody());
                    heat.Repeated = true;
                }
            }
            if (!heat.Repeated && heat.Group == e.SourceGroup.Id)
                heat.Cool();
        }
        Data.messages.RemoveAll(m => m.Heat <= -2);
        return 0;
    }
    [Command("t")]
    [CmdHelp("查看最近的10条消息")]
    public void ForwardMessages(GroupMessageEventArgs e)
    {
        if (!PermissionService.JudgeGroup(e, Info, "RECORD"))
        {
            e.Reply("此群无此功能的权限，请联系权限授权人。");
            return;
        }
        if (!PermissionService.Judge(e, Info, "VIEWFORWARDMSG"))
            return;
        int g = MsgBuffer.FindIndex(m => m.GroupID == e.SourceGroup.Id);
        if (g == -1)
        {
            e.Reply("你群暂无记录。");
            return;
        }
        MessageBody body = new MessageBody();
        foreach (SingleRecordingMsg message in MsgBuffer[g].Collection)
        {
            body += MainModule.GetQQName(e, message.QQ) + "：" + message.Message.ToMessageBody() + "\n";
        }
        e.Reply(body);
    }
    [Command("re remove bygroup <id>")]
    [CmdHelp("群号码", "移除语录库中所有指定群的语录")]
    public void RepeatRemoveGroup(GroupMessageEventArgs e, int id)
    {
        if (!PermissionService.Judge(e, "INTER.NET_" + Info.RootPermission + "_EDIT", PermissionPolicy.RequireAccepted))
            return;
        if (Data == null)
            return;
        e.Reply("好的，移除语录总数：" + Data.messages.FindAll(m => m.Group == id).Count);
        Data.messages.RemoveAll(m => m.Group == id);
        Save();
    }
    [Command("re remain <id>")]
    [CmdHelp("群号码", "保留语录库中指定群的语录，然后删除其余所有语录")]
    public void RepeatReserveGroup(GroupMessageEventArgs e, int id)
    {
        if (!PermissionService.Judge(e, "INTER.NET_" + Info.RootPermission + "_EDIT", PermissionPolicy.RequireAccepted))
            return;
        if (Data == null)
            return;
        e.Reply("好的，移除语录总数：" + Data.messages.FindAll(m => m.Group != id).Count);
        Data.messages.RemoveAll(m => m.Group != id);
        Save();
    }
    [Command("re remove <id>")]
    [CmdHelp("语录ID", "删除指定语录")]
    public void RepeatRemove(GroupMessageEventArgs e, int id)
    {
        if (!PermissionService.Judge(e, "INTER.NET_" + Info.RootPermission + "_EDIT", PermissionPolicy.RequireAccepted))
            return;
        if (Data == null)
            return;
        e.Reply("好的，已经帮您移除语录" + id + "\n" + Data.messages[id].Message.ToMessageBody());
        Data.messages.RemoveAt(id);
        Save();
    }
    [Command("re clean")]
    [CmdHelp("清理语录库中与过滤器匹配的语录")]
    public void RepeatClean(GroupMessageEventArgs e)
    {
        if (!PermissionService.Judge(e, "INTER.NET_" + Info.RootPermission + "_EDIT", PermissionPolicy.RequireAccepted))
            return;
        if (Data == null)
            return;
        e.Reply("正在清理语录库...");
        int c = 0;
        for (int i = 0; i < Data.messages.Count; i++)
        {
            if (i >= Data.messages.Count) break;
            string rawText = "";
            foreach (MessageSegment ms in Data.messages[i].Message)
                rawText += ms.Content;
            if (_filter.Invoke(rawText))
            {
                Data.messages.RemoveAt(i);
                i--; c++;
            }
        }
        Save();
        e.Reply("已成功清理" + c + "条语录！");
    }
    [Command("re byid <id>")]
    [CmdHelp("语录ID", "查看指定语录")]
    public void Repeat(GroupMessageEventArgs e, int id) => GeneralRepeat(e, null!, id.ToString(), false);
    [Command("re")]
    [CmdHelp("随机抽取一条语录")]
    public void Repeat(GroupMessageEventArgs e) => GeneralRepeat(e, null!, "", false);
    [Command("re bycontext <content>")]
    [CmdHelp("内容", "查看上下文消息中包含指定内容的语录")]
    public void RepeatContext(GroupMessageEventArgs e, string content) => GeneralRepeat(e, m => ((List<SingleRecordingMsg>)m.ForwardMessages!).FindIndex(n => n.Message.FindIndex(o => o.Content!.Contains(content)) != -1) != -1, "", false);
    [Command("re <content>")]
    [CmdHelp("内容", "查看包含指定内容的语录")]
    public void Repeat(GroupMessageEventArgs e, string content) => GeneralRepeat(e, m => m.Message.FindIndex(n => n.Content!.Contains(content)) != -1, "", false);
    [Command("re byqq <QQ>")]
    [CmdHelp("QQ号", "随机抽取一条指定用户被收集的语录")]
    public void Repeat(GroupMessageEventArgs e, User QQ) => GeneralRepeat(e, m => m.QQ == QQ.Id, "", false);
    [Command("re context <id>")]
    [CmdHelp("语录ID", "查看指定语录的上下文")]
    public void RepeatContext(GroupMessageEventArgs e, User QQ, int id)
    {
        if (!PermissionService.JudgeGroup(e, Info, "RECORD"))
        {
            e.Reply("此群无此功能的权限，请联系权限授权人。");
            return;
        }
        if (!PermissionService.Judge(e, "INTER.NET_" + Info.RootPermission + "_VIEW", PermissionPolicy.AcceptedIfGroupAccepted))
            return;
        if (Data == null)
            return;
        if (id < 0 || id >= Data.messages.Count)
        {
            e.Reply("查询不到指定的复读语录。");
            return;
        }
        SingleRecordingMsg heat = Data.messages[id];
        MessageBody body = new MessageBody();
        List<SingleRecordingMsg> heats = new List<SingleRecordingMsg>();
        switch (heat.ForwardMessages)
        {
            case JArray jarray:
                JsonSerializer serializer = new();
                heats = (List<SingleRecordingMsg>)serializer.Deserialize(jarray.CreateReader(), typeof(List<SingleRecordingMsg>))!;
                heat.ForwardMessages = heats;
                break;
            case List<SingleRecordingMsg> list:
                heats = list;
                break;
        }
        if (heats.Count > 40)
        {
            heats.RemoveRange(40, heats.Count - 40);
            Save();
        }
        foreach (SingleRecordingMsg message in heats)
        {
            string name = "";
            if (message.Repeaters.Count > 0)
            {
                name = MainModule.GetQQName(e, message.QQ) + "等共" + (message.Repeaters.Count + 1) + "人";
            }
            else
            {
                name = MainModule.GetQQName(e, message.QQ);
            }
            body += name + "：" + message.Message.ToMessageBody() + "\n";
        }
        e.Reply(body);
    }
    [Command("re <QQ> info")]
    [CmdHelp("QQ号", "查看对指定用户的语录的收集情况")]
    public void RepeatI(GroupMessageEventArgs e, User QQ) => GeneralRepeat(e, m => m.QQ == QQ.Id, "", true);
    [Command("re <QQ> <key>")]
    [CmdHelp("QQ号 内容", "查看指定用户中包含指定内容的语录")]
    public void Repeat(GroupMessageEventArgs e, User QQ, string key) => GeneralRepeat(e, m => m.QQ == QQ.Id, key, false);
    [Command("re <QQ> <key> info")]
    [CmdHelp("QQ号 内容", "查看指定用户中包含指定内容的语录的条数")]
    public void RepeatI(GroupMessageEventArgs e, User QQ, string key) => GeneralRepeat(e, m => m.QQ == QQ.Id, key, true);
    private void GeneralRepeat(GroupMessageEventArgs e, Predicate<SingleRecordingMsg> p, string key, bool infoOnly)
    {
        if (!PermissionService.JudgeGroup(e, Info, "RECORD"))
        {
            e.Reply("此群无此功能的权限，请联系权限授权人。");
            return;
        }
        if (!PermissionService.Judge(e, "INTER.NET_" + Info.RootPermission + "_VIEW", PermissionPolicy.AcceptedIfGroupAccepted))
            return;
        if (Data == null)
            return;
        List<SingleRecordingMsg> c = Data.messages;
        if (p != null) c = c.FindAll(p);
        int i = -1;
        if (key == "")
        {
            i = _random.Next(0, c.Count);
        }
        else
        {
            if (!int.TryParse(key, out i))
            {
                List<int> si = new List<int>();
                for (int j = 0; j < c.Count; j++)
                {
                    if (c[j].Message.FindIndex(n => n.Content!.Contains(key)) != -1)
                        si.Add(j);
                }
                if (si.Count == 0)
                {
                    e.Reply(e.Sender.At() + "找不到这样的语录捏。");
                    return;
                }
                i = si[_random.Next(0, si.Count)];
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
            SingleRecordingMsg m = c[i];
            e.Reply("(" + m.Index + ")" + MainModule.GetQQName(e, m.QQ) + "：" + m.Message.ToMessageBody());
        }
    }
}