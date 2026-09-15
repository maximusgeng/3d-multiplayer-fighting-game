using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class Manager : MonoBehaviour
{
    public bool pcInput = true;
    private static Dictionary<string, Player> players = new Dictionary<string, Player>();
    private string localId;
    public GameObject joystick;
    [HideInInspector]
    public GameObject localPlayer;
    [HideInInspector]
    public GameObject oponent;
    public GameObject rightPanel;
    public GameObject controlPanel;
    public GameObject lifePanel;
    public Button jumpButton;
    public Button sheathButton;
    public Button unsheathButton;
    public Button attackButton;
    public Button guardButton;
    public Button unguardButton;
    public Button reMatchButton;
    public Button soundOnButton;
    public Button soundOffButton;
    public TextMeshProUGUI winStatus, loseStatus;
    private int winCount, loseCount;
    private Image attackButtonImage;
    public Sprite swordIcon, fistIcon;
    public float jumpButtonDelay = 1.5f;
    public float sheathUnsheathDelay = 1f;
    public float guardUnguardDelay = .5f;
    public float unarmedAttackButtonDelay = .5f;
    public float armedAttackExtraDelay = .2f;
    private float defaultUnarmedAttackButtonDelay;
    public float attackOtherButtonDelay = .5f;
    public byte currentAttackCount = 0;

    private Animator animator;
    private NetworkAnimator networkAnimator;
    private ActionControl actionControl;

    private bool isGuarding = false;
    private bool isArmed = false;
    private bool isSet = false;
    public static bool isRefresed = false;

    [HideInInspector]
    public bool disableControl = false;

    public static bool isServer => NetworkServer.active;

    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    private float xRotation = 0f;
    private Transform playerBody;
    private bool cameraFound = false;

    private bool IsLocalPlayer()
    {
        if (localPlayer == null) return false;
        Player playerScript = localPlayer.GetComponent<Player>();
        return playerScript != null && playerScript.isLocalPlayer;
    }

    private void Start()
    {
        defaultUnarmedAttackButtonDelay = unarmedAttackButtonDelay;
        if (attackButton != null)
            attackButtonImage = attackButton.GetComponent<Image>();
        if (PlayerPrefs.GetInt("Sound") == 1 && soundOffButton != null)
            soundOffButton.onClick.Invoke();
    }

    void Update()
    {
        if (localPlayer == null)
        {
            localPlayer = GameObject.Find("local player");
            if (controlPanel != null && controlPanel.activeSelf)
                controlPanel.SetActive(false);
        }
        else
        {
            if (animator == null || networkAnimator == null || actionControl == null)
            {
                animator = localPlayer.GetComponent<Animator>();
                networkAnimator = localPlayer.GetComponent<NetworkAnimator>();
                actionControl = localPlayer.GetComponent<ActionControl>();
                if (actionControl != null)
                    localId = actionControl.netId.ToString();
            }

            if (!cameraFound && localPlayer != null && IsLocalPlayer())
            {
                playerCamera = localPlayer.GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    playerBody = localPlayer.transform;
                    Cursor.lockState = CursorLockMode.Locked;
                    cameraFound = true;
                    Debug.Log("Camera found and assigned!");
                    DisableOtherCameras();
                }
            }

            if (controlPanel != null)
            {
                if (!controlPanel.activeSelf && !disableControl)
                    ResetControl();
                if (disableControl && controlPanel.activeSelf)
                    controlPanel.SetActive(false);
            }
        }

        if (oponent == null)
        {
            if (isSet)
                isSet = false;
            oponent = GameObject.Find("oponent");
        }
        else
        {
            if (!isSet)
            {
                RefreshEverything();
                if (localPlayer != null && IsLocalPlayer())
                {
                    NetworkIdentity identity = localPlayer.GetComponent<NetworkIdentity>();
                    if (identity != null && actionControl != null)
                        actionControl.SetOwnRatio(identity.netId.ToString(), localPlayer.GetComponent<Player>().ratio);
                }
                isSet = true;
            }
        }

        if (localPlayer == null && oponent == null && !isRefresed)
            RefreshEverything();

        if (IsLocalPlayer())
        {
            GetKeyInput();
            HandleCamera();
            HandleWeaponSwitch();
        }
    }

    private void DisableOtherCameras()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        foreach (Player p in allPlayers)
        {
            if (!p.isLocalPlayer)
            {
                Camera cam = p.GetComponentInChildren<Camera>();
                if (cam != null)
                    cam.gameObject.SetActive(false);
            }
        }
    }

    private void HandleCamera()
    {
        if (playerCamera == null || playerBody == null) return;
        if (!pcInput) return;
        if (disableControl) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void HandleWeaponSwitch()
    {
        if (disableControl || localPlayer == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f && !isArmed)
        {
            Unsheath();
        }
        else if (scroll < 0f && isArmed)
        {
            Sheath();
        }
    }

    private void GetKeyInput()
    {
        if (!pcInput) return;

        if (Input.GetMouseButtonDown(0) && attackButton != null && attackButton.IsActive() && attackButton.IsInteractable())
        {
            attackButton.onClick.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isGuarding && unguardButton != null && unguardButton.IsActive() && unguardButton.IsInteractable())
                unguardButton.onClick.Invoke();
            else if (!isGuarding && guardButton != null && guardButton.IsActive() && guardButton.IsInteractable())
                guardButton.onClick.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (isArmed && sheathButton != null && sheathButton.IsActive() && sheathButton.IsInteractable())
                sheathButton.onClick.Invoke();
            else if (!isArmed && unsheathButton != null && unsheathButton.IsActive() && unsheathButton.IsInteractable())
                unsheathButton.onClick.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Space) && jumpButton != null && jumpButton.IsActive() && jumpButton.IsInteractable())
            jumpButton.onClick.Invoke();
    }

    private void RefreshEverything()
    {
        winCount = PlayerPrefs.GetInt("Win Count");
        loseCount = PlayerPrefs.GetInt("Lose Count");
        string winText, loseText;
        if (winCount == 0)
            winText = "No\nWin";
        else if (winCount == 1)
            winText = "1\nWin";
        else
            winText = winCount + "\nWins";

        if (loseCount == 0)
            loseText = "No\nDefeat";
        else if (loseCount == 1)
            loseText = "1\nDefeat";
        else
            loseText = loseCount + "\nDefeats";

        if (winStatus != null) winStatus.SetText(winText);
        if (loseStatus != null) loseStatus.SetText(loseText);
        isRefresed = true;
    }

    private void ResetControl()
    {
        if (controlPanel == null) return;
        controlPanel.SetActive(true);
        if (unsheathButton != null) unsheathButton.gameObject.SetActive(!isArmed);
        if (sheathButton != null) sheathButton.gameObject.SetActive(isArmed);
        if (guardButton != null) guardButton.gameObject.SetActive(true);
        if (unguardButton != null) unguardButton.gameObject.SetActive(false);
        if (attackButtonImage != null) attackButtonImage.sprite = isArmed ? swordIcon : fistIcon;
    }

    public void Unsheath()
    {
        if (animator == null) return;
        if (isArmed) return;

        currentAttackCount = 0;
        UnguardIfGuarded();

        if (actionControl != null && IsLocalPlayer())
        {
            actionControl.CmdUnsheath();
        }

        animator.SetBool("Armed", true);
        isArmed = true;
        if (attackButtonImage != null) attackButtonImage.sprite = swordIcon;
        unarmedAttackButtonDelay += armedAttackExtraDelay;

        if (unsheathButton != null) unsheathButton.gameObject.SetActive(false);
        if (sheathButton != null) sheathButton.gameObject.SetActive(true);

        DisableButtons();
        StartCoroutine(EnableButtons(sheathUnsheathDelay));
    }

    public void Sheath()
    {
        if (animator == null) return;
        if (!isArmed) return;

        currentAttackCount = 0;
        UnguardIfGuarded();

        if (actionControl != null && IsLocalPlayer())
        {
            actionControl.CmdSheath();
        }

        animator.SetBool("Armed", false);
        isArmed = false;
        if (attackButtonImage != null) attackButtonImage.sprite = fistIcon;
        unarmedAttackButtonDelay = defaultUnarmedAttackButtonDelay;

        if (unsheathButton != null) unsheathButton.gameObject.SetActive(true);
        if (sheathButton != null) sheathButton.gameObject.SetActive(false);

        DisableButtons();
        StartCoroutine(EnableButtons(sheathUnsheathDelay));
    }

    public void Attack()
    {
        if (animator == null) return;
        currentAttackCount++;
        UnguardIfGuarded();
        animator.SetInteger("AttackNO", Random.Range(1, 6));
        animator.SetTrigger("Attacking");
        DisableButtons();
        StartCoroutine(EnableButtons(unarmedAttackButtonDelay));
    }

    public void Jump()
    {
        if (localPlayer == null) return;
        currentAttackCount = 0;
        PlayerMovement movement = localPlayer.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.Jump();
        UnguardIfGuarded();
        DisableButtons();
        StartCoroutine(EnableButtons(jumpButtonDelay));
    }

    public void Guard()
    {
        if (animator == null) return;
        currentAttackCount = 0;

        if (actionControl != null && IsLocalPlayer())
        {
            actionControl.CmdPlayBlockAnimation();
        }

        animator.SetBool("IsBlocking", true);
        isGuarding = true;
        if (guardButton != null) guardButton.gameObject.SetActive(false);
        if (unguardButton != null) unguardButton.gameObject.SetActive(true);
        DisableButtons();
        StartCoroutine(EnableButtons(guardUnguardDelay));
    }

    public void Unguard()
    {
        if (animator == null) return;
        currentAttackCount = 0;
        animator.SetBool("IsBlocking", false);
        isGuarding = false;
        if (guardButton != null) guardButton.gameObject.SetActive(true);
        if (unguardButton != null) unguardButton.gameObject.SetActive(false);
        DisableButtons();
        StartCoroutine(EnableButtons(guardUnguardDelay));
    }

    private void UnguardIfGuarded()
    {
        if (animator != null && animator.GetBool("IsBlocking"))
        {
            animator.SetBool("IsBlocking", false);
            isGuarding = false;
            if (unguardButton != null) unguardButton.gameObject.SetActive(false);
            if (guardButton != null) guardButton.gameObject.SetActive(true);
        }
    }

    public void ReMatch()
    {
        if (actionControl != null)
            actionControl.ResetALlPlayers();
    }

    private void DisableButtons()
    {
        if (jumpButton != null) jumpButton.interactable = false;
        if (sheathButton != null) sheathButton.interactable = false;
        if (unsheathButton != null) unsheathButton.interactable = false;
        if (attackButton != null) attackButton.interactable = false;
        if (guardButton != null) guardButton.interactable = false;
        if (unguardButton != null) unguardButton.interactable = false;
    }

    IEnumerator EnableButtons(float timer)
    {
        yield return new WaitForSeconds(timer);
        if (attackButton != null) attackButton.interactable = true;
        if (currentAttackCount > 0)
        {
            yield return new WaitForSeconds(attackOtherButtonDelay);
        }
        if (currentAttackCount <= 1)
        {
            if (jumpButton != null) jumpButton.interactable = true;

            if (unsheathButton != null)
            {
                unsheathButton.gameObject.SetActive(!isArmed);
                unsheathButton.interactable = true;
            }
            if (sheathButton != null)
            {
                sheathButton.gameObject.SetActive(isArmed);
                sheathButton.interactable = true;
            }

            if (guardButton != null) guardButton.interactable = true;
            if (unguardButton != null) unguardButton.interactable = true;
        }
        if (currentAttackCount > 0)
            currentAttackCount--;
    }

    public static void RegisterPlayer(string id, Player player)
    {
        if (!players.ContainsKey(id))
            players.Add(id, player);
    }

    public static void UnRegisterPlayer(string _playerID)
    {
        if (players.ContainsKey(_playerID))
            players.Remove(_playerID);
    }

    public static Player GetPlayer(string _playerID)
    {
        if (players.ContainsKey(_playerID))
            return players[_playerID];
        return null;
    }

    public static List<string> GetAllPlayer()
    {
        List<string> allPlayerKey = new List<string>();
        foreach (KeyValuePair<string, Player> player in players)
        {
            allPlayerKey.Add(player.Key);
        }
        return allPlayerKey;
    }

    public void OnSoundOnClicked()
    {
        AudioManager.isMasterVolumeOn = true;
        PlayerPrefs.SetInt("Sound", 0);
    }

    public void OnSoundOffClicked()
    {
        AudioManager.isMasterVolumeOn = false;
        PlayerPrefs.SetInt("Sound", 1);
    }

    public void OnExitClicked()
    {
        Application.Quit();
    }
}