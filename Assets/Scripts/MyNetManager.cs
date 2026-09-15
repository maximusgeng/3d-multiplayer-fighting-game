using kcp2k;
using Mirror;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MyNetManager : NetworkManager
{
    [Header("UI Buttons")]
    public GameObject HostButton;
    public GameObject JoinButton;
    public GameObject CancelButton;
    public GameObject ExitButton;
    public GameObject rematchButton;

    [Header("UI Text")]
    public GameObject WaitingText;
    public GameObject winText;
    public GameObject loseText;

    [Header("Other")]
    public Animator fadeAnimator;
    public string state;

    // Список точек спавна (заполняется автоматически из NetworkStartPosition)
    private Transform[] spawnPoints;
    private int nextSpawnIndex = 0;

    public override void Start()
    {
        base.Start(); // обязательно вызываем базовый Start
        ResetUI();
    }

    // ==================== ТОЧКИ СПАВНА ====================
    // Этот метод вызывается перед спавном игрока
    public override Transform GetStartPosition()
    {
        // Если список ещё не заполнен – ищем все NetworkStartPosition в сцене
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            NetworkStartPosition[] startPositions = FindObjectsOfType<NetworkStartPosition>();
            spawnPoints = new Transform[startPositions.Length];
            for (int i = 0; i < startPositions.Length; i++)
                spawnPoints[i] = startPositions[i].transform;
        }

        // Если точек нет – спавним в (0,0,0)
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points found, spawning at (0,0,0)");
            return null;
        }

        // Циклический перебор (round‑robin)
        Transform point = spawnPoints[nextSpawnIndex];
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
        Debug.Log($"Spawning player at {point.name}");
        return point;
    }

    // ==================== ХОСТ ====================
    public void StartGameHost()
    {
        if (fadeAnimator != null)
            fadeAnimator.SetTrigger("Fade In");
        StartCoroutine(StartingGameHost());
    }

    IEnumerator StartingGameHost()
    {
        yield return new WaitForSeconds(.75f);
        state = "Hosting";

        try
        {
            networkAddress = "localhost";
            StartHost();
            HideUI();
            Debug.Log("=== HOST STARTED ===");

            string localIP = GetLocalIPAddress();
            Debug.Log($"=== Connect from client to: {localIP} ===");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start host: " + e.Message);
        }
    }

    // ==================== ПОДКЛЮЧЕНИЕ ====================
    public void JoinGame()
    {
        Debug.Log($"=== CONNECTING TO: {networkAddress} ===");
        state = "Joining";
        StartClient();
        HideUI();
    }

    // ==================== СОБЫТИЯ NETWORK ====================
    public override void OnStartHost()
    {
        Debug.Log("OnStartHost called");
        // Сбрасываем индекс спавна при старте хоста
        nextSpawnIndex = 0;
        HideUI();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"=== STARTING CLIENT ===");
        Debug.Log($"Network Address: {networkAddress}");
        if (GetComponent<KcpTransport>() != null)
            Debug.Log($"Network Port: {GetComponent<KcpTransport>().Port}");
        Debug.Log($"OnStartClient called. Connecting to: {networkAddress}");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("=== CLIENT CONNECTED SUCCESSFULLY ===");
        HideUI();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("=== CLIENT DISCONNECTED ===");
        StopClient();
        ResetUI();
    }

    public override void OnClientError(TransportError error, string reason)
    {
        base.OnClientError(error, reason);
        Debug.LogError($"=== CLIENT ERROR: {error} - {reason} ===");
        ResetUI();

        if (WaitingText != null)
        {
            WaitingText.SetActive(true);
            var text = WaitingText.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null)
                text.text = $"Connection failed! Check IP";
        }

        StartCoroutine(HideWaitingTextAfterDelay(2f));
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("OnStopClient called");
        ResetUI();
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("OnStopHost called");
        ResetUI();
    }

    // ==================== UI УПРАВЛЕНИЕ ====================
    public void HideUI()
    {
        state = "Playing";

        if (CancelButton != null) CancelButton.SetActive(true);
        if (ExitButton != null) ExitButton.SetActive(false);
        if (HostButton != null) HostButton.SetActive(false);
        if (JoinButton != null) JoinButton.SetActive(false);
        if (WaitingText != null) WaitingText.SetActive(false);
        if (winText != null) winText.SetActive(false);
        if (loseText != null) loseText.SetActive(false);
    }

    public void ResetUI()
    {
        state = "Menu";

        if (HostButton != null) HostButton.SetActive(true);
        if (JoinButton != null) JoinButton.SetActive(true);
        if (winText != null) winText.SetActive(true);
        if (loseText != null) loseText.SetActive(true);
        if (ExitButton != null) ExitButton.SetActive(true);
        if (CancelButton != null) CancelButton.SetActive(false);
        if (WaitingText != null)
        {
            WaitingText.SetActive(false);
            var text = WaitingText.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null)
                text.text = "Waiting for server...";
        }

        Manager.isRefresed = false;
    }

    public void Cancel()
    {
        if (fadeAnimator != null)
            fadeAnimator.SetTrigger("Fade In");
        StartCoroutine(Cancelling());
    }

    IEnumerator Cancelling()
    {
        yield return new WaitForSeconds(.75f);

        if (state == "Joining")
        {
            StopClient();
        }
        else if (state == "Hosting")
        {
            StopHost();
        }

        ResetUI();
    }

    private IEnumerator HideWaitingTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (WaitingText != null)
            WaitingText.SetActive(false);
        ResetUI();
    }

    public void OnExitClicked()
    {
        Application.Quit();
    }

    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}