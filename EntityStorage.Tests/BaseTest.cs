using System;
using EntityStorage.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityStorage.Tests
{
    public class BaseTest
    {
        protected const string DevDb = "dev.db";
        protected BaseTest()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IClock, Clock>();
            serviceCollection.UseEFOnlyTranslator();
            serviceCollection.AddEntityStorage(x=>x.UseSqlite($"Data Source={DevDb}"));
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public IServiceProvider ServiceProvider { get; }
    }
}