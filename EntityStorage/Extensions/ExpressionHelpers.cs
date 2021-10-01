using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EntityStorage.Extensions
{
    public static class ExpressionHelpers
    {
        public static Expression<Func<T, bool>> Or<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
        {
            if (expr == null)
                throw new ArgumentNullException(nameof(expr));

            return Expression.Lambda<Func<T, bool>>(Expression.Not(expr), expr.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            if (left == null)
                return right;

            if (right == null)
                return left;

            var invokedExpr = Expression.Invoke(right, left.Parameters);
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, invokedExpr), left.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this IEnumerable<Expression<Func<T, bool>>> exprs)
        {
            return exprs.Where(x => x != null).Aggregate((acc, next) => acc.And(next));
        }

        public static Expression<Func<TInner, TResult>> Sup<TOuter, TInner, TResult>(
            this Expression<Func<TOuter, TResult>> outer, Expression<Func<TInner, TOuter>> inner)
        {
            var substitutionParam = Expression.Parameter(typeof(TInner), "param");
        
            var newInner = inner.Body.Substitute(inner.Parameters[0], substitutionParam);
            var newOuter = outer.Body.Substitute(outer.Parameters[0], newInner);
        
            return Expression.Lambda<Func<TInner, TResult>>(newOuter, substitutionParam);
        }

        internal class ReplaceVisitor : ExpressionVisitor
        {
            private readonly Expression from, to;

            public ReplaceVisitor(Expression from, Expression to)
            {
                this.from = from;
                this.to = to;
            }

            public override Expression Visit(Expression node)
            {
                return node == from ? to : base.Visit(node);
            }
        }

        private static Expression Substitute(this Expression expression,
            Expression searchEx, Expression replaceEx)
        {
            return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
        }
    }
}