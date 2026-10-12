namespace TransactionalOutbox;

/// <summary>A pretend message broker (think RabbitMQ). Every 10th publish call fails, like a network blip.</summary>
public sealed class FakeBroker
{
    private int _calls;
    public List<string> Delivered { get; } = new();

    public Task PublishAsync(string message)
    {
        _calls++;
        if (_calls % 10 == 0)
            throw new InvalidOperationException("Broker unavailable");

        Delivered.Add(message);
        return Task.CompletedTask;
    }
}
