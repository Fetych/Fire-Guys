using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    [Header("=== ROOM INFO ===")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI playersCountText;

    [Header("=== PLAYERS LIST ===")]
    public Transform playersListParent;
    public GameObject playerSlotPrefab;

    [Header("=== CHAT ===")]
    public TMP_InputField chatInputField;
    public Transform chatMessagesParent;
    public GameObject chatMessagePrefab;

    // Храним только ID игроков в NetworkList (простые ulong)
    private NetworkList<ulong> playersIds;
    // Остальные данные храним в обычных Dictionary (они не синхронизируются автоматически)
    private Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();
    private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();
    private List<GameObject> playerSlots = new List<GameObject>();
    private bool amIReady = false;

    // Имя комнаты и код (от сервера)
    private string roomName = "";
    private string roomCode = "";
    public TextMeshProUGUI copyNotificationText;
    private void Awake()
    {
        playersIds = new NetworkList<ulong>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Загружаем информацию о комнате
        roomCodeText.text = "Room code: " + PlayerPrefs.GetString("RoomID", "???");
        roomNameText.text = PlayerPrefs.GetString("RoomName", "Room");
        // Подписываемся на изменения списка
        playersIds.OnListChanged += OnPlayersListChanged;
        // Регистрируем локального игрока
        ulong myId = NetworkManager.Singleton.LocalClientId;
        string myName = "Player_" + myId;
        // Сохраняем локально
        playerNames[myId] = myName;
        playerReady[myId] = false;
        // Отправляем на сервер
        if (IsServer)
        {
            playersIds.Add(myId);
            OnPlayerJoinedServerRpc(myId, myName);
        }
        else
        {
            AddPlayerServerRpc(myId, myName);
        }
    }
    // Клиент просит добавить игрока
    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerServerRpc(ulong clientId, string playerName)
    {
        playersIds.Add(clientId);
        OnPlayerJoinedServerRpc(clientId, playerName);
    }
    // Сервер рассылает всем информацию о новом игроке
    [ServerRpc(RequireOwnership = false)]
    private void OnPlayerJoinedServerRpc(ulong clientId, string playerName)
    {
        // Сохраняем имя на сервере
        playerNames[clientId] = playerName;
        playerReady[clientId] = false;

        // Рассылаем всем клиентам
        NotifyPlayerJoinedClientRpc(clientId, playerName);
    }
    // Все клиенты получают информацию о новом игроке
    [ClientRpc]
    private void NotifyPlayerJoinedClientRpc(ulong clientId, string playerName)
    {
        if (!playerNames.ContainsKey(clientId))
        {
            playerNames[clientId] = playerName;
            playerReady[clientId] = false;
            RefreshPlayersUI();
            UpdatePlayersCount();
        }
    }
    // Клиент просит удалить игрока
    [ServerRpc(RequireOwnership = false)]
    private void RemovePlayerServerRpc(ulong clientId)
    {
        for (int i = 0; i < playersIds.Count; i++)
        {
            if (playersIds[i] == clientId)
            {
                playersIds.RemoveAt(i);
                break;
            }
        }

        playerNames.Remove(clientId);
        playerReady.Remove(clientId);

        NotifyPlayerLeftClientRpc(clientId);
    }
    // Все клиенты получают информацию об уходе игрока
    [ClientRpc]
    private void NotifyPlayerLeftClientRpc(ulong clientId)
    {
        playerNames.Remove(clientId);
        playerReady.Remove(clientId);
        RefreshPlayersUI();
        UpdatePlayersCount();
    }
    // Обновить статус готовности
    [ServerRpc(RequireOwnership = false)]
    private void UpdateReadyStatusServerRpc(ulong clientId, bool isReady)
    {
        playerReady[clientId] = isReady;
        NotifyReadyStatusClientRpc(clientId, isReady);
    }
    // Все клиенты получают обновление статуса готовности
    [ClientRpc]
    private void NotifyReadyStatusClientRpc(ulong clientId, bool isReady)
    {
        if (playerReady.ContainsKey(clientId))
        {
            playerReady[clientId] = isReady;
            RefreshPlayersUI();
        }
    }
    // Отправить сообщение в чат
    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string senderName, string message)
    {
        ReceiveMessageClientRpc(senderName, message);
    }
    [ClientRpc]
    private void ReceiveMessageClientRpc(string senderName, string message)
    {
        GameObject newMessage = Instantiate(chatMessagePrefab, chatMessagesParent);
        TextMeshProUGUI textComponent = newMessage.GetComponent<TextMeshProUGUI>();
        string time = System.DateTime.Now.ToString("HH:mm:ss");
        textComponent.text = $"[{time}] {senderName}: {message}";

        if (chatMessagesParent.childCount > 50)
            Destroy(chatMessagesParent.GetChild(0).gameObject);
    }
    private void OnPlayersListChanged(NetworkListEvent<ulong> changeEvent)
    {
        RefreshPlayersUI();
        UpdatePlayersCount();
    }
    private void RefreshPlayersUI()
    {
        // Удаляем старые слоты
        foreach (GameObject slot in playerSlots)
            Destroy(slot);
        playerSlots.Clear();
        // Создаём новые слоты для каждого ID
        foreach (ulong id in playersIds)
        {
            if (!playerNames.ContainsKey(id)) continue;
            GameObject slot = Instantiate(playerSlotPrefab, playersListParent);
            playerSlots.Add(slot);
            PlayerSlotUI slotUI = slot.GetComponent<PlayerSlotUI>();
            if (slotUI != null)
            {
                bool isLocal = (id == NetworkManager.Singleton.LocalClientId);
                bool isReady = playerReady.ContainsKey(id) && playerReady[id];
                slotUI.Setup(playerNames[id], isReady, isLocal);
            }
        }
    }

    private void UpdatePlayersCount()
    {
        playersCountText.text = $"Players: {playersIds.Count}";
    }
    private string GetLocalPlayerName()
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        return playerNames.ContainsKey(myId) ? playerNames[myId] : "Unknown";
    }

    // ========== ФУНКЦИИ ДЛЯ ВЕШАНИЯ НА КНОПКИ ==========

    public void OnReadyButtonClick()
    {
        amIReady = !amIReady;

        // Меняем текст на кнопке
        if (GameObject.Find("ReadyButton") != null)
        {
            TextMeshProUGUI btnText = GameObject.Find("ReadyButton").GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = amIReady ? "Not Ready" : "Ready";
        }

        UpdateReadyStatusServerRpc(NetworkManager.Singleton.LocalClientId, amIReady);
    }

    public void OnSendButtonClick()
    {
        if (string.IsNullOrWhiteSpace(chatInputField.text)) return;

        SendMessageServerRpc(GetLocalPlayerName(), chatInputField.text);
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    public void OnLeaveButtonClick()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }
        else
        {
            RemovePlayerServerRpc(NetworkManager.Singleton.LocalClientId);
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnStartGameButtonClick()
    {
        if (!IsServer) return;

        // Проверяем что все готовы
        foreach (var kvp in playerReady)
        {
            if (!kvp.Value) return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
    public void CopyRoomCode()
    {
        string roomCode = PlayerPrefs.GetString("RoomID", "");
        GUIUtility.systemCopyBuffer = roomCode;
        Debug.Log($"Код комнаты {roomCode} скопирован в буфер обмена");
    }
}