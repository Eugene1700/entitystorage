using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EntityStorage.Core
{
    public sealed class EFEntityStorage : IEntityStorage
    {
        private readonly EfDbContext _context;
        private readonly IClock _clock;
        private readonly IMode _mode;
        
        private static readonly PropertyInfo VersionPropertyInfo =
            typeof(ModifiableEntity).GetProperty(nameof(ModifiableEntity.Version));

        private static readonly PropertyInfo ModificationTimePropertyInfo =
            typeof(ModifiableEntity).GetProperty(nameof(ModifiableEntity.ModificationTime));

        public EFEntityStorage(EfDbContext context, IClock clock, IMode mode)
        {
            _context = context;
            _clock = clock;
            _mode = mode;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            // _context.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public IQueryable<T> Select<T>() where T : class, IEntity
        {
            return _mode.TranslatorMode == TranslatorMode.Full
                ? _context.Set<T>().ToLinqToDB().AsNoTracking()
                : _context.Set<T>().AsNoTracking();
        }

        public async Task<long> Create<T>(T entity) where T : class, IEntity, new()
        {
            SetServiceColumnForCreate(entity);
            var entityEntry = await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entityEntry.Entity.Id;
        }

        public async Task<int> CreateIfNotExist<T>(Expression<Func<T, bool>> matchCondition,
            Expression<Func<T>> creator) where T : class, IEntity, new()
        {
            //todo create batch
            if (!(creator.Body is MemberInitExpression))
                throw new InvalidOperationException(
                    $"setter must be lambda expression with init entity body, current: [{creator}]");
            var existance = await EntityFrameworkQueryableExtensions.AnyAsync(_context.Set<T>(), matchCondition);
            if (existance) return 0;
            var newEntity = new T();
            var action = CreateUpdateAction(creator);
            action.Invoke(newEntity);
            SetServiceColumnForCreate(newEntity);
            _context.Add(newEntity);
            return await _context.SaveChangesAsync();
        }

        private void SetServiceColumnsForUpdate<T>(T newEntity) where T : class, IEntity, new()
        {
            if (newEntity is ModifiableEntity modificableEntity)
            {
                modificableEntity.ModificationTime = _clock.Now;
                modificableEntity.Version += 1;
            }
        }

        private void SetServiceColumnForCreate<T>(T newEntity) where T : class, IEntity, new()
        {
            if (newEntity is StandardEntity standardEntity)
            {
                standardEntity.CreationTime = _clock.Now;
                standardEntity.SortDate = _clock.Now.Date;
            }
        }

        public async Task Update<T>(T entity) where T : class, IEntity, new()
        {
            SetServiceColumnsForUpdate(entity);
            _context.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<int> Update<T>(Expression<Func<T, bool>> matchCondition, Expression<Func<T, T>> setter)
            where T : class, IEntity, new()
        {
            return await Z.EntityFramework.Plus.BatchUpdateExtensions.UpdateAsync(Select<T>().Where(matchCondition),
                PrepareUpdateSetter(setter));
        }

        public async Task UpdateSingle<T>(T entity, Expression<Func<T, T>> setter) where T : class, IEntity, new()
        {
            _context.ChangeTracker.Clear();
            var entityEntry = _context.Attach(entity);
            
            var actualSetter = PrepareUpdateSetter(setter);
            var xMemberInit = (MemberInitExpression) actualSetter.Body;
            var action = CreateUpdateAction(actualSetter);
            action.Invoke(entity);
            foreach (var binding in xMemberInit.Bindings)
            {
                var memberAssignment = (MemberAssignment) binding;
                var propertyInfo = (PropertyInfo) memberAssignment.Member;
                entityEntry.Property(propertyInfo.Name).IsModified = true;
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task Remove<T>(Expression<Func<T, bool>> whereExpression) where T : class, IEntity
        {
            await Z.EntityFramework.Plus.BatchDeleteExtensions.DeleteAsync(_context.Set<T>().Where(whereExpression));
        }

        private Expression<Func<T, T>> PrepareUpdateSetter<T>(Expression<Func<T, T>> setter)
            where T : class, IEntity
        {
            if (!(setter.Body is MemberInitExpression memberInitExpression))
                throw new InvalidOperationException(
                    $"setter must be lambda expression with init entity body, current: [{setter}]");
            if (memberInitExpression.Bindings.Count == 0)
                throw new InvalidOperationException($"setter must must contain fields to update, current: [{setter}]");
            var actualSetter = setter;
            var isModifiable = typeof(ModifiableEntity).IsAssignableFrom(typeof(T));
            if (isModifiable)
                actualSetter = PatchVersionAndModificationTime(setter, false);
            return actualSetter;
        }
        
        private static readonly PropertyInfo nowPropertyInfo = typeof(DateTime).GetProperty(nameof(DateTime.Now),
            BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.Public);

        private Expression<Func<T, T>> PatchVersionAndModificationTime<T>(Expression<Func<T, T>> exp,
            bool setZeroVersion) where T : class, IEntity
        {
            var expressions = new Dictionary<PropertyInfo, Expression>
            {
                {
                    ModificationTimePropertyInfo,
                    Expression.Convert(Expression.Call(nowPropertyInfo.GetMethod),
                        ModificationTimePropertyInfo.PropertyType)
                },
            };
            if (setZeroVersion)
                expressions.Add(VersionPropertyInfo, Expression.Constant((int) 1));
            else
                expressions.Add(VersionPropertyInfo,
                    Expression.Add(Expression.Property(exp.Parameters[0], VersionPropertyInfo),
                        Expression.Constant((int) 1)));
            return PatchMemberAssigment(exp, expressions);
        }
        
        private Expression<Func<T, T>> PatchMemberAssigment<T>(Expression<Func<T, T>> setter,
            Dictionary<PropertyInfo, Expression> patchers) where T : class, IEntity
        {
            var xMemberInit = (MemberInitExpression) setter.Body;
            var bindings = new List<MemberBinding>();
            var alreadyAssigned = new List<PropertyInfo>();
            foreach (var binding in xMemberInit.Bindings)
            {
                bindings.Add(binding);
                var bindingMember = binding.Member as PropertyInfo;
                alreadyAssigned.Add(bindingMember);
            }

            foreach (var patcher in patchers)
            {
                if (!alreadyAssigned.Contains(patcher.Key))
                {
                    bindings.Add(Expression.Bind(patcher.Key, patcher.Value));
                }
            }

            var xBody = Expression.MemberInit(xMemberInit.NewExpression, bindings);
            return Expression.Lambda<Func<T, T>>(xBody, setter.Parameters);
        }

        private static Action<T> CreateUpdateAction<T>(Expression<Func<T, T>> setter) where T : class, IEntity
        {
            var xMemberInit = (MemberInitExpression) setter.Body;
            return CreateUpdateAction<T>(xMemberInit);
        }

        private static Action<T> CreateUpdateAction<T>(Expression<Func<T>> creator) where T : class, IEntity
        {
            var xMemberInitExp = (MemberInitExpression) creator.Body;
            //todo add creation time and test  it
            return CreateUpdateAction<T>(xMemberInitExp);
        }

        private static Action<T> CreateUpdateAction<T>(MemberInitExpression xMemberInit) where T : class, IEntity
        {
            var parameter = Expression.Parameter(typeof(T), "target");
            var updateExpressionsList = new List<Expression>();
            foreach (var binding in xMemberInit.Bindings)
            {
                var memberAssignment = (MemberAssignment) binding;
                var propertyInfo = (PropertyInfo) memberAssignment.Member;
                var assignExp = Expression.Assign(Expression.Property(parameter, propertyInfo),
                    GetValueExpression(memberAssignment.Expression, parameter));
                updateExpressionsList.Add(assignExp);
            }

            return Expression.Lambda<Action<T>>(Expression.Block(updateExpressionsList), parameter).Compile();
        }

        private static Expression GetValueExpression(Expression valueExpression,
            ParameterExpression parameter)
        {
            valueExpression = new ReplaceParameterVisitor(parameter).Visit(valueExpression);
            return valueExpression;
        }

        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression parameter;

            public ReplaceParameterVisitor(ParameterExpression parameter)
            {
                this.parameter = parameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return base.VisitParameter(parameter);
            }
        }
    }

    public interface IMode
    {
        public TranslatorMode TranslatorMode { get; }
    }

    internal class FullMode : IMode
    {
        public TranslatorMode TranslatorMode => TranslatorMode.Full;
    }

    internal class EFOnlyMode : IMode
    {
        public TranslatorMode TranslatorMode => TranslatorMode.EFOnly;
    }

    public enum TranslatorMode
    {
        EFOnly,
        Full
    }
}