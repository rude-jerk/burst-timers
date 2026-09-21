namespace BurstTimers;

// One duration per effect, even when several players supplied the same status.
public sealed class ActiveEffects
{
    private readonly Dictionary<uint, float> durations = [];
    public void Clear() => durations.Clear();
    public void Observe(uint statusId, float remaining)
    {
        if (float.IsFinite(remaining) && remaining > 0)
            durations[statusId] = Math.Max(durations.GetValueOrDefault(statusId), remaining);
    }
    public float Remaining(params uint[] ids) => ids.Max(id => durations.GetValueOrDefault(id));
}
