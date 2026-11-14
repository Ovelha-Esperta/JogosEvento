using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _output;

    private bool _isRunning;

    public float Elapsed { get; private set; }

    public bool IsRunning => _isRunning;

    private void Update()
    {
        if (!_isRunning)
            return;

        Elapsed += Time.deltaTime;
        Refresh();
    }

    public void StartTimer()
    {
        Elapsed = 0f;
        _isRunning = true;
        Refresh();
    }

    public void Pause()
    {
        _isRunning = false;
    }

    public void Resume()
    {
        _isRunning = true;
    }

    public void ResetTimer()
    {
        Elapsed = 0f;
        _isRunning = false;
        Refresh();
    }

    public static string Format(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        return $"{minutes:00}:{seconds - minutes * 60f:00.00}";
    }

    private void Refresh()
    {
        if (_output != null)
            _output.text = Format(Elapsed);
    }
}
