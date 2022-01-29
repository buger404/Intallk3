using Intallk.Config;
using Intallk.Models;
using Intallk.Modules;

using Newtonsoft.Json;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;

using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using System.Text;

class Painting : IOneBotController
{
    [Command("draw_experiment build [name] [code]")]
    public void DrawBuild(GroupMessageEventArgs e, string name, string code)
    {
        if (name.Contains('*') || name.Contains('\\') || name.Contains('/') || name.Contains('|') || name.Contains('?')
            || name.Contains(':') || name.Contains('\"') || name.Contains('<') || name.Contains('>'))
        {
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + "设定的模板名字里面不能有特殊符号噢！");
            return;
        }
        if (File.Exists(IntallkConfig.DataPath + "\\DrawingScript\\" + name + ".json"))
        {
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + "设定的模板名字已经被人使用过了。");
            return;
        }
        if (MainModule.hooks.Exists(m => m.QQ == e.Sender.Id && m.Group == e.SourceGroup.Id))
        {
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + "黑嘴还在等待您完成上一个操作呢！");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
            return;
        }
        var paintfile = new PaintFile();
        try
        {
            List<string> picList;
            paintfile = new PaintingCompiler().CompilePaintScript(code, out picList);
            paintfile.Author = e.Sender.Id;
            paintfile.Name = name;
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            serializer.Serialize(new StringWriter(sb), paintfile);
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + sb.ToString());
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + "恭喜您，绘图脚本编译通过了，以下是编译信息：\n" +
                                                             "参数说明：" + paintfile.ParameterDescription + "\n" +
                                                             "绘制步骤：" + paintfile.Commands!.Count.ToString() + "步");
            string picl = "";
            foreach (string s in picList) picl += s + "，";
            if (picList.Count > 0)
            {
                picList.Add(name);
                Directory.CreateDirectory(IntallkConfig.DataPath + "\\DrawingScript\\" + name);
                e.Reply(SoraSegment.Reply(e.Message.MessageId) + "只差一步...接下来按照下面图片的顺序依次发出图片：\n" + picl);
                MainModule.RegisterHook(e.Sender.Id, e.SourceGroup.Id, DrawImageUploadCallBack, picList);
            }
            File.WriteAllText(IntallkConfig.DataPath + "\\DrawingScript\\" + name + ".json", sb.ToString());
        }
        catch (Exception ex)
        {
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + ex.Message);
        }
    }
    public bool DrawImageUploadCallBack(GroupMessageEventArgs e, MainModule.GroupMessageHook hook)
    {
        foreach (SoraSegment msg in e.Message.MessageBody)
        {
            if (msg.MessageType == SegmentType.Image)
            {
                var img = (ImageSegment)msg.Data;
                string file = IntallkConfig.DataPath + "\\DrawingScript\\" + ((List<string>)hook.Data)[^1] + "\\" + ((List<string>)hook.Data)[0];
                if (!File.Exists(file))
                    File.WriteAllBytes(file, new RestClient(img.Url).DownloadDataAsync(new RestRequest("#", Method.Get)).Result);

                ((List<string>)hook.Data).RemoveAt(0);
                if (((List<string>)hook.Data).Count == 1) break;
            }
        }
        if (((List<string>)hook.Data).Count == 1)
        {
            e.Reply(SoraSegment.Reply(e.Message.MessageId) + "感谢哥哥的配合~");
            return true;
        }
        return false;
    }
}
