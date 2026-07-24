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

    [Header("Unlock beats — index-aligned with GameConfigSO.unlockTimes")]
    public UnlockBeat[] unlockBeats;

    [Header("Printed on every napkin")]
    public string napkinHeader = "Tell us what you thought of the food!";

    [System.Serializable]
    public class UnlockBeat
    {
        [Tooltip("Direct SO reference — no string lookups to break")]
        public IngredientSO ingredientToUnlock;
        [TextArea(1, 2)] public string toastLine;

        [Header("Napkin (leave hasNapkin off for Pixie Dust — do not invent text)")]
        public bool hasNapkin;
        public string guestName;
        [Tooltip("Writer's text VERBATIM — never edit here")]
        [TextArea(3, 8)] public string napkinText;

        [Header("Dormant — only used if depth-ladder guest busts happen")]
        public IngredientSO[] idealStew;
    }
}
