using UnityEngine;

// Everything you FEEL at the front desk. One asset — tune while playing, values persist after you stop.
[CreateAssetMenu(fileName = "TavernFeel", menuName = "Tavern Stew/Feel - Tavern (Front Desk)")]
public class TavernFeelSO : ScriptableObject
{
    [Header("Customer entrance & exit — read by CustomerView")]
    [Range(0.05f, 2f)] public float fadeInSeconds  = 0.35f;
    [Range(0.05f, 2f)] public float fadeOutSeconds = 0.25f;
    [Range(0f, 100f)]  public float happyHopHeight = 30f;
    [Range(0.05f, 1f)] public float happyHopSeconds = 0.30f;
    [Range(0f, 60f)]   public float sadDroopPixels = 15f;
    [Tooltip("Hearts needed for the happy face + hop. Below this (but >0) = neutral, 0 = sad")]
    [Range(1, 3)]      public int happyFaceMinHearts = 3;

    [Header("Reaction beat — read by GameManager's coroutine + ReactionFX")]
    [Range(0.3f, 3f)]    public float reactionTotalSeconds = 1.2f;
    [Range(0.02f, 0.6f)] public float heartPopInterval = 0.15f;
    [Range(1f, 1.6f)]    public float heartPopScale = 1.3f;
    [Range(0.1f, 2f)]    public float coinFlySeconds = 0.5f;
    [Range(0f, 200f)]    public float coinRisePixels = 60f;

    [Header("Patience bar — read by PatienceMeter")]
    [Tooltip("Fraction below which the bar turns urgent")]
    [Range(0f, 1f)] public float warningThreshold = 0.3f;
    public Color barColorFull    = new Color(0.45f, 0.75f, 0.35f);
    public Color barColorWarning = new Color(0.85f, 0.30f, 0.25f);

    [Header("Banners & toasts — read by ToastBanner")]
    [Range(0.05f, 1f)] public float bannerSlideSeconds = 0.25f;
    [Range(0.5f, 5f)]  public float toastHoldSeconds = 2.5f;

    [Header("Motion character — every tavern tween samples this curve")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
