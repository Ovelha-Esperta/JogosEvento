using System;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public class PathManager : MonoBehaviour
{
    public enum PathType
    {
        NormalGame,
        InfinityGame,
        LeaderboardMenu,
        OptionsMenu,
    }

    [Serializable]
    private struct Path
    {
        public PathType pathType;
        public SplineContainer splineContainer;
        public float duration;
        public Ease ease;
        public Vector3 endRotationEuler;
    }

    [SerializeField]
    private List<Path> _paths;

    [SerializeField]
    private CinemachineSplineCart _cameraCart;

    [SerializeField]
    private float _defaultDuration = 2f;

    [SerializeField]
    private Ease _defaultEase = Ease.InOutSine;

    private PathType _currentPathType;
    private SplineContainer _currentSpline;
    private float _currentLength;
    private Tweener _pathTweener;

    public PathType CurrentPathType => _currentPathType;
    public SplineContainer CurrentSpline => _currentSpline;
    public float CurrentLength => _currentLength;
    public bool IsTransitioning => _pathTweener != null && _pathTweener.IsActive();

    private void OnDestroy()
    {
        _pathTweener?.Kill();
    }

    private void ResumeCart()
    {
        if (!_cameraCart.enabled)
            _cameraCart.enabled = true;
    }

    private void FreezeCart(Path path)
    {
        _cameraCart.enabled = false;
        _cameraCart.transform.rotation = Quaternion.Euler(path.endRotationEuler);
    }

    public void MoveToPath(
        PathType targetPath,
        Action onComplete = null,
        float? customDuration = null,
        Ease? customEase = null
    )
    {
        Path target = _paths.Find(p => p.pathType == targetPath);

        if (target.splineContainer == null)
        {
            Debug.LogWarning($"Path {targetPath} não encontrado ou sem SplineContainer.");
            onComplete?.Invoke();
            return;
        }

        _pathTweener?.Kill();
        ResumeCart();

        float duration =
            customDuration ?? (target.duration > 0 ? target.duration : _defaultDuration);
        Ease ease = customEase ?? (target.ease != Ease.Unset ? target.ease : _defaultEase);

        bool isReversing =
            _currentSpline == target.splineContainer && _cameraCart.SplinePosition > 0.5f;
        float endValue = isReversing ? 0f : 1f;
        float startPosition = _cameraCart.SplinePosition;

        if (_currentSpline != target.splineContainer)
        {
            startPosition = 0f;
        }

        _currentPathType = targetPath;
        _currentSpline = target.splineContainer;
        _currentLength = target.splineContainer.CalculateLength();

        _cameraCart.Spline = _currentSpline;
        _cameraCart.SplinePosition = startPosition;

        _pathTweener = DOTween
            .To(() => startPosition, x => _cameraCart.SplinePosition = x, endValue, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                FreezeCart(target);
                onComplete?.Invoke();
            });
    }

    public void ReturnToStart(Action onComplete = null)
    {
        if (_currentSpline == null)
        {
            onComplete?.Invoke();
            return;
        }

        _pathTweener?.Kill();
        ResumeCart();

        float duration = _defaultDuration;
        Ease ease = _defaultEase;

        Path current = _paths.Find(p => p.splineContainer == _currentSpline);
        if (current.splineContainer != null)
        {
            duration = current.duration > 0 ? current.duration : _defaultDuration;
            ease = current.ease != Ease.Unset ? current.ease : _defaultEase;
        }

        float startPosition = _cameraCart.SplinePosition;

        _pathTweener = DOTween
            .To(() => startPosition, x => _cameraCart.SplinePosition = x, 0f, duration)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }

    public Awaitable MoveToPathAsync(
        PathType targetPath,
        CancellationToken token = default,
        float? customDuration = null,
        Ease? customEase = null
    )
    {
        var completionSource = new AwaitableCompletionSource();

        CancellationTokenRegistration registration = default;
        if (token.CanBeCanceled)
        {
            registration = token.Register(() =>
            {
                _pathTweener?.Kill();
                completionSource.TrySetCanceled();
            });
        }

        MoveToPath(
            targetPath,
            onComplete: () =>
            {
                registration.Dispose();
                completionSource.TrySetResult();
            },
            customDuration,
            customEase
        );

        return completionSource.Awaitable;
    }

    public Awaitable ReturnToStartAsync(CancellationToken token = default)
    {
        var completionSource = new AwaitableCompletionSource();

        CancellationTokenRegistration registration = default;
        if (token.CanBeCanceled)
        {
            registration = token.Register(() =>
            {
                _pathTweener?.Kill();
                completionSource.TrySetCanceled();
            });
        }

        ReturnToStart(onComplete: () =>
        {
            registration.Dispose();
            completionSource.TrySetResult();
        });

        return completionSource.Awaitable;
    }

    public void SnapToStart(PathType targetPath)
    {
        Path target = _paths.Find(p => p.pathType == targetPath);

        if (target.splineContainer == null)
            return;

        _pathTweener?.Kill();
        ResumeCart();

        _currentPathType = targetPath;
        _currentSpline = target.splineContainer;
        _currentLength = target.splineContainer.CalculateLength();
        _cameraCart.Spline = _currentSpline;
        _cameraCart.SplinePosition = 0f;
    }

    public void SnapToEnd(PathType targetPath)
    {
        Path target = _paths.Find(p => p.pathType == targetPath);

        if (target.splineContainer == null)
            return;

        _pathTweener?.Kill();
        ResumeCart();

        _currentPathType = targetPath;
        _currentSpline = target.splineContainer;
        _currentLength = target.splineContainer.CalculateLength();
        _cameraCart.Spline = _currentSpline;
        _cameraCart.SplinePosition = 1f;

        FreezeCart(target);
    }

    public void StopMovement()
    {
        _pathTweener?.Kill();
        _pathTweener = null;
    }
}
