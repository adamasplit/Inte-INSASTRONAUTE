public static class GameConstants
{
    // Deck
    public const int MaxDeckSize = 12;
    public const int MaxCardsInHand = 5;
    public const int MaxCopiesPerCard = 2;

    // Scoring & economy
    public const int EnemyScoreValue = 10;
    public const int ScorePerToken = 10;

    // Spawning & waves
    public const float DefaultSpawnInterval = 2f;
    public const float MinSpawnInterval = 0.25f;
    public const float SpawnIntervalDecrement = 0.05f;
    public const float WaveDescentInterval = 3f;
    public const float WaveDescentStep = 0.5f;
    public const float LoseThresholdY = -1.2f;
    public const float MinDescentInterval = 0.25f;

    // Enemies
    public const int PrismaticSpawnOdds = 20; // 1 in 20
    public const float EnemyTeleportThreshold = 5f;
    public const float EnemyDeathDelay = 0.8f;

    // Grid
    public const int ColumnCount = 5;
}
