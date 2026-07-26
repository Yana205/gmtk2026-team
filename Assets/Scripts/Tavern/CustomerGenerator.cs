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

    [Header("Encore — after the last scripted visitor, the first returns craving the final catch")]
    [Tooltip("Without this the last catch (Kraken) unlocks as the night ends and is never usable")]
    [SerializeField] private bool encoreVisit = true;

    private readonly List<IngredientSO> unlocked = new List<IngredientSO>();

    private int visitorIndex = -1;
    private IngredientSO lastUnlocked;   // the encore guest's craving
    private bool encoreDrawn;

    public bool FixedMode => visitors != null && visitors.Count > 0;
    public bool HasNext    => FixedMode && (visitorIndex + 1 < visitors.Count || (encoreVisit && !encoreDrawn));
    public bool IsEncore  { get; private set; }
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
        if (ing == null) return;
        if (!unlocked.Contains(ing)) unlocked.Add(ing);
        lastUnlocked = ing;
    }

    public Dictionary<SlotType, IngredientSO> Draw()
    {
        if (FixedMode)
        {
            visitorIndex++;
            if (visitorIndex >= visitors.Count)
            {
                // Encore: the first guest returns, and their craving swaps to today's final catch —
                // that's the whole point of the visit (otherwise the Kraken unlocks into a dead night).
                encoreDrawn = true;
                IsEncore = true;
                CurrentCharacter = visitors[0];
                var encoreOrder = CurrentCharacter.ToOrder();
                if (lastUnlocked != null) encoreOrder[lastUnlocked.slot] = lastUnlocked;
                return encoreOrder;
            }
            IsEncore = false;
            CurrentCharacter = visitors[visitorIndex];
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
