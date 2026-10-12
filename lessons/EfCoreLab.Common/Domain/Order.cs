namespace EfCoreLab.Common.Domain;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    /// <summary>The sales rep (a User) the order is assigned to. Optional.</summary>
    public Guid? SalesRepId { get; set; }
    public User? SalesRep { get; set; }

    public string OrderNumber { get; set; } = default!;
    public string Status { get; set; } = "Pending";   // Pending, Paid, Shipped, Delivered, Cancelled
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
    public ICollection<OrderNote> OrderNotes { get; set; } = new List<OrderNote>();
}
