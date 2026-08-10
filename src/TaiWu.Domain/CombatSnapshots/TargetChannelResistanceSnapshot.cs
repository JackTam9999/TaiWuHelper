namespace TaiWu.Domain.CombatSnapshots;

public sealed record TargetChannelResistanceSnapshot
{
    public TargetChannelResistanceSnapshot(int outer, int inner)
    {
        if (outer <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outer),
                outer,
                "Base outer resistance must be positive when available.");
        }

        if (inner <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inner),
                inner,
                "Base inner resistance must be positive when available.");
        }

        Outer = outer;
        Inner = inner;
    }

    public int Outer { get; }

    public int Inner { get; }

    public bool IsAsymmetric => Outer != Inner;
}
