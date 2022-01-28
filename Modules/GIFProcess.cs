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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:验证平台兼容性", Justification = "<挂起>")]
        public void GIFExtract(GroupMessageEventArgs e)
        {
            foreach (SoraSegment msg in e.Message.MessageBody)
            {
                if (msg.MessageType == SegmentType.Image)
                {
                    ImageSegment img = (ImageSegment)msg.Data;
                    string file = IntallkConfig.DataPath + "\\Images\\" + img.ImgFile + ".png";
                    if (!System.IO.File.Exists(file))
                        System.IO.File.WriteAllBytes(file, new RestClient(img.Url).DownloadDataAsync(new RestRequest("#", Method.Get)).Result);
                    Bitmap bitmap = (Bitmap)Bitmap.FromFile(file);
                    FrameDimension fd = new FrameDimension(bitmap.FrameDimensionsList[0]);
                    Bitmap convert = new Bitmap(bitmap.Width * bitmap.GetFrameCount(fd), bitmap.Height);
                    Graphics g = Graphics.FromImage(convert);
                    for (int i = 0; i < bitmap.GetFrameCount(fd); i++)
                    {
                        bitmap.SelectActiveFrame(fd, i);
                        g.DrawImage(bitmap, new Point(i * bitmap.Width, 0));
                    }
                    string outfile = IntallkConfig.DataPath + "\\Cache\\" + img.ImgFile + "_combined.png";
                    convert.Save(outfile);
                    Console.WriteLine("Succeed: " + outfile);
                    bitmap.Dispose();
                    g.Dispose();
                    convert.Dispose();

                    e.Reply(SoraSegment.Image(outfile));
                }
            }
        }
    }
}
