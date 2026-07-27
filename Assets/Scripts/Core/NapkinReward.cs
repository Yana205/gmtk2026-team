using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Turns a serve result into the napkin the guest leaves. The writer's words are never rewritten —
// a poor serve just earns FEWER of them (the tail hides behind ???), and only a near-miss earns
// a colored nudge toward what the stew was missing. A perfect serve gets the full lore, untouched.
// Static pure functions like ScoringService — trivially testable, no scene refs.
public static class NapkinReward
{
    private const string HintColor = "#B3541E";   // warm ink, readable on the pale napkin

    private static readonly SlotType[] SlotOrder = { SlotType.Main, SlotType.Side, SlotType.Sauce };

    public static string Compose(string fullText, int hearts,
        Dictionary<SlotType, IngredientSO> order,
        Dictionary<SlotType, IngredientSO> picks)
    {
        return Redact(fullText, hearts) + ReceiptLine(hearts, order, picks);
    }

    // 3 hearts keeps every word; below that the tail is hidden, not rewritten.
    private static string Redact(string text, int hearts)
    {
        if (hearts >= 3 || string.IsNullOrEmpty(text)) return text;
        var words = text.Split(' ');
        float keepFraction = hearts == 2 ? 0.6f : hearts == 1 ? 0.3f : 0.12f;
        int keep = Mathf.Clamp(Mathf.CeilToInt(words.Length * keepFraction), 1, words.Length - 1);
        return string.Join(" ", words, 0, keep) + " ... ???";
    }

    // The receipt under the lore. Matched slots are named plainly; missed slots hide behind ??? —
    // except the near-miss reveal: 2 hearts names its one miss in color, 1 heart names only the
    // first of two, 0 hearts names nothing. The worse the serve, the smaller the hint.
    private static string ReceiptLine(int hearts,
        Dictionary<SlotType, IngredientSO> order,
        Dictionary<SlotType, IngredientSO> picks)
    {
        var sb = new StringBuilder("\n\n<size=80%>(").Append(hearts).Append("/3 hearts)");
        if (hearts < 3 && order != null)
        {
            int reveals = hearts > 0 ? 1 : 0;
            sb.Append("   wanted: ");
            bool first = true;
            foreach (var slot in SlotOrder)
            {
                if (!order.TryGetValue(slot, out var want) || want == null) continue;
                if (!first) sb.Append(", ");
                first = false;
                bool hit = picks != null && picks.TryGetValue(slot, out var got) && got == want;
                if (hit) sb.Append(want.displayName);
                else if (reveals-- > 0) sb.Append("<color=").Append(HintColor).Append(">").Append(want.displayName).Append("</color>");
                else sb.Append("???");
            }
        }
        sb.Append("</size>");
        return sb.ToString();
    }
}
