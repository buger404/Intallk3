using Intallk.Models;
using Microsoft.Extensions.Logging;
using OneBot.CommandRoute.Services;
using Sora.Entities.Base;
using Sora.EventArgs.SoraEvent;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using static Intallk.Models.DictionaryReplyModel;

namespace Intallk.Modules;

public class LanMsg : SimpleOneBotController
{
    private readonly ConcurrentDictionary<long, SoraApi> apiManager;
    private readonly MD5 MD5Service = MD5.Create();
    private readonly TcpListener listener;
    private readonly string Secret;

    public LanMsg(ICommandService commandService, ILogger<SimpleOneBotController> logger, PermissionService permissionService) : base(commandService, logger, permissionService)
    {
        if (!File.Exists("lanmsg_secret.txt"))
        {
            Logger.LogWarning("内网消息密钥未指定，使用默认密钥。");
            Secret = "sdg34^#YDffgw565utj.s6d";
        }
        else
        {
            Secret = File.ReadAllText("lanmsg_secret.txt");
        }
        int port;
        if (!File.Exists("lanmsg_port.txt"))
        {
            Logger.LogWarning("内网消息端口未指定，使用默认21405。");
            port = 21405;
        }
        else
        {
            port = int.Parse(File.ReadAllText("lanmsg_port.txt"));
        }
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
        listener = new TcpListener(IPAddress.Any, port);
        new Thread(Listening).Start();
    }

    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            HelpCmd = "lan", ModuleName = "远程消息", ModuleUsage = "用于接收内网其他机器人的发送消息请求。"
        };

    public string LinkByteStr(byte[] data)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in data)
            sb.Append(b.ToString());
        return sb.ToString();
    }

    public void Listening()
    {
        listener.Start();
        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                client.ReceiveBufferSize = 10240;
                byte[] msg = new byte[10240];
                client.GetStream().Read(msg, 0, 10240);
                string data = Encoding.UTF8.GetString(msg);
                string[] arg = data.Split('\0');
                if (arg.Length >= 2 && arg[0].Length < 1024)
                {
                    Logger.LogInformation("Received: " + arg[0] + " " + arg[1] + " " + arg[2]);
                    if (LinkByteStr(MD5Service.ComputeHash(Encoding.UTF8.GetBytes(arg[1] + Secret))) == arg[0])
                    {
                        foreach (var api in apiManager.Values)
                        {
                            Logger.LogInformation("Sent.");
                            api.GetGroup(long.Parse(arg[2])).SendGroupMessage(arg[1].Replace("\\n","\n"));
                        }
                    }
                    else
                    {
                        Logger.LogInformation("Hash check failed.");
                    }
                }
                client.Close();
            }
            catch (Exception err)
            {
                Logger.LogError(err.Message + "\n" + err.StackTrace);
            }
        }
    }
}
