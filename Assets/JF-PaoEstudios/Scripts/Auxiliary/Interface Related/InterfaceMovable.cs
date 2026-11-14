using DG.Tweening;
using UnityEngine;

public class InterfaceMovable : MonoBehaviour, IMenuAnimatable
{
    public enum MovableTarget
    {
        Up,
        Down,
        Left,
        Right,
    }

    [SerializeField]
    private MovableTarget _offsetDirection = MovableTarget.Up;

    [SerializeField]
    private float _distanceOffset = 1000f;

    [SerializeField]
    private float _duration = 0.5f;

    [SerializeField]
    private Ease _showEase = Ease.OutCubic;

    [SerializeField]
    private Ease _hideEase = Ease.InOutCubic;

    [SerializeField]
    private bool _startHidden = true;

    private RectTransform _rectTransform;
    private Vector2 _originalAnchoredPosition;
    private Vector2 _hiddenAnchoredPosition;

    private Tween _translationTween;

    public bool IsVisible { get; private set; }
    public float AnimationDuration => _duration;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        Canvas.ForceUpdateCanvases();

        _originalAnchoredPosition = _rectTransform.anchoredPosition;
        _hiddenAnchoredPosition = _originalAnchoredPosition + GetDirectionOffset();

        if (_startHidden)
        {
            _rectTransform.anchoredPosition = _hiddenAnchoredPosition;
            IsVisible = false;
        }
        else
        {
            IsVisible = true;
        }
    }

    private void OnDestroy()
    {
        _translationTween?.Kill();
    }

    public Tween Show()
    {
        if (_rectTransform == null)
            return null;

        _translationTween?.Kill();

        _translationTween = _rectTransform
            .DOAnchorPos(_originalAnchoredPosition, _duration)
            .SetEase(_showEase)
            .SetUpdate(true)
            .SetLink(gameObject);

        IsVisible = true;
        return _translationTween;
    }

    public Tween Hide()
    {
        if (_rectTransform == null)
            return null;

        _translationTween?.Kill();

        _translationTween = _rectTransform
            .DOAnchorPos(_hiddenAnchoredPosition, _duration)
            .SetEase(_hideEase)
            .SetUpdate(true)
            .SetLink(gameObject);

        IsVisible = false;
        return _translationTween;
    }

    public void ShowInstant()
    {
        _translationTween?.Kill();

        if (_rectTransform == null)
            return;

        _rectTransform.anchoredPosition = _originalAnchoredPosition;
        IsVisible = true;
    }

    public void HideInstant()
    {
        _translationTween?.Kill();

        if (_rectTransform == null)
            return;

        _rectTransform.anchoredPosition = _hiddenAnchoredPosition;
        IsVisible = false;
    }

    public void Toggle()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    private Vector2 GetDirectionOffset()
    {
        return _offsetDirection switch
        {
            MovableTarget.Up => Vector2.up * _distanceOffset,
            MovableTarget.Down => Vector2.down * _distanceOffset,
            MovableTarget.Left => Vector2.left * _distanceOffset,
            MovableTarget.Right => Vector2.right * _distanceOffset,
            _ => Vector2.zero,
        };
    }

    public void SetDuration(float duration) => _duration = duration;

    public void SetDistance(float distance) => _distanceOffset = distance;

    public void SetShowEase(Ease ease) => _showEase = ease;

    public void SetHideEase(Ease ease) => _hideEase = ease;

    public void Move() => Show();

    public void Reset() => Hide();

    public void ResetInstant() => HideInstant();
}
