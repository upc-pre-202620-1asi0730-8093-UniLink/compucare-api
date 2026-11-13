using System.Linq.Expressions;
using CompuCare.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CompuCare.Infrastructure.Persistence;

public sealed class EfEntityStore<T>(CompuCareDbContext database) : IEntityStore<T> where T : class
{
    public Task<T?> FirstAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default) =>
        database.Set<T>().FirstOrDefaultAsync(filter, ct);
    public Task<List<T>> ListAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default) =>
        database.Set<T>().Where(filter).ToListAsync(ct);
    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await database.Set<T>().AddAsync(entity, ct);
    public async Task SaveAsync(CancellationToken ct = default) => await database.SaveChangesAsync(ct);
}
