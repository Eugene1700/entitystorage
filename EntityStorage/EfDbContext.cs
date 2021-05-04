using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace EntityStorage
{
    public class EfDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var iType = typeof(IEntity);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => p.IsClass && !p.IsAbstract &&
                            iType.IsAssignableFrom(p)).ToArray();
            
            types.Select(x => x.Name).ToList().ForEach(Console.WriteLine);

            foreach (var type in types)
            {
                if (type.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;
                MethodInfo method = modelBuilder.GetType().GetMethod("Entity", new Type[] { });
                method = method?.MakeGenericMethod(type);
                method?.Invoke(modelBuilder, null);
                
            }
            base.OnModelCreating(modelBuilder);
        }


        public EfDbContext(DbContextOptions builder) : base(builder)
        {
            //todo
            Database.EnsureCreated();
        }
    }
}