using OneBot.CommandRoute.Services;
using Sora.Entities.Base;
using Sora.EventArgs.SoraEvent;
using System.Collections.Concurrent;

namespace Intallk.Modules;

public class DailyProblemPushService : IHostedService
{
    private readonly ILogger<DailyProblemPushService> logger;
    private readonly System.Timers.Timer timer;
    private readonly DailyProblemService dailyProblemService;
    private readonly ConcurrentDictionary<long, SoraApi> apiManager;
    private readonly PermissionService PermissionService;

    public DailyProblemPushService(ILogger<DailyProblemPushService> logger, ICommandService commandService, DailyProblemService dailyProblemService, PermissionService permissionService)
    {
        this.logger = logger;
        this.dailyProblemService = dailyProblemService;
        this.PermissionService = permissionService;
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
    }
    async Task FetchDaily()
    {
        string? message = await dailyProblemService.FetchDailyMessage();
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
