using System;
using System.Collections.Generic;

public struct ButtonSequence
{
    public List<List<int>> _buttonsSequence;

    public int Count => _buttonsSequence?.Count ?? 0;

    public List<int> Current => Count > 0 ? _buttonsSequence[Count - 1] : null;

    public void Add(List<int> sequence)
    {
        _buttonsSequence ??= new List<List<int>>();
        _buttonsSequence.Add(sequence);
    }

    public void Clear() => _buttonsSequence?.Clear();

    public ButtonSequence Clone()
    {
        ButtonSequence copy = default;

        if (_buttonsSequence == null)
            return copy;

        copy._buttonsSequence = new List<List<int>>(_buttonsSequence.Count);

        foreach (List<int> sequence in _buttonsSequence)
            copy._buttonsSequence.Add(new List<int>(sequence));

        return copy;
    }
}

[Serializable]
public struct GameData
{
    public List<NormalGameData> NormalGameDatas;
    public List<InfinityGameData> InfinityGameDatas;
}

[Serializable]
public struct NormalGameData
{
    public string Name;
    public float BestTime;
}

[Serializable]
public struct InfinityGameData
{
    public string Name;
    public int BestRound;
    public float HiddenTime;
}
