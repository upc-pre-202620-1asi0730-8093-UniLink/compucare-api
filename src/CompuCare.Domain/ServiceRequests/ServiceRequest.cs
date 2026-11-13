using CompuCare.Domain.Common;

namespace CompuCare.Domain.ServiceRequests;
public enum ServiceRequestStatus { PendingAssignment = 0, Assigned = 1, InDiagnosis = 2, Resolved = 3, Cancelled = 4 }

/// <summary>BC04 Core aggregate. Status changes occur through commands, never direct EF field edits.</summary>
public sealed class ServiceRequest
{
    private ServiceRequest() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid EquipmentId { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid? TechnicianId { get; private set; }
    public string Category { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string Diagnosis { get; private set; } = "";
    public string WorkDone { get; private set; } = "";
    public ServiceRequestStatus Status { get; private set; }
    public bool Paused { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public decimal WorkHours { get; private set; }
    private readonly List<RequestProgress> _timeline = [];
    public IReadOnlyCollection<RequestProgress> Timeline => _timeline.AsReadOnly();

    public static ServiceRequest Report(Guid companyId, Guid equipmentId, Guid requesterId, string description,
        string category = "Otro")
    {
        if (companyId == Guid.Empty || equipmentId == Guid.Empty || requesterId == Guid.Empty)
            throw new DomainRuleException("Valid company, equipment and requester identifiers are required.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 10)
            throw new DomainRuleException("A description of at least ten characters is required.");
        if (string.IsNullOrWhiteSpace(category) || category.Length > 100)
            throw new DomainRuleException("Request category is required.");
        var now = DateTime.UtcNow;
        var request = new ServiceRequest
        {
            Id = Guid.NewGuid(), CompanyId = companyId, EquipmentId = equipmentId,
            RequesterId = requesterId, Description = description.Trim(), Category = category.Trim(),
            Status = ServiceRequestStatus.PendingAssignment, CreatedAtUtc = now, UpdatedAtUtc = now
        };
        request.Log("Pendiente de Asignación");
        return request;
    }

    public void AssignTo(Guid technicianId)
    {
        if (Status != ServiceRequestStatus.PendingAssignment || technicianId == Guid.Empty)
            throw new DomainRuleException("Only pending requests can be assigned to a valid technician.");
        TechnicianId = technicianId; Status = ServiceRequestStatus.Assigned;
        Touch(); Log("Asignado");
    }
    public void StartDiagnosis(Guid technicianId)
    {
        if (Status != ServiceRequestStatus.Assigned || technicianId != TechnicianId)
            throw new DomainRuleException("Only the assigned technician can start the diagnosis.");
        Status = ServiceRequestStatus.InDiagnosis; Touch(); Log("En Diagnóstico");
    }
    public void Resolve(Guid technicianId, decimal hours, bool hasUnresolvedRequiredQuotation,
        string diagnosis = "", string workDone = "")
    {
        if (Status != ServiceRequestStatus.InDiagnosis && Status != ServiceRequestStatus.Assigned ||
            technicianId != TechnicianId)
            throw new DomainRuleException("Only the assigned technician can resolve an assigned or diagnosed request.");
        if (hours <= 0m) throw new DomainRuleException("Work hours must be positive.");
        if (Paused || hasUnresolvedRequiredQuotation)
            throw new DomainRuleException("Required quotations must be resolved before completing a ticket.");
        if (string.IsNullOrWhiteSpace(diagnosis) || string.IsNullOrWhiteSpace(workDone))
            throw new DomainRuleException("Diagnosis and completed work are required.");
        WorkHours = hours; Diagnosis = diagnosis.Trim(); WorkDone = workDone.Trim();
        Status = ServiceRequestStatus.Resolved; Touch(); Log("Atendido");
    }
    public void Cancel(Guid requesterId)
    {
        if (Status != ServiceRequestStatus.PendingAssignment || RequesterId != requesterId)
            throw new DomainRuleException("Only the requester can cancel an unassigned request.");
        Status = ServiceRequestStatus.Cancelled; Touch(); Log("Cancelado");
    }
    public void SetPaused(bool paused)
    {
        if (Status is ServiceRequestStatus.Resolved or ServiceRequestStatus.Cancelled)
            throw new DomainRuleException("Cannot pause a completed request.");
        Paused = paused; Touch();
    }
    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
    private void Log(string label) => _timeline.Add(RequestProgress.Create(label));
}

public sealed class RequestProgress
{
    private RequestProgress() { }
    public Guid Id { get; private set; }
    public string Status { get; private set; } = "";
    public DateTime At { get; private set; }
    public static RequestProgress Create(string status) =>
        new() { Id = Guid.NewGuid(), Status = status, At = DateTime.UtcNow };
}
