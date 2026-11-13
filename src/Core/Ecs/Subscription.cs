namespace NightCero.Core.Ecs;

/// <summary>One event handler, keyed by the delegate so it can be removed exactly.</summary>
internal sealed class Subscription(Delegate handler)
{
    private readonly Delegate _handler = handler;

    /// <summary>Same handler, same method and same target (method groups agree on the delegate).</summary>
    public bool Matches(Delegate other) =>
        _handler.Equals(other) || (_handler.Method == other.Method && Equals(_handler.Target, other.Target));

    public void Invoke(object value) => _handler.DynamicInvoke(value);
}
