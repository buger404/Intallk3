using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Intallk.Tests
{
    /// <summary>
    /// 基本测试单元
    /// </summary>
    public abstract class BasicTest : IClassFixture<WebApplicationFactory<Intallk.Startup>>
    {
        /// <summary>
        /// 全局工厂
        /// </summary>
        protected readonly WebApplicationFactory<Startup> Factory;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="factory"></param>
        public BasicTest(WebApplicationFactory<Startup> factory)
        {
            Factory = factory;
        }

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

}