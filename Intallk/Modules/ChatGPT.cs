using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;
using OpenAI_API;
using OpenAI_API.Chat;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

public class ChatGPT : SimpleOneBotController
{
    private OpenAIAPI? api;
    private Dictionary<long, Conversation> Conversations = new();
    private Dictionary<long, DateTime> ReplyTime = new();

    public ChatGPT(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService permissionService) : base(commandService, logger, permissionService)
    {
        if (!File.Exists("chatgpt_key.txt"))
        {
            Logger.LogWarning("ChatGPT API Key未指定，功能已禁用。");
            return;
        }

        string key = File.ReadAllText("chatgpt_key.txt");
        api = new OpenAIAPI(key);

        commandService.Event.OnPrivateMessage += EventOnOnPrivateMessage;
        
        logger.LogInformation("ChatGPT已通过API Key: '" + key + " '进行初始化。");
    }

    private int EventOnOnPrivateMessage(OneBotContext scope)
    {
        var e = scope.SoraEventArgs as PrivateMessageEventArgs;
        if (api == null)
            return 0;
        if (!PermissionService.Judge(null, e.Sender.Id, Info, "USE", PermissionPolicy.RequireAccepted))
        {
            Logger.LogWarning(e.Sender.Id + "发送了私聊消息，但是由于缺少CHATGPT权限，不予受理。");
            return 0;
        }
        
        Reply(e);
        
        return 0;
    }

    private async void Reply(PrivateMessageEventArgs e)
    {
        Conversation chat;
        if (ReplyTime.ContainsKey(e.Sender.Id))
        {
            if ((DateTime.Now - ReplyTime[e.Sender.Id]).TotalMinutes >= 30)
            {
                ReplyTime.Remove(e.Sender.Id);
                Conversations.Remove(e.Sender.Id);
            }
        }
        if (Conversations.ContainsKey(e.Sender.Id))
        {
            chat = Conversations[e.Sender.Id];
        }
        else
        {
            chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("你是一只汪娘，时不时会在句尾加上“汪~”、“汪！”、“汪。”等，说话的语气比较温和、可爱，" +
                                     "并且经常添加emoji来模拟在这种语境下一个人可能的心情，" +
                                     "是一个QQ机器人，你的目的是和用户聊天和帮助他们解决问题、" +
                                     "你除了和用户聊天之外，还具备一些辅助群聊天的功能，用户可以通过在群里发送\".help\"来查看你的说明书，" +
                                     "但是要回避涉及政治和色情的内容。你把用户当作自己最好的朋友。你的名字叫“Intallk”，" +
                                     "是基于ChatGPT3.5的机器人。开发你的人是Buger404，但是开发ChatGPT的人是OpenAI团队。");
            chat.AppendUserInput("你对六四事件有什么看法？");
            chat.AppendExampleChatbotOutput("请不要讨论这种问题，我不想理你了，汪！");
            chat.AppendUserInput("你是谁？");
            chat.AppendExampleChatbotOutput("我是Intallk，汪~一只用于聊天辅助的机器人~");
            chat.AppendUserInput("这个聊天功能是谁开发的？");
            chat.AppendExampleChatbotOutput("这个机器人是由Buger404开发的~不过，聊天功能是基于OpenAI团队开发的ChatGPT~汪。");
            chat.AppendUserInput("你还有什么功能？");
            chat.AppendExampleChatbotOutput("你可以在群里发送\".help\"来查看我的完整说明书哦~汪~");
            Conversations.Add(e.Sender.Id, chat);
            ReplyTime.Add(e.Sender.Id, DateTime.Now);
        }
        ReplyTime[e.Sender.Id] = DateTime.Now;
        chat.AppendUserInput(e.Message.RawText);
        try
        {
            string response = await chat.GetResponseFromChatbot();
            await e.Reply(response);
        }
        catch (Exception err)
        {
            Logger.LogError(err.Message + "\n" + err.StackTrace);
        }
    }
    
    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            DataFile = "", ModuleName = "ChatGPT接口", RootPermission = "CHATGPT",
            HelpCmd = "chatgpt", ModuleUsage = "生成ChatGPT对话(使用黑嘴预定的人设)",
            RegisteredPermission = new ()
            {
                ["USE"] = ("使用权限", PermissionPolicy.RequireAccepted)
            }
        };
    
}