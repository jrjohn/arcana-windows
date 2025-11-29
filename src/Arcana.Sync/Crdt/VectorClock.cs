using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arcana.Sync.Crdt;

/// <summary>
/// Vector clock for tracking causality in distributed systems.
/// </summary>
public class VectorClock : IEquatable<VectorClock>, IComparable<VectorClock>
{
    private readonly Dictionary<string, long> _clock;

    [JsonPropertyName("clock")]
    public IReadOnlyDictionary<string, long> Clock => _clock;

    public VectorClock()
    {
        _clock = new Dictionary<string, long>();
    }

    public VectorClock(Dictionary<string, long> clock)
    {
        _clock = new Dictionary<string, long>(clock);
    }

    /// <summary>
    /// Increments the clock for a specific node.
    /// </summary>
    public VectorClock Increment(string nodeId)
    {
        var newClock = new Dictionary<string, long>(_clock);
        newClock[nodeId] = GetValue(nodeId) + 1;
        return new VectorClock(newClock);
    }

    /// <summary>
    /// Gets the clock value for a specific node.
    /// </summary>
    public long GetValue(string nodeId)
    {
        return _clock.TryGetValue(nodeId, out var value) ? value : 0;
    }

    /// <summary>
    /// Merges this clock with another, taking the maximum of each component.
    /// </summary>
    public VectorClock Merge(VectorClock other)
    {
        var newClock = new Dictionary<string, long>(_clock);

        foreach (var kvp in other._clock)
        {
            if (newClock.TryGetValue(kvp.Key, out var existing))
            {
                newClock[kvp.Key] = Math.Max(existing, kvp.Value);
            }
            else
            {
                newClock[kvp.Key] = kvp.Value;
            }
        }

        return new VectorClock(newClock);
    }

    /// <summary>
    /// Determines the causal relationship with another clock.
    /// </summary>
    public CausalRelation CompareTo(VectorClock other)
    {
        bool thisGreater = false;
        bool otherGreater = false;

        var allKeys = _clock.Keys.Union(other._clock.Keys);

        foreach (var key in allKeys)
        {
            var thisValue = GetValue(key);
            var otherValue = other.GetValue(key);

            if (thisValue > otherValue) thisGreater = true;
            if (otherValue > thisValue) otherGreater = true;
        }

        if (thisGreater && otherGreater) return CausalRelation.Concurrent;
        if (thisGreater) return CausalRelation.HappenedAfter;
        if (otherGreater) return CausalRelation.HappenedBefore;
        return CausalRelation.Equal;
    }

    int IComparable<VectorClock>.CompareTo(VectorClock? other)
    {
        if (other == null) return 1;
        var relation = CompareTo(other);
        return relation switch
        {
            CausalRelation.HappenedAfter => 1,
            CausalRelation.HappenedBefore => -1,
            _ => 0
        };
    }

    public bool Equals(VectorClock? other)
    {
        if (other == null) return false;
        return CompareTo(other) == CausalRelation.Equal;
    }

    public override bool Equals(object? obj) => Equals(obj as VectorClock);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in _clock.OrderBy(k => k.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(_clock);
    }

    public static VectorClock Deserialize(string json)
    {
        var clock = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
        return new VectorClock(clock ?? new Dictionary<string, long>());
    }

    public override string ToString()
    {
        return $"VectorClock({string.Join(", ", _clock.Select(kvp => $"{kvp.Key}:{kvp.Value}"))})";
    }
}

/// <summary>
/// Causal relationship between two vector clocks.
/// </summary>
public enum CausalRelation
{
    /// <summary>Events are causally related, this happened after the other.</summary>
    HappenedAfter,
    /// <summary>Events are causally related, this happened before the other.</summary>
    HappenedBefore,
    /// <summary>Events happened concurrently (no causal relationship).</summary>
    Concurrent,
    /// <summary>Events are identical.</summary>
    Equal
}
