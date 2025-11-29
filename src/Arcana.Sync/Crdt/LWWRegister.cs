namespace Arcana.Sync.Crdt;

/// <summary>
/// Last-Writer-Wins Register CRDT.
/// Uses timestamps to resolve conflicts - the latest write wins.
/// </summary>
/// <typeparam name="T">The type of value stored in the register.</typeparam>
public class LWWRegister<T>
{
    public T? Value { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string NodeId { get; private set; }

    public LWWRegister(T? value, DateTime timestamp, string nodeId)
    {
        Value = value;
        Timestamp = timestamp;
        NodeId = nodeId;
    }

    /// <summary>
    /// Updates the register if the new value is more recent.
    /// </summary>
    public bool Update(T? newValue, DateTime timestamp, string nodeId)
    {
        if (timestamp > Timestamp || (timestamp == Timestamp && string.Compare(nodeId, NodeId, StringComparison.Ordinal) > 0))
        {
            Value = newValue;
            Timestamp = timestamp;
            NodeId = nodeId;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Merges with another register, keeping the most recent value.
    /// </summary>
    public LWWRegister<T> Merge(LWWRegister<T> other)
    {
        if (other.Timestamp > Timestamp ||
            (other.Timestamp == Timestamp && string.Compare(other.NodeId, NodeId, StringComparison.Ordinal) > 0))
        {
            return new LWWRegister<T>(other.Value, other.Timestamp, other.NodeId);
        }
        return new LWWRegister<T>(Value, Timestamp, NodeId);
    }
}

/// <summary>
/// Last-Writer-Wins Map for field-level conflict resolution.
/// Each field has its own timestamp, allowing partial merges.
/// </summary>
public class LWWMap
{
    private readonly Dictionary<string, LWWRegister<object?>> _fields = new();

    public IReadOnlyDictionary<string, LWWRegister<object?>> Fields => _fields;

    /// <summary>
    /// Sets a field value with timestamp.
    /// </summary>
    public void Set(string fieldName, object? value, DateTime timestamp, string nodeId)
    {
        if (_fields.TryGetValue(fieldName, out var existing))
        {
            existing.Update(value, timestamp, nodeId);
        }
        else
        {
            _fields[fieldName] = new LWWRegister<object?>(value, timestamp, nodeId);
        }
    }

    /// <summary>
    /// Gets a field value.
    /// </summary>
    public T? Get<T>(string fieldName)
    {
        if (_fields.TryGetValue(fieldName, out var register) && register.Value is T value)
        {
            return value;
        }
        return default;
    }

    /// <summary>
    /// Merges with another LWW map.
    /// </summary>
    public LWWMap Merge(LWWMap other)
    {
        var result = new LWWMap();

        var allKeys = _fields.Keys.Union(other._fields.Keys);

        foreach (var key in allKeys)
        {
            var hasThis = _fields.TryGetValue(key, out var thisReg);
            var hasOther = other._fields.TryGetValue(key, out var otherReg);

            if (hasThis && hasOther)
            {
                result._fields[key] = thisReg!.Merge(otherReg!);
            }
            else if (hasThis)
            {
                result._fields[key] = new LWWRegister<object?>(thisReg!.Value, thisReg.Timestamp, thisReg.NodeId);
            }
            else
            {
                result._fields[key] = new LWWRegister<object?>(otherReg!.Value, otherReg.Timestamp, otherReg.NodeId);
            }
        }

        return result;
    }
}
