using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class RoomPlayer : NetworkBehaviour
{
    [SyncVar] public string playerName = "Player";
    [SyncVar] public bool isReady = false;

    private Button readyButton;
    private Button leaveButton;
    private TextMeshProUGUI statusText;
    private Canvas lobbyCanvas;

    void Start()
    {
        FindLobbyCanvas();

        if (isLocalPlayer)
        {
            ResetCursor();
            FindUIElements();
        }
    }

    private void FindLobbyCanvas()
    {
        lobbyCanvas = FindObjectOfType<Canvas>();
        if (lobbyCanvas != null && isLocalPlayer)
        {
            lobbyCanvas.gameObject.SetActive(true);
        }
    }

    private void ResetCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void FindUIElements()
    {
        Debug.Log("Searching for UI elements...");

        if (lobbyCanvas == null)
        {
            lobbyCanvas = FindObjectOfType<Canvas>();
            if (lobbyCanvas == null)
            {
                Debug.LogError("Canvas not found!");
                return;
            }
        }

        Transform readyBtn = lobbyCanvas.transform.Find("ReadyButton");
        Transform leaveBtn = lobbyCanvas.transform.Find("LeaveButton");
        Transform status = lobbyCanvas.transform.Find("StatusText");

        if (readyBtn != null)
        {
            readyButton = readyBtn.GetComponent<Button>();
            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(OnReadyButtonClicked);
                Debug.Log("ReadyButton assigned");
            }
        }
        else
        {
            Debug.LogError("ReadyButton not found on Canvas!");
        }

        if (leaveBtn != null)
        {
            leaveButton = leaveBtn.GetComponent<Button>();
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(OnLeaveButtonClicked);
                Debug.Log("LeaveButton assigned");
            }
        }
        else
        {
            Debug.LogError("LeaveButton not found on Canvas!");
        }

        if (status != null)
        {
            statusText = status.GetComponent<TextMeshProUGUI>();
            if (statusText != null)
                statusText.text = isReady ? "Ready" : "Not Ready";
        }
    }

    public void OnReadyButtonClicked()
    {
        if (!isLocalPlayer) return;

        isReady = !isReady;
        CmdSetReady(isReady);

        if (statusText != null)
            statusText.text = isReady ? "Ready" : "Not Ready";

        Debug.Log($"Ready status set to: {isReady}");
    }

    public void OnLeaveButtonClicked()
    {
        if (!isLocalPlayer) return;

        ResetCursor();

        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }

    [Command]
    private void CmdSetReady(bool ready)
    {
        isReady = ready;
    }

    void OnDestroy()
    {
        ResetCursor();
    }
}