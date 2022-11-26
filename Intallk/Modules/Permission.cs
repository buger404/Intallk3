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

namespace Intallk.Modules;

public class Permission : ArchiveOneBotController<PermissionModel>
{
    public enum Policy
    {
        RequireAccepted, AcceptedAsDefault, AcceptedIfGroupAccepted
    }
    public const string AnythingPermission = "ANYTHING";
    public const string GrantPermission = "GRANT";
    public Permission(ICommandService commandService, ILogger<ArchiveOneBotController<PermissionModel>> logger) : base(commandService, logger)
    {
    }
    public override ModuleInformation Initialize() =>
        new ModuleInformation { DataFile = "permission", ModuleName = "权限", RootPermission = "permission" };
    public override void OnDataNull()
    {
        Data = new();
        Data.User.Add(0, new PermissionData());
        Data.User[0].Accepted.Add(AnythingPermission);
    }

    public static bool Judge(GroupMessageEventArgs e, ModuleInformation? info, string permission, Policy policy = Policy.AcceptedAsDefault, bool noInform = false)
        => Judge(e, (info?.RootPermission + "_" ?? "") + permission, policy, noInform);

    public static bool Judge(GroupMessageEventArgs e, string permission, Policy policy = Policy.AcceptedAsDefault, bool noInform = false)
    {
        if (Data == null) return false;
        bool ret = Judge(e, e.Sender.Id, permission, policy);
        if (!noInform && !ret)
            e.Reply(e.Sender.At() + " 您尚无权限'" + permission + "'或权限受拒绝，请向模块权限授权人申请。");
        return ret;
    }

    private static bool Judge(GroupMessageEventArgs e, long qq, string permission, Policy policy)
    {
        if (Data == null) return false;
        if (Data.User.ContainsKey(qq))
        {
            if (Data.User[qq].Denied.Contains(AnythingPermission))
                return false;
            if (Data.User[qq].Accepted.Contains(AnythingPermission))
                return true;
            if (Data.User[qq].Denied.Contains(permission))
                return false;
            if (Data.User[qq].Accepted.Contains(permission) || policy == Policy.AcceptedAsDefault)
                return true;
            else if (policy == Policy.AcceptedIfGroupAccepted)
                return JudgeGroup(e, permission, Policy.RequireAccepted);
            else
                return false;
        }
        else
        {
            return false;
        }
    }

    public static bool JudgeGroup(GroupMessageEventArgs e, ModuleInformation? info, string permission, Policy policy = Policy.AcceptedAsDefault)
        => JudgeGroup(e.SourceGroup.Id, (info?.RootPermission + "_" ?? "") + permission, policy);

    public static bool JudgeGroup(GroupMessageEventArgs e, string permission, Policy policy = Policy.AcceptedAsDefault)
        => JudgeGroup(e.SourceGroup.Id, permission, policy);

    private static bool JudgeGroup(long group, string permission, Policy policy)
    {
        if (Data == null) return false;
        if (Data.Group.ContainsKey(group))
        {
            if (Data.Group[group].Denied.Contains(AnythingPermission))
                return false;
            if (Data.Group[group].Accepted.Contains(AnythingPermission))
                return true;
            if (Data.Group[group].Denied.Contains(permission))
                return false;
            return Data.Group[group].Accepted.Contains(permission) || policy == Policy.AcceptedAsDefault;
        }
        else
        {
            return false;
        }
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
                if (!Judge(e, GrantPermission, Policy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
                }
            }
            else
            {
                if (!Judge(e, t[0] + "_" + GrantPermission, Policy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
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
    public void PermissionAccept(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (!Data!.User[qq.Id].Accepted.Contains(pms))
                Data!.User[qq.Id].Accepted.Add(pms);
        });
        e.Reply("已成功授予对象的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission accept revoke <qq> <permission>")]
    public void PermissionAcceptRevoke(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (Data!.User[qq.Id].Accepted.Contains(pms))
                Data!.User[qq.Id].Accepted.Remove(pms);
        });
        e.Reply("已成功移除对象的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission deny <qq> <permission>")]
    public void PermissionDeny(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (!Data!.User[qq.Id].Denied.Contains(pms))
                Data!.User[qq.Id].Denied.Add(pms);
        });
        e.Reply("已成功授予对象的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission deny revoke <qq> <permission>")]
    public void PermissionDenyRevoke(GroupMessageEventArgs e, User qq, string permission)
    {
        (string, string) ret;
        PermissionOperation(e, qq, permission, out ret, (pms) =>
        {
            if (Data!.User[qq.Id].Denied.Contains(pms))
                Data!.User[qq.Id].Denied.Remove(pms);
        });
        e.Reply("已成功授移除对象的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission view <qq>")]
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
                if (!Judge(e, GrantPermission, Policy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
                }
            }
            else
            {
                if (!Judge(e, t[0] + "_" + GrantPermission, Policy.RequireAccepted, true))
                {
                    fail += c + ",";
                    continue;
                }
            }
            if (!Data.Group.ContainsKey(target))
                Data.Group.Add(target, new PermissionData());
            operation(c);
            succeed += c + ",";
        }
        Dump();
        ret = (succeed, fail);
    }

    [Command("permission group accept <group> <permission>")]
    public void PermissionGroupAccept(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (!Data!.Group[group].Accepted.Contains(pms))
                Data!.Group[group].Accepted.Add(pms);
        });
        e.Reply("已成功授予群的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group accept revoke <group> <permission>")]
    public void PermissionGroupAcceptRevoke(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (Data!.Group[group].Accepted.Contains(pms))
                Data!.Group[group].Accepted.Remove(pms);
        });
        e.Reply("已成功移除群的Accepted权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group deny <group> <permission>")]
    public void PermissionGroupDeny(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (!Data!.Group[group].Denied.Contains(pms))
                Data!.Group[group].Denied.Add(pms);
        });
        e.Reply("已成功授予群的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group deny revoke <group> <permission>")]
    public void PermissionGroupDenyRevoke(GroupMessageEventArgs e, long group, string permission)
    {
        (string, string) ret;
        PermissionOperationGroup(e, group, permission, out ret, (pms) =>
        {
            if (Data!.Group[group].Denied.Contains(pms))
                Data!.Group[group].Denied.Remove(pms);
        });
        e.Reply("已成功授移除群的Denied权限：\n" + ret.Item1 + (ret.Item2 != "" ? "\n以下权限由于未持有相应模块的授权权限，操作失败：\n" + ret.Item2 : ""));
    }

    [Command("permission group view <group>")]
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