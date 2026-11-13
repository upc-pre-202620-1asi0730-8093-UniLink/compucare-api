using CompuCare.Domain.Common;

namespace CompuCare.Domain.ServiceRequests;

public enum ServiceRequestStatus
{
    PendingAssignment = 0,
    Assigned = 1,
    InDiagnosis = 2,
    Resolved = 3,
    Cancelled = 4
}

/// <summary>BC04 — Core. Aggregate Root. All transitions are validated here.</summary>
public sealed class ServiceRequest
{
    private ServiceRequest() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid EquipmentId { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid? TechnicianId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ServiceRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public decimal WorkHours { get; private set; }

    public static ServiceRequest Report(Guid companyId, Guid equipmentId, Guid requesterId, string description)
    {
        if (companyId == Guid.Empty || equipmentId == Guid.Empty || requesterId == Guid.Empty)
            throw new DomainRuleException("Valid company, equipment and requester identifiers are required.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 10)
            throw new DomainRuleException("A description of at least ten characters is required.");
        var now = DateTime.UtcNow;
        return new ServiceRequest
        {
            Id = Guid.NewGuid(), CompanyId = companyId, EquipmentId = equipmentId,
            RequesterId = requesterId, Description = description.Trim(),
            Status = ServiceRequestStatus.PendingAssignment,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
    }

    public void AssignTo(Guid technicianId)
    {
        if (Status != ServiceRequestStatus.PendingAssignment || technicianId == Guid.Empty)
            throw new DomainRuleException("Only pending requests can be assigned to a valid technician.");
        TechnicianId = technicianId;
        Status = ServiceRequestStatus.Assigned;
        Touch();
    }

    public void StartDiagnosis(Guid technicianId)
    {
        if (Status != ServiceRequestStatus.Assigned || technicianId != TechnicianId)
            throw new DomainRuleException("Only the assigned technician can start the diagnosis.");
        Status = ServiceRequestStatus.InDiagnosis;
        Touch();
    }

    /// <summary>
    /// The application layer must verify quotation approval and coverage BEFORE calling this method;
    /// the domain still validates technician ownership, status and positive hours.
    /// </summary>
    public void Resolve(Guid technicianId, decimal hours, bool hasUnresolvedRequiredQuotation)
    {
        if (Status != ServiceRequestStatus.InDiagnosis || technicianId != TechnicianId)
            throw new DomainRuleException("Only the assigned technician can resolve a diagnosed request.");
        if (hours <= 0m)
            throw new DomainRuleException("Recorded work hours must be positive.");
        if (hasUnresolvedRequiredQuotation)
            throw new DomainRuleException("Required quotation must be resolved before completion.");
        WorkHours = hours;
        Status = ServiceRequestStatus.Resolved;
        Touch();
    }

    public void Cancel(Guid requesterId)
    {
        if (Status != ServiceRequestStatus.PendingAssignment || RequesterId != requesterId)
            throw new DomainRuleException("Only the requester can cancel a request before assignment.");
        Status = ServiceRequestStatus.Cancelled;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
