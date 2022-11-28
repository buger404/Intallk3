using OneBot.CommandRoute.Services;
using Sora.Entities.Base;
using Sora.EventArgs.SoraEvent;
using System.Text.Json;

namespace Intallk.Modules;

public class DailyProblem : IHostedService
{
    private readonly HttpClient client;
    private readonly ILogger<DailyProblem> logger;
    private readonly System.Timers.Timer timer;
    private readonly Dictionary<long, SoraApi> apiManager;
    public DailyProblem(IHttpClientFactory factory, ILogger<DailyProblem> logger, ICommandService commandService)
    {
        client = factory.CreateClient("leetcode");
        this.logger = logger;
        timer = new(TimeSpan.FromDays(1).TotalMilliseconds);
        apiManager = new();
        commandService.Event.OnClientConnect += (context) =>
            {
                var args = context.WrapSoraEventArgs<ConnectEventArgs>();
                apiManager.Add(args.LoginUid, args.SoraApi);
                return 0;
            };
        commandService.Event.OnClientStatusChangeEvent += (context) =>
            {
                var args = context.WrapSoraEventArgs<ClientStatusChangeEventArgs>();
                if (args.Online)
                {
                    apiManager.Add(args.LoginUid, args.SoraApi);
                }
                else
                {
                    apiManager.Remove(args.LoginUid);
                }
                return 0;
            };
    }
    readonly static Dictionary<string, string> Mapper = new()
    {
        ["<code>"] = "",
        ["</code>"] = "",
        ["<p>"] = "",
        ["</p>"] = "",
        ["<em>"] = "",
        ["</em>"] = "",
        ["<pre>"] = "",
        ["</pre>"] = "",
        ["<strong>"] = "",
        ["</strong>"] = "",
        ["<li>"] = "",
        ["</li>"] = "",
        ["<ul>"] = "",
        ["</ul>"] = "",
        ["<sup>"] = "^",
        ["</sup>"] = "",
        ["<ol>"] = "",
        ["</ol>"] = ""
    };
    async Task FetchDaily()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent("{\"query\":\"query questionOfToday{todayRecord{question{questionTitleSlug}}}\",\"variables\":{}}")
        };
        var response = await client.SendAsync(request);
        string? title = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement.GetProperty("data").GetProperty("todayRecord")[0].GetProperty("question").GetProperty("questionTitleSlug").GetString();
        if (title is null)
        {
            logger.LogWarning("daily problem service gets null title");
            return;
        }
        request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent($"{{\"query\":\"query{{question(titleSlug: \"{title}\"){{questionId translatedTitle translatedContent difficulty}}}}\",\"variables\":{{}}}}")
        };
        response = await client.SendAsync(request);
        var question = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement.GetProperty("data").GetProperty("question");
        string? content = question.GetProperty("translatedContent").GetString();
        if (content is null)
        {
            logger.LogWarning("daily problem service gets null content");
            return;
        }
        foreach (var pair in Mapper)
        {
            content = content.Replace(pair.Key, pair.Value);
        }
        string message = $"{question.GetProperty("questionId").GetString()}. {question.GetProperty("translatedTitle").GetString()}\r\n难度: {question.GetProperty("difficulty").GetString()}\r\n{content}";
        foreach (var api in apiManager.Values)
        {
            foreach (var group in (await api.GetGroupList()).groupList)
            {
                await api.SendGroupMessage(group.GroupId, message);
            }
        }
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("leetcode daily problem service started");
        timer.Elapsed += (_, _) => _ = FetchDaily();
        timer.Enabled = true;
        _ = FetchDaily();
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("leetcode daily problem service stopped");
        return Task.CompletedTask;
    }
}