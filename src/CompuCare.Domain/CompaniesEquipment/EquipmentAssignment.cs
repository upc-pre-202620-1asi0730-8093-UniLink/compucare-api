namespace CompuCare.Domain.CompaniesEquipment;

/// <summary>Append-only audit trail for equipment ownership changes.</summary>
public sealed class EquipmentAssignment
{
    private EquipmentAssignment() { }
    public Guid Id { get; private set; }
    public Guid EquipmentId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public DateTime At { get; private set; }
    public static EquipmentAssignment Record(Guid equipmentId, Guid? employeeId) =>
        new() { Id = Guid.NewGuid(), EquipmentId = equipmentId, EmployeeId = employeeId, At = DateTime.UtcNow };
}
