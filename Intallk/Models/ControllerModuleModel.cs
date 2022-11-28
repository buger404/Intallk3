using OneBot.CommandRoute.Models.Enumeration;

namespace Intallk.Models;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ModuleInformation : Attribute
{
    public string? DataFile;
    public string? RootPermission;
    public string? ModuleName;
    public string? ModuleUsage;
    public string? HelpCmd;
    public GrantPolicy GrantPolicy = GrantPolicy.RequireGrantPermission;
}
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CmdHelpAttribute : Attribute
{
    public string ArgDescription { get; }
    public string UsageDescription { get; }

    public CmdHelpAttribute(string argDescription, string usageDescription)
    {
        ArgDescription = argDescription;
        UsageDescription = usageDescription;
    }

    public CmdHelpAttribute(string usageDescription)
    {
        ArgDescription = "";
        UsageDescription = usageDescription;
    }
}