using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardMenuManager : MonoBehaviour, IMenuManager
{
    public enum Mode
    {
        Normal,
        Infinity,
    }

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private LeaderboardEntry entryPrefab;
    [SerializeField] private Mode mode = Mode.Normal;
    [SerializeField] private int limit = 10;
    [SerializeField] private float entryDelay = 0.06f;
    [SerializeField] private string normalLabel = "Tempo";
    [SerializeField] private string infinityLabel = "Rodada";

    private readonly List<LeaderboardEntry> _entries = new();
    private Coroutine _sequence;

    public void OnShow()
    {
        Refresh();
    }

    public void OnHide()
    {
        StopSequence();
        Clear();
    }

    public void ShowNormal()
    {
        mode = Mode.Normal;
        Refresh();
    }

    public void ShowInfinity()
    {
        mode = Mode.Infinity;
        Refresh();
    }

    public void Refresh()
    {
        StopSequence();
        Clear();

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("DataManager não encontrado na cena.");
            return;
        }

        if (mode == Mode.Normal)
            BuildNormal();
        else
            BuildInfinity();

        foreach (LeaderboardEntry entry in _entries)
            entry.Movable.HideInstant();

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        _sequence = StartCoroutine(PlaySequence());
    }

    private void BuildNormal()
    {
        List<NormalGameData> ranking = DataManager.Instance.GetNormalRanking(limit);

        for (int i = 0; i < ranking.Count; i++)
        {
            LeaderboardEntry entry = Instantiate(entryPrefab, content);
            entry.Setup(i + 1, ranking[i].Name, normalLabel, FormatTime(ranking[i].BestTime));
            _entries.Add(entry);
        }
    }

    private void BuildInfinity()
    {
        List<InfinityGameData> ranking = DataManager.Instance.GetInfinityRanking(limit);

        for (int i = 0; i < ranking.Count; i++)
        {
            LeaderboardEntry entry = Instantiate(entryPrefab, content);
            entry.Setup(i + 1, ranking[i].Name, infinityLabel, ranking[i].BestRound.ToString());
            _entries.Add(entry);
        }
    }

    private IEnumerator PlaySequence()
    {
        foreach (LeaderboardEntry entry in _entries)
        {
            entry.Movable.Show();
            yield return new WaitForSecondsRealtime(entryDelay);
        }

        _sequence = null;
    }

    private void StopSequence()
    {
        if (_sequence == null)
            return;

        StopCoroutine(_sequence);
        _sequence = null;
    }

    private void Clear()
    {
        foreach (LeaderboardEntry entry in _entries)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }

        _entries.Clear();
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60f);
        float seconds = time - minutes * 60f;
        return $"{minutes:00}:{seconds:00.00}";
    }
}
