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

namespace Intallk.Modules;

public static class SvgString
{
    public static void DrawAsSvg(this string svg, string path)
    {
        XmlDocument xml = new XmlDocument();
        xml.Load(svg);
        SvgDocument doc = SvgDocument.Open(xml);
        doc.Width *= 2; doc.Height *= 2;
        doc.Overflow = SvgOverflow.Inherit;
        doc.FontFamily = "HarmonyOS Sans SC Medium";
        Bitmap bitmap = new Bitmap((int)doc.Width.Value, (int)doc.Height.Value);
        doc.Draw(bitmap);
        bitmap.Save(path);
        bitmap.Dispose();
    }
}

public class UrlPreview : IOneBotController
{
    readonly ILogger<RepeatCollector> _logger;
    private static Regex biliReg = new Regex(
        @"^(((http(s)?:)?//)?((((www|m)\.)?bilibili\.com/(video/)?)|(acg\.tv/)))?(((av)?(?<aid>\d+))|(?<bvid>bv[fZodR9XQDSUm21yCkr6zBqiveYah8bt4xsWpHnJE7jL5VG3guMTKNPAwcF]{10}))(/.*)?(\?.*)?$"
                                             , RegexOptions.IgnoreCase);
    private static string githubRepo = @"https://github-readme-stats.vercel.app/api/pin/?username={0}&repo={1}&show_owner=true";
    private static string githubStat = @"https://github-readme-stats.vercel.app/api?username={0}&show_icons=true&count_private=true&include_all_commits=true&bg_color=62,8EC5FC,E0C3FC&icon_color=000000&title_color=000000&disable_animations=true";
    private static string zhihuFeed = @"/api/v4/questions/{0}/feeds";
    private static string zhihuAnswer = @"/answers/{0}?include=excerpt";

    public UrlPreview(ICommandService commandService, ILogger<RepeatCollector> logger)
    {
        _logger = logger;
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
    }

    private int Event_OnGroupMessage(OneBot.CommandRoute.Models.OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        string url = e!.Message.RawText;
        if (!url.StartsWith("http")) return 0;
        try
        {
            #region Bilibili
            Match match = biliReg.Match(url);
            if (match.Success)
            {
                string id = "", idname = "";
                foreach(object o in match.Groups)
                {
                    string? s = o.ToString();
                    if (s != null)
                    {
                        if (s.ToLower().StartsWith("bv"))
                        {
                            idname = "bvid"; id = s.Substring(2); break;
                        }
                        if (s.ToLower().StartsWith("av"))
                        {
                            idname = "avid"; id = s.Substring(2); break;
                        }
                    }
                }
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
                        e.Reply(((SoraSegment.Image(j["pic"]?.ToString()) + "\n") ?? "") +
                                (j["owner"]?["name"]!.ToString() ?? "未知") + "：" +
                                (j["title"]?.ToString() ?? "未知") + "\n" +
                                (j["desc"]?.ToString() ?? "无简介") + "\n" +
                                $"时长：{duration.Minutes}:{duration.Seconds.ToString("00")}");
                    }
                }
            }
            #endregion
            #region Github
            if (url.ToLower().StartsWith("https://github.com"))
            {
                string[] t = url.Split('/');
                string path = Painting.GetSavePath();
                if(t.Length == 4)
                {
                    string username = t[^1];
                    string.Format(githubStat, username).DrawAsSvg(path);
                    e.Reply(SoraSegment.Image(path));
                }
                else if(t.Length == 5)
                {
                    string username = t[^2], repo = t[^1];
                    string.Format(githubRepo, username, repo).DrawAsSvg(path);
                    e.Reply(SoraSegment.Image(path));
                }
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
                        e.Reply(question + "\n" + author + (headline == "" ? "" : "|" + headline) + "：\n" + answer + "\n" +
                                "回答时间：" + createdTime.ToString("yy.MM.dd HH:mm") +
                                (createdTime.Ticks == updateTime.Ticks ?
                                "" :
                                "\n更新时间：" + updateTime.ToString("yy.MM.dd HH:mm")));
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
