using DG.Tweening;

public interface IMenuAnimatable
{
    Tween Show();
    Tween Hide();
    float AnimationDuration { get; }
}

public interface IMenuManager
{
    void OnShow();
    void OnHide();
}
