using CompuCare.Domain.Common;
using CompuCare.Domain.ServiceRequests;
using Xunit;

namespace CompuCare.Domain.Tests;

public sealed class ServiceRequestTests
{
    private static ServiceRequest Ticket() => ServiceRequest.Report(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "The company laptop cannot start.");

    [Fact]
    public void NewTicket_StartsPendingAssignment()
    {
        var ticket = Ticket();
        Assert.Equal(ServiceRequestStatus.PendingAssignment, ticket.Status);
    }

    [Fact]
    public void Ticket_RejectsTooShortDescription() =>
        Assert.Throws<DomainRuleException>(() => ServiceRequest.Report(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "broken"));

    [Fact]
    public void OnlyAssignedTechnician_CanStartDiagnosis()
    {
        var ticket = Ticket();
        var technician = Guid.NewGuid();
        ticket.AssignTo(technician);
        Assert.Throws<DomainRuleException>(() => ticket.StartDiagnosis(Guid.NewGuid()));
        ticket.StartDiagnosis(technician);
        Assert.Equal(ServiceRequestStatus.InDiagnosis, ticket.Status);
    }

    [Fact]
    public void PendingQuotation_BlocksResolution()
    {
        var ticket = Ticket();
        var technician = Guid.NewGuid();
        ticket.AssignTo(technician);
        ticket.StartDiagnosis(technician);
        Assert.Throws<DomainRuleException>(() => ticket.Resolve(technician, 1.5m, true));
        Assert.Equal(ServiceRequestStatus.InDiagnosis, ticket.Status);
    }

    [Fact]
    public void Requester_CanCancelBeforeAssignmentOnly()
    {
        var ticket = Ticket();
        Assert.Throws<DomainRuleException>(() => ticket.Cancel(Guid.NewGuid()));
        ticket.Cancel(ticket.RequesterId);
        Assert.Equal(ServiceRequestStatus.Cancelled, ticket.Status);
    }
}
