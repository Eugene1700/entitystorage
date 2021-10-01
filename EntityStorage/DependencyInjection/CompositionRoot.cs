using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityStorage.DependencyInjection
{
    public static class CompositionRoot
    {
        public static void AddEntityStorage(this IServiceCollection serviceCollection,
            Action<DbContextOptionsBuilder> builder)
        {
            serviceCollection.AddDbContextPool<EfDbContext>(builder);
            serviceCollection.AddScoped<IEntityStorage, EFEntityStorage>();
        }

        public static void UseFullModeTranslator(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IMode, FullMode>();
        }
        
        public static void UseEFOnlyTranslator(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IMode, EFOnlyMode>();
        }
    }
}