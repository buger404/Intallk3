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
using System.Collections.Generic;

namespace Intallk.Modules
{
    public class MainModule : IOneBotController
    {
        private Dictionary<string,string> drawTemplates = new Dictionary<string, string>();
        private Random random = new Random(Guid.NewGuid().GetHashCode());
        private readonly ILogger<MainModule> _logger;
        public MainModule(ICommandService commandService, ILogger<MainModule> logger)
        {
            _logger = logger;
            foreach (string file in Directory.GetFiles(IntallkConfig.DataPath + "\\DrawingScript"))
            {
                drawTemplates.Add(file.Split('\\')[^1].Split('.')[0], System.IO.File.ReadAllText(file));
            }
            logger.LogInformation("已读入" + drawTemplates.Count + "个绘图模板。");
            commandService.Event.OnException += (scope, e, exception) =>
            {
                logger.LogError(exception.Message + "\n" + exception.StackTrace);
                switch (e)
                {
                    case GroupMessageEventArgs group:
                        group.Reply("我...我才不是为了气死你才出错的呢！");
                        group.Reply(CQCodes.CQImage(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
                        break;
                    case PrivateMessageEventArgs qq:
                        qq.Reply("我...我才不是为了气死你才出错的呢！");
                        qq.Reply(CQCodes.CQImage(IntallkConfig.DataPath + "\\Resources\\error.jpg"));
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

        [Command("draw help [template]")]
        public void DrawHelp(GroupMessageEventArgs e, string template = null)
        {
            string templates = "";
            if(template != null)
            {
                if (!drawTemplates.ContainsKey(template))
                {
                    e.Reply(e.Sender.CQCodeAt() + "这...这是什么呀，黑嘴不会画啦。");
                    e.Reply(CQCodes.CQImage(IntallkConfig.DataPath + "\\Resources\\cannot.jpg"));
                    return;
                }
                string[] helpMsg = drawTemplates[template].Split(new string[] { "\r\n" }, StringSplitOptions.None)[0].Split(':');
                if(helpMsg.Length == 1)
                {
                    string send = "这个模板不用别的附加参数啦~\n" +
                              ".draw " + template + " <艾特对方/对方的QQ号>";
                    e.Reply(send);
                }
                else
                {
                    string send = "这个模板是这样用的哦~\n" +
                              ".draw " + template + " <艾特对方/对方的QQ号>（哼，就不告诉你这里要换行）" + helpMsg[1];
                    e.Reply(send);
                }
            }
            else
            {
                foreach (string t in drawTemplates.Keys)
                {
                    templates += t + "，";
                }
                string send = "哼，你...你就这么想请黑嘴帮你画画吗？那，那我就...勉为其难地帮一下你吧...\n" +
                              "命令格式：.draw <模板名称> <艾特对方/对方的QQ号>（我才不告诉你这里要换行呢）[模板附加参数]\n" +
                              "模板附加参数查询：.draw help <模板名称>\n" +
                              "黑嘴会画这些模板哦，黑嘴厉害吧~：\n" + templates;
                e.Reply(send);
            }
        }

        [Command("draw <template>")]
        public void DrawSha(GroupMessageEventArgs e, string template)
        {
            Draw(e, template, null);
        }

        [Command("draw <template> <qq>")]
        public void Draw(GroupMessageEventArgs e,string template, User qq)
        {
            if(!drawTemplates.ContainsKey(template))
            {
                e.Reply(e.Sender.CQCodeAt() + "什么嘛，黑嘴...可不是因为不会画这个才不帮你画的呢！");
                e.Reply(CQCodes.CQImage(IntallkConfig.DataPath + "\\Resources\\cannot.jpg"));
                return;
            }
            GroupMemberInfo user;
            if(qq != null) user = e.SourceGroup.GetGroupMemberInfo(qq.Id).Result.memberInfo;
            else user = e.SourceGroup.GetGroupMemberInfo(e.Sender.Id).Result.memberInfo;

            string[] tempMsg = e.Message.RawText.Split('\n');
            string msg = "";
            for(int i = 1;i < tempMsg.Length; i++)
            {
                msg += tempMsg[i] + "\n";
            }
            string[] sex = { "男", "女", "不明" };
            string outfile = IntallkConfig.DataPath + "\\Images\\" + qq.Id + "_" + template + ".png";
            ScriptDrawer.Draw(drawTemplates[template],
                              outfile,
                              "[msg]", msg,
                              "[qq]", qq.Id.ToString(),
                              "[time]", DateTime.Now.ToString(),
                              "[nick]", user.Nick,
                              "[card]", user.Card == "" ? user.Nick : user.Card,
                              "[sex]", sex[(int)user.Sex],
                              "[age]", user.Age.ToString(),
                              "[group]", e.SourceGroup.Id.ToString());
            e.Reply(CQCodes.CQImage(outfile));
        }

        [Command("error")]
        public void MakeError(GroupMessageEventArgs e)
        {
            int a = 0;
            e.Reply((1 / a).ToString());
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
                e.Reply(e.Sender.CQCodeAt() + "呜呜呜，服务器不让我偷看他(" + err.Message + ")。");
            }

            if(result == "[]")
            {
                e.Reply(e.Sender.CQCodeAt() + "咦，这是什么的中文缩写呀，查不到呢。");
            }
            else
            {
                e.Reply(e.Sender.CQCodeAt() + result.Split(new char[]{'[',']'})[2]);
            }
        }

        [Command("gifcombine")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:验证平台兼容性", Justification = "<挂起>")]
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
            if(count == 0)
            {
                e.Reply("不听不听");
                e.Reply(CQCodes.CQImage(IntallkConfig.DataPath + "\\Resources\\cannot.jpg"));
                return;
            }
            if(Math.Abs(count) > 100)
            {
                e.Reply("你无聊死啦！");
                e.Reply(CQCodes.CQImage(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
                return;
            }
            char[] send = new char[Math.Abs(count) * 3];
            char[] src = (count > 0 ? (count >= 66 ? "大变态" : "神经病") : (count <= -66 ? "态变大" : "病经神")).ToCharArray();
            for (int i = 0; i < send.Length; i += 3) src.CopyTo(send, i);
            e.Reply($"{(Math.Abs(count) < 66 ? "黑嘴" : e.SenderInfo.Nick)}是{new string(send)}。");
            if(Math.Abs(count) >= 66) e.Reply(CQCodes.CQImage(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
        }

    }

}
