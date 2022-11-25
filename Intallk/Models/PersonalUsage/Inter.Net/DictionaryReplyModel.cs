using Sora.Entities.Segment.DataModel;
using Sora.Entities.Segment;
using Sora.Entities;
using static Intallk.Modules.RepeatCollector;
using System.Runtime.CompilerServices;
using static Intallk.Models.DictionaryReplyModel;
using Intallk.Config;
using Intallk.Modules;
using Sora.Enumeration;

namespace Intallk.Models;

public class DictionaryReplyModel
{
    [Serializable]
    public class Message
    {
        public string? Content { get; set; }
        public SegmentType Type;
    }
    [Serializable]
    public class MsgDictionary
    {
        public Dictionary<long, Dictionary<string, (long, List<Message>)>> Data = new();
    }
}
public static class MessageHelper
{
    public static MessageBody ToMessageBody(this List<Message> list)
    {
        MessageBody body = new MessageBody();
        foreach (Message msg in list)
        {
            if (msg.Type == SegmentType.Image)
                body.Add(SoraSegment.Image(msg.Content));
            else if (msg.Type == SegmentType.Text)
                body.Add(SoraSegment.Text(msg.Content));
            else if (msg.Type == SegmentType.Face)
                body.Add(SoraSegment.Face(int.Parse(msg.Content ?? "0")));
            else if (msg.Type == SegmentType.At)
                body.Add(SoraSegment.At(long.Parse(msg.Content ?? "114514")));
        }
        return body;
    }
    public static List<Message> ToMessageList(this MessageBody body)
    {
        List<Message> messages = new();
        foreach (SoraSegment m in body)
        {
            if (m.MessageType == SegmentType.Text)
            {
                messages.Add(new Message { Content = ((TextSegment)m.Data).Content, Type = SegmentType.Text });
            }
            else if (m.MessageType == SegmentType.Image)
            {
                //string file = "context_" + DateTime.Now.ToString("yy.MM.dd.HH.mm.ss.") + m.GetHashCode() + ".jpg";
                //((ImageSegment)m.Data).Url.DownLoad(file);
                messages.Add(new Message { Content = ((ImageSegment)m.Data).ImgFile, Type = SegmentType.Image });
            }
            else if (m.MessageType == SegmentType.At)
            {
                messages.Add(new Message { Content = ((AtSegment)m.Data).Target, Type = SegmentType.At });
            }
            else if (m.MessageType == SegmentType.Face)
            {
                messages.Add(new Message { Content = ((FaceSegment)m.Data).Id.ToString(), Type = SegmentType.Face });
            }
        }
        return messages;
    }
}
