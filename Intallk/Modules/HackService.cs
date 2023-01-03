namespace Intallk.Modules;

public class HackService
{
    public static string Generate(long uid)
    {
        string w = "";
        while(uid > 0)
        {
            long t = uid % 26;
            uid = (uid - t) / 26;
            w = (char)((int)'a' + t) + w;
        }
        return BugLanguage.Convert(w).Replace(" ","");
    }
}
