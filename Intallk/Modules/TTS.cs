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
using Intallk.Models;

namespace Intallk.Modules;

public class TTS : SimpleOneBotController
{
    public static readonly string api = @"https://tts.baidu.com/text2audio?tex={0}&cuid=baike&lan=ZH&ctp=1&pdt=301&vol=9&rate=32&pelJ";

    public TTS(ICommandService commandService, ILogger<SimpleOneBotController> logger) : base(commandService, logger)
    {
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        { 
            ModuleName = "文字转语音", RootPermission = "TTS",
            HelpCmd = "tts", ModuleUsage = "利用百度TTS合成语音并发送。"
        };

    [Command("tts <text>")]
    [CmdHelp("文本", "合成语音")]
    public async void TTSRequest(GroupMessageEventArgs e, string text)
    {
        if (!Permission.Judge(e, Info, "USE", PermissionPolicy.AcceptedIfGroupAccepted))
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
