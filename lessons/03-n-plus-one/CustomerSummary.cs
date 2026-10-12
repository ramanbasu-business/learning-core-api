namespace NPlusOne;

/// <summary>One line of the customer report.</summary>
public class CustomerSummary
{
    public string Name { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}
