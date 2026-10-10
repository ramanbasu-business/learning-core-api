namespace SoftDelete;

public sealed class NoteRow
{
    public string OrderNumber { get; set; } = default!;
    public string Text { get; set; } = default!;
    public string? Author { get; set; }
}
