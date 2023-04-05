using Intallk.Models;
using Newtonsoft.Json.Linq;
using OneBot.CommandRoute.Services;
using RestSharp;
using Sora.Entities.Base;
using Sora.EventArgs.SoraEvent;
using System.Collections.Concurrent;

namespace Intallk.Modules;

public class MilthmSubscription : ArchiveOneBotController<TaptapSubscriptionModel>
{
    private readonly ConcurrentDictionary<long, SoraApi> apiManager;
    private readonly Timer pushTimer;

    public const string ApiUrl =
        "/webapiv2/app/v2/detail-by-id/{0}?X-UA=V%3D1%26PN%3DWebApp%26LANG%3Dzh_CN%26VN_CODE%3D100%26VN%3D0.1.0%26LOC%3DCN%26PLT%3DPC%26DS%3DAndroid%26UID%3D403e642a-90ca-42a4-9bef-faa806d58dc1%26VID%3D3126483%26DT%3DPC%26OS%3DWindows%26OSV%3D10";
    public const long AppId = 301888;
    
    public MilthmSubscription(ICommandService commandService, ILogger<ArchiveOneBotController<TaptapSubscriptionModel>> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
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
        pushTimer = new Timer(Push, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private async void Push(object? state)
    {
        var response = await new RestClient("https://www.taptap.cn").ExecuteAsync(
            new RestRequest(string.Format(ApiUrl, AppId.ToString()))
                .AddHeader("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.96 Safari/537.36"));
        if (response is { IsSuccessful: true, Content: { } })
        {
            var j = JObject.Parse(response.Content);
            long subscription = long.Parse(j["data"]!["stat"]!["reserve_count"]!.ToString());
            if (subscription / 1000 != Data.Subscriptions / 1000)
            {
                Data.Subscriptions = subscription;
                Save();
                string message = "[Taptap数据速报]\n🎉当前游戏'" + j["data"]!["title"]! + "'预约数已达：" + subscription + "！\n" +
                                 "当前游戏评分：" + j["data"]!["stat"]!["rating"]!["score"]!;
                foreach (var api in apiManager.Values)
                {
                    foreach (var group in (await api.GetGroupList()).groupList)
                    {
                        if (PermissionService.JudgeGroup(group.GroupId, "TAPTAP_PUSH", Models.PermissionPolicy.RequireAccepted))
                        {
                            await api.SendGroupMessage(group.GroupId, message);
                            await Task.Delay(1000);
                        }
                    }
                }
            }
        }
    }
    
    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            DataFile = "taptap", ModuleName = "Taptap游戏相关", RootPermission = "TAPTAP",
            HelpCmd = "taptap", ModuleUsage = "Taptap游戏关注数实时推送",
            RegisteredPermission = new()
            {
                ["PUSH"] = ("推送权限", PermissionPolicy.RequireAccepted)
            }
        };
    
    public override void OnDataNull() =>
        Data = new TaptapSubscriptionModel();
}