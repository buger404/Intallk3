using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Services;
using static Intallk.Models.DictionaryReplyModel;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Sora.EventArgs.SoraEvent;
using OneBot.CommandRoute.Attributes;
using Sora.Entities;
using System.Collections;
using Sora.Enumeration.EventParamsType;

namespace Intallk.Modules;

public class Permission : ArchiveOneBotController<PermissionModel>
{
    public static Permission? Instance { get; private set; }

    public Permission(ICommandService commandService, ILogger<ArchiveOneBotController<PermissionModel>> logger) : base(commandService, logger)
    {
        Instance = this;
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation { DataFile = "permission", ModuleName = "权限", RootPermission = "PERMISSION",
                                HelpCmd = "permission", ModuleUsage = "机器人权限管理，其中较为特殊的权限：\n" +
                                                                      "ANYTHING - 可等效任何权限\n" +
                                                                      "GRANT - 分发授权权限\n" +
                                                                      "具体功能的授权需要视情况而定，权限优先级：Denied权限>Accepted权限。\n" +
                                                                      "使用以下指令时，若需要同时操作多个权限名，可使用符号','隔开。"
        };
    public override void OnDataNull()
    {
        Data = new();
        Data.User.Add(0, new PermissionData());
        Data.User[0].Accepted.Add(PermissionName.Anything);
    }

    public static bool Judge(GroupMessageEventArgs e, ModuleInformation? info, string permission, PermissionPolicy policy = PermissionPolicy.AcceptedAsDefault, bool noInform = false)
        => Judge(e, (info?.RootPermission + "_" ?? "") + permission, policy, noInform);

    public static bool Judge(GroupMessageEventArgs e, string permission, PermissionPolicy policy = PermissionPolicy.AcceptedAsDefault, bool noInform = false)
    {
        if (Instance!.Data == null) return false;
        bool ret = Judge(e, e.Sender.Id, permission, policy);
        if (!noInform && !ret)
            e.Reply(e.Sender.At() + " ⚠️您尚无权限'" + permission + "'或权限受拒绝，请向模块权限授权人申请。");
        return ret;
    }

    public static bool Judge(GroupMessageEventArgs? e, long qq, string permission, PermissionPolicy policy)
    {
        if (Instance!.Data == null) return false;
        if (!Instance!.Data.User.ContainsKey(qq))
            Instance!.Data.User.Add(qq, new PermissionData());
        if (Instance!.Data.User[qq].Denied.Contains(PermissionName.Anything))
            return false;
        if (Instance!.Data.User[qq].Accepted.Contains(PermissionName.Anything))
            return true;
        if (Instance!.Data.User[qq].Denied.Contains(permission))
            return false;
        if (Instance!.Data.User[qq].Accepted.Contains(permission) || policy == PermissionPolicy.AcceptedAsDefault)
            return true;
        else if (policy == PermissionPolicy.AcceptedIfGroupAccepted && e != null)
            return JudgeGroup(e, permission, PermissionPolicy.RequireAccepted);
        else if (policy == PermissionPolicy.AcceptedAdminAsDefault && e != null)
            return e.SenderInfo.Role == MemberRoleType.Admin || e.SenderInfo.Role == MemberRoleType.Owner;
        else
            return false;
    }

    public static bool JudgeGroup(GroupMessageEventArgs e, ModuleInformation? info, string permission, PermissionPolicy policy = PermissionPolicy.AcceptedAsDefault)
        => JudgeGroup(e.SourceGroup.Id, (info?.RootPermission + "_" ?? "") + permission, policy);

    public static bool JudgeGroup(GroupMessageEventArgs e, string permission, PermissionPolicy policy = PermissionPolicy.AcceptedAsDefault)
        => JudgeGroup(e.SourceGroup.Id, permission, policy);

    public static bool JudgeGroup(long group, string permission, PermissionPolicy policy)
    {
        if (Instance!.Data == null) return false;
        if (!Instance!.Data.Group.ContainsKey(group))
            Instance!.Data.Group.Add(group, new PermissionData());
        if (Instance!.Data.Group[group].Denied.Contains(PermissionName.Anything))
            return false;
        if (Instance!.Data.Group[group].Accepted.Contains(PermissionName.Anything))
            return true;
        if (Instance!.Data.Group[group].Denied.Contains(permission))
            return false;
        return Instance!.Data.Group[group].Accepted.Contains(permission) || policy == PermissionPolicy.AcceptedAsDefault;
    }

    public void PermissionOperation(GroupMessageEventArgs e, User target, string permission, out (string, string) ret, Action<string> operation)
    {
        if (Data == null)
        {
            ret = (null!, null!);
            return;
        }
        string[] p = permission.Split(',', StringSplitOptions.RemoveEmptyEntries);
        string succeed = "", fail = "";
        foreach (string c in p)
        {
            string[] t = c.Split('_');
            if (t.Length < 2)
            {
                if (!Judge(e, PermissionName.Grant, PermissionPolicy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
                }
            }
            else
            {
                if (!Judge(e, t[0] + "_" + PermissionName.Grant, PermissionPolicy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
                }
                if (t[1] == PermissionName.Grant)
                {
                    if (!Judge(e, t[0] + "_" + PermissionName.Anything, PermissionPolicy.RequireAccepted, true))
                    {
                        fail += c + ",";
                        continue;
                    }
                }
            }
            if (!Data.User.ContainsKey(target.Id))
                Data.User.Add(target.Id, new PermissionData());
            operation(c);
            succeed += c + ",";
        }
        Dump();
        ret = (succeed, fail);
    }

    [Command("permission accept <qq> <permission>")]
    [CmdHelp("QQ号 权限名", "授予某人对应的Accepted权限")]
    public void PermissionAccept(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (!Data!.User[qq.Id].Accepted.Contains(pms))
                Data!.User[qq.Id].Accepted.Add(pms);
        });
        e.Reply("已成功授予对象的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission accept revoke <qq> <permission>")]
    [CmdHelp("QQ号 权限名", "撤销某人对应的Accepted权限")]
    public void PermissionAcceptRevoke(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (Data!.User[qq.Id].Accepted.Contains(pms))
                Data!.User[qq.Id].Accepted.Remove(pms);
        });
        e.Reply("已成功移除对象的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission deny <qq> <permission>")]
    [CmdHelp("QQ号 权限名", "授予某人对应的Denied权限")]
    public void PermissionDeny(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (!Data!.User[qq.Id].Denied.Contains(pms))
                Data!.User[qq.Id].Denied.Add(pms);
        });
        e.Reply("已成功授予对象的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission deny revoke <qq> <permission>")]
    [CmdHelp("QQ号 权限名", "撤销授予某人对应的Denied权限")]
    public void PermissionDenyRevoke(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (Data!.User[qq.Id].Denied.Contains(pms))
                Data!.User[qq.Id].Denied.Remove(pms);
        });
        e.Reply("已成功授移除对象的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission view <qq>")]
    [CmdHelp("QQ号", "查看某人持有的权限情况")]
    public void PermissionView(GroupMessageEventArgs e, User qq)
    {
        if (Data == null)
            return;
        if (!Data.User.ContainsKey(qq.Id))
        {
            e.Reply("对象无任何权限。");
            return;
        }
        e.Reply("对象的Accepted权限：\n" + String.Join(',', Data.User[qq.Id].Accepted) + "\n对象的Denied权限：\n" + String.Join(',', Data.User[qq.Id].Denied));
    }

    public void PermissionOperationGroup(GroupMessageEventArgs e, long target, string permission, out (string, string) ret, Action<string> operation)
    {
        if (Data == null)
        {
            ret = (null!, null!);
            return;
        }
        string[] p = permission.Split(',', StringSplitOptions.RemoveEmptyEntries);
        string succeed = "", fail = "";
        foreach (string c in p)
        {
            string[] t = c.Split('_');
            if (t.Length < 2)
            {
                if (!Judge(e, PermissionName.Grant, PermissionPolicy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
                }
            }
            else
            {
                if (!Judge(e, t[0] + "_" + PermissionName.Grant, PermissionPolicy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
                }
                if (t[1] == PermissionName.Grant)
                {
                    if (!Judge(e, t[0] + "_" + PermissionName.Anything, PermissionPolicy.RequireAccepted, true))
                    {
                        fail += c + ",";
                        continue;
                    }
                }
            }
            if (!Data.Group.ContainsKey(target))
                Data.Group.Add(target, new PermissionData());
            operation(c);
            succeed += c + ",";
        }
        Dump();
        if (succeed.Length > 0) succeed = succeed.Remove(succeed.Length - 1);
        if (fail.Length > 0) fail = fail.Remove(fail.Length - 1);
        ret = (succeed, fail);
    }

    [Command("permission group accept <group> <permission>")]
    [CmdHelp("群号 权限名", "授予某群指定的Accepted权限")]
    public void PermissionGroupAccept(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (!Data!.Group[group].Accepted.Contains(pms))
                Data!.Group[group].Accepted.Add(pms);
        });
        e.Reply("已成功授予群的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group accept revoke <group> <permission>")]
    [CmdHelp("群号 权限名", "撤回某群指定的Accepted权限")]
    public void PermissionGroupAcceptRevoke(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (Data!.Group[group].Accepted.Contains(pms))
                Data!.Group[group].Accepted.Remove(pms);
        });
        e.Reply("已成功移除群的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group deny <group> <permission>")]
    [CmdHelp("群号 权限名", "授予某群指定的Denied权限")]
    public void PermissionGroupDeny(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (!Data!.Group[group].Denied.Contains(pms))
                Data!.Group[group].Denied.Add(pms);
        });
        e.Reply("已成功授予群的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group deny revoke <group> <permission>")]
    [CmdHelp("群号 权限名", "撤回某群指定的Denied权限")]
    public void PermissionGroupDenyRevoke(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (Data!.Group[group].Denied.Contains(pms))
                Data!.Group[group].Denied.Remove(pms);
        });
        e.Reply("已成功授移除群的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n⚠️以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group view <group>")]
    [CmdHelp("群号", "查看某群持有的权限情况")]
    public void PermissionGroupView(GroupMessageEventArgs e, long group)
    {
        if (Data == null)
            return;
        if (!Data.Group.ContainsKey(group))
        {
            e.Reply("群无任何权限。");
            return;
        }
        e.Reply("群的Accepted权限：\n" + String.Join(',', Data.Group[group].Accepted) + "\n群的Denied权限：\n" + String.Join(',', Data.Group[group].Denied));
    }
}