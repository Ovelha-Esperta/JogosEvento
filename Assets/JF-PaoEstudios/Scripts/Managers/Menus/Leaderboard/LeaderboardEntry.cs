using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private TMP_Text valueOutput;
    [SerializeField] private InterfaceMovable movable;

    public InterfaceMovable Movable => movable;

    public void Setup(int rank, string playerName, string label, string value)
    {
        rankText.text = $"{rank}º";
        nameText.text = playerName;
        valueLabel.text = label;
        valueOutput.text = value;
    }
}
