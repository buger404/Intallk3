using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models.Enumeration;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.EventArgs.SoraEvent;
using System.Drawing.Imaging;
using RestSharp;
using Intallk.Config;
using System.Data;
using Sora.Entities.Info;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Sora.Enumeration;
using System.Threading.Tasks;

namespace Intallk.Modules
{
    public class MainModule : IOneBotController
    {
        private Random random = new Random(Guid.NewGuid().GetHashCode());
        private readonly ILogger<MainModule> _logger;
        public MainModule(ICommandService commandService, ILogger<MainModule> logger)
        {
            _logger = logger;
            foreach (string file in Directory.GetFiles(IntallkConfig.DataPath + "\\DrawingScript"))
            {
                ScriptDrawer.drawTemplates.Add(file.Split('\\')[^1].Split('.')[0], System.IO.File.ReadAllText(file));
            }
            logger.LogInformation("已读入" + ScriptDrawer.drawTemplates.Count + "个绘图模板。");
            commandService.Event.OnException += (context, exception) =>
            {
                logger.LogError(exception.Message + "\n" + exception.StackTrace);
                switch (context.SoraEventArgs)
                {
                    case GroupMessageEventArgs group:
                        group.Reply("我...我才不是为了气死你才出错的呢！");
                        group.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                        break;
                    case PrivateMessageEventArgs qq:
                        qq.Reply("我...我才不是为了气死你才出错的呢！");
                        qq.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                        break;
                }
                // 记小本本
                System.IO.File.AppendAllText(IntallkConfig.DataPath + "\\Logs\\error_" + DateTime.Now.ToString("yy_MM_dd") + ".txt", DateTime.Now.ToString() + "\n" + exception.Message + "\n" + exception.StackTrace + "\n");
            };
            //commandService.Event.OnGroupMessage
        }


        [Command("黑嘴")]
        public void Bark(GroupMessageEventArgs e)
        {
            string[] eg = { "爬", "才...才不告诉你我在呢", "干嘛啦", "老娘活着", "我不在" };
            e.Reply(eg[random.Next(0, eg.Length)]);
        }

        [Command("help")]
        public void Help(GroupMessageEventArgs e)
        {
            e.Reply("没写");
        }

    }

}
