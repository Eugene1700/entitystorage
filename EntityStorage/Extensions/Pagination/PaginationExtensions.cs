using System.Linq;

namespace EntityStorage.Extensions.Pagination
{
    public static class PaginationExtensions
    {
        public static Pagination<T> Paginate<T>(this IOrderedQueryable<T> query, int pageNumber, int limit)
        {
            var cntPagesToSkip = (pageNumber - 1) < 0 ? 0 : pageNumber - 1;
            var skipCnt =  cntPagesToSkip * limit;
            var totalCnt = query.Count();
            var entities = query.Skip(skipCnt).Take(limit).ToArray();
            return new Pagination<T>
            {
                TotalCount = totalCnt,
                Entities = entities,
                PageNumber = pageNumber,
                Limit = limit
            };
        }
        
        public static Pagination<T> Paginate<T>(this IOrderedEnumerable<T> query, int pageNumber, int limit)
        {
            var cntPagesToSkip = (pageNumber - 1) < 0 ? 0 : pageNumber - 1;
            var skipCnt =  cntPagesToSkip * limit;
            var totalCnt = query.Count();
            var entities = query.Skip(skipCnt).Take(limit).ToArray();
            return new Pagination<T>
            {
                TotalCount = totalCnt,
                Entities = entities,
                PageNumber = pageNumber,
                Limit = limit
            };
        }
    }
}