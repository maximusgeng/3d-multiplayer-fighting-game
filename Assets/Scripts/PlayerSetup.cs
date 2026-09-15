using UnityEngine;
using Mirror;

public class PlayerSetup : NetworkBehaviour
{
    public Behaviour[] componentsToDisable;
    public Collider[] collidersToDisable;

    public string player_id;
    public string net_id;

    private Camera playerCamera;

    void Start()
    {
        player_id = GetComponent<NetworkIdentity>().netId.ToString();

        if (!isLocalPlayer)
        {
            this.gameObject.name = "oponent";
            GameObject localPlayer = GameObject.Find("local player");
            if (localPlayer != null)
            {
                PlayerMovement movement = localPlayer.GetComponent<PlayerMovement>();
                if (movement != null)
                    movement.oponent = this.transform;
                print("oponent set for local player");
            }

            for (int i = 0; i < componentsToDisable.Length; i++)
            {
                if (componentsToDisable[i] != null)
                    componentsToDisable[i].enabled = false;
            }
            for (int i = 0; i < collidersToDisable.Length; i++)
            {
                if (collidersToDisable[i] != null)
                    collidersToDisable[i].enabled = false;
            }

            // Отключаем камеру на чужом игроке
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);
        }
        else
        {
            this.gameObject.name = "local player";
            GameObject oponent = GameObject.Find("oponent");
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null && movement.oponent == null && oponent != null)
            {
                movement.oponent = oponent.transform;
                print("oponent set for local player");
            }

            // Включаем камеру только на локальном игроке
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(true);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        net_id = GetComponent<NetworkIdentity>().netId.ToString();
        Player player = GetComponent<Player>();
        if (player != null)
            Manager.RegisterPlayer(net_id, player);
    }

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(net_id))
            Manager.UnRegisterPlayer(net_id);
    }
}