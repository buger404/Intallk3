
using Intallk.Config;
using Intallk.Models;
using Intallk.Modules;
using RestSharp;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using Sora.Util;
using Sora.Entities.Segment.DataModel;
using System.Runtime.InteropServices;
using System.Reflection;

public class PaintingCompiler
{
    string[]? lines;
    int li = 0;
    public PaintFile CompilePaintScript(string src, out List<string> piclist)
    {
        // 脚本语法
        // 用'。'隔开每个绘制指令，第一个指令必须为创建画布。
        // 用'，'隔开参数。
        var paintfile = new PaintFile
        {
            Commands = new List<PaintCommands>(),
            Code = src
        };
        // 保护字符串
        src = src.Replace("\\：", "<protected>").Replace("\\:", "<protected>");
        string[] str = src.Split('\'', StringSplitOptions.RemoveEmptyEntries);
        var strconst = new List<string>();
        var param = new List<string>();
        var customImg = new List<string>();
        piclist = new List<string>();
        src = "";
        for (int i = 0; i < str.Length; i++)
        {
            if (i % 2 == 1)
            {
                string argName = str[i].Replace("<protected>", "：");
                int k = strconst.FindIndex(that => that == argName);
                if (k == -1)
                {
                    strconst.Add(argName);
                    src += (strconst.Count - 1).ToString();
                }
                else
                {
                    src += k.ToString();
                }
            }
            else
            {
                src += str[i];
            }
        }
        lines = src.Split('。', StringSplitOptions.RemoveEmptyEntries);
        string resolve;
        var cmd = new PaintCommands(PaintCommandType.SetCanvas, "");

        li = 0;
        resolve = GetCenter("以", lines[0], "创建画布");
        if (resolve == "")
        {
            ThrowException("[Standard.CreateGraphics]第一句话必须包含创建画布的指令。");
        }
        resolve = GetFront(resolve, "为背景");
        if (resolve != "")
        {
            resolve = strconst[int.Parse(resolve)];
            IsFileNameValid(resolve);
            piclist.Add(resolve);
            cmd = new PaintCommands(PaintCommandType.SetCanvas, 0f, resolve);
        }
        resolve = GetCenter("以", lines[0], "创建画布");
        resolve = GetFront(resolve, "的尺寸");
        if (resolve != "")
        {
            float x = 0, y = 0;
            ParseSize(resolve, out x, out y);
            cmd = new PaintCommands(PaintCommandType.SetCanvas, 1f, (int)x, (int)y);
        }
        paintfile.Commands.Add(cmd);
        bool solved = false;
        li = 1;
        for (; li < lines.Length; li++)
        {
            solved = false;
            string[] sen = lines[li].Split('，', StringSplitOptions.None);
            for (int i = 0; i < sen.Length; i++)
            {
                if (sen[i].Contains("书写"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    string[] t = sen[i].Split('：');
                    if (t.Length == 1) ThrowException("[Writer.ContentMissing]应指定要书写的内容。");
                    resolve = strconst[int.Parse(t[1])];
                    if (resolve.Contains('{') && resolve.Contains('}'))
                    {
                        t = resolve.Replace('{', '}').Split('}');
                        for (int j = 0; j < t.Length; j++)
                        {
                            if (!t[j].StartsWith("QQ"))
                            {
                                if (j % 2 == 1 && !param.Contains(t[j])) param.Add(t[j]);
                                if (param.Count > 15) ThrowException("[Standard.ParameterOverflow]请求的参数个数过多（0~15个）。");
                            }
                        }
                    }
                    cmd = new PaintCommands(PaintCommandType.Write, x, y, 0f, 0f, resolve, 18f, (int)FontStyle.Regular, Color.FromArgb(255, 0, 0, 0), (int)PaintAdjustWriteMode.None, false, "", 1f, (int)PaintAlign.Left, (int)PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("绘制图片"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    string[] t = sen[i].Split('：');
                    if (t.Length == 1) ThrowException("[ImagePainter.FileMissing]应指定要绘制的图片。");
                    resolve = strconst[int.Parse(t[1])];
                    IsFileNameValid(resolve);
                    if (!piclist.Contains(resolve) && resolve != "QQ头像") piclist.Add(resolve);
                    if (resolve == "QQ头像") paintfile.NeedQQParameter = true;
                    cmd = new PaintCommands(PaintCommandType.DrawImage, x, y, 0f, 0f, resolve, (int)PaintAlign.Left, (int)PaintAlign.Left);
                    if (resolve.StartsWith("img;")){
                        customImg.Add(resolve.Substring(4));
                    }
                    solved = true;
                }
                if (sen[i].Contains("应用滤镜"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    string[] t = sen[i].Split('：');
                    if (t.Length == 1) ThrowException("[Effect.EffectMissing]应指定滤镜名称。");
                    resolve = strconst[int.Parse(t[1])];
                    EffectType ef = EffectType.Blur;
                    if (resolve == "模糊") ef = EffectType.Blur;
                    if (resolve == "黑白") ef = EffectType.GrayScale;
                    cmd = new PaintCommands(PaintCommandType.Effect, x, y, 0f, 0f, (int)ef, 0, 0);
                    solved = true;
                }
                if (sen[i].Contains("填充矩形"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.FillRectangle, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), (int)PaintAlign.Left, (int)PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("填充椭圆"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.FillEllipse, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), (int)PaintAlign.Left, (int)PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("描边矩形"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.DrawRectangle, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), 1f, (int)PaintAlign.Left, (int)PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("描边椭圆"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.DrawEllipse, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), 1f, (int)PaintAlign.Left, (int)PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].StartsWith("大小为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    resolve = GetBack("为", sen[i]);
                    float x = 0, y = 0;
                    ParseSize(resolve, out x, out y);
                    cmd.Args[2] = x; cmd.Args[3] = y;
                }
                if (sen[i].StartsWith("字号为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    resolve = GetBack("为", sen[i]);
                    float size = 0;
                    if (!float.TryParse(resolve, out size)) ThrowException("[NumberParser.InvalidValue]指定字号的值是无效的。");
                    if (size < 1 || size > 256) ThrowException("[Writer.InvalidFontSize]指定字号的值超出了规定范围（1~256）。");
                    cmd.Args[5] = size;
                }
                if (sen[i].StartsWith("正常字体"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = (int)FontStyle.Regular;
                }
                if (sen[i].StartsWith("斜体"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = (int)FontStyle.Italic;
                }
                if (sen[i].StartsWith("粗体"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = (int)FontStyle.Bold;
                }
                if (sen[i].StartsWith("带下划线"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = (int)FontStyle.Underline;
                }
                if (sen[i].StartsWith("带删除线"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = (int)FontStyle.Strikeout;
                }
                if (sen[i].StartsWith("颜色为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    resolve = GetBack("为", sen[i]);
                    Color color;
                    ParseColor(resolve, out color);
                    if (cmd.CommandType != PaintCommandType.Write) cmd.Args[4] = color; else cmd.Args[7] = color;
                }
                if (sen[i].StartsWith("半径为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Effect) ThrowException("[Standard.InvalidParameter]该参数只对滤镜有效。");
                    resolve = GetBack("为", sen[i]);
                    float radius = 0;
                    if (!float.TryParse(resolve, out radius))
                    {
                        ThrowException("[Effect.InvalidRadius]指定的半径数据无效。");
                    }
                    else
                    {
                        if(radius < 0) ThrowException("[Effect.InvalidRadius]指定的半径数据无效。");
                        cmd.Args[5] = radius;
                    }
                }
                if (sen[i].EndsWith("色"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    resolve = GetFront(sen[i], "色");
                    var color = Color.Black;
                    try
                    {
                        color = Color.FromName(resolve);
                    }
                    catch
                    {
                        ThrowException("[ColorParser.InvalidValue]无法根据所给的名字'" + resolve + "'找到对应颜色。");
                    }
                    if (cmd.CommandType != PaintCommandType.Write) cmd.Args[4] = color; else cmd.Args[7] = color;
                }
                if (sen[i].StartsWith("自动调整大小"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    if ((float)cmd.Args[2] == 0 && (float)cmd.Args[3] == 0) ThrowException("[Writer.AutoAdjuster]无法在不定义文本绘制尺寸的情况下自动调整大小。");
                    cmd.Args[8] = (int)PaintAdjustWriteMode.Auto;
                }
                if (sen[i].StartsWith("适应宽度"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    if ((float)cmd.Args[2] == 0 && (float)cmd.Args[3] == 0) ThrowException("[Writer.AutoAdjuster]无法在不定义文本绘制尺寸的情况下自动调整大小。");
                    cmd.Args[8] = (int)PaintAdjustWriteMode.XFirst;
                }
                if (sen[i].StartsWith("适应高度"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    if ((float)cmd.Args[2] == 0 && (float)cmd.Args[3] == 0) ThrowException("[Writer.AutoAdjuster]无法在不定义文本绘制尺寸的情况下自动调整大小。");
                    cmd.Args[8] = (int)PaintAdjustWriteMode.YFirst;
                }
                if (sen[i].StartsWith("描边"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[9] = true;
                }
                if (sen[i].StartsWith("字体为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    resolve = GetBack("为", sen[i]).Replace("_"," ");
                    try
                    {
                        FontFamily font = new(resolve);
                    }
                    catch
                    {
                        ThrowException("[Writer.UnsupportedFont]指定的字体不受支持。");
                    }
                    cmd.Args[10] = resolve;
                }
                if (sen[i].StartsWith("粗为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.DrawEllipse && cmd.CommandType != PaintCommandType.DrawRectangle && cmd.CommandType != PaintCommandType.Write) 
                        ThrowException("[Standard.InvalidParameter]该参数只对描边命令有效。");
                    if (cmd.CommandType == PaintCommandType.Write && !(bool)cmd.Args[9])
                        ThrowException("[Writer.InvalidParameter]要指定描边线条粗细之前，要先将文本书写定义为描边。");
                    resolve = GetBack("为", sen[i]);
                    float b = 0;
                    if (!float.TryParse(resolve, out b)) ThrowException("[WidthParser.InvalidValue]提供的线条粗细值是无效的。");
                    if (b < 1 || b > 100) ThrowException("[WidthParser.InvalidValue]提供的线条粗细值超出了规定范围（1~100）。");
                    if (cmd.CommandType != PaintCommandType.Write) cmd.Args[5] = b; else cmd.Args[11] = b;
                }
                if (sen[i] == "向左对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = (int)PaintAlign.Left; cmd.Args[^2] = (int)PaintAlign.Left;
                }
                if (sen[i] == "向右对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = (int)PaintAlign.Right; cmd.Args[^2] = (int)PaintAlign.Right;
                }
                if (sen[i] == "居中")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = (int)PaintAlign.Center; cmd.Args[^2] = (int)PaintAlign.Center;
                }
                if (sen[i] == "横向向左对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^2] = (int)PaintAlign.Left;
                }
                if (sen[i] == "横向向右对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^2] = (int)PaintAlign.Right;
                }
                if (sen[i] == "横向居中")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^2] = (int)PaintAlign.Center;
                }
                if (sen[i] == "纵向向上对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = (int)PaintAlign.Left;
                }
                if (sen[i] == "纵向向下对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = (int)PaintAlign.Right;
                }
                if (sen[i] == "纵向居中")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = (int)PaintAlign.Center;
                }
            }
            if (!solved) ThrowException("[Standard.UnknownGrammer]无法识别语义。");
            paintfile.Commands.Add(cmd);
        }
        if (src.Contains("{QQ")) paintfile.NeedQQParameter = true;
        paintfile.ParameterDescription = "";
        paintfile.Parameters = param;
        paintfile.CustomImages = customImg;
        foreach (string s in param)
        {
            paintfile.ParameterDescription += $"<{s}> ";
        }
        if (paintfile.ParameterDescription.Length == 0) paintfile.ParameterDescription = "不需要任何参数。";
        if (paintfile.NeedQQParameter) paintfile.ParameterDescription = "<QQ号/艾特对方> " + paintfile.ParameterDescription;
        if (customImg.Count > 0)
        {
            paintfile.ParameterDescription += "\n*需要您额外提供" + customImg.Count + "张图片。";
        }
        return paintfile;
    }
    public void ThrowException(string message) => throw new Exception($"第{li + 1}句中存在错误，黑嘴无法为您编译绘图脚本。\n" + lines![li] + "\n" + message);
    public void ParseColor(string src, out Color color)
    {
        string[] t = src.Split(',');
        int[] tn = new int[t.Length];
        color = Color.Black;
        if (t.Length == 1)
        {
            try
            {
                color = Color.FromArgb(int.Parse(src.Substring(1, 2), NumberStyles.AllowHexSpecifier), int.Parse(src.Substring(3, 2), NumberStyles.AllowHexSpecifier), int.Parse(src.Substring(5, 2), NumberStyles.AllowHexSpecifier));
            }
            catch
            {
                ThrowException("[ColorParser.InvalidValue]值'" + src + "'是非法的。");
            }
        }
        else
        {
            for (int i = 0; i < tn.Length; i++)
            {
                if (!int.TryParse(t[i], out tn[i]))
                {
                    ThrowException("[ColorParser.InvalidValue]值'" + t[i] + "'是非法的。");
                }
            }
            if (tn.Length == 4)
            {
                color = Color.FromArgb(tn[0], tn[1], tn[2], tn[3]);
            }
            else if (tn.Length == 3)
            {
                color = Color.FromArgb(1, tn[0], tn[1], tn[2]);
            }
            else
            {
                ThrowException("[ColorParser.InvalidFormat]无法识别您提供的颜色参数的格式。");
            }
        }
    }
    public void ParsePos(string src, out float x, out float y)
    {
        string[] t = src.Split(',');
        x = 0; y = 0;
        if (t.Length == 1)
        {
            ThrowException("[Standard.PositionGrammer]坐标的描述必须为“x,y”。");
        }
        if (!float.TryParse(t[0], out x))
        {
            ThrowException("[PositionParser.InvalidValue]x的值是非法的。");
        }
        if (!float.TryParse(t[1], out y))
        {
            ThrowException("[PositionParser.InvalidValue]y的值是非法的。");
        }
    }
    public void ParseSize(string src, out float x, out float y)
    {
        string[] t = src.Split('x');
        x = 0; y = 0;
        if (t.Length == 1)
        {
            ThrowException("[Standard.SizeGrammer]尺寸的描述必须为“宽度x高度”。");
        }
        if (!float.TryParse(t[0], out x))
        {
            ThrowException("[SizeParser.InvalidValue]尺寸的宽度是非法的。");
        }
        if (x > 6000 || x <= 0)
        {
            ThrowException("[SizeParser.InvalidValue]尺寸的宽度超出了规定的范围（1~6000）。");
        }
        if (!float.TryParse(t[1], out y))
        {
            ThrowException("[SizeParser.InvalidValue]尺寸的高度是非法的。");
        }
        if (y > 6000 || y <= 0)
        {
            ThrowException("[SizeParser.InvalidValue]尺寸的高度超出了规定的范围（1~6000）。");
        }
    }
    public string GetCenter(string front, string src, string back)
    {
        string[] temp;
        temp = src.Split(front);
        if (temp.Length == 1) return "";
        temp = temp[1].Split(back);
        if (temp.Length == 1) return "";
        return temp[0];
    }
    public string GetFront(string src, string back)
    {
        string[] temp;
        temp = src.Split(back);
        if (temp.Length == 1) return "";
        return temp[0];
    }
    public string GetBack(string front, string src)
    {
        string[] temp;
        temp = src.Split(front);
        if (temp.Length == 1) return "";
        return temp[1];
    }
    public void IsFileNameValid(string name)
    {
        char[] c = { '*', '\\', '/', '|', '?', ':', '\"', '<', '>' };
        foreach (char cc in c)
        {
            if (name.Contains(cc)) ThrowException("[InvalidFileName]设定的文件名存在非法字符。");
        }
        // 试探
        try
        {
            File.ReadAllText(IntallkConfig.DataPath + "\\FileDetection\\" + name);
        }
        catch(Exception ex)
        {
            if(ex.GetType() != typeof(FileNotFoundException))
                ThrowException("[InvalidFileName]设定的文件名非法。");
        }
    }
    public static bool IsDirectoryNameValid(string name)
    {
        char[] c = { '*', '\\', '/', '|', '?', ':', '\"', '<', '>' };
        foreach (char cc in c)
        {
            if (name.Contains(cc)) return false;
        }
        // 试探
        try
        {
            Directory.GetFiles(IntallkConfig.DataPath + "\\FileDetection\\" + name);
        }
        catch (Exception ex)
        {
            if (ex.GetType() != typeof(DirectoryNotFoundException))
                return false;
        }
        return true;
    }
}

public class PaintingProcessing
{
    public PaintFile Source;
    public PaintingProcessing(PaintFile src) => Source = src;
    public BaseSoraEventArgs? MsgSender;
    public async Task<Bitmap> Paint(string path, GroupMessageEventArgs e, User qq, object[] args, string errorImg = null!,string assetsPath = null!)
    {
        Bitmap bitmap;
        List<PaintCommands> cmd = Source.Commands!;
        string dataPath = IntallkConfig.DataPath + "\\DrawingScript\\" + Source.Name + "\\";
        if (assetsPath != null) dataPath = assetsPath;
        if (PfO<int>(cmd[0].Args[0]) == 0)
        {
            if (File.Exists(dataPath + (string)cmd[0].Args[1]))
                bitmap = new(dataPath + (string)cmd[0].Args[1]);
            else
                bitmap = new(errorImg == null ? IntallkConfig.DataPath + "\\Resources\\example.jpg" : errorImg);
        } 
        else
        {
            bitmap = new(PfO<int>(cmd[0].Args[1]), PfO<int>(cmd[0].Args[2]));
        }

        Graphics g = Graphics.FromImage(bitmap);
        g.InterpolationMode = InterpolationMode.High;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        SolidBrush brush = new(Color.Transparent);
        Pen pen = new(Color.Transparent);
        FontFamily font = new("HarmonyOS Sans SC Medium");
        StringFormat stf = new();
        Bitmap image = null!;
        PaintAlign[] align = new PaintAlign[2];
        DateTime drawTime = DateTime.Now;
        float x, y, w, h;
        for (int i = 1;i < cmd.Count; i++)
        {
            if((DateTime.Now - drawTime).TotalSeconds >= 5)
            {
                if (MsgSender != null)
                {
                    if (MsgSender is GroupMessageEventArgs) 
                        await (MsgSender as GroupMessageEventArgs)!.Reply("该模板的绘制用时超出了5秒，为防止机器人卡死，被黑嘴打断了。");
                    if (MsgSender is PrivateMessageEventArgs)
                        await (MsgSender as PrivateMessageEventArgs)!.Reply("该模板的绘制用时超出了5秒，为防止机器人卡死，被黑嘴打断了。");
                }
                
                goto last;
            }
            x = PfO<float>(cmd[i].Args[0]); y = PfO<float>(cmd[i].Args[1]);
            w = PfO<float>(cmd[i].Args[2]); h = PfO<float>(cmd[i].Args[3]);
            align[0] = (PaintAlign)PfO<int>(cmd[i].Args[^2]);
            align[1] = (PaintAlign)PfO<int>(cmd[i].Args[^1]);
            if (x < 1) x *= bitmap.Width;
            if (y < 1) y *= bitmap.Height;
            if (w < 1) w *= bitmap.Width;
            if (h < 1) h *= bitmap.Height;
            if (cmd[i].CommandType == PaintCommandType.DrawImage)
            {
                if((string)cmd[i].Args[4] == "QQ头像")
                {
                    if(qq == null)
                    {
                        image = new(errorImg == null ? IntallkConfig.DataPath + "\\Resources\\defaultQQFace.png" : errorImg);
                    }
                    else
                    {
                        string qqface = IntallkConfig.DataPath + "\\Resources\\face_" + qq!.Id.ToString() + ".jpg";
                        if (File.Exists(qqface)) File.Delete(qqface);
                        await DownLoad("http://q.qlogo.cn/headimg_dl?dst_uin=" + qq!.Id + "&spec=640", qqface);
                        image = new(qqface);
                    }
                }
                else
                {
                    string filename = (string)cmd[i].Args[4];
                    if (!File.Exists(dataPath + filename))
                    {
                        image = new(errorImg == null ? IntallkConfig.DataPath + "\\Resources\\example.jpg" : errorImg);
                    }
                    else
                    {
                        image = new(dataPath + filename);
                    }
                }
                if (w == 0) w = image.Width;
                if (h == 0) h = image.Height;
            }
            if (cmd[i].CommandType != PaintCommandType.Write)
            {
                if (align[0] == PaintAlign.Center) x -= w / 2;
                if (align[1] == PaintAlign.Center) y -= h / 2;
            }
            if (align[0] == PaintAlign.Right) x -= w;
            if (align[1] == PaintAlign.Right) y -= h;
            switch (cmd[i].CommandType)
            {
                case PaintCommandType.FillRectangle:
                    brush.Color = ParseColor(cmd[i].Args[4]);
                    g.FillRectangle(brush, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.FillEllipse:
                    brush.Color = ParseColor(cmd[i].Args[4]);
                    g.FillEllipse(brush, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.DrawRectangle:
                    pen.Color = ParseColor(cmd[i].Args[4]);
                    pen.Width = PfO<float>(cmd[i].Args[5]);
                    g.DrawRectangle(pen, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.DrawEllipse:
                    pen.Color = ParseColor(cmd[i].Args[4]);
                    pen.Width = PfO<float>(cmd[i].Args[5]);
                    g.DrawEllipse(pen, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.Effect:
                    EffectType et = (EffectType)PfO<int>(cmd[i].Args[4]);
                    Rectangle rect;
                    if (et == EffectType.Blur)
                    {
                        float radius = PfO<float>(cmd[i].Args[5]);
                        rect = new Rectangle((int)x, (int)y, (int)(w + x), (int)(h + y));
                        bitmap.GaussianBlur(ref rect, radius, false);
                    }
                    if (et == EffectType.GrayScale)
                    {
                        rect = new Rectangle((int)x, (int)y, (int)w, (int)h);
                        bitmap.ImageGrayscale(ref rect);
                    }
                    break;
                case PaintCommandType.DrawImage:
                    g.DrawImage(image!, new Rectangle((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.Write:
                    float fsize = PfO<float>(cmd[i].Args[5]); 
                    FontStyle fstyle = (FontStyle)PfO<int>(cmd[i].Args[6]);
                    var gp = new GraphicsPath(FillMode.Winding);
                    switch (align[0])
                    {
                        case PaintAlign.Left: stf.Alignment = StringAlignment.Near; break;
                        case PaintAlign.Center: stf.Alignment = StringAlignment.Center; break;
                        case PaintAlign.Right: stf.Alignment = StringAlignment.Far; break;
                    }
                    string s = (string)cmd[i].Args[4];
                    string fontname = (string)cmd[i].Args[10];
                    if(fontname != "" && fontname != font.Name)
                    {
                        font.Dispose();
                        font = new(fontname);
                    }
                    if(args != null)
                    {
                        int k;
                        string rep = "";
                        for (int j = 0; j < Source.Parameters!.Count; j++)
                        {
                            rep = "";
                            k = j + 2 + (qq != null ? 1 : 0);
                            switch (args[k])
                            {
                                case string ss:
                                    rep = ss;
                                    break;
                                case MessageBody mb:
                                    rep = mb.SerializeMessage();
                                    break;
                            }
                            s = s.Replace("{" + Source.Parameters[j] + "}", rep);
                        }
                        if (qq != null)
                        {
                            UserInfo info = (await qq.GetUserInfo()).userInfo;
                            GroupMemberInfo ginfo = (await e.SourceGroup.GetGroupMemberInfo(qq.Id)).memberInfo;
                            s = s.Replace("{QQ名称}", MainModule.GetQQName(e, qq.Id));
                            s = s.Replace("{QQ号}", qq.Id.ToString());
                            s = s.Replace("{QQ年龄}", ginfo.Age.ToString());
                            s = s.Replace("{QQ等级}", ginfo.Level == null ? "？" : ginfo.Level);
                            s = s.Replace("{QQ地区}", ginfo.Area == null ? "？" : ginfo.Area);
                            s = s.Replace("{QQ名片}", ginfo.Card);
                            s = s.Replace("{QQ昵称}", ginfo.Nick);
                            s = s.Replace("{QQ禁言截止时间}", ginfo.ShutUpTime.ToString());
                            s = s.Replace("{QQ头衔}", ginfo.Title == null ? "无" : ginfo.Title);
                            s = s.Replace("{QQ性别}", ginfo.Sex.ToString());
                            s = s.Replace("{QQ入群时间}", ginfo.JoinTime.ToString());
                            s = s.Replace("{QQ上次发言时间}", ginfo.LastSentTime.ToString());
                            s = s.Replace("{QQ群角色}", ginfo.Role.ToString());
                            s = s.Replace("{QQ登录天数}", info.LoginDays.ToString());
                        }
                    }
                    s = s.Replace("\\n", "\n");
                    PaintAdjustWriteMode adjust = (PaintAdjustWriteMode)PfO<int>(cmd[i].Args[8]);
                    if (adjust != PaintAdjustWriteMode.None)
                    {
                        Font ffont = new(font, fsize, fstyle);
                        SizeF size = g.MeasureString(s, ffont); 
                        float xsize = fsize * (w / ((size.Width / 7.5f * 1.8f) / (bitmap.HorizontalResolution / 330f)));
                        float ysize = fsize * (h / ((size.Height / 7.5f * 1.8f) / (bitmap.VerticalResolution / 330f)));
                        if (adjust == PaintAdjustWriteMode.XFirst) fsize = xsize;
                        if (adjust == PaintAdjustWriteMode.YFirst) fsize = ysize;
                        if (adjust == PaintAdjustWriteMode.Auto) fsize = (xsize < ysize) ? xsize : ysize;
                        ffont.Dispose();
                        ffont = new(font, fsize, fstyle);
                        size = g.MeasureString(s, ffont);
                        if (align[1] == PaintAlign.Center) y += h / 2 - ((size.Height / 7.5f * 1.8f) / (bitmap.HorizontalResolution / 330f)) / 2;
                        if (align[1] == PaintAlign.Right) y += h - ((size.Height / 7.5f * 1.8f) / (bitmap.VerticalResolution / 330f));
                        if (align[1] != PaintAlign.Left) h = size.Height / 7.5f * 1.8f;
                        ffont.Dispose();
                    }
                    if (w == 0 && h == 0)
                    {
                        gp.AddString(s, font, (int)fstyle, fsize, new Point((int)x, (int)y), stf);
                    }
                    else
                    {
                        gp.AddString(s, font, (int)fstyle, fsize, new Rectangle((int)x, (int)y, (int)w, (int)h), stf);
                    }
                    if ((bool)cmd[i].Args[9])
                    {
                        pen.Color = ParseColor(cmd[i].Args[7]);
                        pen.Width = PfO<float>(cmd[i].Args[11]);
                        g.DrawPath(pen, gp);
                    }
                    else
                    {
                        brush.Color = ParseColor(cmd[i].Args[7]);
                        g.FillPath(brush, gp);
                    }
                    gp.Dispose();
                    break;
            }
            if(image != null)
            {
                image.Dispose();
                image = null!;
            }
        }
        last:
        font.Dispose();
        stf.Dispose();
        brush.Dispose();
        pen.Dispose();
        g.Dispose();
        if (path == "")
        {
            return bitmap;
        }
        else
        {
            bitmap.Save(path);
            bitmap.Dispose();
            return null!;
        }
    }
    // Parse From Object
    public T PfO<T>(object src)
    {
        double d = 0;
        switch (src)
        {
            case int i: d = i; break;
            case long l: d = l; break;
            case float f: d = f; break;
            case double dd: d = dd; break;
        }
        if (typeof(T) == typeof(int)) return (T)(object)(int)d;
        if (typeof(T) == typeof(long)) return (T)(object)(long)d;
        if (typeof(T) == typeof(float)) return (T)(object)(float)d;
        if (typeof(T) == typeof(double)) return (T)(object)d;
        return default(T)!;
    }
    public Color ParseColor(object src)
    {
        switch (src)
        {
            case string str:
                string[] p = str.Split(',');
                if (p.Length == 1) return Color.FromName(str);
                if(p.Length == 4)
                {
                    return Color.FromArgb(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                }
                else
                {
                    return Color.FromArgb(255, int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));
                }
            case Color color:
                if (color.A == 1) color = Color.FromArgb(255, color.R, color.G, color.B);
                return color;
            case int i:
                return Color.FromArgb(i);
        }
        return Color.Black;
    }
    static async Task DownLoad(string url, string path) 
    {
        byte[]? data = await new RestClient().DownloadDataAsync(new RestRequest(url, Method.Get));
        if (data != null) File.WriteAllBytes(path, data!); else throw new Exception("下载失败。");
    }
}

// ***************** GDI+ Effect函数的示例代码 *********************
// 作者     ： laviewpbt 
// 作者简介 ： 对图像处理（非识别）有着较深程度的理解
// 使用语言 ： VB6.0/C#/VB.NET
// 联系方式 ： QQ-33184777  E-Mail:laviewpbt@sina.com
// 开发时间 ： 2012.12.10-2012.12.12
// 致谢     ： Aaron Lee Murgatroyd
// 版权声明 ： 复制或转载请保留以上个人信息
// *****************************************************************

public static class ImageEffect
{
    private static Guid BlurEffectGuid = new Guid("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");
    private static Guid UsmSharpenEffectGuid = new Guid("{63CBF3EE-C526-402C-8F71-62C540BF5142}");

    [StructLayout(LayoutKind.Sequential)]
    private struct BlurParameters
    {
        internal float Radius;
        internal bool ExpandEdges;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SharpenParams
    {
        internal float Radius;
        internal float Amount;
    }

    internal enum PaletteType               // GDI+1.1还可以针对一副图像获取某种特殊的调色
    {
        PaletteTypeCustom = 0,
        PaletteTypeOptimal = 1,
        PaletteTypeFixedBW = 2,
        PaletteTypeFixedHalftone8 = 3,
        PaletteTypeFixedHalftone27 = 4,
        PaletteTypeFixedHalftone64 = 5,
        PaletteTypeFixedHalftone125 = 6,
        PaletteTypeFixedHalftone216 = 7,
        PaletteTypeFixedHalftone252 = 8,
        PaletteTypeFixedHalftone256 = 9
    };

    internal enum DitherType                    // 这个主要用于将真彩色图像转换为索引图像，并尽量减低颜色损失
    {
        DitherTypeNone = 0,
        DitherTypeSolid = 1,
        DitherTypeOrdered4x4 = 2,
        DitherTypeOrdered8x8 = 3,
        DitherTypeOrdered16x16 = 4,
        DitherTypeOrdered91x91 = 5,
        DitherTypeSpiral4x4 = 6,
        DitherTypeSpiral8x8 = 7,
        DitherTypeDualSpiral4x4 = 8,
        DitherTypeDualSpiral8x8 = 9,
        DitherTypeErrorDiffusion = 10
    }


    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipCreateEffect(Guid guid, out IntPtr effect);

    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDeleteEffect(IntPtr effect);

    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipGetEffectParameterSize(IntPtr effect, out uint size);

    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);

    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipGetEffectParameters(IntPtr effect, ref uint size, IntPtr parameters);

    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref Rectangle rectOfInterest, bool useAuxData, IntPtr auxData, int auxDataSize);

    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipBitmapCreateApplyEffect(ref IntPtr SrcBitmap, int numInputs, IntPtr effect, ref Rectangle rectOfInterest, ref Rectangle outputRect, out IntPtr outputBitmap, bool useAuxData, IntPtr auxData, int auxDataSize);


    // 这个函数我在C#下已经调用成功
    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipInitializePalette(IntPtr palette, int palettetype, int optimalColors, int useTransparentColor, int bitmap);

    // 该函数一致不成功，不过我在VB6下调用很简单，也很成功，主要是结构体的问题。
    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipBitmapConvertFormat(IntPtr bitmap, int pixelFormat, int dithertype, int palettetype, IntPtr palette, float alphaThresholdPercent);

    /// <summary>
    /// 获取对象的私有字段的值，感谢Aaron Lee Murgatroyd
    /// </summary>
    /// <typeparam name="TResult">字段的类型</typeparam>
    /// <param name="obj">要从其中获取字段值的对象</param>
    /// <param name="fieldName">字段的名称.</param>
    /// <returns>字段的值</returns>
    /// <exception cref="System.InvalidOperationException">无法找到该字段.</exception>
    /// 
    internal static TResult GetPrivateField<TResult>(this object obj, string fieldName)
    {
        if (obj == null) return default(TResult);
        Type ltType = obj.GetType();
        FieldInfo lfiFieldInfo = ltType.GetField(fieldName, System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (lfiFieldInfo != null)
            return (TResult)lfiFieldInfo.GetValue(obj);
        else
            throw new InvalidOperationException(string.Format("Instance field '{0}' could not be located in object of type '{1}'.", fieldName, obj.GetType().FullName));
    }

    public static IntPtr NativeHandle(this Bitmap Bmp)
    {
        return Bmp.GetPrivateField<IntPtr>("nativeImage");
        /*  用Reflector反编译System.Drawing.Dll可以看到Image类有如下的私有字段
            internal IntPtr nativeImage;
            private byte[] rawData;
            private object userData;
            然后还有一个 SetNativeImage函数
            internal void SetNativeImage(IntPtr handle)
            {
                if (handle == IntPtr.Zero)
                {
                    throw new ArgumentException(SR.GetString("NativeHandle0"), "handle");
                }
                this.nativeImage = handle;
            }
            这里在看看FromFile等等函数，其实也就是调用一些例如GdipLoadImageFromFile之类的GDIP函数，并把返回的GDIP图像句柄
            通过调用SetNativeImage赋值给变量nativeImage，因此如果我们能获得该值，就可以调用VS2010暂时还没有封装的GDIP函数
            进行相关处理了，并且由于.NET肯定已经初始化过了GDI+，我们也就无需在调用GdipStartup初始化他了。
         */
    }

    /// <summary>
    /// 对图像进行高斯模糊,参考：http://msdn.microsoft.com/en-us/library/ms534057(v=vs.85).aspx
    /// </summary>
    /// <param name="Rect">需要模糊的区域，会对该值进行边界的修正并返回.</param>
    /// <param name="Radius">指定高斯卷积核的半径，有效范围[0，255],半径越大，图像变得越模糊.</param>
    /// <param name="ExpandEdge">指定是否对边界进行扩展，设置为True，在边缘处可获得较为柔和的效果. </param>

    public static void GaussianBlur(this Bitmap Bmp, ref Rectangle Rect, float Radius = 10, bool ExpandEdge = false)
    {
        int Result;
        IntPtr BlurEffect;
        BlurParameters BlurPara;
        if ((Radius < 0) || (Radius > 255))
        {
            throw new ArgumentOutOfRangeException("半径必须在[0,255]范围内");
        }
        BlurPara.Radius = Radius;
        BlurPara.ExpandEdges = ExpandEdge;
        Result = GdipCreateEffect(BlurEffectGuid, out BlurEffect);
        if (Result == 0)
        {
            IntPtr Handle = Marshal.AllocHGlobal(Marshal.SizeOf(BlurPara));
            Marshal.StructureToPtr(BlurPara, Handle, true);
            GdipSetEffectParameters(BlurEffect, Handle, (uint)Marshal.SizeOf(BlurPara));
            GdipBitmapApplyEffect(Bmp.NativeHandle(), BlurEffect, ref Rect, false, IntPtr.Zero, 0);
            // 使用GdipBitmapCreateApplyEffect函数可以不改变原始的图像，而把模糊的结果写入到一个新的图像中
            GdipDeleteEffect(BlurEffect);
            Marshal.FreeHGlobal(Handle);
        }
        else
        {
            throw new ExternalException("不支持的GDI+版本，必须为GDI+1.1及以上版本，且操作系统要求为Win Vista及之后版本.");
        }
    }


    /// <summary>
    /// 对图像进行锐化,参考：http://msdn.microsoft.com/en-us/library/ms534073(v=vs.85).aspx
    /// </summary>
    /// <param name="Rect">需要锐化的区域，会对该值进行边界的修正并返回.</param>
    /// <param name="Radius">指定高斯卷积核的半径，有效范围[0，255],因为这个锐化算法是以高斯模糊为基础的，所以他的速度肯定比高斯模糊妈妈</param>
    /// <param name="ExpandEdge">指定锐化的程度，0表示不锐化。有效范围[0,255]. </param>
    /// 
    public static void UsmSharpen(this Bitmap Bmp, ref Rectangle Rect, float Radius = 10, float Amount = 50f)
    {
        int Result;
        IntPtr UnSharpMaskEffect;
        SharpenParams sharpenParams;
        if ((Radius < 0) || (Radius > 255))
        {
            throw new ArgumentOutOfRangeException("参数Radius必须在[0,255]范围内");
        }
        if ((Amount < 0) || (Amount > 100))
        {
            throw new ArgumentOutOfRangeException("参数Amount必须在[0,255]范围内");
        }
        sharpenParams.Radius = Radius;
        sharpenParams.Amount = Amount;
        Result = GdipCreateEffect(UsmSharpenEffectGuid, out UnSharpMaskEffect);
        if (Result == 0)
        {
            IntPtr Handle = Marshal.AllocHGlobal(Marshal.SizeOf(sharpenParams));
            Marshal.StructureToPtr(sharpenParams, Handle, true);
            GdipSetEffectParameters(UnSharpMaskEffect, Handle, (uint)Marshal.SizeOf(sharpenParams));
            GdipBitmapApplyEffect(Bmp.NativeHandle(), UnSharpMaskEffect, ref Rect, false, IntPtr.Zero, 0);
            GdipDeleteEffect(UnSharpMaskEffect);
            Marshal.FreeHGlobal(Handle);
        }
        else
        {
            throw new ExternalException("不支持的GDI+版本，必须为GDI+1.1及以上版本，且操作系统要求为Win Vista及之后版本.");
        }
    }

    public static void ImageGrayscale(this Bitmap image, ref Rectangle Rect)
    {
        Color pixel;
        for (int x = Rect.Left; x < Rect.Right; x++)
        {
            for (int y = Rect.Top; y < Rect.Bottom; y++)
            {
                pixel = image.GetPixel(x, y);
                int r, g, b, Result = 0;
                r = pixel.R;
                g = pixel.G;
                b = pixel.B;
                int iType = 2;
                switch (iType)
                {
                    case 0://平均值法
                        Result = ((r + g + b) / 3);
                        break;
                    case 1://最大值法
                        Result = r > g ? r : g;
                        Result = Result > b ? Result : b;
                        break;
                    case 2://加权平均值法
                        Result = ((int)(0.7 * r) + (int)(0.2 * g) + (int)(0.1 * b));
                        break;
                }
                //pixel.A
                //alpha 分量指定颜色的透明度：0 是完全透明的，255 是完全不透明的。 同样，值为 A 255 表示不透明颜色。 从 A 1 到 254 的值表示半透明颜色。 接近 A 255 时，颜色变得更加不透明。
                image.SetPixel(x, y, Color.FromArgb(pixel.A, Result, Result, Result));
            }
        }
    }
}