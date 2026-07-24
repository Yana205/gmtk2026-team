using System.Collections.Generic;
using UnityEngine;

// Static pure functions. No state, no scene refs — trivially testable, impossible to break from the inspector.
public static class ScoringService
{
    public struct Result { public int hearts; public int coins; }

    public static Result Score(
        Dictionary<SlotType, IngredientSO> order,
        Dictionary<SlotType, IngredientSO> picks,
        float patienceFraction, bool isLastCall, GameConfigSO config)
    {
        int hearts = 0;
        foreach (var kv in order)
            if (picks != null && picks.TryGetValue(kv.Key, out var p) && p == kv.Value)
                hearts++;

        int coins = hearts * config.coinsPerHeart;
        if (patienceFraction > config.patienceBonusThreshold) coins += config.patienceBonusCoins;
        if (isLastCall) coins *= config.lastCallMultiplier;

        return new Result { hearts = hearts, coins = coins };
    }

    public static int RankIndex(int totalHearts, GameConfigSO config)
    {
        int idx = 0;
        for (int i = 0; i < config.rankThresholds.Length; i++)
            if (totalHearts >= config.rankThresholds[i]) idx = i;
        return Mathf.Min(idx, config.rankLetters.Length - 1);
    }
}
