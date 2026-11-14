using TMPro;
using UnityEngine;

public class InfinityGameMenuManager : GameMenuManager
{
    [Header("Timer")]
    [SerializeField]
    private TimerManager _timer;

    [Header("Output")]
    [SerializeField]
    private TextMeshProUGUI _bestRoundOutput;

    private int _lastCompletedRound;
    private float _lastRoundTime;
    private int _bestRound;

    protected override bool IsLastRound(int round)
    {
        return false;
    }

    protected override void LoadGame()
    {
        if (DataManager.Instance.TryGetInfinityBestRound(out int bestRound))
        {
            _bestRound = bestRound;
            RefreshBestTimeOutput();
        }
    }

    protected override void OnGameStarted()
    {
        _lastCompletedRound = 0;
        _lastRoundTime = 0f;
        _timer.StartTimer();
    }

    protected override void OnGamePaused()
    {
        _timer.Pause();
    }

    protected override void OnGameResumed()
    {
        _timer.Resume();
    }

    protected override void OnRoundStarted(int round, float interval)
    {
        _currentRoundOutput.text = $"{round}";
    }

    protected override void OnRoundCompleted(int round)
    {
        _lastCompletedRound = round;
        _lastRoundTime = _timer.Elapsed;
    }

    protected override void OnGameOver(int roundsCompleted)
    {
        _timer.Pause();
        base.OnGameOver(roundsCompleted);
    }

    protected override bool HasProgressToSave(bool won)
    {
        return _lastCompletedRound > 0 && _lastRoundTime > 0f;
    }

    protected override void SaveProgress()
    {
        DataManager.Instance.SubmitInfinityRound(_lastCompletedRound, _lastRoundTime);
    }

    private void RefreshBestTimeOutput()
    {
        _bestRoundOutput.text = _bestRound.ToString();
    }
}
