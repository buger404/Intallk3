using Intallk.Config;
using Intallk.Models;
using JiebaNet.Analyser;
using Newtonsoft.Json;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using OneBot.CommandRoute.Services.Implements;
using Sora.Entities;
using Sora.EventArgs.SoraEvent;
using System.Text;
using static Intallk.Models.DictionaryReplyModel;

namespace Intallk.Modules;

public class DictionaryReply : ArchiveOneBotController<MsgDictionary>
{
    public DictionaryReply(ICommandService commandService, ILogger<ArchiveOneBotController<MsgDictionary>> logger) : base(commandService, logger)
    {
    }
    public override ModuleInformation Initialize()
    {
        Service!.Event.OnGroupMessage += Event_OnGroupMessage;
        return new ModuleInformation { DataFile = "dictionary", ModuleName = "消息字典", RootPermission = "MSGDICT",
                                       HelpCmd = "dict", ModuleUsage = "根据字典中记录的键，在合适的时机发送对应的值。\n" +
                                                                       "例如，当字典存在键值'早上好-早上好'，则当群内发送的消息包含'你好'时，机器人将自动回应'你好'\n" +
                                                                       "特别地，希望同时匹配多个键时，可以使用符号'|'分割。\n" +
                                                                       "其中，以<except>开头的表示预期消息中不应含有此项；以<fullmatch>则意为全字匹配。"};
    }

    private int Event_OnGroupMessage(OneBot.CommandRoute.Models.OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if (e == null)
            return 0;
        if (!Permission.JudgeGroup(e, Info, "REPLY"))
            return 0;
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            return 0;
        foreach(string key in Data.Data[e.SourceGroup.Id].Keys)
        {
            string[] p = key.ToLower().Split('|');
            string msg = e.Message.RawText.ToLower();
            bool condiction = true;
            foreach(string c in p)
            {
                if (c.StartsWith("<except>"))
                {
                    condiction &= !msg.Contains(c);
                }
                else if (c.StartsWith("<fullmatch>"))
                {
                    condiction &= (msg == c);
                }
                else
                {
                    condiction &= msg.Contains(c);
                }
            }
            if (condiction)
            {
                e.Reply(Data.Data[e.SourceGroup.Id][key].Item2.ToMessageBody());
                break;
            }
        }
        return 0;
    }

    [Command("dict add <key> <value>")]
    [CmdHelp("键 值", "追加新的消息字典项")]
    public void DictionaryAdd(GroupMessageEventArgs e, string key, MessageBody value)
    {
        if (!Permission.Judge(e, Info, "EDIT", PermissionPolicy.AcceptedIfGroupAccepted))
            return;
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            Data.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (Data.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            (long, List<Message>) val = Data.Data[e.SourceGroup.Id][key];
            e.Reply($"键'{key}'的值已被'{MainModule.GetQQName(e, val.Item1)}'设定为：\n" + val.Item2.ToMessageBody() + "\n*请使用.dict update <key> <value>覆盖字典。");
            return;
        }
        List<Message> msg = value.ToMessageList();
        if (msg[0].Type == Sora.Enumeration.SegmentType.Text && (msg[0].Content?.ToLower().StartsWith("dy") ?? false))
        {
            if (!Permission.Judge(e, Info, "DYCONTENT", PermissionPolicy.RequireAccepted))
                return;
        }
        Data.Data[e.SourceGroup.Id].Add(key, (e.Sender.Id, msg));
        e.Reply($"已添加新的键'{key}'：\n" + value);
        Dump();
    }

    [Command("dict update <key> <value>")]
    [CmdHelp("键 值", "将已有的消息字典项的值覆盖为新的值")]
    public void DictionaryUpdate(GroupMessageEventArgs e, string key, MessageBody value)
    {
        if (!Permission.Judge(e, Info, "EDIT", PermissionPolicy.AcceptedIfGroupAccepted))
            return;
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            Data.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (!Data.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            e.Reply($"键'{key}'尚未录入字典，无需使用update覆盖。");
            return;
        }
        List<Message> msg = value.ToMessageList();
        if (msg[0].Type == Sora.Enumeration.SegmentType.Text && (msg[0].Content?.ToLower().StartsWith("dy") ?? false))
        {
            if (!Permission.Judge(e, Info, "DYCONTENT", PermissionPolicy.RequireAccepted))
                return;
        }
        (long, List<Message>) val = Data.Data[e.SourceGroup.Id][key];
        Data.Data[e.SourceGroup.Id][key] = (e.Sender.Id, msg);
        e.Reply($"键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody() + "\n*已被覆盖为：\n" + value);
        Dump();
    }

    [Command("dict view <key>")]
    [CmdHelp("键", "浏览消息字典中指定键的情况")]
    public void DictionaryView(GroupMessageEventArgs e, string key)
    {
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            Data.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (!Data.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            e.Reply($"键'{key}'尚未录入字典");
            return;
        }
        (long, List<Message>) val = Data.Data[e.SourceGroup.Id][key];
        e.Reply($"键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody());
    }

    [Command("dict test <content>")]
    [CmdHelp("内容", "使用所给内容测试将触发哪个键")]
    public void DictionaryTest(GroupMessageEventArgs e, string content)
    {
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            Data.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        foreach (string key in Data.Data[e.SourceGroup.Id].Keys)
        {
            if (e.Message.RawText.ToLower().Contains(key.ToLower()))
            {
                (long, List<Message>) val = Data.Data[e.SourceGroup.Id][key];
                e.Reply($"命中：键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody());
                return;
            }
        }
        e.Reply($"没有找到会因此消息触发的键。");
    }

    [Command("dict remove <key>")]
    [CmdHelp("内容", "移除指定的消息字典的项")]
    public void DictionaryRemove(GroupMessageEventArgs e, string key)
    {
        if (!Permission.Judge(e, Info, "EDIT", PermissionPolicy.AcceptedIfGroupAccepted))
            return;
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            Data.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (!Data.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            e.Reply($"键'{key}'尚未录入字典。");
            return;
        }
        (long, List<Message>) val = Data.Data[e.SourceGroup.Id][key];
        Data.Data[e.SourceGroup.Id].Remove(key);
        e.Reply($"键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody() + "\n*已被删除。");
        Dump();
    }
}
