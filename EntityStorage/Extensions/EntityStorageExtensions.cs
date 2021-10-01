using System.Linq;
using System.Threading.Tasks;
using EntityStorage.Core;

namespace EntityStorage.Extensions
{
    public static class EntityStorageExtensions
    {
        public static T GetById<T>(this IEntityStorage entityStorage, long id) where T : class, IEntity
        {
            return entityStorage.Select<T>().Single(x => x.Id == id);
        }
        
        public static T Reload<T>(this IEntityStorage entityStorage, T entity) where T : class, IEntity
        {
            return entityStorage.GetById<T>(entity.Id);
        }

        public static async Task<T> CreateEntity<T>(this IEntityStorage entityStorage, T entity) where T : class, IEntity, new()
        {
            var id = await entityStorage.Create(entity);
            entity.Id = id;
            return entity;
        }
    }
}