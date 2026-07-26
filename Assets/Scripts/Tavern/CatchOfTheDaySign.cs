using UnityEngine;
using UnityEngine.UI;

// The tavern's "Catch of the Day" sign. Default state is the plain plaque (SignPlate) over the
// empty cauldron (Face). When a visitor drops off their kill, the whole thing swaps to that
// catch's full composition (plaque + pot + catch in one image, catch-sign-rat/fox/kraken) shown
// on FullArt, with a little pop. Driven from GameManager.
public class CatchOfTheDaySign : MonoBehaviour
{
    [Tooltip("The cauldron image — empty pot shown before any catch is dropped off.")]
    [SerializeField] private Image face;
    [Tooltip("Plain plaque shown before any catch — hidden once FullArt takes over.")]
    [SerializeField] private GameObject signPlate;
    [Tooltip("Full composition (plaque + pot + catch). Disabled until the first drop-off.")]
    [SerializeField] private Image fullArt;

    public void Show(Sprite catchArt)
    {
        if (fullArt && catchArt)
        {
            fullArt.sprite = catchArt;
            fullArt.color  = Color.white;
            fullArt.gameObject.SetActive(true);
            if (signPlate) signPlate.SetActive(false);
            if (face) face.enabled = false;
        }
        else if (face && catchArt)   // fallback if FullArt was never wired
        {
            face.sprite = catchArt;
            face.color  = Color.white;
            face.enabled = true;
        }
        if (isActiveAndEnabled) StartCoroutine(Tween.Punch(transform, 1.15f, 0.28f));
    }
}
