using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MyRoomManager : NetworkRoomManager
{
    public GameObject gamePlayerPrefab;

    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
    {
        GameObject newGamePlayerObj = Instantiate(gamePlayerPrefab, GetStartPosition().position, Quaternion.identity);
        return newGamePlayerObj;
    }

    public void ReturnAllPlayersToLobby()
    {
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player p in players)
        {
            if (p != null && p.gameObject != null)
            {
                NetworkServer.Destroy(p.gameObject);
            }
        }
        ServerChangeScene(RoomScene);
    }

    public void ReturnPlayerToLobby(GameObject playerToReturn)
    {
        if (playerToReturn != null)
        {
            NetworkServer.Destroy(playerToReturn);
        }
    }

    public override void Update()
    {
        base.Update();

        if (NetworkServer.active && isSceneRoom && allPlayersReady)
        {
            ServerChangeScene(GameplayScene);
        }
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        ResetCursor();
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        ResetCursor();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        ResetCursor();

        // Принудительно загружаем лобби
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != RoomScene)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(RoomScene);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        ResetCursor();
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        ResetCursor();
    }

    private void ResetCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private bool isSceneRoom
    {
        get
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == RoomScene;
        }
    }

    private bool allPlayersReady
    {
        get
        {
            foreach (RoomPlayer player in FindObjectsOfType<RoomPlayer>())
            {
                if (!player.isReady)
                    return false;
            }
            return true;
        }
    }
    public void ExitGame()
    {
        if (NetworkServer.active)
        {
            StopHost();
        }
        else if (NetworkClient.active)
        {
            StopClient();
        }
        else
        {
            Application.Quit();
        }
    }
}