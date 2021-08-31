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
using Sora.Entities.MessageElement;
using Sora.Entities.MessageElement.CQModel;
using Sora.EventArgs.SoraEvent;
using static System.Net.WebRequestMethods;
using System.Drawing.Imaging;
using RestSharp;
using Intallk.Config;
using System.Data;
using Sora.Entities.Info;

namespace Intallk.Modules
{
    public class MainModule : IOneBotController
    {
        private Random random = new Random(Guid.NewGuid().GetHashCode());
        private readonly ILogger<MainModule> _logger;
        public MainModule(ICommandService commandService, ILogger<MainModule> logger)
        {
            _logger = logger;
            commandService.Event.OnException += (scope, e, exception) =>
            {
                Console.WriteLine(exception.Message + "\n" + exception.StackTrace);
                switch (e)
                {
                    case GroupMessageEventArgs group:
                        group.Reply("☹发生错误");
                        break;
                    case PrivateMessageEventArgs qq:
                        qq.Reply("☹发生错误");
                        break;
                }
            };
        }

        [Command("bark")]
        public void Bark(GroupMessageEventArgs e)
        {
            string[] eg = { "汪", "喵~", "干嘛啦", "老娘活着", "我不在" };
            e.Reply(eg[random.Next(0, eg.Length)]);
        }

        [Command("draw -help")]
        public void DrawHelp(GroupMessageEventArgs e)
        {
            string temples = "";
            foreach (string file in Directory.GetFiles(IntallkConfig.DataPath + "\\DrawingScript"))
            {
                temples += file.Split('\\')[^1].Split('.')[0] + "，";
            }
            string send = "欢迎使用黑嘴表情包生成工具！\n" +
                          "命令格式：.draw <模板名称> <艾特对方/对方的QQ号>\n" +
                          "可用的模板：\n" + temples;
            e.Reply(send);
        }

        [Command("draw <template> <qq>")]
        public void Draw(string template, User qq,GroupMessageEventArgs e)
        {
            if(!System.IO.File.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".txt"))
            {
                e.Reply(e.Sender.CQCodeAt() + "指定的绘制模板未找到。");
                return;
            }
            GroupMemberInfo user = e.SourceGroup.GetGroupMemberInfo(qq.Id).Result.memberInfo;
            string[] tempMsg = e.Message.RawText.Split('\n');
            string msg = "";
            for(int i = 1;i < tempMsg.Length; i++)
            {
                msg += tempMsg[i] + "\n";
            }
            string[] sex = { "男", "女", "不明" };
            string outfile = IntallkConfig.DataPath + "\\Images\\" + qq.Id + "_" + template + ".png";
            ScriptDrawer.Draw(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".txt",
                              outfile,
                              "[msg]", msg,
                              "[qq]", qq.Id.ToString(),
                              "[nick]", user.Nick,
                              "[card]", user.Card == "" ? user.Nick : user.Card,
                              "[sex]", sex[(int)user.Sex],
                              "[age]", user.Age.ToString(),
                              "[group]", e.SourceGroup.Id.ToString());
            e.Reply(CQCodes.CQImage(outfile));
        }

        [Command("solve <function>")]
        public void Solve(string function,GroupMessageEventArgs e)
        {

        }

        [Command("help")]
        public void Help(GroupMessageEventArgs e)
        {
            e.Reply("没写");
        }

        [Command("sx <content>")]
        public void SXSearch(string content, GroupMessageEventArgs e)
        {
            byte[] buffer = Encoding.Default.GetBytes("{\"text\":\"" + content + "\"}");

            HttpWebRequest request = WebRequest.CreateHttp("https://lab.magiconch.com/api/nbnhhsh/guess");
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = MediaTypeNames.Application.Json;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            WebResponse response;
            string result = "";

            try
            {
                response = request.GetResponse();
                result = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch(Exception err)
            {
                e.Reply(e.Sender.CQCodeAt() + "查询失败(" + err.Message + ")。");
            }

            if(result == "[]")
            {
                e.Reply(e.Sender.CQCodeAt() + "找不到该中文缩写的全称或它不是中文缩写。");
            }
            else
            {
                e.Reply(e.Sender.CQCodeAt() + result.Split('[')[2].Split(']')[0]);
            }
        }

        [Command("gifcombine")]
        public void GIFCombine(GroupMessageEventArgs e)
        {
            foreach(CQCode msg in e.Message.MessageBody)
            {
                if(msg.MessageType == Sora.Enumeration.CQType.Image)
                {
                    Sora.Entities.MessageElement.CQModel.Image img = 
                        (Sora.Entities.MessageElement.CQModel.Image)msg.DataObject;
                    string file = IntallkConfig.DataPath + "\\Images\\" + img.ImgFile + ".png";
                    if(!System.IO.File.Exists(file))
                        System.IO.File.WriteAllBytes(file, new RestClient(img.Url).DownloadData(new RestRequest("#", Method.GET)));
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(file);
                    FrameDimension fd = new FrameDimension(bitmap.FrameDimensionsList[0]);
                    Bitmap convert = new Bitmap(bitmap.Width * bitmap.GetFrameCount(fd), bitmap.Height);
                    Graphics g = Graphics.FromImage(convert);
                    for (int i = 0; i < bitmap.GetFrameCount(fd); i++)
                    {
                        bitmap.SelectActiveFrame(fd, i);
                        g.DrawImage(bitmap, new Point(i * bitmap.Width, 0));
                    }
                    string outfile = IntallkConfig.DataPath + "\\Cache\\" + img.ImgFile + "_combined.png";
                    convert.Save(outfile);
                    Console.WriteLine("Succeed: " + outfile);
                    bitmap.Dispose();
                    g.Dispose();
                    convert.Dispose();

                    e.Reply(CQCodes.CQImage(outfile));
                }
            }
        }

        [Command("bug <content>")]
        public void Bug(string content,GroupMessageEventArgs e)
        {
            e.Reply(BugLanguage.BugLanguage.Convert(content));
        }

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
            string send = "";
            for (int i = 1; i <= count; i++) send += "神经病";
            e.Reply($"黑嘴是{send}。");
        }

    }

}
