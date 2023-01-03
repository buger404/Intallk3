using Intallk.Models;
using Microsoft.International.Converters.PinYinConverter;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

// 404私用功能

// BugLanguage
// 这个功能纯属娱乐，是我小学时期想的一套替换拼音的暗号（？）
// 这个功能的实现完全摆烂，不会优化，不会优化，不会优化（逃）
// 读下面的代码之前，请做好心理准备（指不要被气死）
class BugLanguage : SimpleOneBotController
{
    public BugLanguage(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        { 
            ModuleName = "Bug语言", RootPermission = "BUGLAN",
            RegisteredPermission = new ()
            {
                ["USE"] = ("Bug语言功能使用权限", PermissionPolicy.RequireAccepted)
            }
        };

    [Command("bug <content>")]
    public void Bug(string content, GroupMessageEventArgs e)
    {
        if (!PermissionService.Judge(e, Info, "USE"))
            return;
        e.Reply(Convert(content));
    }

    public static string GetPY(string src)
    {
        string ret = "";
        for (int i = 0; i < src.Length; i++)
        {
            try { ret += new ChineseChar(src[i]).Pinyins[0] + " "; }
            catch { ret += src[i] + "p "; }
        }
        return ret;
    }
    public static string Exchange(string src, string t1, string t2)
    {
        string p = src;
        // 暴力替换
        p = p.Replace(t1, "⚐"); p = p.Replace(t2, t1); p = p.Replace("⚐", t2);
        return p;
    }
    public static string Convert(string src)
    {
        string[] PY = GetPY(src).ToLower().Split(' ');
        string ret = ""; int c = 0;
        string buff = "";
        foreach (string py in PY)
        {
            string p = "";
            if (py != "") p = py.Remove(py.Length - 1);
            p = p.Replace("y", "i");
            if (p == "de") { p = "' "; c = 16; }
            if (p == "bu") { p = "eys-"; c = 0; }
            if (p == "le") { p = ret[^1] + "eq"; c = 16; }
            //if (p == "ma" || p == "ne" || p == "a" || p == "ya" || p == "ba") p = ";";
            if (p == "ni") p = "ms";
            if (p == "wo") p = "i";
            if (p == "men") p = "ss ";
            p = p.Replace("ao", "em");
            p = Exchange(p, "o", "x");
            p = Exchange(p, "f", "t");
            p = Exchange(p, "l", "j");
            p = Exchange(p, "v", "w");
            p = Exchange(p, "q", "d");
            p = Exchange(p, "b", "p");
            p = p.Replace("ng", "''");
            p = p.Replace("n", "﷼"); p = p.Replace("m", "₾"); p = p.Replace("u", "⪹");
            p = p.Replace("﷼", "m"); p = p.Replace("₾", "u"); p = p.Replace("⪹", "n");
            p = p.Replace("zh", "qw");
            p = p.Replace("ch", "cw");
            p = p.Replace("sh", "sw");
            buff += p;
            c += py.Length;
            if (c >= 8)
            {
                c = 0;
                ret += buff; buff = "";
                if (ret[^1] == 'i') ret = ret.Remove(ret.Length - 1) + "y";
                ret += " ";
            }
        }
        ret += buff;
        return ret;
    }
}
