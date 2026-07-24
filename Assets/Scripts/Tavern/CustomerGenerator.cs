using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Two modes. FIXED: a scripted list of named visitors, served in order (the shipping night).
// RANDOM: draws 1 SO per slot from the unlocked pool (the old grey-box loop). Data decides which.
public class CustomerGenerator : MonoBehaviour
{
    [Header("Fixed visitors — served IN ORDER. Non-empty = fixed mode (overrides the random draw).")]
    [SerializeField] private List<CharacterSO> visitors;

    [Header("Random mode — all 10 ingredient cards (used only when no visitors are set)")]
    [SerializeField] private List<IngredientSO> allIngredients;
    [SerializeField] private DebugConfigSO debug;   // optional — AllJars cheat

    private readonly List<IngredientSO> unlocked = new List<IngredientSO>();

    private int visitorIndex = -1;

    public bool FixedMode => visitors != null && visitors.Count > 0;
    public bool HasNext    => FixedMode && visitorIndex + 1 < visitors.Count;
    public CharacterSO CurrentCharacter { get; private set; }

    private void Awake()
    {
        if (allIngredients == null) return;
        foreach (var ing in allIngredients)
            if (!ing.startsLocked || (debug && debug.AllJarsOn))
                unlocked.Add(ing);
    }

    public void Unlock(IngredientSO ing)
    {
        if (ing != null && !unlocked.Contains(ing)) unlocked.Add(ing);
    }

    public Dictionary<SlotType, IngredientSO> Draw()
    {
        if (FixedMode)
        {
            visitorIndex++;
            CurrentCharacter = visitors[Mathf.Min(visitorIndex, visitors.Count - 1)];
            return CurrentCharacter.ToOrder();
        }

        CurrentCharacter = null;
        var order = new Dictionary<SlotType, IngredientSO>();
        foreach (SlotType slot in System.Enum.GetValues(typeof(SlotType)))
        {
            var pool = unlocked.Where(i => i.slot == slot).ToList();
            order[slot] = pool[Random.Range(0, pool.Count)];
        }
        return order;
    }
}
