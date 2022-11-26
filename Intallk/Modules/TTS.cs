using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intallk.Modules;

public class TTS : IOneBotController
{
    public static string api = @"https://tts.baidu.com/text2audio?tex={0}&cuid=baike&lan=ZH&ctp=1&pdt=301&vol=9&rate=32&pelJ";
    [Command("speak <text>")]
    public async void Speak(GroupMessageEventArgs e, string text)
    {
        if (!Permission.Judge(e, "TTS_USE"))
            return;
        byte[]? data = await new RestClient().DownloadDataAsync(new RestRequest(string.Format(api, Uri.EscapeDataString(text))));
        if (data == null)
        {
            await e.Reply("请求失败。");
        }
        else
        {
            string file = IntallkConfig.DataPath + "\\TTS\\" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".mp3";
            File.WriteAllBytes(file, data);
            await e.Reply(SoraSegment.Record(file));
        }
    }
}
