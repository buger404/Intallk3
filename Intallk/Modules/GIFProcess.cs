using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;

using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using System.Drawing;
using System.Drawing.Imaging;

namespace Intallk.Modules;

class GIFProcess : IOneBotController
{
    [Command("gifextract")]
    public void GIFExtract(GroupMessageEventArgs e)
    {
        if (MainModule.hooks.Exists(m => m.QQ == e.Sender.Id && m.Group == e.SourceGroup.Id))
        {
            e.Reply(e.Sender.At() + "还有上一个操作未完成。");
            e.Reply(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\angry.jpg"));
            return;
        }
        e.Reply(e.Sender.At() + "请发送一张gif图片。");
        MainModule.RegisterHook(e.Sender.Id, e.SourceGroup.Id, GIFExtractCallBack);
    }

    public async Task<bool> GIFExtractCallBack(GroupMessageEventArgs e, MainModule.GroupMessageHook hook)
    {
        bool hasImage = false;
        foreach (SoraSegment msg in e.Message.MessageBody)
        {
            if (msg.MessageType == SegmentType.Image)
            {
                hasImage = true;

                var img = (ImageSegment)msg.Data;
                string file = IntallkConfig.DataPath + "\\Images\\" + img.ImgFile + ".png";
                if (!File.Exists(file))
                    File.WriteAllBytes(file, (await new RestClient(img.Url).DownloadDataAsync(new RestRequest("#", Method.Get)))!);
                var bitmap = (Bitmap)Bitmap.FromFile(file);
                var fd = new FrameDimension(bitmap.FrameDimensionsList[0]);
                var convert = new Bitmap(bitmap.Width, bitmap.Height * bitmap.GetFrameCount(fd));
                var g = Graphics.FromImage(convert);
                for (int i = 0; i < bitmap.GetFrameCount(fd); i++)
                {
                    bitmap.SelectActiveFrame(fd, i);
                    g.DrawImage(bitmap, new Point(0, i * bitmap.Height));
                }
                string outfile = IntallkConfig.DataPath + "\\Cache\\" + img.ImgFile + "_combined.png";
                convert.Save(outfile);
                Console.WriteLine("Succeed: " + outfile);
                bitmap.Dispose();
                g.Dispose();
                convert.Dispose();

                await e.Reply(e.Sender.At() + SoraSegment.Image(outfile));
            }
        }
        if (!hasImage)
        {
            await e.Reply(e.Sender.At() + "已取消gif展开命令。");
        }
        return true;
    }
}
