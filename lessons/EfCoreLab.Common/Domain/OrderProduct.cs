namespace EfCoreLab.Common.Domain;

/// <summary>Order line: the join between Order and Product, with its own data (quantity, price at the time).</summary>
public class OrderProduct
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
