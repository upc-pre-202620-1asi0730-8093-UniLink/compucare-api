using CompuCare.Domain.Common;
using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Domain.QuotationsPayments;
using CompuCare.Domain.ServiceRequests;
using CompuCare.Domain.SubscriptionsCoverage;
using Xunit;

namespace CompuCare.Domain.Tests;

public sealed class IdentityAccessTests
{
    [Fact]
    public void CompanyManagerRequiresCompany() =>
        Assert.Throws<DomainRuleException>(() =>
            UserAccount.Register("Alice", "alice@example.org", "hashed-secret", "admin", null));
    [Fact]
    public void UsersCannotObtainUnknownRole() =>
        Assert.Throws<DomainRuleException>(() =>
            UserAccount.Register("Bob", "bob@example.org", "hashed-secret", "godmode"));
    [Fact]
    public void PasswordResetTokenCanBeConsumedOnce()
    {
        var reset = PasswordResetToken.Issue(Guid.NewGuid(), "hash",
            DateTime.UtcNow.AddMinutes(15));
        reset.Consume();
        Assert.Throws<DomainRuleException>(() => reset.Consume());
    }
}
public sealed class CompaniesAndEquipmentTests
{
    [Fact]
    public void AssignmentRejectsCrossCompanyUser()
    {
        var device = Equipment.Register(Guid.NewGuid(), "SN-42", "Office laptop", "Laptop");
        Assert.Throws<DomainRuleException>(() => device.AssignTo(Guid.NewGuid(), false));
        Assert.Null(device.AssignedEmployeeId);
    }
    [Fact]
    public void ValidAssignmentCanBeRemoved()
    {
        var device = Equipment.Register(Guid.NewGuid(), "SN-42", "Office laptop", "Laptop");
        device.AssignTo(Guid.NewGuid(), true);
        Assert.NotNull(device.AssignedEmployeeId);
        device.Unassign();
        Assert.Null(device.AssignedEmployeeId);
    }
    [Fact]
    public void LocationRejectsMissingDesk() =>
        Assert.Throws<DomainRuleException>(() => Location.Register(Guid.NewGuid(), "Headquarters", ""));
}
public sealed class SubscriptionCoverageTests
{
    private static Subscription Active() =>
        Subscription.Activate(Guid.NewGuid(), Plan.Publish("test", "Test", 10m, 5m, 1));
    [Fact]
    public void RepeatedTicketConsumptionIsIdempotent()
    {
        var subscription = Active();
        var ticket = Guid.NewGuid();
        Assert.True(subscription.Consume(ticket, 1.5m));
        Assert.False(subscription.Consume(ticket, 1.5m));
        Assert.Equal(1.5m, subscription.UsedHours);
    }
    [Fact]
    public void NewPeriodResetsHoursButKeepsConsumptionAudit()
    {
        var subscription = Active();
        subscription.Consume(Guid.NewGuid(), 3m);
        subscription.Renew(Plan.Publish("new", "New Plan", 20m, 10m, 2));
        Assert.Equal(0m, subscription.UsedHours);
        Assert.Single(subscription.Consumptions);
    }
    [Fact]
    public void PreventiveQuotaCannotBeExceeded()
    {
        var subscription = Active();
        subscription.BookPreventive();
        Assert.Throws<DomainRuleException>(() => subscription.BookPreventive());
    }
}
public sealed class QuotationPaymentTests
{
    private static Quotation Quote() => Quotation.IssueWithItems(Guid.NewGuid(),
        Guid.NewGuid(), Guid.NewGuid(), new List<(string Description, decimal Cost)>
        {
            ("Part", 20.50m), ("Labor", 9.25m)
        });
    [Fact]
    public void TotalIsCalculatedFromLines()
    {
        var quote = Quote();
        Assert.Equal(29.75m, quote.Total);
        Assert.Equal(2, quote.Items.Count);
    }
    [Fact]
    public void CannotPayBeforeApproval() =>
        Assert.Throws<DomainRuleException>(() => Quote().RecordSimulatedPayment());
    [Fact]
    public void RejectedQuoteCannotBePaid()
    {
        var quote = Quote();
        quote.Reject(true);
        Assert.Throws<DomainRuleException>(() => quote.RecordSimulatedPayment());
    }
    [Fact]
    public void ApprovedQuoteGeneratesReceiptWithoutCardData()
    {
        var quote = Quote();
        quote.Approve(true);
        quote.RecordSimulatedPayment();
        Assert.Equal(QuotationStatus.PaidSimulated, quote.Status);
        Assert.StartsWith("REC-", quote.Receipt);
    }
}
public sealed class TicketLifecycleTests
{
    [Fact]
    public void TechnicianMustRecordWorkAndDiagnosis()
    {
        var ticket = ServiceRequest.Report(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "The office workstation will not power on.");
        var technician = Guid.NewGuid();
        ticket.AssignTo(technician);
        Assert.Throws<DomainRuleException>(() => ticket.Resolve(technician, 1m, false));
        ticket.StartDiagnosis(technician);
        ticket.Resolve(technician, 1m, false, "Power failure", "Replaced the PSU");
        Assert.Equal(ServiceRequestStatus.Resolved, ticket.Status);
        Assert.Contains(ticket.Timeline, x => x.Status == "Atendido");
    }
    [Fact]
    public void CancelledTicketCannotBeAssigned()
    {
        var user = Guid.NewGuid();
        var ticket = ServiceRequest.Report(Guid.NewGuid(), Guid.NewGuid(), user,
            "The company printer cannot connect to the network.");
        ticket.Cancel(user);
        Assert.Throws<DomainRuleException>(() => ticket.AssignTo(Guid.NewGuid()));
    }
}
