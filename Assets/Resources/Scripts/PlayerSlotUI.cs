using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI playerNameText;
    public Image readyStatusImage;
    public GameObject localPlayerMark;

    public void Setup(string playerName, bool isReady, bool isLocal)
    {
        playerNameText.text = playerName;
        readyStatusImage.color = isReady ? Color.green : Color.gray;
        localPlayerMark.SetActive(isLocal);
    }
}
