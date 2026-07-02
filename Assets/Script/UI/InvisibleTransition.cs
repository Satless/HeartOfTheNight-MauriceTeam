using UnityEngine;
using System.Collections;
using DG.Tweening;
public class InvisibleTransition : SceneTransition
{
    public CanvasGroup invisibleTransition;

    public override IEnumerator AnimateTransitionIn()
    {
        var tweener = invisibleTransition.DOFade(1f, 1f);
        yield return tweener.WaitForCompletion();
    }

    public override IEnumerator AnimateTransitionOut()
    {
        var tweener = invisibleTransition.DOFade(0f, 1f);
        yield return tweener.WaitForCompletion();
    }
}
