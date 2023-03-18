using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

public class FakeMsg : SimpleOneBotController
{
    public FakeMsg(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService permissionService) : base(commandService, logger, permissionService)
    {
    }
    
    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            DataFile = "", ModuleName = "合并转发创建", RootPermission = "FAKEMSG",
            HelpCmd = "fmsg", ModuleUsage = "生成合并转发消息(仅供娱乐)。",
            RegisteredPermission = new ()
            {
                ["USE"] = ("使用权限", PermissionPolicy.AcceptedIfGroupAccepted)
            }
        };

    [Command("fmsg")]
    [CmdHelp("生成合并转发消息")]
    public void SummonForwardMsg(GroupMessageEventArgs e)
    {
        if (!PermissionService.Judge(e, Info, "USE"))
            return;
        if (MainModule.hooks.Exists(m => m.QQ == e.Sender.Id && m.Group == e.SourceGroup.Id))
        {
            e.Reply(e.Sender.At() + "还有上一个操作未完成。");
            return;
        }
        e.Reply("请发送数据。");
        MainModule.RegisterHook(e.Sender.Id, e.SourceGroup.Id, FakeMsgCallBack);
    }

    private static async Task<bool> FakeMsgCallBack(GroupMessageEventArgs e, MainModule.GroupMessageHook hook)
    {
        long qq = -1;
        List<CustomNode> nodes = new();
        MessageBody mb = new();
        
        nodes.Add(new CustomNode("温馨提示", e.SoraApi.GetLoginUserId(), "⚠️本合并消息转发为虚假信息，不受信任，其中的内容与本机器人无关，不代表本机器人立场，本机器人为此不负任何责任。"));
        
        foreach (SoraSegment seg in e.Message.MessageBody)
        {
            if (seg.MessageType == SegmentType.Text)
            {
                string[] t = ((TextSegment)(seg.Data)).Content.Replace("\r", "").Split("\n\n");
                for (int i = 0; i < t.Length; i++)
                {
                    if (qq < 0)
                    {
                        if (!long.TryParse(t[i], out qq))
                        {
                            await e.Reply("无效的QQ号：" + t[i]);
                            return true;
                        }
                    }
                    else
                    {
                        if (t[i] != "")
                            mb.Add(t[i]);
                        if (i != t.Length - 1)
                        {
                            nodes.Add(new CustomNode(MainModule.GetQQName(e, qq), qq, mb));
                            mb = new();
                            qq = -1;
                        }
                    }
                }
            }
            else
            {
                mb.Add(seg);
            }
        }
        if (qq > 0)
        {
            nodes.Add(new CustomNode(MainModule.GetQQName(e, qq), qq, mb));
        }

        await e.SourceGroup.SendGroupForwardMsg(nodes);

        return true;
    }
}