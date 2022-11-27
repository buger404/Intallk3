using System.Text;

namespace Intallk.Models;

[Serializable]
class MessageRecord
{
    public StringBuilder StrBuilder = new StringBuilder();
    public long GroupID;
    public MessageRecord(long id)
    {
        GroupID = id;
    }
}
[Serializable]
class MessageRecordFile
{
    public List<MessageRecord> Msg = new List<MessageRecord>();
}