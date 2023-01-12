using Intallk.Config;
using Intallk.Models;
using Microsoft.CSharp;
using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;
using RestSharp;
using Sora.Entities.Base;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using System.Buffers.Text;
using System.CodeDom.Compiler;
using System.Data;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static Intallk.Modules.MathExpression;

namespace Intallk.Modules;

public class MathExpression : ArchiveOneBotController<VarData>
{
    [Serializable]
    public class VarData
    {
        public List<(string, double)> vars = new List<(string, double)>();
        public List<(string, string)> defines = new List<(string, string)>();
    }
    readonly DataTable table = new DataTable();
    const string WolframAlphaUrl = "http://api.wolframalpha.com/v1/simple?appid=H9EKY4-Q4J5LV5Q49&i={0}";
    public MathExpression(ICommandService commandService, ILogger<ArchiveOneBotController<VarData>> logger, PermissionService permissionService) : base(commandService, logger, permissionService)
    {
        commandService.Event.OnGroupMessage += Event_OnGroupMessageAsync;
    }

    public override void OnDataNull() =>
        Data = new VarData();

    public override ModuleInformation Initialize() =>
        new ModuleInformation
        {
            HelpCmd = "math", ModuleName = "数学相关辅助", ModuleUsage = "例如消息内容是四则表达式时，自动计算。",
            RootPermission = "MATH", DataFile = "math",
            RegisteredPermission = new()
            {
                ["USE"] = ("使用权限", PermissionPolicy.AcceptedIfGroupAccepted)
            }
        };

    private int Event_OnGroupMessageAsync(OneBot.CommandRoute.Models.OneBotContext scope)
    {
        GroupMessageEventArgs? e = scope.SoraEventArgs as GroupMessageEventArgs;
        if (e == null)
            return 0;
        if (!PermissionService.Judge(e, Info, "USE", true))
            return 0;
        try
        {
            if (Data == null)
                Data = new VarData();
            string expression = e.Message.RawText;
            if (expression.StartsWith("let "))
            {
                string key = expression.Split(' ')[1].Split('=')[0];
                double value = double.Parse(expression.Split('=')[1]);
                int i = Data.vars.FindIndex(x => x.Item1 == key);
                if (i != -1)
                    Data.vars[i] = (key, value);
                else
                    Data.vars.Add((key, value));
                Save();
                e.Reply("成功: " + key + "=" + value);
                return 0;
            }
            if (expression.StartsWith("def "))
            {
                string key = expression.Split(' ')[1].Split('=')[0];
                string value = expression.Split('=')[1];
                int i = Data.defines.FindIndex(x => x.Item1 == key);
                if (i != -1)
                    Data.defines[i] = (key, value);
                else
                    Data.defines.Add((key, value));
                Save();
                e.Reply("成功: " + key + "=" + value);
                return 0;
            }
            if (expression.StartsWith("del "))
            {
                string key = expression.Split(' ')[1];
                Data.vars.RemoveAll(x => x.Item1 == key);
                Save();
                e.Reply("移除: " + key);
                return 0;
            }
            if (expression.StartsWith("undef "))
            {
                string key = expression.Split(' ')[1];
                Data.defines.RemoveAll(x => x.Item1 == key);
                Save();
                e.Reply("移除: " + key);
                return 0;
            }
            foreach ((string, double) pair in Data.vars)
            {
                expression = expression.Replace(pair.Item1, pair.Item2.ToString());
            }
            foreach ((string, string) pair in Data.defines)
            {
                expression = expression.Replace(pair.Item1, pair.Item2);
            }
            string? result = table.Compute(expression, null).ToString();
            if (result != null && result != e.Message.RawText)
                e.Reply(result);
        }
        catch
        {

        }
        return 0;
    }
    [Command(".wa <ques>")]
    [CmdHelp("求解内容", "调用WolframAlpha求解")]
    public async void WolframAlpha(GroupMessageEventArgs e, string ques)
    {
        string file = IntallkConfig.DataPath + "\\Images\\wa_" + DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".png";
        byte[]? data = await new RestClient().DownloadDataAsync(new RestRequest(WolframAlphaUrl.Replace("{0}", ques.Replace("+", "%2B").Replace(" ","+")), Method.Get));
        if (data == null)
            return;
        await e.Reply(SoraSegment.Image("base64://" + Convert.ToBase64String(data), false));
    }
}
