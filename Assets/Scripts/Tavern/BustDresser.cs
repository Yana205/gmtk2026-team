using System.Collections.Generic;
using TMPro;
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

    [Header("Grey-box name labels (optional — real art replaces these)")]
    [SerializeField] private TMP_Text hatLabel;
    [SerializeField] private TMP_Text sideLabel;
    [SerializeField] private TMP_Text sauceLabel;

    [Header("Face sprites (optional in grey-box)")]
    [SerializeField] private Sprite faceNeutral;
    [SerializeField] private Sprite faceHappy;
    [SerializeField] private Sprite faceSad;

    [Header("Portrait mode — whole-character art (real visitors). Hides the dressing layers.")]
    [SerializeField] private Image portraitImage;   // the bust Base image

    // Real named visitor: show their one hand-drawn portrait, hide the procedural dressing.
    public void ShowPortrait(Sprite sprite, Color fallbackTint)
    {
        Dress(null);                 // disables hat/side/sauce layers + their labels
        if (!portraitImage) return;
        portraitImage.enabled = true;
        portraitImage.sprite  = sprite;
        portraitImage.color   = sprite ? Color.white : fallbackTint;
    }

    public void Dress(Dictionary<SlotType, IngredientSO> order)
    {
        SetLayer(hatLayer,   hatLabel,   order, SlotType.Main);
        SetLayer(sideLayer,  sideLabel,  order, SlotType.Side);
        SetLayer(sauceLayer, sauceLabel, order, SlotType.Sauce);
        if (face && faceNeutral) face.sprite = faceNeutral;
    }

    public void SetFace(int hearts, int happyMin)
    {
        if (!face) return;
        Sprite s = hearts >= happyMin ? faceHappy : hearts == 0 ? faceSad : faceNeutral;
        if (s) face.sprite = s;
    }

    private void SetLayer(Image img, TMP_Text label, Dictionary<SlotType, IngredientSO> order, SlotType slot)
    {
        if (!img) return;   // optional layer — portrait-only busts (real visitors) leave these unassigned
        bool has = order != null && order.TryGetValue(slot, out var ing) && ing != null;
        img.enabled = has;
        if (label) label.enabled = has;
        if (!has) return;
        var i = order[slot];
        img.sprite = i.wornSprite;
        img.color  = i.wornSprite ? Color.white : i.placeholderColor;
        if (label)
        {
            label.text = i.displayName;
            // readable on any placeholder tint
            float lum = 0.299f * img.color.r + 0.587f * img.color.g + 0.114f * img.color.b;
            label.color = lum > 0.5f ? new Color(.12f, .10f, .08f) : new Color(.95f, .93f, .88f);
        }
    }
}
