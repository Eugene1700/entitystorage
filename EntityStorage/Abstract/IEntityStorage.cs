using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EntityStorage.Core;

namespace EntityStorage
{
    public interface IEntityStorage
    {
        IQueryable<T> Select<T>() where T : class, IEntity;
        Task<long> Create<T>(T entity) where T : class, IEntity, new();
        Task<int> CreateIfNotExist<T>(Expression<Func<T, bool>> matchCondition, Expression<Func<T>> creator)
            where T : class, IEntity, new ();
        Task Update<T>(T entity) where T : class, IEntity, new();
        Task<int> Update<T>(Expression<Func<T, bool>> matchCondition, Expression<Func<T, T>> setter)
            where T : class, IEntity, new();
        Task UpdateSingle<T>(T entity, Expression<Func<T, T>> setter) where T : class, IEntity, new ();
        Task Remove<T>(Expression<Func<T, bool>> whereExpression) where T : class, IEntity;
    }
}