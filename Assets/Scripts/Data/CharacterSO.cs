using System.Collections.Generic;
using UnityEngine;

// One named visitor. Their order (favorite stew) and napkin live on the same card as their portrait —
// clue & answer & words can never desync. Adding/reordering visitors = data only, zero code.
[CreateAssetMenu(fileName = "CHAR_", menuName = "Tavern Stew/Character")]
public class CharacterSO : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    [Tooltip("Hand-drawn front-view portrait shown in the tavern — the visual is the ONLY order clue")]
    public Sprite portrait;

    [Header("Their fixed order — the favorite stew (Honeymead is the always-served mug, not a slot)")]
    public IngredientSO main;
    public IngredientSO side;
    public IngredientSO sauce;

    [Header("Napkin left after being served — writer's text VERBATIM, never edit in code")]
    [TextArea(3, 8)] public string napkinText;

    // The secret craving, in the same shape GameManager/ScoringService already speak.
    public Dictionary<SlotType, IngredientSO> ToOrder() => new Dictionary<SlotType, IngredientSO>
    {
        { SlotType.Main,  main  },
        { SlotType.Side,  side  },
        { SlotType.Sauce, sauce },
    };
}
