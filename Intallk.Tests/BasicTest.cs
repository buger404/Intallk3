using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Intallk.Tests;

/// <summary>
/// 基本测试单元
/// </summary>
public abstract class BasicTest : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    /// 全局工厂
    /// </summary>
    internal readonly WebApplicationFactory<Program> Factory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="factory"></param>
    internal BasicTest(WebApplicationFactory<Program> factory) => Factory = factory;

    /// <summary>
    /// 创建一个 Scope
    /// </summary>
    /// <returns></returns>
    public IServiceScope CreateScope()
    {
        var serviceScopeFactory = Factory.Services.GetRequiredService<IServiceScopeFactory>();
        return serviceScopeFactory.CreateScope();
    }
}
