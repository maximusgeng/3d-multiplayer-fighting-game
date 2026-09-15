using UnityEngine;

public class LobbyCanvasInitializer : MonoBehaviour
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameObject.SetActive(true);
    }
}