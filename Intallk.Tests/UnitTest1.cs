using Intallk.Models;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace Intallk.Tests;

public class UnitTest1 : BasicTest
{
    internal UnitTest1(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public void Glory()
    {
        PaintFile paintfile;
        paintfile = new PaintingCompiler().CompilePaintScript(
            "以1392x2475的尺寸创建画布。在698,618处绘制图片：'QQ头像'，大小为715x715，居中。在0,0处绘制图片：'遮盖.png'。在479,1135处书写：'{QQ名称}'，大小为427x113，居中，自动调整大小，颜色为204,33,19。在195,1853处书写：'{注释}'，大小为991x269，居中，自动调整大小，颜色为239,231,220。在409,1351处书写：'{获得荣誉}'，大小为585x349，居中，自动调整大小，颜色为239,231,220，字体为zihun110hao-wulinjianghuti。"
            , out _);
        Assert.NotNull(paintfile);
    }
    [Fact]
    public void CreateGraphicsTest1()
    {
        PaintFile paintfile;
        paintfile = new PaintingCompiler().CompilePaintScript("以back.png为背景创建画布。", out _);
        Assert.NotNull(paintfile);
    }
    [Fact]
    public void CreateGraphicsTest2()
    {
        PaintFile paintfile;
        paintfile = new PaintingCompiler().CompilePaintScript("以100x300的尺寸创建画布。在0.5,0.3处书写：哈哈，字号为15，Black色。", out _);
        Assert.NotNull(paintfile);
    }
}
