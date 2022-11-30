﻿using Intallk.Models;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;

using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Serialization;

namespace Intallk.Modules;

// 能不能好好说话
// 中文缩写查询
// 感谢Repo（API来源）：'itorr/nbnhhsh'
class Nbnhhsh : SimpleOneBotController
{
    public Nbnhhsh(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService pmsService) : base(commandService, logger, pmsService)
    {
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        { 
            ModuleName = "能不能好好说话", RootPermission = "NBNHHSH",
            HelpCmd = "hhsh", ModuleUsage = "感谢Repo（API来源）：'itorr/nbnhhsh'，可以帮助你搜索一些网络上的简写。\n" +
                                            "例如，'yyds'，可以查询得到'永远的神'。",
            RegisteredPermission = new()
            {
                ["USE"] = ("查询功能使用权限", PermissionPolicy.AcceptedIfGroupAccepted)
            }
        };

    [Command("hhsh <content>")]
    [CmdHelp("内容", "查找指定简写的可能含义")]
    public async Task NbnhhshSearchAsync(string content, GroupMessageEventArgs e)
    {
        if (!PermissionService.Judge(e, Info, "USE"))
            return;
        var client = new RestClient("https://lab.magiconch.com/api/nbnhhsh");
        var request = new RestRequest("/guess", Method.Post).AddJsonBody(new NbnhhshRequest { Text = content }).AddHeader("content-type", "application/json");
        var response = await client.ExecuteAsync<List<NbnhhshRespond>>(request);
        if (!response.IsSuccessful)
        {
            await e.Reply(e.Sender.At() + $"查询失败({response.ErrorMessage})。");
            return;
        }
        if (response.Data == null)
        {
            await e.Reply(e.Sender.At() + "查找不到结果。");
        }
        else
        {
            StringBuilder trans = new();
            bool hasResult = false;
            for (int i = 0; i < response.Data.Count; i++)
            {
                if (response.Data.Count > 1) trans.Append($"结果{i + 1}：");
                if (response.Data[i].Trans != null)
                {
                    foreach (string s in response.Data[i]!.Trans!) trans.Append($"\"{s}\"，");
                    hasResult = true;
                }
                if (i < response.Data.Count - 1) trans.AppendLine();
            }
            if (hasResult)
            {
                await e.Reply(e.Sender.At() + trans.ToString().Remove(trans.ToString().Length - 1, 1));
            }
            else
            {
                await e.Reply(e.Sender.At() + "查找不到结果。");
            }

        }
    }
}
