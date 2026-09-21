using BurstTimers;

var timer = new StatusCooldownTimer();
int checks = 0;
void Check(double now, double expected, string scenario)
{
    var actual = timer.Remaining(now);
    if (Math.Abs(actual - expected) > 0.001)
        throw new Exception($"{scenario}: expected {expected}, got {actual}");
    checks++;
}

timer.Update(10, false);
Check(10, 0, "No debuff");
timer.Update(20, true);
Check(20, 120, "First detection");
timer.Update(25, true);
Check(25, 115, "Repeated detection");
timer.Update(30, false);
Check(30, 110, "Lost target/debuff");
timer.Update(40, true);
Check(40, 100, "Second application or new target while active");
timer.Update(140, true);
Check(140, 0, "No restart from uninterrupted detection");
timer.Update(141, false);
timer.Update(142, true);
Check(142, 120, "New detection after expiration");
timer.Update(150, false);
timer.Update(261, true);
Check(261, 1, "Application immediately before expiration ignored");
timer.Update(263, true);
Check(263, 0, "Ignored application does not queue another timer");
timer.Reset();
Check(263, 0, "Logout clears timer");
timer.Update(264, true);
Check(264, 120, "Detection after reset");
Check(1000, 0, "Remaining never negative");
var buffs = PartyBuffTracker.CreateAll();
void CheckBuff(int index, double now, double expected, string scenario)
{
    if (Math.Abs(buffs[index].Timer.Remaining(now) - expected) > 0.001)
        throw new Exception(scenario);
    checks++;
}
void Observe(double now, params uint[] statuses)
{
    var active = statuses.ToHashSet();
    foreach (var buff in buffs)
        buff.Update(now, active);
}
Observe(10, 1182, 9999);
for (int i = 0; i < buffs.Length; i++)
    CheckBuff(i, 10, 0, "Unrelated statuses do not start timers");
Observe(20, 1297, 3685, 1185, 2703);
for (int i = 0; i < buffs.Length; i++)
    CheckBuff(i, 20, 120, "All four party buffs detected simultaneously");
Observe(25, 1239, 1297, 1297, 3685, 1185, 2703);
for (int i = 0; i < buffs.Length; i++)
    CheckBuff(i, 25, 115, "Duplicate or self/party statuses do not restart timers");
Observe(50);
Observe(60, 1297);
CheckBuff(0, 60, 80, "Second caster cannot restart Embolden");
CheckBuff(1, 60, 80, "Other buffs continue after statuses disappear");
Observe(140);
Observe(150, 1239);
CheckBuff(0, 150, 120, "Self Embolden starts next cycle");
for (int i = 1; i < buffs.Length; i++)
    CheckBuff(i, 150, 0, "Expired buffs stay independent");
foreach (var buff in buffs) buff.Timer.Reset();
for (int i = 0; i < buffs.Length; i++)
    CheckBuff(i, 151, 0, "Logout clears all party timers");
var effects = new ActiveEffects();
void CheckDuration(float actual, float expected, string scenario)
{
    if (Math.Abs(actual - expected) > 0.001f) throw new Exception(scenario);
    checks++;
}
effects.Observe(1297, 17);
effects.Observe(1297, 9);
effects.Observe(1239, 12);
CheckDuration(buffs[0].DurationRemaining(effects), 17, "Duplicate buffs use longest remaining duration");
effects.Clear();
effects.Observe(1297, 8);
CheckDuration(buffs[0].DurationRemaining(effects), 8, "Duration follows current game status");
effects.Clear();
effects.Observe(1297, 20);
CheckDuration(buffs[0].DurationRemaining(effects), 20, "Reapplication refreshes duration independently of cooldown");
effects.Clear();
CheckDuration(buffs[0].DurationRemaining(effects), 0, "Removed buff disappears immediately");
effects.Observe(3849, 14);
CheckDuration(effects.Remaining(3849), 14, "Dokumori duration from current target");
effects.Clear();
CheckDuration(effects.Remaining(3849), 0, "Changing or losing target clears duration");
effects.Observe(49, 25);
CheckDuration(effects.Remaining(49), 25, "Potion effect duration independent of recast");
effects.Observe(3685, float.NaN);
effects.Observe(3685, float.PositiveInfinity);
effects.Observe(3685, -1);
CheckDuration(effects.Remaining(3685), 0, "Invalid or expired durations ignored");
Console.WriteLine($"Passed {checks} cooldown, party buff, and active duration regression checks.");
