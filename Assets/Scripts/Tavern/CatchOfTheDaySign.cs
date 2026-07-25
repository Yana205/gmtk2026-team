using UnityEngine;
using UnityEngine.UI;

// The tavern's "Catch of the Day" sign. A single cauldron image sits under a label: it starts as the
// empty pot ("catch of the day.png") and, each time a visitor drops off their kill, swaps to that
// catch's art (CATCH OF THE DAY RAT / FOX / KRAKEN) with a little pop. Driven from GameManager.
public class CatchOfTheDaySign : MonoBehaviour
{
    [Tooltip("The cauldron image — empty pot by default, swapped to the current catch's art.")]
    [SerializeField] private Image face;

    public void Show(Sprite catchArt)
    {
        if (face && catchArt)
        {
            face.sprite = catchArt;
            face.color  = Color.white;
            face.enabled = true;
        }
        if (isActiveAndEnabled) StartCoroutine(Tween.Punch(transform, 1.15f, 0.28f));
    }
}
