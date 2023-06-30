namespace EntityStorage.Extensions.Pagination
{
    public class Pagination<T>: IPagination
    {
        public T[] Entities { get; set; }
        
        public long TotalCount { get; set; }
        public int Limit { get; set; }
        public int PageNumber { get; set; }
    }
}