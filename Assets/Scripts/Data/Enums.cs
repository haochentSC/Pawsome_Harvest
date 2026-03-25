/// <summary>
/// Shared enums used across multiple scripts.
/// Pure C# -- no MonoBehaviour, no Unity imports.
/// </summary>

public enum PotState
{
    Empty,    // no crop planted; shows ButtonPlant
    Seeded,   // 1-frame transitional state; triggers EaseScale on seedling model
    Growing,  // timer counting down; contributes to rate; shows tap button + cooldown ring
    Mature    // fully grown; contributes to rate; shows ButtonHarvest
}

public enum UpgradeType
{
    SoilQuality,  // multiplies money rate:  1.0 / 1.5 / 2.25 / 3.375
    GrowLights,   // multiplies money rate:  1.0 / 1.2 / 1.4  / 1.6
    Irrigation    // divides grow time:      1.0 / 1.3 / 1.7  / 2.2
}
