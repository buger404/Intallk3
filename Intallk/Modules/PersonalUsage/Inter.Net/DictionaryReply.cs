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
        return new ModuleInformation { DataFile = "dictionary", ModuleName = "消息字典", RootPermission = "MSGDICT" };
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
            if (e.Message.RawText.ToLower().Contains(key.ToLower()))
            {
                e.Reply(Data.Data[e.SourceGroup.Id][key].Item2.ToMessageBody());
                break;
            }
        }
        return 0;
    }

    [Command("dict add <key> <value>")]
    public void DictionaryAdd(GroupMessageEventArgs e, string key, MessageBody value)
    {
        if (!Permission.Judge(e, Info, "EDIT", Permission.Policy.AcceptedIfGroupAccepted))
            return;
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            Data.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (Data.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            (long, List<Message>) val = Data.Data[e.SourceGroup.Id][key];
            e.Reply($"键'{key}'的值已被'{MainModule.GetQQName(e, val.Item1)}'设定为：\n" + val.Item2.ToMessageBody() + "\n*请使用.dict update <key> <value>覆盖字典。");
            return;
        }
        Data.Data[e.SourceGroup.Id].Add(key, (e.Sender.Id, value.ToMessageList()));
        e.Reply($"已添加新的键'{key}'：\n" + value);
        Dump();
    }

    [Command("dict update <key> <value>")]
    public void DictionaryUpdate(GroupMessageEventArgs e, string key, MessageBody value)
    {
        if (!Permission.Judge(e, Info, "EDIT", Permission.Policy.AcceptedIfGroupAccepted))
            return;
        if (!Data!.Data.ContainsKey(e.SourceGroup.Id))
            Data.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (!Data.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            e.Reply($"键'{key}'尚未录入字典，无需使用update覆盖。");
            return;
        }
        (long, List<Message>) val = Data.Data[e.SourceGroup.Id][key];
        Data.Data[e.SourceGroup.Id][key] = (e.Sender.Id, value.ToMessageList());
        e.Reply($"键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody() + "\n*已被覆盖为：\n" + value);
        Dump();
    }

    [Command("dict view <key>")]
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

    [Command("dict search <content>")]
    public void DictionarySearch(GroupMessageEventArgs e, string content)
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
    public void DictionaryRemove(GroupMessageEventArgs e, string key)
    {
        if (!Permission.Judge(e, Info, "EDIT", Permission.Policy.AcceptedIfGroupAccepted))
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
