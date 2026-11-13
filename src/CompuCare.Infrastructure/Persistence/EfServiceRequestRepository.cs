using CompuCare.Application.ServiceRequests;
using CompuCare.Domain.ServiceRequests;
using Microsoft.EntityFrameworkCore;

namespace CompuCare.Infrastructure.Persistence;

public sealed class EfServiceRequestRepository(CompuCareDbContext db) : IServiceRequestRepository
{
    public Task<ServiceRequest?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(ServiceRequest request, CancellationToken ct = default) =>
        db.ServiceRequests.AddAsync(request, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
