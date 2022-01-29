using OneBot.CommandRoute.Services;

using System;
using System.Collections.Generic;

namespace Intalk.Models;

public class PaintDatabase
{
    public List<PaintFile>? Files { get; set; }
}

public class PaintFile
{
    public long Author { get; set; }
    public string? Name { get; set; }
    public List<PaintCommands>? Commands { get; set; }
}

public class PaintCommands
{
    public PaintCommandType CommandType { get; set; }
    public List<object>? Args { get; set; }
}

public enum PaintCommandType
{
    SetCanvas, FillImage, FillRectangle, FillEllipse, DrawImage, DrawEllipse, Write
}
