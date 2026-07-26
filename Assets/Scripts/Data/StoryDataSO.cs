using UnityEngine;

// ALL narrative is data. The writer's words go in VERBATIM. Adding a guest later = pasting text, zero code.
[CreateAssetMenu(fileName = "StoryData", menuName = "Tavern Stew/Story Data")]
public class StoryDataSO : ScriptableObject
{
    [Header("Intro card — placeholder until writer approves")]
    [TextArea(2, 4)] public string introLine1;
    [TextArea(2, 4)] public string introLine2;

    [Header("Ending lines — index-aligned with GameConfigSO.rankLetters (C,B,A,S)")]
    [TextArea(2, 4)] public string[] endingLinesByRank = new string[4];

    [Header("Banners")]
    public string lastCallBanner = "LAST CALL! Coins x2";
    [Tooltip("Shown when the first guest returns after the scripted list — they crave today's catch")]
    public string encoreBanner = "The first guest is back — craving today's catch!";

    [Header("Unlock beats — index-aligned with GameConfigSO.unlockTimes")]
    public UnlockBeat[] unlockBeats;

    [Header("Printed on every napkin")]
    public string napkinHeader = "Tell us what you thought of the food!";

    [Header("Reaction napkins — lore left by every served customer, picked by hearts")]
    [TextArea(1, 3)] public string[] reactionNapkinsGood;   // 3 hearts
    [TextArea(1, 3)] public string[] reactionNapkinsMid;    // 1-2 hearts
    [TextArea(1, 3)] public string[] reactionNapkinsBad;    // 0 hearts (served, all wrong)
    [Tooltip("Guest names cycle in order as customers are served")]
    public string[] guestNamePool;

    // Data-owned lookup: null when no lines are authored yet (feature stays dormant).
    public UnlockBeat MakeReactionNapkin(int hearts, int coins, int servedCount)
    {
        string[] pool = hearts >= 3 ? reactionNapkinsGood : hearts > 0 ? reactionNapkinsMid : reactionNapkinsBad;
        if (pool == null || pool.Length == 0) return null;
        var beat = new UnlockBeat
        {
            hasNapkin  = true,
            guestName  = guestNamePool != null && guestNamePool.Length > 0
                         ? guestNamePool[servedCount % guestNamePool.Length] : "A customer",
            napkinText = pool[Random.Range(0, pool.Length)]
                         + "\n\n(" + hearts + "/3 hearts · +" + coins + " coins)",
        };
        return beat;
    }

    [System.Serializable]
    public class UnlockBeat
    {
        [Tooltip("Direct SO reference — no string lookups to break")]
        public IngredientSO ingredientToUnlock;
        [Tooltip("Poster art for the tavern 'Catch of the Day' sign (CATCH OF THE DAY RAT/FOX/KRAKEN)")]
        public Sprite signArt;
        [TextArea(1, 2)] public string toastLine;

        [Header("Napkin (leave hasNapkin off for Pixie Dust — do not invent text)")]
        public bool hasNapkin;
        public string guestName;
        [Tooltip("Writer's text VERBATIM — never edit here")]
        [TextArea(3, 8)] public string napkinText;
        [Tooltip("Small portrait shown on the open napkin — set from CharacterSO for named visitors")]
        public Sprite portrait;

        [Header("Dormant — only used if depth-ladder guest busts happen")]
        public IngredientSO[] idealStew;
    }
}
