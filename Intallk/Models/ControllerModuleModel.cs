namespace Intallk.Models;

public class ModuleInformation
{
    public string? DataFile;
    public string? RootPermission;
    public string? ModuleName;
    public GrantPolicy GrantPolicy = GrantPolicy.RequireGrantPermission;
}