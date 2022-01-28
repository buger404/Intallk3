using Intallk.Config;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using RestSharp;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Intallk.Modules
{
    // 制图功能[弃用]
    // 正在等待重构，详见Painting.cs。
    class ScriptDrawer : IOneBotController
    {
        public static Dictionary<string, string> drawTemplates = new Dictionary<string, string>();

        [Command("draw help [template]")]
        public void DrawHelp(GroupMessageEventArgs e, string template = null)
        {
            string templates = "";
            if (template != null)
            {
                if (!drawTemplates.ContainsKey(template))
                {
                    e.Reply(SoraSegment.Reply(e.Message.MessageId) + "这...这是什么呀，黑嘴不会画啦。");
                    e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\cannot.jpg"));
                    return;
                }
                string[] helpMsg = drawTemplates[template].Split(new string[] { "\r\n" }, StringSplitOptions.None)[0].Split(':');
                if (helpMsg.Length == 1)
                {
                    string send = "这个模板不用别的附加参数啦~\n" +
                              ".draw " + template + " <艾特对方/对方的QQ号>";
                    e.Reply(send);
                }
                else
                {
                    string send = "这个模板是这样用的哦~\n" +
                              ".draw " + template + " <艾特对方/对方的QQ号>（哼，就不告诉你这里要换行）" + helpMsg[1];
                    e.Reply(send);
                }
            }
            else
            {
                foreach (string t in drawTemplates.Keys)
                {
                    templates += t + "，";
                }
                string send = "哼，你...你就这么想请黑嘴帮你画画吗？那，那我就...勉为其难地帮一下你吧...\n" +
                              "命令格式：.draw <模板名称> <艾特对方/对方的QQ号>（我才不告诉你这里要换行呢）[模板附加参数]\n" +
                              "模板附加参数查询：.draw help <模板名称>\n" +
                              "黑嘴会画这些模板哦，黑嘴厉害吧~：\n" + templates;
                e.Reply(send);
            }
        }

        [Command("draw <template>")]
        public void DrawSha(GroupMessageEventArgs e, string template)
        {
            Draw(e, template, null);
        }

        [Command("draw <template> <qq>")]
        public void Draw(GroupMessageEventArgs e, string template, User qq)
        {
            if (!drawTemplates.ContainsKey(template))
            {
                e.Reply(SoraSegment.Reply(e.Message.MessageId) + "什么嘛，黑嘴...可不是因为不会画这个才不帮你画的呢！");
                e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\cannot.jpg"));
                return;
            }
            GroupMemberInfo user;
            if (qq != null) user = e.SourceGroup.GetGroupMemberInfo(qq.Id).Result.memberInfo;
            else user = e.SourceGroup.GetGroupMemberInfo(e.Sender.Id).Result.memberInfo;

            string[] tempMsg = e.Message.RawText.Split('\n');
            string msg = "";
            for (int i = 1; i < tempMsg.Length; i++)
            {
                msg += tempMsg[i] + "\n";
            }
            string[] sex = { "男", "女", "不明" };
            string outfile = IntallkConfig.DataPath + "\\Images\\" + qq.Id + "_" + template + ".png";
            ScriptDrawer.Draw(drawTemplates[template],
                              outfile,
                              "[msg]", msg,
                              "[qq]", qq.Id.ToString(),
                              "[time]", DateTime.Now.ToString(),
                              "[nick]", user.Nick,
                              "[card]", user.Card == "" ? user.Nick : user.Card,
                              "[sex]", sex[(int)user.Sex],
                              "[age]", user.Age.ToString(),
                              "[group]", e.SourceGroup.Id.ToString());
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + SoraSegment.Image(outfile));
        }

        private struct drawParamArray
        {
            public string key;
            public string name;
            public string width;
            public string height;
        }
        private static List<drawParamArray> paramList = new List<drawParamArray>();
        private static DataTable dt = new DataTable();
        private static FontFamily font = new FontFamily("HarmonyOS Sans SC Medium");
        private static StringFormat stf = new StringFormat();
        private static SolidBrush brush = new SolidBrush(Color.Transparent);
        private static Pen pen = new Pen(Color.Transparent);
        public static string AssetsPath = IntallkConfig.DataPath + "\\Resources\\";

        public static object Eval(string s)
        {
            return dt.Compute(s, null);
        }
        public static object FinalValue(string s)
        {
            string r = RepValue(s);
            return Eval(r);
        }
        public static string RepValue(string s)
        {
            string r = s;
            foreach (drawParamArray pa in paramList)
            {
                r = r.Replace(pa.name + "s", pa.key.Length.ToString())
                     .Replace(pa.name + "w", pa.width)
                     .Replace(pa.name + "h", pa.height)
                     .Replace(pa.name, pa.key);
            }
            return r;
        }
        private static Color GoColor(string s)
        {
            string[] t = s.Split('-');
            int a = Convert.ToInt32(FinalValue(t[0]));
            int r = Convert.ToInt32(FinalValue(t[1]));
            int g = Convert.ToInt32(FinalValue(t[2]));
            int b = Convert.ToInt32(FinalValue(t[3]));
            return Color.FromArgb(a, r, g, b);
        }
        private static void DownLoad(string url, string path)
        {
            File.WriteAllBytes(path, new RestClient(url).DownloadDataAsync(new RestRequest("#", Method.Get)).Result);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:验证平台兼容性", Justification = "<挂起>")]
        public static void Draw(string code, string oufile, params string[] param)
        {
            int fail = 0; int fi = 0; string[] cmd = new string[] { "" };
            Graphics g = Graphics.FromHwnd(IntPtr.Zero); Bitmap b = new Bitmap(1, 1);

        tryagain:

            try
            {
                fail++;
                paramList.Clear();
                for (int j = 0; j < param.Length; j += 2)
                {
                    drawParamArray pa = new drawParamArray();
                    pa.name = param[j]; pa.key = param[j + 1];
                    pa.width = (param[j + 1].Length * 20).ToString();//g.MeasureString(param[j + 1], font).Width.ToString();
                    pa.height = "20";  //g.MeasureString(param[j + 1], font).Height.ToString();
                    paramList.Add(pa);
                    if (pa.name == "[msg]")
                    {
                        int msgc = 1;
                        foreach(string content in param[j + 1].Split(' '))
                        {
                            pa = new drawParamArray();
                            pa.name = "[msg" + msgc + "]"; pa.key = content;
                            pa.width = (content.Length * 20).ToString();//g.MeasureString(param[j + 1], font).Width.ToString();
                            pa.height = "20";
                            msgc++;
                            paramList.Add(pa);
                        }
                    }
                }

                cmd = code.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                for (int i = 0; i < cmd.Length; i++)
                {
                    fi = i;
                    string[] p = cmd[i].Split(',');
                    for (int s = 0; s < p.Length; s++)
                    {
                        p[s] = p[s].Trim();
                    }
                    switch (p[0])
                    {
                        case ("img"):
                            if (p[1].StartsWith("net:"))
                            {
                                string f = "";
                                if (p[1].StartsWith("net:<qq>"))
                                {
                                    f = RepValue(p[1].Replace("net:<qq>", ""));
                                    DownLoad("http://q.qlogo.cn/headimg_dl?dst_uin=" + f + "&spec=100", AssetsPath + f + "_face.jpg");
                                }
                                p[1] = f + "_face.jpg";
                            }
                            Image im;
                            try
                            {
                                im = Image.FromFile(AssetsPath + RepValue(p[1]));
                            }
                            catch
                            {
                                im = new Bitmap(1, 1);
                            }

                            switch (p.Length)
                            {
                                case (4):
                                    g.DrawImage(im,
                                                new Point(
                                                    Convert.ToInt32(FinalValue(p[2])), Convert.ToInt32(FinalValue(p[3]))
                                                )
                                                );
                                    break;
                                case (5):
                                    g.DrawImage(im,
                                                new Rectangle(
                                                    Convert.ToInt32(FinalValue(p[2])), Convert.ToInt32(FinalValue(p[3])),
                                                    Convert.ToInt32(Convert.ToDouble(FinalValue(p[4])) * im.Width),
                                                    Convert.ToInt32(Convert.ToDouble(FinalValue(p[4])) * im.Height)
                                                )
                                                );
                                    break;
                                case (6):
                                    g.DrawImage(im,
                                                new Rectangle(
                                                    Convert.ToInt32(FinalValue(p[2])), Convert.ToInt32(FinalValue(p[3])),
                                                    Convert.ToInt32(FinalValue(p[4])), Convert.ToInt32(FinalValue(p[5]))
                                                )
                                                );
                                    break;
                            }
                            im.Dispose();
                            break;
                        case ("load"):
                            //Console.WriteLine(p[1] + "and" + FinalValue(p[1]).ToString() + "and" + RepValue(p[1]));
                            b = new Bitmap(Convert.ToInt32(FinalValue(p[1])), Convert.ToInt32(FinalValue(p[2])));
                            g = Graphics.FromImage(b);
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                            g.Clear(GoColor(p[3]));
                            break;
                        case ("rectl"):
                            pen.Color = GoColor(p[5]);
                            g.DrawRectangle(pen,
                                            new Rectangle(
                                            Convert.ToInt32(FinalValue(p[1])), Convert.ToInt32(FinalValue(p[2])),
                                            Convert.ToInt32(FinalValue(p[3])), Convert.ToInt32(FinalValue(p[4])))
                                            );
                            break;
                        case ("rect"):
                            brush.Color = GoColor(p[5]);
                            g.FillRectangle(brush,
                                            new Rectangle(
                                            Convert.ToInt32(FinalValue(p[1])), Convert.ToInt32(FinalValue(p[2])),
                                            Convert.ToInt32(FinalValue(p[3])), Convert.ToInt32(FinalValue(p[4])))
                                            );
                            break;
                        case ("oval"):
                            brush.Color = GoColor(p[5]);
                            g.FillEllipse(brush,
                                            new Rectangle(
                                            Convert.ToInt32(FinalValue(p[1])), Convert.ToInt32(FinalValue(p[2])),
                                            Convert.ToInt32(FinalValue(p[3])), Convert.ToInt32(FinalValue(p[4])))
                                            );
                            break;
                        case ("rrect"):
                            break;
                        case ("blur"):
                            Rectangle r = new Rectangle(0, 0, b.Width, b.Height);
                            b.GaussianBlur(ref r, Convert.ToInt32(FinalValue(p[1])), false);
                            break;
                        case ("write"):
                        rewrite:
                            try
                            {
                                int fsize = 20; int fstyle = (int)FontStyle.Regular;
                                if (p.Length > 6) { fsize = Convert.ToInt32(RepValue(p[6])); }
                                if (p.Length > 7) { fstyle = Convert.ToInt32(RepValue(p[7])); }
                                brush.Color = GoColor(p[4]);
                                GraphicsPath gp = new GraphicsPath(FillMode.Winding);
                                stf.Alignment = (StringAlignment)Convert.ToInt32(p[5]);
                                Point pos = new Point(Convert.ToInt32(FinalValue(p[1])), Convert.ToInt32(FinalValue(p[2])));
                                foreach (string s in RepValue(p[3]).Split('\n', StringSplitOptions.RemoveEmptyEntries))
                                {
                                    gp.AddString(s, font, fstyle, fsize, pos, stf);
                                    pos.Y += fsize + 10;
                                }

                                g.FillPath(brush, gp);
                                gp.Dispose();
                            }
                            catch
                            {
                                p[3] = "<有毒的文本>";
                                goto rewrite;
                            }

                            //g.DrawString(RepValue(p[3]), font, brush,
                            //             new Point(Convert.ToInt32(FinalValue(p[1])), Convert.ToInt32(FinalValue(p[2]))));
                            break;
                    }
                }

                //System.Drawing.Imaging.ImageAttributes attr = new System.Drawing.Imaging.ImageAttributes();
                //float[][] colorMatrixElements = {
                //new float[] {.33f,  .33f,  .33f,  0, 0},        // r = (r+g+b)/3
                //new float[] {.33f,  .33f,  .33f,  0, 0},        // g = (r+g+b)/3
                //new float[] {.33f,  .33f,  .33f,  0, 0},        // b = (r+g+b)/3
                //new float[] {0,  0,  0,  1, 0},        // alpha scaling factor of 1
                //new float[] {0,  0,  0,  0, 1}};    // 
                //System.Drawing.Imaging.ColorMatrix matrix = new System.Drawing.Imaging.ColorMatrix(colorMatrixElements);
                //attr.SetColorMatrix(matrix);
                //g.DrawImage(b, new Rectangle(0,0,b.Width+1,b.Height+1), 0, 0, b.Width + 1, b.Height + 1, GraphicsUnit.Pixel, attr);

                b.Save(oufile);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Draw succeed :" + oufile);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Draw failed , retry :" + fail + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.TargetSite
                                  + "\n\n" + cmd[fi]);
                g.Dispose(); b.Dispose();
                if (fail >= 13) { return; }
                goto tryagain;
            }
            g.Dispose(); b.Dispose();
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

    public static class Effect
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
    }
}
