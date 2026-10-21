using Microsoft.EntityFrameworkCore;
namespace SchoolManagement.Data;

public static class QueryLoading
{
    // Optional paging keeps lookup callers compatible while list screens request bounded pages.
    public static IQueryable<T> ApplyPage<T>(this IQueryable<T> query, int? page, int pageSize)
    {
        if (!page.HasValue) return query;
        var size = Math.Clamp(pageSize, 1, 200);
        return query.Skip((Math.Clamp(page.Value, 1, 100000) - 1) * size).Take(size);
    }
    public static Task<List<T>> ToListWhenAsync<T>(this IQueryable<T> query, bool enabled) =>
        enabled ? query.ToListAsync() : Task.FromResult(new List<T>());
    public static Task<T?> FirstOrDefaultWhenAsync<T>(this IQueryable<T> query, bool enabled) where T : class =>
        enabled ? query.FirstOrDefaultAsync() : Task.FromResult<T?>(null);
}
