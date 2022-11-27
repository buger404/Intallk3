using Intallk.Config;
using Sora.Entities.Segment.DataModel;
using Sora.Entities.Segment;
using Sora.Entities;
using Intallk.Modules;

namespace Intallk.Models;

[Serializable]
public class RepeatCollection
{
    public List<SingleRecordingMsg> messages = new List<SingleRecordingMsg>();
}
[Serializable]
public class MessageSegment
{
    public const int DownloadImgTimedOut = 5;
    public static List<MessageSegment> Copy(List<MessageSegment> seg)
    {
        List<MessageSegment> segs = new List<MessageSegment>();
        foreach (MessageSegment se in seg)
        {
            segs.Add(new MessageSegment { Content = se.Content, isImage = se.isImage, Url = se.Url });
        }
        return segs;
    }
    public static bool Compare(List<MessageSegment> seg1, List<MessageSegment> seg2)
    {
        if (seg1.Count != seg2.Count) return false;
        for (int i = 0; i < seg1.Count; i++)
        {
            if (seg1[i].isImage != seg2[i].isImage || seg1[i].Content != seg2[i].Content) return false;
        }
        return true;
    }
    public static List<MessageSegment> Parse(MessageBody mb)
    {
        List<MessageSegment> messages = new List<MessageSegment>();
        foreach (SoraSegment m in mb)
        {
            if (m.MessageType == Sora.Enumeration.SegmentType.Text)
            {
                messages.Add(new MessageSegment { Content = ((TextSegment)m.Data).Content, isImage = false });
            }
            else if (m.MessageType == Sora.Enumeration.SegmentType.Image)
            {
                messages.Add(new MessageSegment { Content = ((ImageSegment)m.Data).ImgFile, isImage = true, Url = ((ImageSegment)m.Data).Url });
            }
            else if (m.MessageType == Sora.Enumeration.SegmentType.RedBag)
            {
                RedbagSegment redbag = (RedbagSegment)m.Data;
                messages.Add(new MessageSegment { Content = "[红包:" + redbag.Title + "]", isImage = false });
            }
        }
        return messages;
    }

    public string? Content { get; set; }
    public string? Url { get; set; }
    public bool isImage;
    public void DownloadImage()
    {
        if (!Content!.EndsWith(".image") || Url == null) 
            return;
        string file = "context_" + DateTime.Now.ToString("yy.MM.dd.HH.mm.ss.") + this.GetHashCode() + ".jpg";
        Url.DownLoad(IntallkConfig.DataPath + "\\Resources\\" + file);
        DateTime time = DateTime.Now;
        Content = file;
        while (!File.Exists(IntallkConfig.DataPath + "\\Resources\\" + file))
        {
            if ((DateTime.Now - time).TotalSeconds > DownloadImgTimedOut)
            {
                // Timed Out
                Content = "oh.png";
                break;
            }
            Thread.Sleep(100);
        }
    }
}
[Serializable]
public class SingleRecordingMsg
{
    public bool Repeated = false;
    public int Index = 0;
    public object? ForwardMessages;
    public List<MessageSegment> Message = new List<MessageSegment>();
    public DateTime SendTime;
    public long QQ { get; set; }
    public List<long> Repeaters = new List<long>();
    public long Group { get; set; }
    public int RepeatCount = 0;
    public float Heat = 1.0f;
    public void Cool() => 
        Heat -= 0.1f;
    public void Hot()
    {
        if (Heat < 0) Heat = 0.5f;
        Heat *= 2f;
        RepeatCount++;
    }
}
public class MessageCollection
{
    public long GroupID;
    public List<SingleRecordingMsg> Collection = new List<SingleRecordingMsg>();
}
public static class MessageCollectionHelper
{
    public static MessageBody ToMessageBody(this List<MessageSegment> seg)
    {
        MessageBody body = new MessageBody();
        foreach (MessageSegment s in seg)
        {
            if (s.isImage)
            {
                // 漏网之鱼
                if (s.Content!.EndsWith(".image"))
                    s.DownloadImage();
                body.Add(SoraSegment.Image(IntallkConfig.DataPath + "\\Resources\\" + s.Content));
            }
            else
            {
                body.Add(SoraSegment.Text(s.Content));
            }
        }
        return body;
    }
}