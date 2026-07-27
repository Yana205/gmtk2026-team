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

    [Header("Entrance/exit motion v2 — read by CustomerView")]
    [Tooltip("How far below home the bust starts before sliding up on entrance")]
    [Range(0f, 250f)]  public float enterRisePixels = 70f;
    [Tooltip("How far the bust drifts as it leaves (up when happy, down when sad) while fading out")]
    [Range(0f, 250f)]  public float exitDriftPixels = 60f;
    [Tooltip("Seconds to ease into the sad droop (0-heart reaction)")]
    [Range(0.05f, 1f)] public float sadDroopSeconds = 0.35f;

    [Header("Idle life while ordering — read by CustomerView")]
    [Tooltip("Vertical bob amplitude of the waiting bust (0 = off)")]
    [Range(0f, 30f)]   public float idleBobPixels = 5f;
    [Tooltip("Seconds for one full bob cycle")]
    [Range(0.5f, 4f)]  public float idleBobSeconds = 2.2f;

    [Header("Reaction beat — read by GameManager's coroutine + ReactionFX")]
    [Range(0.3f, 3f)]    public float reactionTotalSeconds = 1.2f;
    [Range(0.02f, 0.6f)] public float heartPopInterval = 0.15f;
    [Range(1f, 1.6f)]    public float heartPopScale = 1.3f;
    [Range(0.1f, 2f)]    public float coinFlySeconds = 0.5f;
    [Range(0f, 200f)]    public float coinRisePixels = 60f;
    [Tooltip("All 3 heart slots always show; earned = full colour, the rest sit dimmed as empty slots")]
    public Color heartFullColor  = new Color(0.90f, 0.25f, 0.35f, 1f);
    public Color heartEmptyColor = new Color(0.35f, 0.35f, 0.40f, 0.5f);

    [Header("Unhappy buzz — 0-1 heart reactions shake the bust (read by CustomerView)")]
    [Range(0f, 40f)]   public float buzzPixels  = 12f;
    [Tooltip("Full left-right cycles over the buzz")]
    [Range(1f, 12f)]   public float buzzCycles  = 6f;
    [Range(0.1f, 1.5f)] public float buzzSeconds = 0.5f;

    [Header("Serve beat — the dish landing on the counter (read by GameManager)")]
    [Range(1f, 1.5f)]  public float dishLandScale   = 1.1f;
    [Range(0.05f, 1f)] public float dishLandSeconds = 0.35f;

    [Header("Banners & toasts — read by ToastBanner")]
    [Range(0.05f, 1f)] public float bannerSlideSeconds = 0.25f;
    [Range(0.5f, 5f)]  public float toastHoldSeconds = 2.5f;

    [Header("Motion character — every tavern tween samples this curve")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
