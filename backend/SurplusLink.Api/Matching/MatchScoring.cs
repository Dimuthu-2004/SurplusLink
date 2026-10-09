namespace SurplusLink.Api.Matching;

public static class MatchScoring
{
    public static int ConditionRank(string condition) => condition.ToUpperInvariant() switch
    {
        "NEW" or "EXCELLENT" => 4, "GOOD" => 3, "FAIR" => 2, "POOR" => 1, _ => 0
    };

    // Missing logistics have no final score; they are never free transport.
    public static decimal Score(string condition, decimal materialCost, decimal budget,
        decimal? distance, decimal? transport, int matchedPreferences = 0, int consideredPreferences = 0)
    {
        if (distance is not >= 0 || transport is not >= 0 || budget <= 0) return 0m;

        var quality = ConditionRank(condition) / 4m;
        var costHeadroom = Math.Max(0, 1 - (materialCost + transport.Value) / budget);
        var hasPreferences = consideredPreferences > 0;
        var preferenceFit = hasPreferences
            ? Math.Clamp(matchedPreferences, 0, consideredPreferences) / (decimal)consideredPreferences : 0m;
        return Math.Round(hasPreferences
            ? .3m * quality + .3m * preferenceFit + .25m * costHeadroom + .15m / (1 + distance.Value / 100m)
            : .5m * quality + .3m * costHeadroom + .2m / (1 + distance.Value / 100m), 4, MidpointRounding.ToEven);
    }
}
