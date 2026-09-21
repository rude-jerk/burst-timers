namespace BurstTimers;

public sealed class PartyBuffTracker(string name, params uint[] statusIds)
{
    public string Name { get; } = name;
    public StatusCooldownTimer Timer { get; } = new();
    public float DurationRemaining(ActiveEffects effects) => effects.Remaining(statusIds);

    public void Update(double now, IReadOnlySet<uint> activeStatuses) =>
        Timer.Update(now, statusIds.Any(activeStatuses.Contains));

    public static PartyBuffTracker[] CreateAll() =>
    [
        // Embolden uses separate statuses for the caster and party recipients.
        new("Embolden", 1239, 1297),
        new("Starry Muse", 3685),
        // Track the damage buff, not the separate Meditative Brotherhood effect.
        new("Brotherhood", 1185),
        new("Searing Light", 2703),
    ];
}
