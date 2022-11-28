using OneBot.CommandRoute.Configuration;

namespace Intallk.Config;

public class IntallkConfig : IOneBotCommandRouteConfiguration
{
    public string[] CommandPrefix => new[] { "." };

    public bool IsCaseSensitive => false;

    public static string DataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Intallk";
}
