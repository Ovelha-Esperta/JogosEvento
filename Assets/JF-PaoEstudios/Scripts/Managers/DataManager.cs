using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private const string FileName = "GameData.enc";
    private const float TimePrecision = 100f;

    public static DataManager Instance { get; private set; }

    public string PlayerName { get; set; } = "Player";

    private GameData _data;

    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Já existe um DataManager na cena. Este componente será removido.");
            Destroy(this);
            return;
        }

        Instance = this;
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Load()
    {
        _data = default;

        if (File.Exists(FilePath))
        {
            try
            {
                _data = Disc.ReadJson<GameData>(FilePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Falha ao ler {FileName}: {exception.Message}");
            }
        }

        _data.NormalGameDatas ??= new List<NormalGameData>();
        _data.InfinityGameDatas ??= new List<InfinityGameData>();
    }

    public bool Save()
    {
        try
        {
            string temp = FilePath + ".tmp";
            Disc.WriteJson(_data, temp);

            if (File.Exists(FilePath))
                File.Replace(temp, FilePath, null);
            else
                File.Move(temp, FilePath);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Falha ao salvar {FileName}: {exception.Message}");
            return false;
        }
    }

    public bool TryGetNormalBestTime(out float bestTime)
    {
        int index = _data.NormalGameDatas.FindIndex(entry => entry.Name == PlayerName);
        bestTime = index >= 0 ? _data.NormalGameDatas[index].BestTime : 0f;
        return index >= 0;
    }

    public bool SubmitNormalTime(float time)
    {
        if (time <= 0f)
            return false;

        int index = _data.NormalGameDatas.FindIndex(entry => entry.Name == PlayerName);

        if (index < 0)
        {
            _data.NormalGameDatas.Add(new NormalGameData { Name = PlayerName, BestTime = time });
        }
        else if (time < _data.NormalGameDatas[index].BestTime)
        {
            NormalGameData entry = _data.NormalGameDatas[index];
            entry.BestTime = time;
            _data.NormalGameDatas[index] = entry;
        }
        else
        {
            return false;
        }

        return Save();
    }

    public List<NormalGameData> GetNormalRanking(int limit = 10)
    {
        List<NormalGameData> ranking = new(_data.NormalGameDatas);
        ranking.Sort(
            (a, b) =>
            {
                int compare = a.BestTime.CompareTo(b.BestTime);
                return compare != 0 ? compare : CompareNames(a.Name, b.Name);
            }
        );

        if (ranking.Count > limit)
            ranking.RemoveRange(limit, ranking.Count - limit);

        return ranking;
    }

    public bool TryGetInfinityBestRound(out int bestRound)
    {
        int index = _data.InfinityGameDatas.FindIndex(entry => entry.Name == PlayerName);
        bestRound = index >= 0 ? _data.InfinityGameDatas[index].BestRound : 0;
        return index >= 0;
    }

    public bool SubmitInfinityRound(int round, float time)
    {
        time = RoundTime(time);

        if (round <= 0 || time <= 0f)
            return false;

        int index = _data.InfinityGameDatas.FindIndex(entry => entry.Name == PlayerName);

        if (index < 0)
        {
            _data.InfinityGameDatas.Add(
                new InfinityGameData
                {
                    Name = PlayerName,
                    BestRound = round,
                    HiddenTime = time,
                }
            );
        }
        else
        {
            InfinityGameData entry = _data.InfinityGameDatas[index];

            bool betterRound = round > entry.BestRound;
            bool betterTime = round == entry.BestRound && time < GetEffectiveTime(entry);

            if (!betterRound && !betterTime)
                return false;

            entry.BestRound = round;
            entry.HiddenTime = time;
            _data.InfinityGameDatas[index] = entry;
        }

        return Save();
    }

    public List<InfinityGameData> GetInfinityRanking(int limit = 10)
    {
        List<InfinityGameData> ranking = new(_data.InfinityGameDatas);
        ranking.Sort(CompareInfinity);

        if (ranking.Count > limit)
            ranking.RemoveRange(limit, ranking.Count - limit);

        return ranking;
    }

    private int CompareInfinity(InfinityGameData a, InfinityGameData b)
    {
        int compare = b.BestRound.CompareTo(a.BestRound);
        if (compare != 0)
            return compare;

        compare = GetEffectiveTime(a).CompareTo(GetEffectiveTime(b));
        if (compare != 0)
            return compare;

        return CompareNames(a.Name, b.Name);
    }

    private int CompareNames(string a, string b)
    {
        int compare = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        return compare != 0 ? compare : string.CompareOrdinal(a, b);
    }

    private float GetEffectiveTime(InfinityGameData entry) =>
        entry.HiddenTime > 0f ? entry.HiddenTime : float.MaxValue;

    private float RoundTime(float time) => Mathf.Round(time * TimePrecision) / TimePrecision;
}
