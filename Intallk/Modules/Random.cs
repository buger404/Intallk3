﻿using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;

using System.Drawing;
using System.Drawing.Imaging;
using static Intallk.Modules.RepeatCollector;

namespace Intallk.Modules;

public class IntallkRandom : SimpleOneBotController
{
    readonly Random ran = new(Guid.NewGuid().GetHashCode());

    public IntallkRandom(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
    }

    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        { 
            ModuleName = "抽签", RootPermission = "RANDOM",
            HelpCmd = "random", ModuleUsage = "抽签功能，由机器人随机生成数字。",
            RegisteredPermission = new()
            {
                ["USE"] = ("抽签功能使用权限", PermissionPolicy.AcceptedAdminAsDefault)
            }
        };


    [Command("random <min> <max>")]
    [CmdHelp("最小数 最大数", "在区间[最小数,最大数]中随机抽取一个整数")]
    public void random(GroupMessageEventArgs e, int min, int max)
    {
        if (min >= max)
        {
            e.Reply("哼！不许捉弄我！" + min + "可不比" + max + "小。");
            return;
        }
        e.Reply("在" + min + "~" + max + "之间抽中了：" + ran.Next(min, max + 1).ToString());
    }
    [Command("random <count> in <day> d [at]")]
    [CmdHelp("人数 天数 艾特", "抽取群内在指定天数内发言过的几个人，当艾特=at时，抽奖结果将艾特被抽到的人")]
    public void random(GroupMessageEventArgs e, int count, long day, string at = "")
    {
        if (!PermissionService.Judge(e, Info, "USE"))
            return;
        List<GroupMemberInfo> members = e.SourceGroup.GetGroupMemberList().Result.groupMemberList;
        members.RemoveAll(x => x.UserId == e.LoginUid);
        if (count > 19)
        {
            e.Reply("一次性最多只能抽取19人哦。");
            return;
        }
        members.RemoveAll(x => (DateTime.Now - x.LastSentTime).TotalDays > day);
        if (count <= 0)
        {
            e.Reply("好的哦，黑嘴这就帮你抽......" + count + "个人？\n哼，你自己抽吧！🐢💈");
            return;
        }
        if (members.Count < count)
        {
            e.Reply("当前群里设定范围内只有" + members.Count + "人哦，不足够抽取。");
            return;
        }
        MessageBody body = new MessageBody();
        body.Add(e.Sender.At());
        body.Add("发起了抽奖！\n" + "本群一共有" + members.Count + "人(不包含本机器人)在近" + day + "天内发送过消息，设定只抽取该范围的成员。\n" + "🎉🎉恭喜以下成员被抽中！\n");
        for (int i = 1; i <= count; i++)
        {
            int j = ran.Next(0, members.Count);
            if (at == "at")
            {
                body.Add(SoraSegment.At(members[j].UserId));
                if (i < count) body.Add("，");
            }
            else
            {
                body.Add(MainModule.GetQQName(e, members[j].UserId) + "(qq" + members[j].UserId + ")");
                if (i < count) body.Add("\n");
            }
            members.RemoveAt(j);
        }
        e.Reply(body);
    }
    [Command("random <count>")]
    [CmdHelp("人数", "抽取群内的几个人（将艾特被抽中者）")]
    public void random(GroupMessageEventArgs e, int count)
    {
        if (!PermissionService.Judge(e, Info, "USE"))
            return;
        List<GroupMemberInfo> members = e.SourceGroup.GetGroupMemberList().Result.groupMemberList;
        members.RemoveAll(x => x.UserId == e.LoginUid);
        if (count > 19)
        {
            e.Reply("一次性最多只能抽取19人哦。");
            return;
        }
        if (count <= 0)
        {
            e.Reply("好的哦，黑嘴这就帮你抽......" + count + "个人？\n哼，你自己抽吧！🐢💈");
            return;
        }
        if (members.Count < count)
        {
            e.Reply("当前群里只有" + (members.Count + 1) + "人哦，把黑嘴去掉是不够抽取" + count + "人的。");
            return;
        }
        MessageBody body = new MessageBody();
        body.Add(e.Sender.At());
        body.Add("发起了抽奖！\n" + "🎉🎉恭喜以下成员被抽中！\n");
        for (int i = 1; i <= count; i++)
        {
            int j = ran.Next(0, members.Count);
            body.Add(SoraSegment.At(members[j].UserId));
            if (i < count) body.Add("，");
            members.RemoveAt(j);
        }
        e.Reply(body);
    }
}
