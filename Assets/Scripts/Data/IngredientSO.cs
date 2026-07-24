using UnityEngine;

// One card per ingredient (x10 assets). Clue & answer live on the same card - can never desync.
[CreateAssetMenu(fileName = "ING_", menuName = "Tavern Stew/Ingredient")]
public class IngredientSO : ScriptableObject
{
    [Header("Identity")]
    public string displayName;                  // "Kraken"
    public SlotType slot;

    [Header("Sprites — clue & answer on the same card")]
    [Tooltip("Icon on the shelf jar")]
    public Sprite jarSprite;
    [Tooltip("Overlay layer inside the pot")]
    public Sprite stewSprite;
    [Tooltip("Prop worn by the customer bust")]
    public Sprite wornSprite;

    [Header("Unlock")]
    [Tooltip("True for Kraken, Dragon, Pixie Dust — hidden until their unlock beat")]
    public bool startsLocked;

    [Header("Grey-box")]
    [Tooltip("Tint used wherever a sprite is still missing — unique color per ingredient = playable M1 with zero art")]
    public Color placeholderColor = Color.white;
}
