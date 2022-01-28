using System;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using Intallk.Config;

namespace Intallk.Modules
{
    internal class Testing : IOneBotController
    {
        [Command("repeat")]
        public void Repeat(GroupMessageEventArgs e)
        {
            string content = e.Message.RawText, send = "";
            for (int i = 0; i < content.Length; i++) send += content[i] + " ";
            e.Reply(send);
        }

        [Command("test <count>")]
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
        public void MakeError(GroupMessageEventArgs e)
        {
            int a = 0;
            e.Reply((1 / a).ToString());
        }
    }
}
