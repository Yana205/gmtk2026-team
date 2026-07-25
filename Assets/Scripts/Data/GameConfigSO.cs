using UnityEngine;

// ALL balance numbers. Zero hardcoded numbers anywhere else. Tune Saturday — edits during Play Mode persist.
[CreateAssetMenu(fileName = "GameConfig", menuName = "Tavern Stew/Game Config")]
public class GameConfigSO : ScriptableObject
{
    [Header("Night")]
    [Range(60f, 600f)] public float nightSeconds = 180f;
    [Range(10f, 60f)]  public float lastCallSeconds = 30f;

    [Header("Pacing")]
    [Tooltip("Breathing room between one customer leaving and the next entering")]
    [Range(0f, 3f)] public float delayBetweenCustomers = 0.5f;

    [Header("Serving")]
    [Tooltip("Slots required before Serve unlocks. 3 = original locked plan. Lower = partial serves allowed (fewer hearts, player's gamble)")]
    [Range(0, 3)] public int minSlotsToServe = 3;

    [Header("Coins")]
    public int coinsPerHeart = 5;
    public int lastCallMultiplier = 2;

    [Header("Ranks — total hearts needed, index-aligned with rankLetters")]
    public int[]    rankThresholds = { 0, 8, 14, 20 };
    public string[] rankLetters    = { "C", "B", "A", "S" };

    [Header("Unlocks — elapsed seconds; index i pairs with StoryDataSO.unlockBeats[i]")]
    public float[] unlockTimes = { 60f, 105f, 135f };
}
