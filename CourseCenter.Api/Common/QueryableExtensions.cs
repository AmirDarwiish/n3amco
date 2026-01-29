using Microsoft.EntityFrameworkCore;

namespace CourseCenter.Api.Common
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResponse<T>> ToPagedAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize)
        {
            var totalCount = await query.CountAsync();

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<T>
            {
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }

}
