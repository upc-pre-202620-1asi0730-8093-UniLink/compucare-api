using CompuCare.Domain.Common;

namespace CompuCare.Domain.SubscriptionsCoverage;

/// <summary>BC03 Core — published plan catalog.</summary>
public sealed class Plan
{
    private Plan() { }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public decimal Price { get; private set; }
    public decimal Hours { get; private set; }
    public int PreventiveVisits { get; private set; }
    public static Plan Publish(string code, string name, decimal price, decimal hours, int visits)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) ||
            price < 0 || hours <= 0 || visits < 0)
            throw new DomainRuleException("Plan values are invalid.");
        return new Plan { Id = Guid.NewGuid(), Code = code, Name = name,
            Price = price, Hours = hours, PreventiveVisits = visits };
    }
}
