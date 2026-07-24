using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Draws 1 SO per slot from the currently unlocked pool. Remembers nothing about past customers.
public class CustomerGenerator : MonoBehaviour
{
    [Header("All 10 ingredient cards")]
    [SerializeField] private List<IngredientSO> allIngredients;
    [SerializeField] private DebugConfigSO debug;   // optional — AllJars cheat

    private readonly List<IngredientSO> unlocked = new List<IngredientSO>();

    private void Awake()
    {
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
        var order = new Dictionary<SlotType, IngredientSO>();
        foreach (SlotType slot in System.Enum.GetValues(typeof(SlotType)))
        {
            var pool = unlocked.Where(i => i.slot == slot).ToList();
            order[slot] = pool[Random.Range(0, pool.Count)];
        }
        return order;
    }
}
