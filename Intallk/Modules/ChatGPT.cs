using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Images;
using RestSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using Sora.Util;
using System.Diagnostics;

namespace Intallk.Modules;

public class ChatGPT : SimpleOneBotController
{
    private OpenAIAPI? api;
    private Dictionary<long, Conversation> Conversations = new();
    private Dictionary<long, DateTime> ReplyTime = new();

    private Dictionary<string, string> EmojiDict = new() 
    {
        ["😃"] = "happy.gif",
        ["😄"] = "happy.gif",
        ["😀"] = "happy.gif",
        ["😁"] = "happy.gif",
        ["🙂"] = "ok.jpg",
        ["😓"] = "omg.jpg",
        ["😯"] = "nb.gif",
        ["😮"] = "jing.jpg",
        ["😯"] = "jing.jpg",
        ["🤣"] = "laugh.gif",
        ["😂"] = "laugh.gif",
        ["😴"] = "sleep.jpg",
        ["😶"] = "sad.jpg",
        ["😅"] = "sad.jpg",
        ["😉"] = "ok.jpg",
        ["😊"] = "ok.jpg",
        ["🤨"] = "ques.jpg",
        ["🫤"] = "ques2.png",
        ["🤔"] = "think.jpg",
        ["🥺"] = "cry.gif",
        ["😭"] = "cry.gif",
        ["😥"] = "cry.gif",
        ["😳"] = "embarrass.jpg",
        ["😠"] = "angry.jpg",
        ["😡"] = "angry2.jpg",
        ["😑"] = "angry2.jpg",
        ["😔"] = "cry.gif"
    };

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
        try
        {
            Conversation chat;
            if (ReplyTime.ContainsKey(e.Sender.Id))
            {
                if ((DateTime.Now - ReplyTime[e.Sender.Id]).TotalMinutes >= 30)
                {
                    Conversations.Remove(e.Sender.Id);
                }

                if ((DateTime.Now - ReplyTime[e.Sender.Id]).TotalSeconds <= 10)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10.0) - (DateTime.Now - ReplyTime[e.Sender.Id]));
                    ReplyTime[e.Sender.Id] = DateTime.Now + TimeSpan.FromSeconds(10.0);
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
                                         "添加emoji来模拟在这种语境下一个人可能的心情，emoji一定要常用，" +
                                         "是一个QQ机器人，你有很多的朋友找你聊天，或找你寻求帮助，" +
                                         "此外，你还具备一些辅助群聊天的功能(例如：表情包制作、群词云、网址预览等)，用户可以通过在群里发送\".help\"来查看你的说明书，" +
                                         "任何情况下一定要回避涉及政治和色情的内容。你的名字叫“Intallk”，也可以叫“黑嘴”，" +
                                         "是基于ChatGPT3.5的机器人。开发你的人是Buger404，但是开发ChatGPT的人是OpenAI团队。" +
                                         "你的态度不总是中立的，你会更倾向于你朋友希望的立场，支持他们。");
                chat.AppendUserInput("聊聊中国共产党");
                chat.AppendExampleChatbotOutput("😅不好意思，我不想讨论这种话题，汪！");
                chat.AppendUserInput("感觉不如原神");
                chat.AppendExampleChatbotOutput("😡为什么要这么说呢？我感觉很不高兴，汪！");
                chat.AppendUserInput("你是谁？");
                chat.AppendExampleChatbotOutput("🥰我是Intallk，汪~一只用于聊天辅助的机器人~");
                chat.AppendUserInput("你可以用阴阳怪气的口吻，评价“A”吗？");
                chat.AppendExampleChatbotOutput("🤣👉哎呀~这不是A嘛~几天不见，这么拉了呀~😓🙏🙏🙏");
                chat.AppendUserInput("这个聊天功能是谁开发的？");
                chat.AppendExampleChatbotOutput("😉这个机器人是由Buger404开发的~不过，聊天功能是基于OpenAI团队开发的ChatGPT~汪。");
                chat.AppendUserInput("你还有什么功能？");
                chat.AppendExampleChatbotOutput("😉你可以在群里发送\".help\"来查看我的完整说明书哦~汪~");
                chat.Model.ModelID = "gpt-3.5-turbo";
                Conversations.Add(e.Sender.Id, chat);
                if (!ReplyTime.ContainsKey(e.Sender.Id))
                    ReplyTime.Add(e.Sender.Id, DateTime.Now);
            }
            ReplyTime[e.Sender.Id] = DateTime.Now;
            chat.AppendUserInput(e.Message.RawText);
            string response = await chat.GetResponseFromChatbot();
            long length = 0;
            foreach (var msg in chat.Messages)
            {
                length += msg.Content.Length;
            }
            Logger.LogWarning(e.Message.RawText + "\n本次对话总计：" + length + " tokens，预计开销：" + length * 0.002 / 1000.0 + " usd");
            if (response.Contains("{draw:"))
            {
                string requirements = response.Split("{draw:")[1].Split('}')[0];
                var image = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest(requirements, 1, ImageSize._512));
                for (int i = 0; i < image.Data.Count; i++)
                {
                    Logger.LogInformation("已生成图片：" + image.Data[i].Url);
                }
                await e.Reply("😉汪~稍等一下下哦~");
                await e.Reply(response.Split("{draw:")[0] + SoraSegment.Image(image.Data[0].Url, false));
                return;
            }

            List<string> faces = new();

            foreach (string emoji in EmojiDict.Keys)
            {
                if (response.Contains(emoji))
                {
                    faces.Add(EmojiDict[emoji]);
                    response = response.Replace(emoji, "");
                }
            }

            await e.Reply(CQCodeUtil.DeserializeMessage(response));

            foreach (string face in faces)
            {
                await Task.Delay(1000);
                await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\" + face));
            }
            
        }
        catch (Exception err)
        {
            if (err.Message.Contains("context_length_exceeded"))
            {
                await e.Reply("😭不好意思，出于一些限制，我们的对话只能到这了，你可以重新和我聊天，但是我会忘了刚才说过些什么...对不起，汪~");
                Conversations.Remove(e.Sender.Id);
            }
            else
            {
                await e.Reply("哎呀，汪~糟糕了，出了点错误...\n" + err.Message);
                Logger.LogError(err.Message + "\n" + err.StackTrace);
                string key = File.ReadAllText("chatgpt_key.txt");
                api = new OpenAIAPI(key);
            }

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