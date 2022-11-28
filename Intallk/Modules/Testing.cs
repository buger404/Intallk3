using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

class Testing : SimpleOneBotController
{
    public Testing(ICommandService commandService, ILogger<SimpleOneBotController> logger) : base(commandService, logger)
    {
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            ModuleName = "基本功能测试", RootPermission = "TESTING",
            HelpCmd = "testing", ModuleUsage = "用于测试机器人的基本功能，无实际意义。"
        };

    [Command("repeats")]
    [CmdHelp("重复发言，但使用空格分隔")]
    public void Repeats(GroupMessageEventArgs e)
    {
        string content = e.Message.RawText, send = "";
        for (int i = 0; i < content.Length; i++) send += content[i] + " ";
        e.Reply(send);
    }
    [Command("repeat")]
    [CmdHelp("重复发言")]
    public void Repeat(GroupMessageEventArgs e)
    {
        string content = e.Message.RawText.Substring(".repeat ".Length);
        if (content.ToLower().StartsWith("dy")) 
            if (!Permission.Judge(e, Info, "DYCONTENT", PermissionPolicy.RequireAccepted))
                return;
        e.Reply(content);
    }
    [Command("test <count>")]
    [CmdHelp("次数", "测试用")]
    public void TestIll(int count, GroupMessageEventArgs e)
    {
        if (count == 0)
        {
            e.Reply("不听不听");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\cannot.jpg"));
            return;
        }
        if (Math.Abs(count) > 100)
        {
            e.Reply("你无聊死啦！");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
            return;
        }
        char[] send = new char[Math.Abs(count) * 3];
        char[] src = (count > 0 ? (count >= 66 ? "大变态" : "神经病") : (count <= -66 ? "态变大" : "病经神")).ToCharArray();
        for (int i = 0; i < send.Length; i += 3) src.CopyTo(send, i);
        e.Reply($"{(Math.Abs(count) < 66 ? "黑嘴" : e.SenderInfo.Nick)}是{new string(send)}。");
        if (Math.Abs(count) >= 66) e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
    }

    [Command("error")]
    [CmdHelp("刻意抛出异常")]
    public void MakeError(GroupMessageEventArgs e)
    {
        if (!Permission.Judge(e, Info, "THROWEXCEPTION", PermissionPolicy.RequireAccepted))
            return;
        int a = 0;
        e.Reply((1 / a).ToString());
    }
}
