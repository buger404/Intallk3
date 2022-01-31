
using Intallk.Config;
using Intallk.Models;
using Intallk.Modules;
using RestSharp;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.EventArgs.SoraEvent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

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
            Commands = new List<PaintCommands>()
        };
        // 保护字符串
        src = src.Replace("\\：", "<protected>").Replace("\\:", "<protected>");
        string[] str = src.Split('\'', StringSplitOptions.RemoveEmptyEntries);
        var strconst = new List<string>();
        var param = new List<string>();
        piclist = new List<string>();
        src = "";
        for (int i = 0; i < str.Length; i++)
        {
            if (i % 2 == 1)
            {
                strconst.Add(str[i].Replace("<protected>", "："));
                src += (strconst.Count - 1).ToString();
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
            if (resolve.Contains('*') || resolve.Contains('\\') || resolve.Contains('/') || resolve.Contains('|') || resolve.Contains('?')
                || resolve.Contains(':') || resolve.Contains('\"') || resolve.Contains('<') || resolve.Contains('>'))
            {
                ThrowException("[ImageUploading.InvalidFileName]设定的图片文件名存在非法字符。");
            }
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
                                if (j % 2 == 1) param.Add(t[j]);
                                if (param.Count > 15) ThrowException("[Standard.ParameterOverflow]请求的参数个数过多（0~15个）。");
                            }
                        }
                    }
                    cmd = new PaintCommands(PaintCommandType.Write, x, y, 0f, 0f, resolve, 18f, FontStyle.Regular, Color.FromArgb(255, 0, 0, 0), PaintAdjustWriteMode.None, false, "", 1f, PaintAlign.Left, PaintAlign.Left);
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
                    if (resolve.Contains('*') || resolve.Contains('\\') || resolve.Contains('/') || resolve.Contains('|') || resolve.Contains('?')
                        || resolve.Contains(':') || resolve.Contains('\"') || resolve.Contains('<') || resolve.Contains('>'))
                    {
                        ThrowException("[ImageUploading.InvalidFileName]设定的图片文件名存在非法字符。");
                    }
                    if (!piclist.Contains(resolve) && resolve != "QQ头像") piclist.Add(resolve);
                    if (resolve == "QQ头像") paintfile.NeedQQParameter = true;
                    cmd = new PaintCommands(PaintCommandType.DrawImage, x, y, 0f, 0f, resolve, PaintAlign.Left, PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("填充矩形"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.FillRectangle, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), PaintAlign.Left, PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("填充椭圆"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.FillEllipse, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), PaintAlign.Left, PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("描边矩形"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.DrawRectangle, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), 1f, PaintAlign.Left, PaintAlign.Left);
                    solved = true;
                }
                if (sen[i].Contains("描边椭圆"))
                {
                    if (solved) ThrowException("[Standard.DoubleCommand]不应在一个句子中同时指定多个绘制指令。");
                    resolve = GetCenter("在", sen[i], "处");
                    if (resolve == "") ThrowException("[Standard.InproperPaintingHeader]每句绘制指令的开头都应用'在x,y处'指定绘制位置。");
                    float x = 0, y = 0;
                    ParsePos(resolve, out x, out y);
                    cmd = new PaintCommands(PaintCommandType.DrawEllipse, x, y, 16f, 16f, Color.FromArgb(255, 0, 0, 0), 1f, PaintAlign.Left, PaintAlign.Left);
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
                    if (cmd.CommandType != PaintCommandType.Write) cmd.Args[4] = color; else cmd.Args[7] = color;
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
                    cmd.Args[8] = PaintAdjustWriteMode.Auto;
                }
                if (sen[i].StartsWith("适应宽度"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    if ((float)cmd.Args[2] == 0 && (float)cmd.Args[3] == 0) ThrowException("[Writer.AutoAdjuster]无法在不定义文本绘制尺寸的情况下自动调整大小。");
                    cmd.Args[8] = PaintAdjustWriteMode.XFirst;
                }
                if (sen[i].StartsWith("适应高度"))
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    if (cmd.CommandType != PaintCommandType.Write) ThrowException("[Standard.InvalidParameter]该参数只对书写命令有效。");
                    if ((float)cmd.Args[2] == 0 && (float)cmd.Args[3] == 0) ThrowException("[Writer.AutoAdjuster]无法在不定义文本绘制尺寸的情况下自动调整大小。");
                    cmd.Args[8] = PaintAdjustWriteMode.YFirst;
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
                    cmd.Args[^1] = PaintAlign.Left; cmd.Args[^2] = PaintAlign.Left;
                }
                if (sen[i] == "向右对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = PaintAlign.Right; cmd.Args[^2] = PaintAlign.Right;
                }
                if (sen[i] == "居中")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = PaintAlign.Center; cmd.Args[^2] = PaintAlign.Center;
                }
                if (sen[i] == "横向向左对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^2] = PaintAlign.Left;
                }
                if (sen[i] == "横向向右对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^2] = PaintAlign.Right;
                }
                if (sen[i] == "横向居中")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^2] = PaintAlign.Center;
                }
                if (sen[i] == "纵向向左对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = PaintAlign.Left;
                }
                if (sen[i] == "纵向向右对齐")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = PaintAlign.Right;
                }
                if (sen[i] == "纵向居中")
                {
                    if (!solved) ThrowException("[Standard.ParameterBeforeCommand]在主绘制命令出现之前，不应该提供参数。");
                    cmd.Args[^1] = PaintAlign.Center;
                }
            }
            if (!solved) ThrowException("[Standard.UnknownGrammer]无法识别语义。");
            paintfile.Commands.Add(cmd);
        }
        if (src.Contains("{QQ")) paintfile.NeedQQParameter = true;
        paintfile.ParameterDescription = "";
        paintfile.Parameters = param;
        foreach (string s in param)
        {
            paintfile.ParameterDescription += $"<{s}> ";
        }
        if (paintfile.ParameterDescription.Length == 0) paintfile.ParameterDescription = "不需要任何参数。";
        if (paintfile.NeedQQParameter) paintfile.ParameterDescription = "<QQ号/艾特对方> " + paintfile.ParameterDescription;
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
}

