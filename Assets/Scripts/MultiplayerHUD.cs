using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MultiplayerHUD : MonoBehaviour
{
    [System.Serializable]
    public class PlayerUI
    {
        public GameObject healthHolder;      // серый холдер
        public RectTransform healthBar;      // красная полоска
        public TextMeshProUGUI healthText;   // текст здоровья (опционально)
        public TextMeshProUGUI playerName;   // ТЕКСТ С ИМЕНЕМ ИГРОКА
    }

    [Header("Order: 0=local, 1=top-right, 2=bottom-left, 3=bottom-right")]
    public List<PlayerUI> playerUIs = new List<PlayerUI>();

    private Dictionary<uint, int> playerToSlot = new Dictionary<uint, int>();
    private bool[] slotUsed;

    void Start()
    {
        slotUsed = new bool[playerUIs.Count];
        for (int i = 0; i < playerUIs.Count; i++)
        {
            PlayerUI ui = playerUIs[i];
            if (ui.healthHolder != null)
            {
                if (ui.healthBar == null)
                {
                    Image img = ui.healthHolder.GetComponentInChildren<Image>();
                    if (img != null)
                        ui.healthBar = img.rectTransform;
                }
                ui.healthHolder.SetActive(false);
            }
        }
    }

    public void RegisterPlayer(uint netId, string playerName, int health, int maxHealth, bool isLocal)
    {
        if (playerToSlot.ContainsKey(netId)) return;

        int slot = -1;
        if (isLocal) slot = 0;
        else
        {
            for (int i = 1; i < slotUsed.Length; i++)
                if (!slotUsed[i]) { slot = i; break; }
        }

        if (slot == -1 || slot >= playerUIs.Count)
        {
            Debug.LogError($"No free HUD slot for {playerName}");
            return;
        }

        slotUsed[slot] = true;
        playerToSlot[netId] = slot;
        PlayerUI ui = playerUIs[slot];

        if (ui.healthHolder != null)
        {
            ui.healthHolder.SetActive(true);
            if (ui.healthBar == null)
            {
                Image img = ui.healthHolder.GetComponentInChildren<Image>();
                if (img != null) ui.healthBar = img.rectTransform;
            }
        }

        // Устанавливаем имя игрока
        if (ui.playerName != null)
        {
            ui.playerName.text = playerName;
            ui.playerName.gameObject.SetActive(true);
        }

        UpdateHealth(netId, health, maxHealth);
        Debug.Log($"Registered {playerName} (netId={netId}) to slot {slot}");
    }

    public void UpdateHealth(uint netId, int currentHealth, int maxHealth)
    {
        if (!playerToSlot.TryGetValue(netId, out int slot)) return;
        PlayerUI ui = playerUIs[slot];
        if (ui.healthBar != null)
        {
            float fill = (float)currentHealth / maxHealth;
            ui.healthBar.localScale = new Vector3(fill, 1f, 1f);
        }
        if (ui.healthText != null)
            ui.healthText.text = $"{currentHealth}/{maxHealth}";
    }

    public void UnregisterPlayer(uint netId)
    {
        if (playerToSlot.TryGetValue(netId, out int slot))
        {
            if (playerUIs[slot].healthHolder != null)
                playerUIs[slot].healthHolder.SetActive(false);
            slotUsed[slot] = false;
            playerToSlot.Remove(netId);
            Debug.Log($"Unregistered netId={netId}, slot {slot} freed");
        }
    }
}