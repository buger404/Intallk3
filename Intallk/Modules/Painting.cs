using Intallk.Config;
using Intallk.Models;
using Intallk.Modules;

using Newtonsoft.Json;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Models.Enumeration;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using Sora.Util;
using System.Reflection;
using System.Text;

class Painting : IOneBotController
{
    public class GroupImageUploadData
    {
        public string? template;
        public object[]? args;
        public List<string>? imgs;
        public User qq;
    }
    public static List<PaintingProcessing> paints = new List<PaintingProcessing>();
    public static string GetSavePath()
    {
        return IntallkConfig.DataPath + "\\Images\\draw_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".png";
    }
    [Command("draw showcode <template>")]
    public async void ShowCode(GroupMessageEventArgs e, string template)
    {
        int pi = -1;
        if (!int.TryParse(template, out pi)) pi = paints.FindIndex(m => m.Source.Name == template); else pi--;
        if (pi < 0 || pi >= paints.Count)
        {
            await e.Reply(e.Sender.At() + "未找到指定模板。");
            await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
            return;
        }
        if(paints[pi].Source.Code == null || paints[pi].Source.Code == "")
        {
            await e.Reply(e.Sender.At() + "该模板投稿时间较早，无源码记录。");
        }
        else
        {
            await e.Reply(e.Sender.At() + paints[pi].Source.Code);
        }
    }
    [Command("draw <template> <qq> [s1] [s2] [s3] [s4] [s5] [s6] [s7] [s8] [s9] [s10] [s11] [s12] [s13] [s14] [s15]")]
    public async void Draw(GroupMessageEventArgs e, string template, User qq, [ParsedArguments] object[] args)
    {
        int pi = -1;
        if (MainModule.hooks.Exists(m => m.QQ == e.Sender.Id))
        {
            await e.Reply("请先完成上一个操作。");
            //await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
            return;
        }
        if (!int.TryParse(template, out pi)) pi = paints.FindIndex(m => m.Source.Name == template); else pi--;
        if (pi < 0 || pi >= paints.Count)
        {
            await e.Reply(e.Sender.At() + "未找到指定模板。");
            //await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
            return;
        }
        if ((paints[pi].Source.NeedQQParameter && qq == null) || 
            args.Length < paints[pi].Source.Parameters!.Count + 2 + (paints[pi].Source.NeedQQParameter ? 1 : 0))
        {
            await e.Reply(e.Sender.At() + "指令有误，您可以发送“.draw help " + template + "”取得帮助。");
            return;
        }
        for(int i = 0;i < paints[pi].Source.Parameters!.Count; i++)
        {
            string[] t = paints[pi].Source.Parameters![i].Split('/');
            if(t.Length == 2)
            {
                if (t[1][^1] == '字')
                {
                    //Console.WriteLine("限定符：" + t[1]);
                    string n = t[1].Substring(1, t[1].Length - 2);
                    //Console.WriteLine("试图Parse：" + n);
                    int wordCount = 0;
                    if (int.TryParse(n, out wordCount))
                    {
                        int k = i + 2 + (qq != null ? 1 : 0);
                        //Console.WriteLine("取得参数：" + k);
                        string s = "";
                        switch (args[k])
                        {
                            case string ss:
                                s = ss;
                                break;
                            case MessageBody mb:
                                s = mb.SerializeMessage();
                                break;
                        }
                        //Console.WriteLine("取得参数：" + s);
                        if (t[1][0] == '需')
                        {
                            if (s.Length != wordCount)
                            {
                                await e.Reply(e.Sender.At() + "绘制指令有误，参数<" + paints[pi].Source.Parameters![i] + ">必须填写" + wordCount.ToString() + "个字的内容。");
                                return;
                            }
                        }
                        if (t[1][0] == '限')
                        {
                            if (s.Length > wordCount)
                            {
                                await e.Reply(e.Sender.At() + "绘制指令有误，参数<" + paints[pi].Source.Parameters![i] + ">的内容字数不得超过" + wordCount.ToString() + "字。");
                                return;
                            }
                        }
                    }
                }
            }
            
        }
        string outfile = IntallkConfig.DataPath + "\\Images\\draw_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".png";
        if (paints[pi].Source.CustomImages != null)
        {
            if (paints[pi].Source.CustomImages!.Count > 0)
            {
                if (!Directory.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + template))
                    Directory.CreateDirectory(IntallkConfig.DataPath + "\\DrawingScript\\" + template);
                string picl = "";
                GroupImageUploadData giud = new GroupImageUploadData();
                giud.imgs = new List<string>();
                foreach (string s in paints[pi].Source.CustomImages!)
                {
                    picl += s + "，";
                    giud.imgs.Add(s);
                }
                giud.template = paints[pi].Source.Name;
                giud.args = args;
                giud.qq = qq;
                await e.Reply("请按下面的顺序依次发出图片：\n" + picl);
                MainModule.RegisterHook(e.Sender.Id, e.SourceGroup.Id, DrawGroupImageUploadCallBack, giud);
                return;
            }
        }
        paints[pi].MsgSender = e;
        await paints[pi].Paint(outfile, e, qq!, args);
        string? info = paints[pi].Source.AdditionalInfo;
        if (info != null || info != "") info = "\n" + info;
        await e.Reply(SoraSegment.Image(outfile, false) + info);
    }
    [Command("draw <template> [s1] [s2] [s3] [s4] [s5] [s6] [s7] [s8] [s9] [s10] [s11] [s12] [s13] [s14] [s15]")]
    public void Draw(GroupMessageEventArgs e, string template, [ParsedArguments] object[] args) => Draw(e, template, null!, args);
    public async Task<bool> DrawGroupImageUploadCallBack(GroupMessageEventArgs e, MainModule.GroupMessageHook hook)
    {
        GroupImageUploadData giud = (hook.Data as GroupImageUploadData)!;
        bool hasImg = false;
        foreach (SoraSegment msg in e.Message.MessageBody)
        {
            if (msg.MessageType == SegmentType.Image)
            {
                var img = (ImageSegment)msg.Data;
                string file = IntallkConfig.DataPath + "\\DrawingScript\\" + giud.template + "\\img;" + giud.imgs![0];
                if (File.Exists(file)) File.Delete(file);
                File.WriteAllBytes(file, await new RestClient(img.Url).DownloadDataAsync(new RestRequest("#", Method.Get)));
                giud.imgs!.RemoveAt(0);
                hasImg = true;
                if (giud.imgs!.Count == 0) break;
            }
        }
        if (!hasImg)
        {
            await e.Reply(e.Sender.At() + "已取消绘图命令。");
            return true;
        }
        if (giud.imgs!.Count == 0)
        {
            string outfile = IntallkConfig.DataPath + "\\Images\\draw_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".png";
            PaintingProcessing painter = paints.Find(m => m.Source.Name == giud.template)!;
            await painter.Paint(outfile, e, giud.qq, giud.args!);
            string? info = painter.Source.AdditionalInfo;
            if (info != null || info != "") info = "\n" + info;
            await e.Reply(SoraSegment.Image(outfile, false) + info);
            return true;
        }
        return false;
    }
    [Command("draw")]
    public void DrawHelp(GroupMessageEventArgs e)
    {
        e.Reply(e.Sender.At() + "黑嘴制图功能\n" +
            "绘图脚本说明：https://github.com/buger404/Intallk3/blob/main/PaintScript.md" + "\n" +
            "制图辅助工具下载：https://github.com/buger404/Intallk3/releases/tag/tool\n" +
            "绘图功能指令指南：\n" +
            ".draw list：列出制图库的第一页。\n" +
            ".draw list <页数>：导航到制图库的第几页。\n" +
            ".draw help <模板>：查看指定模板的使用说明。\n" +
            ".draw <模板> (因模板而异)：制图。\n" +
            "（私聊）.draw build <模板> <模板脚本>：投稿新的模板。\n" +
            "（私聊）.draw edit <模板> <模板脚本>：修改已有的模板。\n" +
            "（私聊）.draw remove <模板>：删除已有的模板。");
        return;
    }
    [Command("draw help <template>")]
    public void DrawHelp(GroupMessageEventArgs e, string template)
    {
        int pi = -1;
        if (!int.TryParse(template, out pi)) pi = paints.FindIndex(m => m.Source.Name == template); else pi--;
        if (pi < 0 || pi >= paints.Count)
        {
            e.Reply(e.Sender.At() + "未找到指定模板。");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
            return;
        }
        e.Reply(e.Sender.At() + "作者：" + MainModule.GetQQName(e, paints[pi].Source.Author) + "\n"
                                    + "绘制步骤：共" + paints[pi].Source.Commands!.Count.ToString() + "步\n"
                                    + "使用方法：.draw " + (pi+1).ToString() + "/" + paints[pi].Source.Name + " " + paints[pi].Source.ParameterDescription);
        return;
    }
    [Command("draw list")]
    public void DrawList(GroupMessageEventArgs e) => DrawList(e, 1);
    [Command("draw list <index>")]
    public void DrawList(GroupMessageEventArgs e, int index)
    {
        string ret = "";
        int pagetotal = (int)Math.Ceiling(paints.Count * 1.0 / 10.0);
        if (index > pagetotal || index < 1) return;
        for(int i = (index - 1) * 10;i <= (index - 1) * 10 + 9; i++)
        {
            if (i >= paints.Count) break;
            ret += $"{i + 1}.{paints[i].Source.Name} by {MainModule.GetQQName(e,paints[i].Source.Author)}\n";
        }
        e.Reply($"黑嘴现总计收录绘图模板{paints.Count}个\n{ret}第{index}/{pagetotal}页，使用指令“.draw list 页数”查看更多模板。");
    }
    [Command("draw remove <template>", EventType = EventType.PrivateMessage)]
    public void DrawRemove(PrivateMessageEventArgs e, string template)
    {
        int pi = -1;
        if (!int.TryParse(template, out pi)) pi = paints.FindIndex(m => m.Source.Name == template); else pi--;
        if (pi < 0 || pi >= paints.Count)
        {
            e.Reply("这啥呀，找不到啊。");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
            return;
        }
        if (paints[pi].Source.Author != e.Sender.Id && e.Sender.Id != 1361778219)
        {
            e.Reply("不行，这是别人的模板，不能删...");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\no.png"));
            return;
        }
        if (File.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".json"))
            File.Delete(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".json");
        if (Directory.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + template))
        {
            foreach (string f in Directory.GetFiles(IntallkConfig.DataPath + "\\DrawingScript\\" + template))
                File.Delete(f);
            Directory.Delete(IntallkConfig.DataPath + "\\DrawingScript\\" + template);
        }
        paints.RemoveAt(pi);
        e.Reply("删掉啦~");
    }
    [Command("draw setinfo <template> <info>", EventType = EventType.PrivateMessage)]
    public void DrawInfo(PrivateMessageEventArgs e, string template, string info)
    {
        int pi = -1;
        if (!int.TryParse(template, out pi)) pi = paints.FindIndex(m => m.Source.Name == template); else pi--;
        if (pi < 0 || pi >= paints.Count)
        {
            e.Reply("这啥呀，找不到啊。");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
            return;
        }
        if (paints[pi].Source.Author != e.Sender.Id && e.Sender.Id != 1361778219)
        {
            e.Reply("不行，这是别人的模板，不能修改...");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\no.png"));
            return;
        }
        paints[pi].Source.AdditionalInfo = info;
        JsonSerializer serializer = new();
        StringBuilder code = new StringBuilder();
        serializer.Serialize(new StringWriter(code), paints[pi].Source);
        File.WriteAllText(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".json", code.ToString());
        e.Reply("已设定生成图片的额外信息：" + info);
    }
    [Command("draw edit <name> <code>", EventType = EventType.PrivateMessage)]
    public void DrawEdit(PrivateMessageEventArgs e, string name, string code) => DrawBuild(e, name, code, true);
    [Command("draw build <name> <code>", EventType = EventType.PrivateMessage)]
    public void DrawBuild(PrivateMessageEventArgs e, string name, string code) => DrawBuild(e, name, code, false);
    public async void DrawBuild(PrivateMessageEventArgs e, string name, string code, bool skipNameCheck)
    {
        if (!PaintingCompiler.IsDirectoryNameValid(name))
        {
            await e.Reply("设定的模板名字里面不能有特殊符号的。");
            await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
            return;
        }
        if(name.ToLower() == "list" || name.ToLower() == "help")
        {
            await e.Reply("设定的模板名字里面不能与命令冲突的。");
            await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
            return;
        }
        if (name == "")
        {
            await e.Reply("设定的模板名字不能为空。");
            return;
        }
        int pi = paints.FindIndex(m => m.Source.Name == name);
        if (skipNameCheck)
        {
            if (pi == -1)
            {
                await e.Reply("没有这个模板啦！");
                await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
                return;
            }
            if (paints[pi].Source.Author != e.Sender.Id && e.Sender.Id != 1361778219)
            {
                await e.Reply("但是，这个模板文件不是你的，你不能修改它。");
                await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\oh.png"));
                return;
            }
        }
        else
        {
            if (pi != -1)
            {
                await e.Reply("设定的模板名字'" + name + "'已经被'" + MainModule.GetQQName(e, paints[pi].Source.Author) + "'使用过了。");
                return;
            }
        }
        if (MainModule.hooks2.Exists(m => m.QQ == e.Sender.Id))
        {
            await e.Reply("黑嘴还在等待您完成上一个操作呢！");
            await e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
            return;
        }
        var paintfile = new PaintFile();
        try
        {
            List<string> picList;
            paintfile = new PaintingCompiler().CompilePaintScript(code, out picList);
            picList.RemoveAll(m => m.StartsWith("img;"));
            paintfile.Author = e.Sender.Id;
            paintfile.Name = name;
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            serializer.Serialize(new StringWriter(sb), paintfile);
            //e.Reply(e.Sender.At() + sb.ToString());
            await e.Reply("恭喜您，绘图脚本编译通过了，以下是模板信息：\n" +
                                                             "作者：" + e.SenderInfo.Nick + "\n" +
                                                             "模板名称：" + name + "\n" +
                                                             "参数说明：" + paintfile.ParameterDescription + "\n" +
                                                             "绘制步骤：共" + paintfile.Commands!.Count.ToString() + "步");
            string picl = "";
            foreach (string s in picList) picl += s + "，";
            if (picList.Count > 0)
            {
                picList.Add(name);
                Directory.CreateDirectory(IntallkConfig.DataPath + "\\DrawingScript\\" + name);
                await e.Reply("接着，请按照下面图片的顺序依次发出图片：\n" + picl, new TimeSpan(0,0,2));
                MainModule.RegisterHook(e.Sender.Id, DrawImageUploadCallBack, picList);
            }
            else
            {
                string outfile = IntallkConfig.DataPath + "\\Images\\draw_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".png";
                PaintingProcessing painter = new PaintingProcessing(paintfile);
                painter.MsgSender = e;
                await painter.Paint(outfile, null!, null!, null!);
                await e.Reply("感谢~以下是根据您提交的模板绘制的~\n" +
                        "如果您觉得满意，请回复“是”；放弃本次提交，请回复“取消”；回复其他内容则当作修改脚本重新绘制~");
                await e.Reply(SoraSegment.Image(outfile, false));
                MainModule.RegisterHook(e.Sender.Id, DrawImageConfirmCallBack, painter);
            }
            File.WriteAllText(IntallkConfig.DataPath + "\\DrawingScript\\" + name + ".json", sb.ToString());
        }
        catch (Exception ex)
        {
            await e.Reply(ex.Message);
        }
    }
    public async Task<bool> DrawImageUploadCallBack(PrivateMessageEventArgs e, MainModule.PrivateMessageHook hook)
    {
        foreach (SoraSegment msg in e.Message.MessageBody)
        {
            if (msg.MessageType == SegmentType.Image)
            {
                var img = (ImageSegment)msg.Data;
                string file = IntallkConfig.DataPath + "\\DrawingScript\\" + ((List<string>)hook.Data!)[^1] + "\\" + ((List<string>)hook.Data)[0];
                if (File.Exists(file)) File.Delete(file);
                File.WriteAllBytes(file, await new RestClient(img.Url).DownloadDataAsync(new RestRequest("#", Method.Get)));

                ((List<string>)hook.Data).RemoveAt(0);
                if (((List<string>)hook.Data).Count == 1) break;
            }
        }
        if (((List<string>)hook.Data!).Count == 1)
        {
            string template = ((List<string>)hook.Data!)[^1];
            string outfile = IntallkConfig.DataPath + "\\Images\\draw_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".png";
            string code = File.ReadAllText(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".json");
            JsonSerializer serializer = new();
            PaintFile paintfile = (PaintFile)serializer.Deserialize(new StringReader(code), typeof(PaintFile))!;
            PaintingProcessing painter = new(paintfile);
            await painter.Paint(outfile, null!, null!, null!);
            await e.Reply("感谢~以下是根据您提交的模板绘制的~\n" + 
                    "如果您觉得满意，请回复“是”；放弃本次提交，请回复“取消”；回复其他内容则当作修改脚本重新绘制~");
            await e.Reply(SoraSegment.Image(outfile, false));
            MainModule.RegisterHook(e.Sender.Id, DrawImageConfirmCallBack, painter);
            return true;
        }
        return false;
    }
    public async Task<bool> DrawImageConfirmCallBack(PrivateMessageEventArgs e, MainModule.PrivateMessageHook hook)
    {
        string? template = ((PaintingProcessing)hook.Data!).Source.Name;
        if (e.Message.RawText == "是")
        {
            await e.Reply("🎉非常感谢，绘图模板已收录！");
            int pi = paints.FindIndex(m => m.Source.Name == template);
            if (pi != -1)
                paints[pi] = (PaintingProcessing)hook.Data!;
            else
                paints.Add((PaintingProcessing)hook.Data!);
            return true;
        } 
        else if (e.Message.RawText == "取消")
        {
            await e.Reply("好的。");
            int pi = paints.FindIndex(m => m.Source.Name == template);
            if (pi == -1)
            {
                if (File.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".json"))
                    File.Delete(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".json");
                if (Directory.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + template))
                {
                    foreach(string f in Directory.GetFiles(IntallkConfig.DataPath + "\\DrawingScript\\" + template))
                        File.Delete(f);
                    Directory.Delete(IntallkConfig.DataPath + "\\DrawingScript\\" + template);
                }    
            }
            return true;
        } 
        else
        {
            try
            {
                List<string> picList;
                PaintFile paintfile = new();
                PaintingProcessing painter = (PaintingProcessing)hook.Data!;
                paintfile = new PaintingCompiler().CompilePaintScript(e.Message.RawText, out picList);
                paintfile.Author = e.Sender.Id;
                paintfile.Name = template;
                var serializer = new JsonSerializer();
                var sb = new StringBuilder();
                serializer.Serialize(new StringWriter(sb), paintfile);
                for (int i = 0; i < picList.Count; i++)
                {
                    if (!File.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + template + "\\" + picList[i]) && !picList[i].StartsWith("img;"))
                    {
                        await e.Reply("脚本更正失败，请不要在更正过程中添加新的图片。");
                        return false;
                    }
                }
                await e.Reply("脚本更正成功，并已重新为您生成预览图片。\n" +
                        "如果您觉得满意，请回复“是”；放弃本次提交，请回复“取消”；回复其他内容则当作修改脚本重新绘制~");
                painter.Source = paintfile;
                string outfile = IntallkConfig.DataPath + "\\Images\\draw_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".png";
                await painter.Paint(outfile, null!, null!, null!);
                await e.Reply(SoraSegment.Image(outfile, false));
                File.WriteAllText(IntallkConfig.DataPath + "\\DrawingScript\\" + template + ".json", sb.ToString());
            }
            catch (Exception ex)
            {
                await e.Reply(ex.Message + "\n更正脚本失败。");
            }
        }
        return false;
    }
}
