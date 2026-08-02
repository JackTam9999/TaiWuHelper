namespace TaiWu.Domain.CombatRecommendations;

public sealed record CandidateDamageEvidence
{
    public CandidateDamageEvidence(
        string candidateStableKey,
        decimal score,
        string evidenceReference)
    {
        if (string.IsNullOrWhiteSpace(candidateStableKey))
        {
            throw new ArgumentException(
                "Candidate stable key cannot be blank.",
                nameof(candidateStableKey));
        }

        if (score is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(score),
                score,
                "Damage score must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "Damage evidence requires a reference.",
                nameof(evidenceReference));
        }

        CandidateStableKey = candidateStableKey;
        Score = score;
        EvidenceReference = evidenceReference.Trim();
    }

    public string CandidateStableKey { get; }

    public decimal Score { get; }

    public string EvidenceReference { get; }
}
