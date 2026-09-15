using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private AudioManager audioManager;
    [SerializeField] private Shake camShake;
    public Manager manager;
    private PlayerMovement playerMovement;
    private Animator animator;
    private NetworkAnimator networkAnimator;
    private MultiplayerHUD multiplayerHUD;

    public Collider shieldCollider;
    public int maxHealth = 100;
    public int currentHealth;
    public Transform hitHolder, blockHolder;
    public GameObject fistHitParticle, swordHitParticle;
    public float shakeDelay = 0.1f;

    private Vector3 initialPos;
    private Vector3 initialRot;
    [SerializeField] private MeshRenderer shield;
    private TextMeshProUGUI myRatio;
    private int winCount, loseCount;

    [SyncVar] public string ratio;
    [SyncVar] public bool isDead, isWinner;

    private static int alivePlayersCount = 0;

    private void Start()
    {
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        audioManager = GetComponent<AudioManager>();
        manager = FindObjectOfType<Manager>();
        playerMovement = GetComponent<PlayerMovement>();

        initialPos = transform.position;
        initialRot = transform.eulerAngles;

        ResetAll();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        alivePlayersCount++;
        UnityEngine.Debug.Log($"Player {netId} spawned. Alive players: {alivePlayersCount}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Invoke(nameof(RegisterInHUD), 0.2f);
    }

    private void OnDisable()
    {
        if (multiplayerHUD != null)
            multiplayerHUD.UnregisterPlayer(netId);
    }

    private void RegisterInHUD()
    {
        if (multiplayerHUD == null)
            multiplayerHUD = FindObjectOfType<MultiplayerHUD>();

        if (multiplayerHUD != null)
        {
            // Формируем имя игрока
            string displayName;
            if (isLocalPlayer)
            {
                displayName = "YOU";  // Текущий игрок всегда "YOU"
            }
            else
            {
                // Для остальных игроков: "Player 2", "Player 3", "Player 4"
                displayName = $"Player {netId}";
            }

            multiplayerHUD.RegisterPlayer(netId, displayName, currentHealth, maxHealth, isLocalPlayer);
        }
    }

    private void UpdateHUDHealth()
    {
        if (multiplayerHUD == null)
            multiplayerHUD = FindObjectOfType<MultiplayerHUD>();
        multiplayerHUD?.UpdateHealth(netId, currentHealth, maxHealth);
    }

    [ClientRpc]
    private void RpcUnregisterFromHUD()
    {
        if (multiplayerHUD != null)
            multiplayerHUD.UnregisterPlayer(netId);
    }

    public void TakeDamage(int damage, string type)
    {
        if (isDead) return;

        if (camShake != null)
        {
            camShake.ShakeCam(shakeDelay);
        }

        if (type == "Sword")
        {
            if (audioManager != null) audioManager.PlaySFX("Sword Hit");
            if (swordHitParticle != null && hitHolder != null)
            {
                GameObject particle = Instantiate(swordHitParticle, hitHolder);
                Destroy(particle, 1f);
            }
        }
        else if (type == "Fist")
        {
            if (audioManager != null) audioManager.PlaySFX("Fist Hit");
            if (fistHitParticle != null && hitHolder != null)
            {
                GameObject particle = Instantiate(fistHitParticle, hitHolder);
                Destroy(particle, 1f);
            }
        }

        if (audioManager != null) audioManager.PlaySFX("Pain");

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHUDHealth();

        if (currentHealth <= 0 && isLocalPlayer)
        {
            CmdDie();
        }
    }

    [Command]
    private void CmdDie()
    {
        Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        alivePlayersCount--;

        UnityEngine.Debug.Log($"Player {netId} died. Alive players left: {alivePlayersCount}");

        RpcDisablePlayerCompletely();
        RpcUnregisterFromHUD();

        if (animator != null)
            animator.SetBool("Dead", true);
        if (audioManager != null)
            audioManager.PlaySFX("Defeat");

        Player lastAlive = null;
        int trulyAlive = 0;

        foreach (Player p in FindObjectsOfType<Player>())
        {
            if (!p.isDead)
            {
                trulyAlive++;
                lastAlive = p;
            }
        }

        if (trulyAlive == 1 && lastAlive != null && lastAlive != this)
        {
            lastAlive.Win();
        }

        if (trulyAlive >= 1 && lastAlive != this)
        {
            StartCoroutine(ReturnToLobbyAfterDelay(0.5f));
        }
    }

    [ClientRpc]
    private void RpcDisablePlayerCompletely()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerMovement.canMove = false;
        }

        if (shieldCollider != null)
            shieldCollider.enabled = false;

        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider c in allColliders)
        {
            if (c != null)
                c.enabled = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ActionControl actionControl = GetComponent<ActionControl>();
        if (actionControl != null)
            actionControl.enabled = false;

        Attack[] attacks = GetComponentsInChildren<Attack>();
        foreach (Attack a in attacks)
        {
            a.enabled = false;
        }

        enabled = false;

        UnityEngine.Debug.Log($"Player components disabled on {gameObject.name}");
    }

    private IEnumerator ReturnToLobbyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MyRoomManager roomManager = FindObjectOfType<MyRoomManager>();
        if (roomManager != null)
        {
            if (isLocalPlayer)
            {
                roomManager.ServerChangeScene(roomManager.RoomScene);
            }
            NetworkServer.Destroy(gameObject);
        }
    }

    public void Win()
    {
        if (isWinner) return;
        isWinner = true;

        UnityEngine.Debug.Log($"Player {netId} WON the match!");

        RpcPlayWinAnimation();

        PlayerPrefs.SetInt("Win Count", PlayerPrefs.GetInt("Win Count") + 1);
        RefreshRatio();
    }

    [ClientRpc]
    private void RpcPlayWinAnimation()
    {
        if (isLocalPlayer)
        {
            if (animator != null)
                animator.SetBool("Win", true);
            if (audioManager != null)
                audioManager.PlaySFX("Victory");
            if (manager != null)
                manager.disableControl = true;
            if (playerMovement != null)
                playerMovement.enabled = false;
        }
    }

    public void ResetAll()
    {
        if (manager != null)
            manager.disableControl = false;

        isDead = false;
        isWinner = false;
        Attack.isWinner = false;

        if (animator != null)
        {
            animator.SetBool("Dead", false);
            animator.SetBool("Win", false);
            animator.SetBool("Armed", false);
        }

        currentHealth = maxHealth;
        UpdateHUDHealth();

        transform.position = initialPos;
        transform.eulerAngles = initialRot;

        if (shield != null)
            shield.enabled = true;
        if (shieldCollider != null)
            shieldCollider.enabled = true;

        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider c in allColliders)
        {
            if (c != null)
                c.enabled = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.canMove = true;
            playerMovement.isGrounded = true;
        }

        ActionControl actionControl = GetComponent<ActionControl>();
        if (actionControl != null)
            actionControl.enabled = true;

        Attack[] attacks = GetComponentsInChildren<Attack>();
        foreach (Attack a in attacks)
        {
            a.enabled = true;
        }

        if (isLocalPlayer)
        {
            RefreshRatio();
        }

        if (isServer)
        {
            alivePlayersCount = 0;
            foreach (Player p in FindObjectsOfType<Player>())
            {
                if (!p.isDead)
                    alivePlayersCount++;
            }
        }

        RegisterInHUD();
        enabled = true;
    }

    public void DisableShield()
    {
        if (shieldCollider != null) shieldCollider.enabled = false;
    }

    public void EnableShield()
    {
        if (shieldCollider != null) shieldCollider.enabled = true;
    }

    public void HideShield()
    {
        if (shield != null) shield.enabled = false;
    }

    public void BlockAttack(string type)
    {
        if (animator != null) animator.SetTrigger("Blocked");
    }

    public void RefreshRatio()
    {
        winCount = PlayerPrefs.GetInt("Win Count");
        loseCount = PlayerPrefs.GetInt("Lose Count");
        if (winCount == 0 && loseCount == 0)
            ratio = "First Match!";
        else if (loseCount == 0)
            ratio = "Undefeated!";
        else
            ratio = ((float)winCount / loseCount).ToString();
        GetComponent<ActionControl>()?.SetOwnRatio(netId.ToString(), ratio);
    }

    public void SetMyRatio(string ratio)
    {
        if (myRatio == null)
        {
            if (isLocalPlayer)
                myRatio = GameObject.Find("local player ratio")?.GetComponent<TextMeshProUGUI>();
            else
                myRatio = GameObject.Find("oponent ratio")?.GetComponent<TextMeshProUGUI>();
        }
        if (myRatio != null)
            myRatio.SetText(ratio);
    }
}