public class PaintingProcessing
{
    public PaintFile Source;
    public PaintingProcessing(PaintFile src) => Source = src;
    public async Task Paint(string path, GroupMessageEventArgs e, User qq, object[] args)
    {
        Bitmap bitmap;
        List<PaintCommands> cmd = Source.Commands!;
        string dataPath = IntallkConfig.DataPath + "\\DrawingScript\\" + Source.Name + "\\";
        if ((double)cmd[0].Args[0] == 0) bitmap = new(dataPath + (string)cmd[0].Args[1]); else bitmap = new((int)(long)cmd[0].Args[1], (int)(long)cmd[0].Args[2]);
        Graphics g = Graphics.FromImage(bitmap);
        SolidBrush brush = new(Color.Transparent);
        Pen pen = new(Color.Transparent);
        FontFamily font = new("HarmonyOS Sans SC Medium");
        StringFormat stf = new();
        Bitmap image = null!;
        PaintAlign[] align = new PaintAlign[2];
        float x, y, w, h;

        for (int i = 1;i < cmd.Count; i++)
        {
            x = (float)(double)cmd[i].Args[0]; y = (float)(double)cmd[i].Args[1];
            w = (float)(double)cmd[i].Args[2]; h = (float)(double)cmd[i].Args[3];
            align[0] = (PaintAlign)(long)cmd[i].Args[^2];
            align[1] = (PaintAlign)(long)cmd[i].Args[^1];
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
                        image = new(IntallkConfig.DataPath + "\\Resources\\defaultQQFace.png");
                    }
                    else
                    {
                        string qqface = IntallkConfig.DataPath + "\\Resources\\face_" + qq!.Id.ToString() + ".jpg";
                        if (!File.Exists(qqface))
                        {
                            await DownLoad("http://q.qlogo.cn/headimg_dl?dst_uin=" + qq!.Id + "&spec=160", qqface);
                        }
                        image = new(qqface);
                    }
                }
                else
                {
                    image = new(dataPath + (string)cmd[i].Args[4]);
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
                    brush.Color = ParseColor((string)cmd[i].Args[4]);
                    g.FillRectangle(brush, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.FillEllipse:
                    brush.Color = ParseColor((string)cmd[i].Args[4]);
                    g.FillEllipse(brush, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.DrawRectangle:
                    pen.Color = ParseColor((string)cmd[i].Args[4]);
                    pen.Width = (float)(double)cmd[i].Args[5];
                    g.DrawRectangle(pen, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.DrawEllipse:
                    pen.Color = ParseColor((string)cmd[i].Args[4]);
                    pen.Width = (float)(double)cmd[i].Args[5];
                    g.DrawEllipse(pen, new((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.DrawImage:
                    g.DrawImage(image!, new Rectangle((int)x, (int)y, (int)w, (int)h));
                    break;
                case PaintCommandType.Write:
                    float fsize = (float)(double)cmd[i].Args[5]; 
                    FontStyle fstyle = (FontStyle)(long)cmd[i].Args[6];
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
                        for (int j = 0; j < Source.Parameters!.Count; j++)
                        {
                            s = s.Replace("{" + Source.Parameters[j] + "}", (string)args[j + 2 + (qq != null ? 1 : 0)]);
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
                    PaintAdjustWriteMode adjust = (PaintAdjustWriteMode)(long)cmd[i].Args[8];
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
                        pen.Color = ParseColor((string)cmd[i].Args[7]);
                        pen.Width = (float)cmd[i].Args[11];
                        g.DrawPath(pen, gp);
                    }
                    else
                    {
                        brush.Color = ParseColor((string)cmd[i].Args[7]);
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
        bitmap.Save(path);
        font.Dispose();
        stf.Dispose();
        brush.Dispose();
        pen.Dispose();
        g.Dispose();
        bitmap.Dispose();
    }
    public Color ParseColor(string str)
    {
        string[] p = str.Split(',');
        if (p.Length == 1) return Color.FromName(str);
        return Color.FromArgb((int)(float.Parse(p[0]) * 255), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
    }
    static async Task DownLoad(string url, string path) 
    {
        byte[]? data = await new RestClient().DownloadDataAsync(new RestRequest(url, Method.Get));
        if (data != null) File.WriteAllBytes(path, data!); else throw new Exception("下载失败。");
    }
}