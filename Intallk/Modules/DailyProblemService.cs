using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using OneBot.CommandRoute.Services;
using Sora.Entities.Base;
using Sora.EventArgs.SoraEvent;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace Intallk.Modules;


public class DailyProblemService
{
    private readonly HttpClient client;
    private readonly ILogger<DailyProblemService> logger;

    public DailyProblemService(IHttpClientFactory factory, ILogger<DailyProblemService> logger, ICommandService commandService)
    {
        client = factory.CreateClient("leetcode");
        this.logger = logger;
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
        ["</ol>"] = "",
        ["&lt;"] = "<",
        ["&gt;"] = ">"
    };
    public async Task<string?> FetchDailyMessage()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent("{\"query\":\"query questionOfToday{todayRecord{question{questionTitleSlug}}}\",\"variables\":{}}", System.Text.Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(request);
        string? title = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement.GetProperty("data").GetProperty("todayRecord")[0].GetProperty("question").GetProperty("questionTitleSlug").GetString();
        if (title is null)
        {
            logger.LogWarning("daily problem service gets null title");
            return null;
        }
        request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent($"{{\"query\":\"query{{question(titleSlug:\\\"{title}\\\"){{questionId translatedTitle translatedContent difficulty}}}}\",\"variables\":{{}}}}", System.Text.Encoding.UTF8, "application/json")
        };
        string cookies = string.Join(';', response.Headers.GetValues(HeaderNames.SetCookie).Select(s => s.Substring(0, s.IndexOf(';'))));
        request.Headers.Add(HeaderNames.Cookie, cookies);
        response = await client.SendAsync(request);
        logger.LogInformation(await request.Content.ReadAsStringAsync());
        logger.LogInformation(await response.Content.ReadAsStringAsync());
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
        string message = $"✨力扣每日一题推送~\n{question.GetProperty("questionId").GetString()}. {question.GetProperty("translatedTitle").GetString()}\n难度: {question.GetProperty("difficulty").GetString()}\n{content}";
        while(message.Contains("\n\n"))
            message = message.Remove(message.IndexOf("\n\n"), 1);
        return message;
    }
}