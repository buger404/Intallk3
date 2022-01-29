namespace Intallk.Models;

[Serializable]
public class PaintFile
{
    public long Author { get; set; }
    public string? Name { get; set; }
    public string? ParameterDescription { get; set; }
    public List<PaintCommands>? Commands { get; set; }
}
[Serializable]
public class PaintCommands
{
    public PaintCommandType CommandType { get; set; }
    public object[] Args { get; set; }
    public PaintCommands(PaintCommandType type, params object[] args)
    {
        CommandType = type;
        Args = args;
    }
}
[Serializable]
public enum PaintCommandType
{
    SetCanvas, FillImage, FillRectangle, FillEllipse, DrawImage, DrawRectangle, DrawEllipse, Write
}
[Serializable]
public enum PaintAdjustWriteMode
{
    None, XFirst, YFirst, Auto
}
[Serializable]
public enum PaintAlign
{
    Left, Center, Right
}
