using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models.Enumeration;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules
{
    public class MainModule : IOneBotController
    {
        private Random random = new Random(Guid.NewGuid().GetHashCode());
        private readonly ILogger<MainModule> _logger;
        public MainModule(ICommandService commandService, ILogger<MainModule> logger)
        {
            _logger = logger;
        }

        [Command(".bark")]
        public void Bark(GroupMessageEventArgs e)
        {
            string[] eg = { "汪", "喵~", "干嘛啦", "老娘活着", "我不在" };
            e.Reply(eg[random.Next(0, eg.Length)]);
        }

        [Command(".help")]
        public void Help(GroupMessageEventArgs e)
        {
            e.Reply("没写");
        }

        [Command(".test <count>")]
        public void TestIll([CommandParameter("count")] int count, GroupMessageEventArgs e)
        {
            string send = "";
            for (int i = 1; i <= count; i++) send += "神经病";
            e.Reply($"黑嘴是{send}。");
        }
        /// <summary>
        /// 全局异常处理函数测试
        /// </summary>
        [Command("exception", EventType = EventType.GroupMessage | EventType.PrivateMessage)]
        public void ExceptionTest(BaseSoraEventArgs e)
        {
            switch (e)
            {
                case GroupMessageEventArgs s1:
                    s1.Reply("☹发生错误");
                    break;
                case PrivateMessageEventArgs s2:
                    s2.Reply("☹发生错误");
                    break;
            }
        }

    }

}
