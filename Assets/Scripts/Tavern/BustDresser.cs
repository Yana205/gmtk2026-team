using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Reusable dressing component: used by the tavern Customer AND the kitchen Portrait (small instance).
// Missing sprite -> solid rectangle tinted with the ingredient's placeholderColor (grey-box mode, free).
public class BustDresser : MonoBehaviour
{
    [Header("Layer images")]
    [SerializeField] private Image hatLayer;    // Main  (hat)
    [SerializeField] private Image sideLayer;   // Side  (shoulder garment)
    [SerializeField] private Image sauceLayer;  // Sauce (face-level accessory)
    [SerializeField] private Image face;        // optional (portrait can skip it)

    [Header("Face sprites (optional in grey-box)")]
    [SerializeField] private Sprite faceNeutral;
    [SerializeField] private Sprite faceHappy;
    [SerializeField] private Sprite faceSad;

    public void Dress(Dictionary<SlotType, IngredientSO> order)
    {
        SetLayer(hatLayer,   order, SlotType.Main);
        SetLayer(sideLayer,  order, SlotType.Side);
        SetLayer(sauceLayer, order, SlotType.Sauce);
        if (face && faceNeutral) face.sprite = faceNeutral;
    }

    public void SetFace(int hearts, int happyMin)
    {
        if (!face) return;
        Sprite s = hearts >= happyMin ? faceHappy : hearts == 0 ? faceSad : faceNeutral;
        if (s) face.sprite = s;
    }

    private void SetLayer(Image img, Dictionary<SlotType, IngredientSO> order, SlotType slot)
    {
        bool has = order != null && order.TryGetValue(slot, out var ing) && ing != null;
        img.enabled = has;
        if (!has) return;
        var i = order[slot];
        img.sprite = i.wornSprite;
        img.color  = i.wornSprite ? Color.white : i.placeholderColor;
    }
}
