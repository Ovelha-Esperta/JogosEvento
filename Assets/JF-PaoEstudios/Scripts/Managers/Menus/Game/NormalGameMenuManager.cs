using TMPro;
using UnityEngine;

public class NormalGameMenuManager : GameMenuManager
{
    [Header("UI")]
    [SerializeField]
    TextMeshProUGUI _bestTimeOutput;

    [Header("Timer")]
    [SerializeField]
    private TimerManager _timer;

    [Header("Rounds")]
    [SerializeField]
    private int _maxRounds = 5;

    private float _bestTime;

    protected override bool IsLastRound(int round)
    {
        return round >= _maxRounds;
    }

    protected override void LoadGame()
    {
        if (DataManager.Instance.TryGetNormalBestTime(out float bestTime))
        {
            _bestTime = bestTime;
            RefreshBestTimeOutput();
        }
    }

    protected override void OnGameStarted()
    {
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

    protected override void OnGameWon()
    {
        _timer.Pause();
    }

    protected override void OnRoundStarted(int round, float interval)
    {
        _currentRoundOutput.text = $"{round}/{_maxRounds}";
    }

    protected override bool HasProgressToSave(bool won)
    {
        return won;
    }

    protected override void SaveProgress()
    {
        if (DataManager.Instance.SubmitNormalTime(_timer.Elapsed))
            _bestTime = _timer.Elapsed;

        RefreshBestTimeOutput();
    }

    protected override void OnGameOver(int roundsCompleted)
    {
        _timer.Pause();
        base.OnGameOver(roundsCompleted);
    }

    private void RefreshBestTimeOutput()
    {
        _bestTimeOutput.text = _bestTime > 0f ? TimerManager.Format(_bestTime) : "--:--";
    }
}
