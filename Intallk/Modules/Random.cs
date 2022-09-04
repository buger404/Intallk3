using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using System.Drawing;
using System.Drawing.Imaging;
using static Intallk.Modules.RepeatCollector;

namespace Intallk.Modules;

public class IntallkRandom : IOneBotController
{
    readonly System.Random ran = new(Guid.NewGuid().GetHashCode());
    [Command("random <min> <max>")]
    public void random(GroupMessageEventArgs e, int min, int max)
    {
        if (min <= max)
        {
            e.Reply("哼！不许捉弄我！" + min + "可不比" + max + "小。");
            return;
        }
        e.Reply("在" + min + "~" + max + "之间抽中了：" + ran.Next(min, max + 1).ToString());
    }
    [Command("random <count>")]
    public void random(GroupMessageEventArgs e, int count)
    {
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
        body.Add("🎉🎉恭喜以下成员被抽中！\n");
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
