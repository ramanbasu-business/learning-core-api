namespace EfCoreLab.Common.Domain;

public class OrderNote
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;

    public string Text { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
