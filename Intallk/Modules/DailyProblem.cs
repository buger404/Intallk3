using OneBot.CommandRoute.Services;
using Sora.Entities.Base;
using Sora.EventArgs.SoraEvent;
using System.Collections.Concurrent;
using System.Text.Json;
using Intallk.Models;

namespace Intallk.Modules;


public class DailyProblem : IHostedService
{
    public static DailyProblem? Instance { get; private set; }
    private readonly IHttpClientFactory factory;
    private readonly ILogger<DailyProblem> logger;
    private readonly System.Timers.Timer timer;
    private readonly ConcurrentDictionary<long, SoraApi> apiManager;
    public PermissionService PermissionService;
    public DailyProblem(IHttpClientFactory factory, ILogger<DailyProblem> logger, ICommandService commandService, PermissionService permissionService)
    {
        this.factory = factory;
        this.logger = logger;
        timer = new(TimeSpan.FromDays(1).TotalMilliseconds);
        apiManager = new();
        commandService.Event.OnClientConnect += (context) =>
            {
                var args = context.WrapSoraEventArgs<ConnectEventArgs>();
                apiManager.TryAdd(args.LoginUid, args.SoraApi);
                return 0;
            };
        commandService.Event.OnClientStatusChangeEvent += (context) =>
            {
                var args = context.WrapSoraEventArgs<ClientStatusChangeEventArgs>();
                if (args.Online)
                {
                    apiManager.TryAdd(args.LoginUid, args.SoraApi);
                }
                else
                {
                    apiManager.TryRemove(args.LoginUid, out _);
                }
                return 0;
            };
        this.PermissionService = permissionService;
        Instance = this;
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
    public async Task<string?> FetchDailyMessage()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent("{\"query\":\"query questionOfToday{todayRecord{question{questionTitleSlug}}}\",\"variables\":{}}", System.Text.Encoding.UTF8, "application/json")
        };
        var response = await factory.CreateClient("leetcode").SendAsync(request);
        string? title = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement.GetProperty("data").GetProperty("todayRecord")[0].GetProperty("question").GetProperty("questionTitleSlug").GetString();
        if (title is null)
        {
            logger.LogWarning("daily problem service gets null title");
            return null;
        }
        request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent($"{{\"query\":\"query{{question(titleSlug: \"{title}\"){{questionId translatedTitle translatedContent difficulty}}}}\",\"variables\":{{}}}}")
        };
        response = await factory.CreateClient("leetcode").SendAsync(request);
        var question = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement.GetProperty("data").GetProperty("question");
        string? content = question.GetProperty("translatedContent").GetString();
        if (content is null)
        {
            logger.LogWarning("daily problem service gets null content");
            return null;
        }
        foreach (var pair in Mapper)
        {
            content = content.Replace(pair.Key, pair.Value);
        }
        string message = $"{question.GetProperty("questionId").GetString()}. {question.GetProperty("translatedTitle").GetString()}\r\n难度: {question.GetProperty("difficulty").GetString()}\r\n{content}";
        return message;
    }
    async Task FetchDaily()
    {
        string? message = await FetchDailyMessage();
        if (message == null) return;
        foreach (var api in apiManager.Values)
        {
            foreach (var group in (await api.GetGroupList()).groupList)
            {
                if (PermissionService.JudgeGroup(group.GroupId, "LEETCODETODAY_PUSH", Models.PermissionPolicy.RequireAccepted))
                {
                    await api.SendGroupMessage(group.GroupId, message);
                    await Task.Delay(1000);
                }
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