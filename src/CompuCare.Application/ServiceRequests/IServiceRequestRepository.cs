using CompuCare.Domain.ServiceRequests;

namespace CompuCare.Application.ServiceRequests;

/// <summary>BC04 persistence port. Implemented by Infrastructure.</summary>
public interface IServiceRequestRepository
{
    Task<ServiceRequest?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ServiceRequest request, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
