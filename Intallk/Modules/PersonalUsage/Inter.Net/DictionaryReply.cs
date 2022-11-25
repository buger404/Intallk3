using Intallk.Config;
using Intallk.Models;
using JiebaNet.Analyser;
using Newtonsoft.Json;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.EventArgs.SoraEvent;
using System.Text;
using static Intallk.Models.DictionaryReplyModel;

namespace Intallk.Modules;

public class DictionaryReply : IOneBotController
{
    static ILogger<DictionaryReply>? _logger;
    public static MsgDictionary Dictionary = new();
    public static readonly long[] OpenGroup = { 554272507, 490623220 };
    public static readonly long[] OpenUsers = { 1583753193, 1361778219 };
    public static bool JudgePermission(GroupMessageEventArgs e)
        => OpenGroup.Contains(e.SourceGroup.Id) || OpenUsers.Contains(e.Sender.Id);
    public static string DictionaryPath {
        get {
            return Path.Combine(IntallkConfig.DataPath, "dictionary.json");
        }
    }
    public static void Dump(int failCount = 0)
    {
        try
        {
            JsonSerializer serializer = new();
            var sb = new StringBuilder();
            serializer.Serialize(new StringWriter(sb), Dictionary);
            File.WriteAllText(DictionaryPath, sb.ToString());
            _logger?.LogInformation("消息字典文件已保存：{file}。", DictionaryPath);
        }
        catch(Exception err)
        {
            if (failCount > 10)
            {
                if (_logger != null)
                    _logger.LogCritical("无法储存消息字典，重试次数已超过预定次数。\n诱因：{message}\n调用堆栈：\n{stacktrace}", err.Message, err.StackTrace);
                return;
            }
            Dump(++failCount);
        }
    }

    public DictionaryReply(ICommandService commandService, ILogger<DictionaryReply> logger)
    {
        _logger = logger;
        if (File.Exists(DictionaryPath))
        {
            JsonSerializer serializer = new();
            Dictionary = (MsgDictionary)serializer.Deserialize(new StringReader(File.ReadAllText(DictionaryPath)), typeof(MsgDictionary))!;
            _logger?.LogInformation("消息字典文件已读取：{file}。", DictionaryPath);
        }
        else
        {
            _logger?.LogWarning("未发现消息字典文件：{file}。", DictionaryPath);
        }
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
    }

    private int Event_OnGroupMessage(OneBot.CommandRoute.Models.OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if (e == null)
            return 0;
        if (!Dictionary.Data.ContainsKey(e.SourceGroup.Id))
            return 0;
        if (Dictionary.Data[e.SourceGroup.Id].ContainsKey(e.Message.RawText))
            e.Reply(Dictionary.Data[e.SourceGroup.Id][e.Message.RawText].Item2.ToMessageBody());
        return 0;
    }

    [Command("dict add <key> <value>")]
    public void DictionaryAdd(GroupMessageEventArgs e, string key, MessageBody value)
    {
        if (!JudgePermission(e))
            return;
        if (!Dictionary.Data.ContainsKey(e.SourceGroup.Id))
            Dictionary.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (Dictionary.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            (long, List<Message>) val = Dictionary.Data[e.SourceGroup.Id][key];
            e.Reply($"键'{key}'的值已被'{MainModule.GetQQName(e, val.Item1)}'设定为：\n" + val.Item2.ToMessageBody() + "\n*请使用.dict update <key> <value>覆盖字典。");
            return;
        }
        Dictionary.Data[e.SourceGroup.Id].Add(key, (e.Sender.Id, value.ToMessageList()));
        e.Reply($"已添加新的键'{key}'：\n" + value);
        Dump();
    }

    [Command("dict update <key> <value>")]
    public void DictionaryUpdate(GroupMessageEventArgs e, string key, MessageBody value)
    {
        if (!JudgePermission(e))
            return;
        if (!Dictionary.Data.ContainsKey(e.SourceGroup.Id))
            Dictionary.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (!Dictionary.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            e.Reply($"键'{key}'尚未录入字典，无需使用update覆盖。");
            return;
        }
        (long, List<Message>) val = Dictionary.Data[e.SourceGroup.Id][key];
        Dictionary.Data[e.SourceGroup.Id][key] = (e.Sender.Id, value.ToMessageList());
        e.Reply($"键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody() + "\n*已被覆盖为：\n" + value);
        Dump();
    }

    [Command("dict view <key>")]
    public void DictionaryView(GroupMessageEventArgs e, string key)
    {
        if (!JudgePermission(e))
            return;
        if (!Dictionary.Data.ContainsKey(e.SourceGroup.Id))
            Dictionary.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (!Dictionary.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            e.Reply($"键'{key}'尚未录入字典");
            return;
        }
        (long, List<Message>) val = Dictionary.Data[e.SourceGroup.Id][key];
        e.Reply($"键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody());
    }

    [Command("dict remove <key>")]
    public void DictionaryRemove(GroupMessageEventArgs e, string key)
    {
        if (!JudgePermission(e))
            return;
        if (!Dictionary.Data.ContainsKey(e.SourceGroup.Id))
            Dictionary.Data.Add(e.SourceGroup.Id, new Dictionary<string, (long, List<Message>)>());
        if (!Dictionary.Data[e.SourceGroup.Id].ContainsKey(key))
        {
            e.Reply($"键'{key}'尚未录入字典。");
            return;
        }
        (long, List<Message>) val = Dictionary.Data[e.SourceGroup.Id][key];
        Dictionary.Data[e.SourceGroup.Id].Remove(key);
        e.Reply($"键'{key}' by '{MainModule.GetQQName(e, val.Item1)}'：\n" + val.Item2.ToMessageBody() + "\n*已被删除。");
        Dump();
    }
}
