using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Networking.Transport.Relay;

public class NetworkRoomManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private List<GameObject> openPanels, closePanels;
    private GameObject lastPanel;

    [Header("Build Room Panel Elements")]
    [SerializeField] private Toggle closeRoomToggle;
    [SerializeField] private TMP_InputField roomNameInput;

    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("Network Settings")]
    [SerializeField] private int lobbySceneIndex;
    public string scenePath, sceneName;
    private void Awake()
    {
        foreach (GameObject panel in openPanels)
        {
            panel.SetActive(true);
        }
        foreach (GameObject panel in closePanels)
        {
            panel.SetActive(false);
        }
    }
    public void OpenOrClosePanel(GameObject targerPanel)
    {
        if(lastPanel != targerPanel)
        {
            if(lastPanel != null)
                lastPanel.SetActive(false);
            lastPanel = targerPanel;
            lastPanel.SetActive(true);
        }
        else
        {
            lastPanel.SetActive(false);
            lastPanel = null;
        }
    }
    public void CreateRoom()
    {
        NetworkManager netManager = NetworkManager.Singleton;
        if (netManager == null)
        {
            Debug.LogError("NetworkManager не найден!");
            return;
        }
        string roomID = System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        string roomName = (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text)) ? roomNameInput.text : "Room " + roomID;
        PlayerPrefs.SetInt("RoomIsClosed", closeRoomToggle.isOn ? 1 : 0);
        PlayerPrefs.SetString("RoomID", roomID);
        PlayerPrefs.SetString("RoomName", roomName);
        scenePath = SceneUtility.GetScenePathByBuildIndex(lobbySceneIndex);
        sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        netManager.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public void JoinRoom()
    {
        if (joinCodeInput == null || string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.LogError("Введите код комнаты!");
            return;
        }
        string roomID = joinCodeInput.text.Trim().ToUpper();
        PlayerPrefs.SetString("RoomID", roomID);
        PlayerPrefs.SetString("RoomName", "Connecting...");
        NetworkManager netManager = NetworkManager.Singleton;
        if (netManager == null)
        {
            Debug.LogError("NetworkManager не найден!");
            return;
        }
        netManager.StartClient();
    }
}
