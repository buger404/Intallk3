using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using Newtonsoft.Json;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using System.Drawing;
using System.Drawing.Imaging;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Svg;
using System.Xml;
using Intallk.Models;

namespace Intallk.Modules;

public class UrlPreview : SimpleOneBotController
{
    private readonly static Regex biliReg = new Regex(
        @"^(((http(s)?:)?//)?((((www|m)\.)?bilibili\.com/(video/)?)|(acg\.tv/)))?(((av)?(?<aid>\d+))|(?<bvid>bv[fZodR9XQDSUm21yCkr6zBqiveYah8bt4xsWpHnJE7jL5VG3guMTKNPAwcF]{10}))(/.*)?(\?.*)?$"
                                             , RegexOptions.IgnoreCase);
    private static string githubImg = @"https://opengraph.githubassets.com/6d7553a62b54a4e1ce5ec6db91e70e2775a230d045a7a3097f4474228446247a/{0}";
    private static string zhihuFeed = @"/api/v4/questions/{0}/feeds";
    private static string zhihuAnswer = @"/answers/{0}?include=excerpt";

    public UrlPreview(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
    }

    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        {
            ModuleName = "网址预览", RootPermission = "URLPREVIEW",
            HelpCmd = "url", ModuleUsage = "当群内发送网址时，机器人将获取其包含内容，并展示在群内。\n" +
                                           "目前支持：b站视频、知乎问答、Github页面"
        };

    private int Event_OnGroupMessage(OneBot.CommandRoute.Models.OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if (e == null)
            return 0;
        if (!PermissionService.JudgeGroup(e, Info, "USE"))
            return 0;

        string url = e!.Message.RawText;
        if (!url.ToLower().StartsWith("http") && !url.ToLower().StartsWith("bv")) return 0;
        try
        {
            #region Bilibili
            Match match = biliReg.Match(url);
            if (match.Success || url.ToLower().StartsWith("bv"))
            {
                string id = "", idname = "";
                if (url.ToLower().StartsWith("bv"))
                {
                    idname = "bvid"; id = url.Substring(2);
                }
                else
                {
                    if (match.Groups["bvid"].Value != "")
                    {
                        idname = "bvid"; id = match.Groups["bvid"].Value.Substring(2);
                    }
                    if (match.Groups["aid"].Value != "")
                    {
                        idname = "aid"; id = match.Groups["aid"].Value.Substring(2);
                    }
                }
                if (idname == "") return 0;
                RestResponse response = new RestClient("https://api.bilibili.com").Execute(
                                        new RestRequest("/x/web-interface/view").AddParameter(idname,id));
                if (response.IsSuccessful && response.Content != null)
                {
                    JObject? j = JObject.Parse(response.Content);
                    if (j == null) return 0;
                    if (j["code"]?.ToString() == "0")
                    {
                        j = j["data"] as JObject;
                        if (j == null) return 0;
                        TimeSpan duration = TimeSpan.FromSeconds(double.Parse(j["duration"]?.ToString() ?? "0"));
                        string description = (j["desc"]?.ToString() ?? "无简介");
                        string[] t = description.Split("\n");
                        if (t.Length > 3)
                        {
                            description = t[0] + "\n" + t[1] + "\n" + t[2] + "...";
                        }
                        if (description.Length > 200) description = description.Substring(0, 200) + "...";
                        if (!url.ToLower().StartsWith("http")) url = @"https://www.bilibili.com/video/" + (idname == "bvid" ? "BV" : "av") + id;
                        e.Reply(SoraSegment.Reply(e.Message.MessageId) + ((SoraSegment.Image(j["pic"]?.ToString()) + "\n") ?? "") +
                                (j["owner"]?["name"]!.ToString() ?? "未知") + "：" +
                                (j["title"]?.ToString() ?? "未知") + "\n" +
                                (description == "" || description == null ? "" : description + "\n") +
                                $"时长：{duration.Minutes}:{duration.Seconds.ToString("00")}   {idname}：{(idname == "bvid" ? "BV" : "av") + id}\n" +
                                $"链接：{url}");
                    }
                }
            }
            #endregion
            #region Github
            if (url.ToLower().StartsWith("https://github.com"))
            {
                e.Reply(SoraSegment.Image(string.Format(githubImg, url.Substring("https://github.com/".Length))) +
                        "\n链接：" + url);
            }
            #endregion
            #region 知乎
            if (url.ToLower().StartsWith("https://www.zhihu.com/question"))
            {
                string[] t = url.Split('/');
                string question = t[4], answer = "";
                if (t.Length == 7) answer = t[^1];
                RestResponse response;

                if (answer == "")
                {
                    response = new RestClient("https://www.zhihu.com").Execute(new RestRequest(string.Format(zhihuFeed, question)));
                }
                else
                {
                    response = new RestClient("https://api.zhihu.com").Execute(new RestRequest(string.Format(zhihuAnswer, answer)));
                }
                
                if (response.IsSuccessful && response.Content != null)
                {
                    JObject? j = JObject.Parse(response.Content);
                    if (j != null)
                    {
                        if (answer == "")
                        {
                            JToken[]? ans = j["data"]?.ToArray();
                            if (ans == null) return 0;
                            j = ans[0]["target"] as JObject;
                        }
                        if (j == null) return 0;
                        if (j["author"] == null) return 0;
                        string author = j["author"]!["name"]?.ToString() ?? "未知";
                        string headline =  j["author"]!["headline"]?.ToString() ?? "";
                        DateTime createdTime = DateTime.UnixEpoch.AddSeconds(long.Parse(j["created_time"]?.ToString() ?? "0")).ToLocalTime(),
                                    updateTime = DateTime.UnixEpoch.AddSeconds(long.Parse(j["updated_time"]?.ToString() ?? "0")).ToLocalTime();
                        if (j["question"] == null) return 0;
                        question = j["question"]!["title"]?.ToString() ?? "未知";
                        answer = j["excerpt"]?.ToString() ?? "未知";
                        if(answer.Length >= 200) answer = answer.Substring(0, 200) + "...";
                        e.Reply(SoraSegment.Reply(e.Message.MessageId) +  question + "\n" + author + (headline == "" ? "" : "|" + headline) + "：\n" + answer + "\n" +
                                "回答时间：" + createdTime.ToString("yy.MM.dd HH:mm") +
                                (createdTime.Ticks == updateTime.Ticks ?
                                "" :
                                "\n修改时间：" + updateTime.ToString("yy.MM.dd HH:mm") + 
                                "\n链接：" + url));
                    }
                }
            }
            #endregion
        }
        catch(Exception err)
        {
            Console.WriteLine(err.Message + "\n" + err.StackTrace);
        }
        return 0;
    }
}
