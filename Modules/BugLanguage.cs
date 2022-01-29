using Intallk.Services;

using Microsoft.International.Converters.PinYinConverter;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using Sora.EventArgs.SoraEvent;

using System.Text;

namespace Intallk.Modules;

internal class BugLanguage : IOneBotController
{
    [Command("bug <content>")]
    public static async void Bug(string content, GroupMessageEventArgs e)
    {
        await e.Reply(BugLanguageService.Convert(content));
    }

}