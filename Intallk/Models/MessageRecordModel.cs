using System.Text;

namespace Intallk.Models;

[Serializable]
struct MessageRecord
{
    public StringBuilder StrBuilder = new StringBuilder();
    public long GroupID;
    public MessageRecord(long id)
    {
        GroupID = id;
    }
}
[Serializable]
struct MessageRecordFile
{
    public List<MessageRecord> Msg;
}