namespace EntityStorage.Extensions.Pagination
{
    public interface IPagination
    {
        public long TotalCount { get; }
        public int Limit { get; }
        public int PageNumber { get; }
    }
}