using System;

/// <summary>
/// Inserts a short presentation-only charge before a Kael ultimate resolves. Combat time,
/// cooldowns and scheduled hits receive only the unconsumed portion of each frame.
/// </summary>
public sealed class KaelUltimateFocusClock
{
    public const float Duration = .62f;
    public const float ChargePoseAge = .32f;
    public const float ImpactAge = .4f;
    public const float ImpactHoldDuration = .08f;

    private double elapsed;
    private float startActionAge;
    private long latestSequence = -1;
    private long lastImpactSequence = -1;
    private double impactHoldRemaining;
    public bool IsActive { get; private set; }
    public float Age => (float)elapsed;
    public float FrameDelta { get; private set; }
    public long Sequence { get; private set; } = -1;
    public int StartCount { get; private set; }
    public bool IsImpactHoldActive => impactHoldRemaining > 0.0;
    public float ImpactHoldFrameDelta { get; private set; }
    public int ImpactHoldCount { get; private set; }

    public bool Begin(long sequence, float authoritativeActionAge)
    {
        // Late legacy records have already resolved their hit and cannot introduce a charge.
        if (sequence <= latestSequence || float.IsNaN(authoritativeActionAge) ||
            authoritativeActionAge < 0f || authoritativeActionAge >= ImpactAge) return false;
        Sequence = latestSequence = sequence;
        startActionAge = authoritativeActionAge;
        elapsed = FrameDelta = 0f;
        IsActive = true;
        StartCount++;
        return true;
    }

    public float Consume(float frameDelta)
    {
        FrameDelta = ImpactHoldFrameDelta = 0f;
        if (float.IsNaN(frameDelta) || float.IsInfinity(frameDelta) || frameDelta <= 0f) return 0f;
        if (!IsActive)
        {
            if (!IsImpactHoldActive) return frameDelta;
            var hold = Math.Min(frameDelta, impactHoldRemaining);
            impactHoldRemaining -= hold;
            ImpactHoldFrameDelta = (float)hold;
            if (impactHoldRemaining < .0000001) impactHoldRemaining = 0.0;
            return Math.Max(0f, frameDelta - ImpactHoldFrameDelta);
        }
        var consumed = Math.Min(frameDelta, Duration - elapsed);
        elapsed += consumed;
        FrameDelta = (float)consumed;
        if (Duration - elapsed < .0000001) { elapsed = Duration; IsActive = false; }
        return Math.Max(0f, frameDelta - FrameDelta);
    }

    public bool HoldResolvedImpact(long sequence)
    {
        if (IsActive || sequence != Sequence || sequence <= lastImpactSequence) return false;
        lastImpactSequence = sequence;
        impactHoldRemaining = ImpactHoldDuration;
        ImpactHoldCount++;
        return true;
    }

    public void BeginEndPose()
    {
        // A killing ultimate still needs its hit hold and focus fade. An unfinished
        // charge belongs to a cancelled action and must not survive the battle result.
        if (Sequence < 0 || lastImpactSequence != Sequence) Cancel();
    }

    public float MapActionAge(long sequence, float authoritativeActionAge)
    {
        if (sequence != Sequence || authoritativeActionAge >= ImpactAge || authoritativeActionAge < 0f)
            return authoritativeActionAge;
        var heldPoseAge = Math.Max(startActionAge, ChargePoseAge);
        if (IsActive)
        {
            var progress = Math.Min(1f, Age / Duration);
            var eased = progress * progress * (3f - 2f * progress);
            return startActionAge + (heldPoseAge - startActionAge) * eased;
        }
        // The extra charge ends at a held wind-up. Compress only the remaining lead-in;
        // the blade still reaches its contact pose at the authoritative 400ms impact.
        var recovery = Math.Max(0f, Math.Min(1f,
            (authoritativeActionAge - startActionAge) / (ImpactAge - startActionAge)));
        return heldPoseAge + (ImpactAge - heldPoseAge) * recovery;
    }

    public void Cancel()
    {
        IsActive = false;
        elapsed = FrameDelta = 0f;
        impactHoldRemaining = ImpactHoldFrameDelta = 0f;
        Sequence = -1;
        startActionAge = 0f;
    }

    public void Reset()
    {
        Cancel();
        latestSequence = -1;
        lastImpactSequence = -1;
        StartCount = 0;
        ImpactHoldCount = 0;
    }
}
