using OneBot.CommandRoute.Configuration;

namespace Intallk.Config;

public class IntallkConfig : IOneBotCommandRouteConfiguration
{
    public string[] CommandPrefix => new[] { "." };

    public bool IsCaseSensitive => false;

    public static string DataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Intallk";

    public static string DbData = "";

    static IntallkConfig()
    {
        if (File.Exists("data_prename.txt"))
            DbData = File.ReadAllText("data_prename.txt") + "_";
    }
}
