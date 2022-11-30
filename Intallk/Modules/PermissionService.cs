using Intallk.Models;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;

namespace Intallk.Modules;

public class PermissionService
{
    public ArchiveOneBotController<PermissionModel>? ControllerInstance {
        get {
            return Permission.Instance;
        }
    }

    public bool Judge(GroupMessageEventArgs e, ModuleInformation? info, string permission, bool noInform = false)
    {
        PermissionPolicy policy;
        if (!info!.RegisteredPermission!.ContainsKey(permission))
            throw new ArgumentException("指定的权限未注册！");
        policy = info!.RegisteredPermission![permission].Item2;
        return Judge(e, (info?.RootPermission + "_" ?? "") + permission, policy, noInform);
    }

    public bool Judge(GroupMessageEventArgs e, string permission, PermissionPolicy policy = PermissionPolicy.AcceptedAsDefault, bool noInform = false)
    {
        if (ControllerInstance!.Data == null) return false;
        bool ret = Judge(e, e.Sender.Id, permission, policy);
        if (!noInform && !ret)
            e.Reply(e.Sender.At() + " ⚠️您尚无权限'" + permission + "'或权限受拒绝，请向模块权限授权人申请。");
        return ret;
    }

    public bool Judge(GroupMessageEventArgs? e, long qq, ModuleInformation? info, string permission, PermissionPolicy policy)
        => Judge(e, qq, (info?.RootPermission + "_" ?? "") + permission, policy);

    public bool Judge(GroupMessageEventArgs? e, long qq, string permission, PermissionPolicy policy)
    {
        if (ControllerInstance!.Data == null) return false;
        if (!ControllerInstance!.Data.User.ContainsKey(qq))
            ControllerInstance!.Data.User.Add(qq, new PermissionData());
        if (ControllerInstance!.Data.User[qq].Denied.Contains(PermissionName.Anything))
            return false;
        if (ControllerInstance!.Data.User[qq].Accepted.Contains(PermissionName.Anything))
            return true;
        if (ControllerInstance!.Data.User[qq].Denied.Contains(permission))
            return false;
        if (ControllerInstance!.Data.User[qq].Accepted.Contains(permission) || policy == PermissionPolicy.AcceptedAsDefault)
            return true;
        else if (policy == PermissionPolicy.AcceptedIfGroupAccepted && e != null)
            return JudgeGroup(e, permission, PermissionPolicy.RequireAccepted);
        else if (policy == PermissionPolicy.AcceptedAdminAsDefault && e != null)
            return e.SenderInfo.Role == MemberRoleType.Admin || e.SenderInfo.Role == MemberRoleType.Owner;
        else
            return false;
    }

    public bool JudgeGroup(GroupMessageEventArgs e, ModuleInformation? info, string permission)
        => JudgeGroup(e.SourceGroup.Id, info, permission);

    public bool JudgeGroup(GroupMessageEventArgs e, string permission, PermissionPolicy policy = PermissionPolicy.AcceptedAsDefault)
        => JudgeGroup(e.SourceGroup.Id, permission, policy);

    public bool JudgeGroup(long group, ModuleInformation? info, string permission)
    {
        PermissionPolicy policy;
        if (!info!.RegisteredPermission!.ContainsKey(permission))
            throw new ArgumentException("指定的权限未注册！");
        policy = info!.RegisteredPermission![permission].Item2;
        return JudgeGroup(group, (info?.RootPermission + "_" ?? "") + permission, policy);
    }

    public bool JudgeGroup(long group, string permission, PermissionPolicy policy)
    {
        if (ControllerInstance!.Data == null) return false;
        if (!ControllerInstance!.Data.Group.ContainsKey(group))
            ControllerInstance!.Data.Group.Add(group, new PermissionData());
        if (ControllerInstance!.Data.Group[group].Denied.Contains(PermissionName.Anything))
            return false;
        if (ControllerInstance!.Data.Group[group].Accepted.Contains(PermissionName.Anything))
            return true;
        if (ControllerInstance!.Data.Group[group].Denied.Contains(permission))
            return false;
        return ControllerInstance!.Data.Group[group].Accepted.Contains(permission) || policy == PermissionPolicy.AcceptedAsDefault;
    }
}
