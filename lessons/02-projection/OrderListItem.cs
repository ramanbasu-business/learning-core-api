namespace Projection;

/// <summary>
/// What the orders grid actually shows: five values. A small "shape" like this (a DTO) is
/// what we want to get out of the database - not the full Order, Customer and OrderProduct entities.
/// </summary>
public class OrderListItem
{
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}
