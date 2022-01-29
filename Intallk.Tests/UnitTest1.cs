using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Intallk.Tests
{
    public class UnitTest1: BasicTest
    {
        public UnitTest1(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public void 喜报()
        {
            PaintingModel.PaintFile paintfile;
            paintfile = new PaintingCompiler().CompilePaintScript(
                "以喜报.png为背景创建画布。在0.5,100处书写：{喜报内容}，大小为0.9x0.8，自动调整大小，颜色为255,255,0。"
                , out _);
            Assert.NotNull(paintfile);
        }
        [Fact]
        public void CreateGraphicsTest1()
        {
            PaintingModel.PaintFile paintfile;
            paintfile = new PaintingCompiler().CompilePaintScript("以back.png为背景创建画布。", out _);
            Assert.NotNull(paintfile);
        }
        [Fact]
        public void CreateGraphicsTest2()
        {
            PaintingModel.PaintFile paintfile;
            paintfile = new PaintingCompiler().CompilePaintScript("以100x300的尺寸创建画布。在0.5,0.3处书写：哈哈，字号为15，Black色。", out _);
            Assert.NotNull(paintfile);
        }
    }
}