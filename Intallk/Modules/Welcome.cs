using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using System.Text;

namespace Intallk.Modules;

public class Welcome : ArchiveOneBotController<WelcomeModel>
{
    List<long> sent = new List<long>();
    readonly Random random = new(Guid.NewGuid().GetHashCode());

    public Welcome(ICommandService commandService, ILogger<ArchiveOneBotController<WelcomeModel>> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
        commandService.Event.OnGroupMemberChange += Event_OnGroupMemberChange;
    }

    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            DataFile = "welcome", ModuleName = "入群欢迎", ModuleUsage = "当有新的成员入群时，从设定的发言中随机抽取一条发送。",
            HelpCmd = "welcome", RootPermission = "WELCOME",
            RegisteredPermission = new()
            {
                ["USE"] = ("群新成员自动欢迎权限（群权限）", PermissionPolicy.AcceptedAsDefault),
                ["EDIT"] = ("新成员欢迎消息修改权限", PermissionPolicy.AcceptedAdminAsDefault)
            }
        };

    public override void OnDataNull() =>
        Data = new WelcomeModel();

    private int Event_OnGroupMemberChange(OneBotContext scope)
    {
        GroupMemberChangeEventArgs? e = scope.SoraEventArgs as GroupMemberChangeEventArgs;
        if (e == null) 
            return 0;
        if (sent.Contains(e.ChangedUser.Id))
            return 0;
        sent.Add(e.ChangedUser.Id);
        if (e.SubType == MemberChangeType.Approve || e.SubType == MemberChangeType.Invite)
        {
            if (PermissionService.JudgeGroup(e.SourceGroup.Id, Info, "USE"))
            {
                if (Data!.WelcomeMsg.ContainsKey(e.SourceGroup.Id))
                {
                    List<List<DictionaryReplyModel.Message>> msg = Data!.WelcomeMsg[e.SourceGroup.Id];
                    e.SourceGroup.SendGroupMessage(e.ChangedUser.At() + " " + msg[random.Next(0, msg.Count)].ToMessageBody());
                }
            }
        }
        return 0;
    }

    [Command("welcome view")]
    [CmdHelp("查看本群设定的欢迎消息列表")]
    public void WelcomeView(GroupMessageEventArgs e)
    {
        if (!PermissionService.JudgeGroup(e.SourceGroup.Id, Info, "USE"))
        {
            e.Reply("该群缺少权限'WELCOME_USE'或被拒绝，请联系权限授权人。");
            return;
        }
        if (!Data!.WelcomeMsg.ContainsKey(e.SourceGroup.Id))
        {
            e.Reply("该群暂无设定。");
            return;
        }
        MessageBody mb = new MessageBody();
        mb.AddText("该群设定的欢迎消息：\n");
        for(int i = 0;i < Data!.WelcomeMsg[e.SourceGroup.Id].Count; i++)
        {
            mb += ((i + 1) + ".“" + Data!.WelcomeMsg[e.SourceGroup.Id][i].ToMessageBody() + "”");
        }
        e.Reply(mb);
    }

    [Command("welcome add <content>")]
    [CmdHelp("消息", "追加新的欢迎消息")]
    public void WelcomeAdd(GroupMessageEventArgs e, MessageBody content)
    {
        if (!PermissionService.JudgeGroup(e.SourceGroup.Id, Info, "USE"))
        {
            e.Reply("该群缺少权限'WELCOME_USE'或被拒绝，请联系权限授权人。");
            return;
        }
        if (!PermissionService.Judge(e, Info, "EDIT"))
            return;
        if (!Data!.WelcomeMsg.ContainsKey(e.SourceGroup.Id))
            Data!.WelcomeMsg.Add(e.SourceGroup.Id, new List<List<DictionaryReplyModel.Message>>());
        Data!.WelcomeMsg[e.SourceGroup.Id].Add(content.ToMessageList());
        e.Reply("添加成功！");
        Save();
    }

    [Command("welcome remove <index>")]
    [CmdHelp("编号", "删除指定编号的欢迎消息")]
    public void WelcomeRemove(GroupMessageEventArgs e, int index)
    {
        if (!PermissionService.JudgeGroup(e.SourceGroup.Id, Info, "USE"))
        {
            e.Reply("该群缺少权限'WELCOME_USE'或被拒绝，请联系权限授权人。");
            return;
        }
        if (!PermissionService.Judge(e, Info, "EDIT"))
            return;
        if (!Data!.WelcomeMsg.ContainsKey(e.SourceGroup.Id))
            Data!.WelcomeMsg.Add(e.SourceGroup.Id, new List<List<DictionaryReplyModel.Message>>());
        index--;
        if (index >= Data!.WelcomeMsg[e.SourceGroup.Id].Count || index < 0)
        {
            e.Reply("输入的编号不正确。");
            return;
        }
        e.Reply("已删除：" + (index + 1) + ".“" + Data!.WelcomeMsg[e.SourceGroup.Id][index].ToMessageBody() + "”");
        Data!.WelcomeMsg[e.SourceGroup.Id].RemoveAt(index);
        Save();
    }
}
