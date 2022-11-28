using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models;
using OneBot.CommandRoute.Services;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using static Intallk.Models.DictionaryReplyModel;

namespace Intallk.Modules;

public class RhythmGameSong : SimpleOneBotController
{
    public RhythmGameSong(ICommandService commandService, ILogger<SimpleOneBotController> logger) : base(commandService, logger)
    {
        foreach(string file in Directory.GetFiles(SongPath))
        {
            Song.Add(Path.GetFileNameWithoutExtension(file));
        }
        logger.LogInformation("已成功从{path}导入{count}首音游曲。", SongPath, Song.Count);
        commandService.Event.OnGroupMessage += Event_OnGroupMessage;
    }

    public override ModuleInformation Initialize() =>
        new ModuleInformation { ModuleName = "音游曲回应", RootPermission = "RHYTHMGAMESONG",
                                HelpCmd = "rgs", ModuleUsage = "当消息里包含已记录的人声采样时，将发送对应的曲目片段。\n" +
                                                               "曲目库暂时无法开放修改。"
        };

    public readonly static string SongPath = Path.Combine(IntallkConfig.DataPath, "RhythmGameSong");
    public static List<string> Song = new List<string>();

    private int Event_OnGroupMessage(OneBotContext scope)
    {
        GroupMessageEventArgs e = (GroupMessageEventArgs)scope.SoraEventArgs;
        if (!Permission.JudgeGroup(e, Info, "RESPOND"))
            return 0;
        if (!Permission.Judge(e, Info, "TRIGGER"))
            return 0;
        string msg = e.Message.RawText.ToLower();
        int index = Song.FindIndex(x => msg.StartsWith(x.ToLower()));
        if (index == -1)
            return 0;
        e.Reply(SoraSegment.Record(SongPath + "\\" + Song[index] + ".mp3"));
        return 0;
    }

    [Command("rgs reload")]
    [CmdHelp("重新载入曲目库")]
    public void ReloadSongs(GroupMessageEventArgs e)
    {
        if (!Permission.Judge(e, Info, "EDIT", PermissionPolicy.RequireAccepted))
            return;
        Song.Clear();
        foreach (string file in Directory.GetFiles(SongPath))
        {
            Song.Add(Path.GetFileNameWithoutExtension(file));
        }
        e.Reply("重新载入完毕，一共" + Song.Count + "首曲目。");
    }
}
