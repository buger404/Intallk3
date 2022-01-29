using OneBot.CommandRoute.Services;
using System;
using System.Drawing;
using System.Collections.Generic;
using static PaintingModel;
using System.Globalization;

public class PaintingCompiler
{
    string[]? lines;
    int li = 0;
    public PaintFile CompilePaintScript(string src,out List<string> piclist)
    {
        // 脚本语法
        // 用'。'隔开每个绘制指令，第一个指令必须为创建画布。
        // 用'，'隔开参数。
        PaintFile paintfile = new PaintFile();
        paintfile.Commands = new List<PaintCommands>();
        // 保护字符串
        src = src.Replace("\\：", "<protected>").Replace("\\:", "<protected>");
        string[] str = src.Split(new char[] { '：', ':' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> strconst = new List<string>();
        List<string> param = new List<string>();
        piclist = new List<string>();
        src = "";
        for (int i = 0; i < str.Length; i++)
        {
            if (i % 2 == 1)
            {
                string[] t = str[i].Split("'");
                if (t.Length == 1)
                {
                    lines = new string[]{ str[i] }; li = 0;
                    ThrowException("[Standard.StringGrammer]检测到有字符串不被引号“'”包括。");
                }
                strconst.Add(t[1].Replace("<protected>", "："));
                src += t[2];
            }
            else
            {
                src += str[i];
                if (i < str.Length - 1) src += "：" + strconst.Count;
            }
        }
        lines = src.Split('。', StringSplitOptions.RemoveEmptyEntries);
        string resolve;
        PaintCommands cmd = new PaintCommands(PaintCommandType.SetCanvas,"");

        li = 0;
        resolve = GetCenter("以", lines[0], "创建画布");
        if (resolve == "")
        {
            ThrowException("[Standard.CreateGraphics]第一句话必须包含创建画布的指令。");
        }
        resolve = GetFront(resolve, "为背景");
        if (resolve != "")
        {
            if (resolve.Contains('*') || resolve.Contains('\\') || resolve.Contains('/') || resolve.Contains('|') || resolve.Contains('?')
                || resolve.Contains(':') || resolve.Contains('\"') || resolve.Contains('<') || resolve.Contains('>'))
            {
                ThrowException("[ImageUploading.InvalidFileName]设定的图片文件名存在非法字符。");
            }
            piclist.Add(resolve);
            cmd = new PaintCommands(PaintCommandType.SetCanvas, 0, resolve);
        }
        resolve = GetFront(resolve, "的尺寸");
        if (resolve != "")
        {
            float x = 0, y = 0;
            ParseSize(resolve, out x, out y);
            cmd = new PaintCommands(PaintCommandType.SetCanvas, 1, x, y);
        }
        paintfile.Commands.Add(cmd);
        bool solved = false;
        li = 1;
        for (; li < lines.Length; li++)
        {
            solved = false;
            string[] sen = lines[li].Split('，', StringSplitOptions.None);
            for(int i = 0;i < sen.Length; i++)
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
                        for(int j = 0;j < t.Length; j++)
                        {
                            if(j % 2 == 1) param.Add(t[j]);
                        }
                    }
                    cmd = new PaintCommands(PaintCommandType.Write, x, y, 0, 0, resolve, 18, FontStyle.Regular, Color.FromArgb(255, 0, 0, 0), PaintAdjustWriteMode.None, PaintAlign.Left);
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
                    resolve = t[1];
                    if (resolve.Contains('*') || resolve.Contains('\\') || resolve.Contains('/') || resolve.Contains('|') || resolve.Contains('?')
                        || resolve.Contains(':') || resolve.Contains('\"') || resolve.Contains('<') || resolve.Contains('>'))
                    {
                        ThrowException("[ImageUploading.InvalidFileName]设定的图片文件名存在非法字符。");
                    }
                    if (!piclist.Contains(resolve)) piclist.Add(resolve);

                    cmd = new PaintCommands(PaintCommandType.DrawImage, x, y, 0, 0, resolve, PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("填充矩形"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.FillRectangle, x, y, 16, 16, Color.FromArgb(255, 0, 0, 0), PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("填充椭圆"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.FillEllipse, x, y, 16, 16, Color.FromArgb(255, 0, 0, 0), PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("描边矩形"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.DrawRectangle, x, y, 16, 16, Color.FromArgb(255, 0, 0, 0), PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("描边椭圆"))
                {
                    if(solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.DrawEllipse, x, y, 16, 16, Color.FromArgb(255, 0, 0, 0), PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].StartsWith("大小为"))
                {
                    if(!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    resolve = GetBack("为", sen[i]);
                    float x = 0, y = 0;
                    ParseSize(resolve, out x, out y);
                    cmd.Args[2] = x; cmd.Args[3] = y;
                }
                if (sen[i].StartsWith("字号为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if(cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    resolve = GetBack("为", sen[i]);
                    float size = 0;
                    if(!float.TryParse(resolve, out size)) ThrowException("[NumberParser.InvalidValue]指定字号的值是无效的。");
                    if(size < 1 || size > 256) ThrowException("[Writer.InvalidFontSize]指定字号的值超出了规定范围（1~256）。");
                    cmd.Args[5] = size;
                }
                if (sen[i].StartsWith("正常字体"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = FontStyle.Regular;
                }
                if (sen[i].StartsWith("斜体"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = FontStyle.Italic;
                }
                if (sen[i].StartsWith("粗体"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = FontStyle.Bold;
                }
                if (sen[i].StartsWith("带下划线"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = FontStyle.Underline;
                }
                if (sen[i].StartsWith("带删除线"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[6] = FontStyle.Strikeout;
                }
                if (sen[i].StartsWith("颜色为"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    resolve = GetBack("为", sen[i]);
                    Color color;
                    ParseColor(resolve, out color);
                    if(cmd.CommandType != PaintCommandType.Write) cmd.Args[4] = color; else cmd.Args[7] = color;
                }
                if (sen[i].EndsWith("色"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    resolve = GetFront(sen[i],"色");
                    Color color = Color.Black;
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
                    cmd.Args[7] = PaintAdjustWriteMode.Auto;
                }
                if (sen[i].StartsWith("适应宽度"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[7] = PaintAdjustWriteMode.XFirst;
                }
                if (sen[i].StartsWith("适应高度"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    cmd.Args[7] = PaintAdjustWriteMode.YFirst;
                }
                if (sen[i] == "向左对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[cmd.Args.Length - 1] = PaintAlign.Left;
                }
                if (sen[i] == "向右对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[cmd.Args.Length - 1] = PaintAlign.Right;
                }
                if (sen[i] == "居中")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[cmd.Args.Length - 1] = PaintAlign.Center;
                }
            }
            if (!solved) ThrowException("[Standard.UnknownGrammer]无法识别语义。");
            paintfile.Commands.Add(cmd);
        }
        paintfile.ParameterDescription = "";
        foreach (string s in param)
        {
            paintfile.ParameterDescription += s + "，";
        }
        if (paintfile.ParameterDescription.Length > 0)
            paintfile.ParameterDescription!.Remove(paintfile.ParameterDescription.Length - 1, 1);
        else
            paintfile.ParameterDescription = "不需要任何参数。";
        return paintfile;
    }
    public void ThrowException(string message)
    {
        throw new Exception($"第{li + 1}句中存在错误，黑嘴无法为您编译绘图脚本。\n" + lines![li] + "\n" + message);
    }
    public void ParseColor(string src, out Color color)
    {
        string[] t = src.Split(',');
        int[] tn = new int[t.Length];
        color = Color.Black;
        if(t.Length == 1)
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
            }else if(tn.Length == 3)
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
}
