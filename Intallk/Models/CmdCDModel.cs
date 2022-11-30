namespace Intallk.Models;

public class CDInfo
{
    public string? CmdHead;
    public double Duration;
    public string? RootPermission;

}
public class CmdCDModel
{
    public Dictionary<long, List<CDInfo>> CD = new Dictionary<long, List<CDInfo>>();
}
