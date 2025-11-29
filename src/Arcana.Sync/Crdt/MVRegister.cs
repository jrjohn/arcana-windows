namespace Arcana.Sync.Crdt;

/// <summary>
/// Multi-Value Register CRDT.
/// Preserves all concurrent values instead of picking one.
/// Users must resolve conflicts manually.
/// </summary>
/// <typeparam name="T">The type of value stored.</typeparam>
public class MVRegister<T>
{
    private readonly List<(T Value, VectorClock Clock)> _values = new();

    public IReadOnlyList<(T Value, VectorClock Clock)> Values => _values;

    /// <summary>
    /// Gets whether there are conflicts (multiple concurrent values).
    /// </summary>
    public bool HasConflict => _values.Count > 1;

    /// <summary>
    /// Gets the single value if no conflict, or throws if there are conflicts.
    /// </summary>
    public T? SingleValue => _values.Count == 1 ? _values[0].Value : throw new InvalidOperationException("Multiple concurrent values exist");

    public MVRegister() { }

    public MVRegister(T value, VectorClock clock)
    {
        _values.Add((value, clock));
    }

    /// <summary>
    /// Sets a new value with the given vector clock.
    /// </summary>
    public void Set(T value, VectorClock clock)
    {
        // Remove all values that are causally before the new value
        _values.RemoveAll(v => v.Clock.CompareTo(clock) == CausalRelation.HappenedBefore);

        // Only add if not dominated by existing values
        if (!_values.Any(v => v.Clock.CompareTo(clock) == CausalRelation.HappenedAfter))
        {
            _values.Add((value, clock));
        }
    }

    /// <summary>
    /// Merges with another MV register.
    /// </summary>
    public MVRegister<T> Merge(MVRegister<T> other)
    {
        var result = new MVRegister<T>();
        var allValues = _values.Concat(other._values).ToList();

        foreach (var (value, clock) in allValues)
        {
            bool dominated = allValues.Any(v =>
                !ReferenceEquals(v.Clock, clock) &&
                v.Clock.CompareTo(clock) == CausalRelation.HappenedAfter);

            if (!dominated)
            {
                // Check for duplicates
                if (!result._values.Any(v => v.Clock.Equals(clock)))
                {
                    result._values.Add((value, clock));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves conflicts by selecting a value.
    /// </summary>
    public void Resolve(T resolvedValue, VectorClock mergedClock)
    {
        _values.Clear();
        _values.Add((resolvedValue, mergedClock));
    }
}
