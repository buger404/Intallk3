using Microsoft.International.Converters.PinYinConverter;

using System.Text;

namespace Intallk.Services;

// BugLanguage
// 这个功能纯属娱乐，是我小学时期想的一套替换拼音的暗号（？）
// 这个功能的实现完全摆烂，不会优化，不会优化，不会优化（逃）
// 读下面的代码之前，请做好心理准备（指不要被气死）
public static class BugLanguageService
{
    public static List<string> GetPY(string src)
    {
        List<string> ret = new();
        foreach (char c in src)
        {
            if (ChineseChar.IsValidChar(c))
            {
                ret.Add(new ChineseChar(c).Pinyins[0]);
            }
            else
            {
                ret.Add($"{char.ToLower(c)}p");
            }
        }
        return ret;
    }

    public static string Exchange(string s, char t1, char t2)
    {
        char[] src = s.ToCharArray();
        foreach(int i in Enumerable.Range(0,src.Length))
        {
            if(src[i] == t1) src[i] = t2;
            else if(src[i] == t2) src[i] = t1;
        }
        return new(src);
    }

    public static string Convert(string src)
    {
        var PY = GetPY(src);
        StringBuilder ret = new(),buff = new();
        int c = 0;
        foreach (string py in PY)
        {
            string p = py[..^1];
            p = p.Replace("y", "i");
            if (p.Equals("de")) { p = "' "; c = 16; }
            if (p == "bu") { p = "eys-"; c = 0; }
            if (p == "le") { p = ret[^1] + "eq"; c = 16; }
            if (p == "ma" || p == "ne" || p == "a" || p == "ya" || p == "ba") p = ";";
            if (p == "ni") p = "ms";
            if (p == "wo") p = "i";
            if (p == "men") p = "ss ";
            p = p.Replace("ao", "em");
            p = Exchange(p, 'o', 'x');
            p = Exchange(p, 'l', 'j');
            p = Exchange(p, 'f', 't');
            p = Exchange(p, 'v', 'w');
            p = Exchange(p, 'q', 'd');
            p = Exchange(p, 'b', 'p');
            p = p.Replace("ng", "''");
            p = p.Replace("n", "﷼"); p = p.Replace("m", "₾"); p = p.Replace("u", "⪹");
            p = p.Replace("﷼", "m"); p = p.Replace("₾", "u"); p = p.Replace("⪹", "n");
            p = p.Replace("zh", "qw");
            p = p.Replace("ch", "cw");
            p = p.Replace("sh", "sw");
            buff.Append(p);
            c += py.Length;
            if (c >= 8)
            {
                c = 0;
                ret.Append(buff);
                buff.Clear();
                if (ret[^1] == 'i') ret[^1] = 'y';
                ret.Append(" ");
            }
        }
        ret.Append(buff);
        return ret.ToString();
    }

}
