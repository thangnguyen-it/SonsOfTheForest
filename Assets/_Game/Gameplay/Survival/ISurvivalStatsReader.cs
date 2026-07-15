namespace SonsOfTheForest.Gameplay.Survival
{
    public interface ISurvivalStatsReader
    {
        bool TryGetStat(SurvivalStatKind kind, out StatValue value);
    }
}
