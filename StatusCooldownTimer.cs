namespace BurstTimers;

// Uses monotonic seconds, so changing the system clock cannot shift a countdown.
public sealed class StatusCooldownTimer
{
    private double expiresAt;
    private bool observed;
    public double Remaining(double now) => Math.Max(0, expiresAt - now);

    public void Update(double now, bool present)
    {
        if (present && !observed && Remaining(now) <= 0)
            expiresAt = now + 120;
        observed = present;
    }

    public void Reset()
    {
        expiresAt = 0;
        observed = false;
    }
}
