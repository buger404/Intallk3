namespace Intallk.Models;

public class DictionaryReplyModel
{
    [Serializable]
    public class MsgDictionary
    {
        public Dictionary<long, Dictionary<string, (long, string)>> Data = new();
    }
}
