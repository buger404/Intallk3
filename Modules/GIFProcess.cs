using System;
using System.Drawing;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.EventArgs.SoraEvent;
using System.Drawing.Imaging;
using RestSharp;
using Intallk.Config;
using Sora.Enumeration;

namespace Intallk.Modules
{
    internal class GIFProcess : IOneBotController
    {
        [Command("gifextract")]
        public async void GIFExtract(GroupMessageEventArgs e)
        {
            await e.Reply(SoraSegment.Reply(e.Message.MessageId) + "黑嘴准备好啦，快把gif交出来~如果你不发图片，黑嘴就不理你了哦！");
            MainModule.RegisterHook(e.Sender.Id, e.SourceGroup.Id, GIFExtractCallBack);
        }

        public bool GIFExtractCallBack(GroupMessageEventArgs e)
        {
            bool hasImage = false;
            foreach (SoraSegment msg in e.Message.MessageBody)
            {
                if (msg.MessageType == SegmentType.Image)
                {
                    hasImage = true;

                    ImageSegment img = (ImageSegment)msg.Data;
                    string file = IntallkConfig.DataPath + "\\Images\\" + img.ImgFile + ".png";
                    if (!System.IO.File.Exists(file))
                        System.IO.File.WriteAllBytes(file, new RestClient(img.Url).DownloadDataAsync(new RestRequest("#", Method.Get)).Result!);
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(file);
                    FrameDimension fd = new FrameDimension(bitmap.FrameDimensionsList[0]);
                    Bitmap convert = new Bitmap(bitmap.Width, bitmap.Height * bitmap.GetFrameCount(fd));
                    Graphics g = Graphics.FromImage(convert);
                    for (int i = 0; i < bitmap.GetFrameCount(fd); i++)
                    {
                        bitmap.SelectActiveFrame(fd, i);
                        g.DrawImage(bitmap, new Point(0, i * bitmap.Width));
                    }
                    string outfile = IntallkConfig.DataPath + "\\Cache\\" + img.ImgFile + "_combined.png";
                    convert.Save(outfile);
                    Console.WriteLine("Succeed: " + outfile);
                    bitmap.Dispose();
                    g.Dispose();
                    convert.Dispose();

                    e.Reply(SoraSegment.Reply(e.Message.MessageId) + SoraSegment.Image(outfile));
                }
            }
            if (!hasImage)
            {
                e.Reply(SoraSegment.Reply(e.Message.MessageId) + "那好吧~你不想发图片，那黑嘴就不帮你展开了嘤嘤嘤。");
            }
            return true;
        }
    }
}
