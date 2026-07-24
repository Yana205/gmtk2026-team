using UnityEngine;

// Everything you FEEL while cooking. Separate from TavernFeel so each screen tunes from one asset.
[CreateAssetMenu(fileName = "KitchenFeel", menuName = "Tavern Stew/Feel - Kitchen")]
public class KitchenFeelSO : ScriptableObject
{
    [Header("Jar click — read by IngredientJar")]
    [Tooltip("Jar scale on click — 1.12 = 12% punch")]
    [Range(1f, 1.5f)]    public float jarPunchScale   = 1.12f;
    [Range(0.02f, 0.5f)] public float jarPunchSeconds = 0.12f;

    [Header("Ingredient arc into the pot — read by StewBuilder")]
    [Range(0.05f, 1.5f)] public float arcSeconds = 0.35f;
    [Range(0f, 400f)]    public float arcHeight  = 140f;

    [Header("Pot response — read by StewBuilder")]
    [Range(0f, 40f)]     public float potBouncePixels  = 12f;
    [Range(0.02f, 0.5f)] public float potBounceSeconds = 0.15f;

    [Header("Serve button — pops when it unlocks")]
    [Range(1f, 1.5f)]    public float servePopScale   = 1.15f;
    [Range(0.02f, 0.5f)] public float servePopSeconds = 0.15f;

    [Header("Motion character — every kitchen tween samples this curve")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
