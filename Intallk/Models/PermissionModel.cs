﻿namespace Intallk.Models;

public class PermissionData
{
    public List<string> Accepted = new List<string>();
    public List<string> Denied = new List<string>();
}
public class PermissionModel
{
    public Dictionary<long, PermissionData> User = new ();
    public Dictionary<long, PermissionData> Group = new ();
}
public class PermissionName
{
    public const string Anything = "ANYTHING",
                        Grant = "GRANT";
}
public enum PermissionPolicy
{
    RequireAccepted, AcceptedAsDefault, AcceptedIfGroupAccepted, AcceptedAdminAsDefault
}
public enum GrantPolicy
{
    RequireGrantPermission, AdminOrGrantPermission
}