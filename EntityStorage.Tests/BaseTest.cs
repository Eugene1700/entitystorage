using System;
using EntityStorage.Core;
using EntityStorage.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EntityStorage.Tests
{
    public class BaseTest
    {
        private const string DevDb = "dev.db";
        protected BaseTest()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IClock, Clock>();
            serviceCollection.UseEFOnlyTranslator();
            serviceCollection.AddEntityStorage(x=>x.UseSqlite($"Data Source={DevDb}"));
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }
        
        [SetUp]
        public virtual void Setup()
        {
            var context = ServiceProvider.GetRequiredService<EfDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        protected IServiceProvider ServiceProvider { get; }
    }
}