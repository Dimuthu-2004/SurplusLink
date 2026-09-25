namespace SurplusLink.Api.Matching;

public static class MatchScoring
{
    public static int ConditionRank(string condition) => condition.ToUpperInvariant() switch
    {
        "NEW" or "EXCELLENT" => 4, "GOOD" => 3, "FAIR" => 2, "POOR" => 1, _ => 0
    };

    // Missing logistics have no final score; they are never free transport.
    public static decimal Score(string condition, decimal materialCost, decimal budget,
        decimal? distance, decimal? transport) => distance is >= 0 && transport is >= 0 && budget > 0
        ? Math.Round(.5m * ConditionRank(condition) / 4m +
            .3m * Math.Max(0, 1 - (materialCost + transport.Value) / budget) +
            .2m / (1 + distance.Value / 100m), 4, MidpointRounding.ToEven)
        : 0m;
}
