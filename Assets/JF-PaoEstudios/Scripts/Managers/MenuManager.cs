using System;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Serializable]
    public struct Menu
    {
        public PathManager.PathType pathCorrespondent;
        public GameObject menuObject;
        public float delayToShow;
    }

    [SerializeField]
    private List<Menu> _menus = new();

    [SerializeField]
    private Transform _canvasTarget;

    [SerializeField]
    private PathManager _pathManager;

    [SerializeField]
    private bool _instantiateOnCanvas = true;

    [SerializeField]
    private bool _deactivateOthersOnShow = true;

    [SerializeField]
    private GameObject _mainMenuObject;

    [SerializeField]
    private float _mainMenuDelay = 0.5f;

    private Menu _currentMenu;
    private PathManager.PathType _currentPathType;
    private bool _isInMainMenu = true;

    private CancellationTokenSource _transitionCts;

    private readonly Dictionary<GameObject, CancellationTokenSource> _pendingHides = new();

    public PathManager.PathType CurrentPathType => _currentPathType;
    public bool IsInMainMenu => _isInMainMenu;
    public bool IsMenuTransitioning { get; private set; }

    private void Start()
    {
        InitializeMenus();
        ShowMainMenuObject();
    }

    private void OnDestroy()
    {
        _transitionCts?.Cancel();
        _transitionCts?.Dispose();

        List<CancellationTokenSource> pending = new(_pendingHides.Values);
        _pendingHides.Clear();

        foreach (CancellationTokenSource cts in pending)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void GoToNormalGame() => GoToMenu(PathManager.PathType.NormalGame);

    public void GoToInfinityGame() => GoToMenu(PathManager.PathType.InfinityGame);

    public void GoToLeaderboardMenu() => GoToMenu(PathManager.PathType.LeaderboardMenu);

    public void GoToOptionsMenu() => GoToMenu(PathManager.PathType.OptionsMenu);

    public void GoToMainMenuButton() => GoToMainMenu();

    private void InitializeMenus()
    {
        foreach (Menu menu in _menus)
        {
            if (menu.menuObject == null)
                continue;

            GameObject target = GetMenuInstance(menu);

            if (target != null)
                target.SetActive(false);
        }

        if (_mainMenuObject != null)
        {
            GameObject mainTarget = GetMainMenuInstance();
            if (mainTarget != null)
                mainTarget.SetActive(false);
        }
    }

    private GameObject GetMenuInstance(Menu menu)
    {
        if (!_instantiateOnCanvas || _canvasTarget == null)
            return menu.menuObject;

        if (menu.menuObject.transform.parent == _canvasTarget)
            return menu.menuObject;

        GameObject instance = Instantiate(menu.menuObject, _canvasTarget);
        instance.name = menu.menuObject.name;
        return instance;
    }

    private GameObject GetMainMenuInstance()
    {
        if (!_instantiateOnCanvas || _canvasTarget == null)
            return _mainMenuObject;

        if (_mainMenuObject.transform.parent == _canvasTarget)
            return _mainMenuObject;

        GameObject instance = Instantiate(_mainMenuObject, _canvasTarget);
        instance.name = _mainMenuObject.name;
        return instance;
    }

    public async void GoToMenu(PathManager.PathType targetPath)
    {
        if (IsMenuTransitioning || _isInMainMenu == false && _currentPathType == targetPath)
            return;

        Menu targetMenu = _menus.Find(m => m.pathCorrespondent == targetPath);

        if (targetMenu.menuObject == null)
        {
            Debug.LogWarning($"Menu para path {targetPath} não configurado.");
            return;
        }

        _transitionCts?.Cancel();
        _transitionCts = new CancellationTokenSource();
        CancellationToken token = _transitionCts.Token;

        IsMenuTransitioning = true;
        _currentPathType = targetPath;
        _isInMainMenu = false;

        HideMainMenu();

        try
        {
            await _pathManager.MoveToPathAsync(targetPath, token);
            await Awaitable.WaitForSecondsAsync(targetMenu.delayToShow, token);

            ShowMenu(targetMenu);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsMenuTransitioning = false;
        }
    }

    public async void GoToMainMenu()
    {
        if (IsMenuTransitioning || _isInMainMenu)
            return;

        _transitionCts?.Cancel();
        _transitionCts = new CancellationTokenSource();
        CancellationToken token = _transitionCts.Token;

        IsMenuTransitioning = true;
        _currentPathType = PathManager.PathType.NormalGame;

        HideAllMenus();

        try
        {
            await _pathManager.ReturnToStartAsync(token);
            await Awaitable.WaitForSecondsAsync(_mainMenuDelay, token);

            ShowMainMenuObject();
            _isInMainMenu = true;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsMenuTransitioning = false;
        }
    }

    public void GoToMainMenuInstant()
    {
        _transitionCts?.Cancel();

        _pathManager.StopMovement();
        _currentPathType = PathManager.PathType.NormalGame;
        _isInMainMenu = true;
        IsMenuTransitioning = false;

        HideAllMenusInstant();
        _pathManager.SnapToStart(PathManager.PathType.NormalGame);
        ShowMainMenuInstant();
    }

    public void ShowMainMenuInstant()
    {
        _isInMainMenu = true;

        if (_mainMenuObject == null)
            return;

        GameObject target = GetMainMenuInstance();
        if (target != null)
        {
            target.SetActive(true);
        }
    }

    private void ShowMainMenuObject()
    {
        if (_mainMenuObject == null)
            return;

        if (_deactivateOthersOnShow)
            HideAllMenus();

        GameObject target = GetMainMenuInstance();
        ActivateMenuObject(target);
    }

    private void HideMainMenu()
    {
        if (_mainMenuObject == null)
            return;

        GameObject target = GetMainMenuInstance();
        DeactivateMenuObject(target);
    }

    public void ShowMenu(PathManager.PathType pathType)
    {
        Menu targetMenu = _menus.Find(m => m.pathCorrespondent == pathType);

        if (targetMenu.menuObject != null)
            ShowMenu(targetMenu);
    }

    public void HideCurrentMenu()
    {
        if (_currentMenu.menuObject != null)
        {
            GameObject target = GetMenuInstance(_currentMenu);
            DeactivateMenuObject(target);
        }
    }

    public void HideAllMenus()
    {
        foreach (Menu menu in _menus)
        {
            GameObject target = GetMenuInstance(menu);
            DeactivateMenuObject(target);
        }

        HideMainMenu();
    }

    private void HideAllMenusInstant()
    {
        foreach (Menu menu in _menus)
        {
            GameObject target = GetMenuInstance(menu);
            if (target != null)
                target.SetActive(false);
        }

        if (_mainMenuObject != null)
        {
            GameObject mainTarget = GetMainMenuInstance();
            if (mainTarget != null)
                mainTarget.SetActive(false);
        }
    }

    private void ShowMenu(Menu menu)
    {
        if (_deactivateOthersOnShow)
            HideAllMenus();

        GameObject target = GetMenuInstance(menu);

        if (target != null)
        {
            ActivateMenuObject(target);
            _currentMenu = menu;
        }
    }

    private void ActivateMenuObject(GameObject target)
    {
        if (target == null)
            return;

        if (_pendingHides.TryGetValue(target, out CancellationTokenSource pending))
        {
            pending.Cancel();
        }

        target.SetActive(true);

        var animatables = target.GetComponentsInChildren<IMenuAnimatable>(true);
        var managers = target.GetComponentsInChildren<IMenuManager>(true);

        if (animatables.Length > 0)
        {
            int completedCount = 0;
            int totalCount = animatables.Length;

            foreach (var animatable in animatables)
            {
                Tween tween = animatable.Show();

                tween.OnComplete(() =>
                {
                    completedCount++;

                    if (completedCount >= totalCount && target != null && target.activeSelf)
                    {
                        foreach (var manager in managers)
                        {
                            manager.OnShow();
                        }
                    }
                });
            }
        }
        else
        {
            foreach (var manager in managers)
            {
                manager.OnShow();
            }
        }
    }

    private void DeactivateMenuObject(GameObject target)
    {
        if (target == null)
            return;

        var animatables = target.GetComponentsInChildren<IMenuAnimatable>(true);
        var managers = target.GetComponentsInChildren<IMenuManager>(true);

        if (animatables.Length == 0)
        {
            foreach (var manager in managers)
                manager.OnHide();
            target.SetActive(false);
            return;
        }

        if (_pendingHides.TryGetValue(target, out CancellationTokenSource existing))
        {
            existing.Cancel();
        }

        var cts = new CancellationTokenSource();
        _pendingHides[target] = cts;

        int completedCount = 0;
        int totalCount = animatables.Length;

        foreach (var animatable in animatables)
        {
            Tween tween = animatable.Hide();
            tween.OnComplete(() =>
            {
                completedCount++;

                if (completedCount >= totalCount)
                {
                    if (target != null)
                    {
                        target.SetActive(false);

                        foreach (var manager in managers)
                        {
                            manager.OnHide();
                        }
                    }

                    if (
                        _pendingHides.TryGetValue(target, out CancellationTokenSource stored)
                        && stored == cts
                    )
                    {
                        _pendingHides.Remove(target);
                    }
                    cts.Dispose();
                }
            });
        }
    }

    private async void DeactivateAfterDelay(
        GameObject target,
        float delay,
        CancellationTokenSource cts
    )
    {
        try
        {
            await Awaitable.WaitForSecondsAsync(delay, cts.Token);

            if (target != null)
                target.SetActive(false);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (
                _pendingHides.TryGetValue(target, out CancellationTokenSource stored)
                && stored == cts
            )
                _pendingHides.Remove(target);

            cts.Dispose();
        }
    }
}
