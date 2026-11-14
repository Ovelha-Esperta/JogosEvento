using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class PopupTween
{
    [Header("Show")]
    [SerializeField, Min(0.01f)]
    private float _showDuration = 0.3f;

    [SerializeField]
    private Ease _showScaleEase = Ease.OutBack;

    [SerializeField]
    private Ease _showFadeEase = Ease.OutQuad;

    [Header("Hide")]
    [SerializeField, Min(0.01f)]
    private float _hideDuration = 0.25f;

    [SerializeField]
    private Ease _hideScaleEase = Ease.InBack;

    [SerializeField]
    private Ease _hideFadeEase = Ease.InQuad;

    [Header("Shared")]
    [SerializeField, Min(0f)]
    private float _hiddenScale = 0.8f;

    [SerializeField]
    private bool _fade = true;

    [SerializeField]
    private bool _ignoreTimeScale = false;

    public Tween Show(CanvasGroup group, TweenCallback onComplete = null)
    {
        group.gameObject.SetActive(true);
        group.interactable = false;
        group.alpha = _fade ? 0f : 1f;
        group.transform.localScale = Vector3.one * _hiddenScale;

        Sequence sequence = DOTween.Sequence().SetUpdate(_ignoreTimeScale);

        if (_fade)
            sequence.Insert(0f, group.DOFade(1f, _showDuration).SetEase(_showFadeEase));

        sequence.Insert(0f, group.transform.DOScale(1f, _showDuration).SetEase(_showScaleEase));

        sequence.OnComplete(() =>
        {
            group.interactable = true;
            onComplete?.Invoke();
        });

        return sequence;
    }

    public Tween Hide(CanvasGroup group, TweenCallback onComplete = null)
    {
        group.interactable = false;

        Sequence sequence = DOTween.Sequence().SetUpdate(_ignoreTimeScale);

        if (_fade)
            sequence.Insert(0f, group.DOFade(0f, _hideDuration).SetEase(_hideFadeEase));

        sequence.Insert(
            0f,
            group.transform.DOScale(_hiddenScale, _hideDuration).SetEase(_hideScaleEase)
        );

        sequence.OnComplete(() =>
        {
            group.gameObject.SetActive(false);
            onComplete?.Invoke();
        });

        return sequence;
    }
}
