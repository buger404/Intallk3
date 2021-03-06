using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;

using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;

using System.Text;
using System.Text.Json.Serialization;

namespace Intallk.Modules;

public class SXRequest
{
    [JsonPropertyName("text")] public string? Text { get; set; }
}

public class SXRespond
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("trans")] public string[]? Trans { get; set; }
    [JsonPropertyName("inputting")] public string[]? Inputting { get; set; }
}

// 能不能好好说话
// 中文缩写查询
// 感谢Repo（API来源）：'itorr/nbnhhsh'
class Nbnhhsh : IOneBotController
{
    [Command("sx <content>")]
    public async Task SXSearchAsync(string content, GroupMessageEventArgs e)
    {
        var client = new RestClient("https://lab.magiconch.com/api/nbnhhsh");
        var request = new RestRequest("/guess", Method.Post).AddJsonBody(new SXRequest { Text = content });
        var response = await client.ExecuteAsync<List<SXRespond>>(request);
        if (!response.IsSuccessful)
        {
            await e.Reply(e.Sender.At() + $"黑嘴试图气死你({response.ErrorMessage})。");
            return;
        }
        if (response.Data == null)
        {
            await e.Reply(e.Sender.At() + "咦，这是什么的中文缩写呀，查不到呢。");
        }
        else
        {
            StringBuilder trans = new();
            bool hasResult = false;
            for (int i = 0; i < response.Data.Count; i++)
            {
                if (response.Data.Count > 1) trans.Append("结果{i + 1}：");
                if (response.Data[i].Trans != null)
                {
                    foreach (string s in response.Data[i]!.Trans!) trans.Append($"\"{s}\"，");
                    hasResult = true;
                }
                if (i < response.Data.Count - 1) trans.AppendLine();
            }
            if (hasResult)
            {
                await e.Reply(e.Sender.At() + trans.ToString());
            }
            else
            {
                await e.Reply(e.Sender.At() + "咦，这是什么的中文缩写呀，查不到呢。");
            }

        }
    }
}
