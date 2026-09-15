using UnityEngine;
using Mirror;

public class CameraController : NetworkBehaviour
{
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.6f, -2);

    private float xRotation = 0f;
    private Transform playerBody;
    private Camera playerCamera;

    void Start()
    {
        // Только локальный игрок управляет камерой
        if (!isLocalPlayer)
        {
            // Отключаем камеру для чужих игроков
            playerCamera = GetComponent<Camera>();
            if (playerCamera != null)
                playerCamera.enabled = false;
            enabled = false;
            return;
        }

        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        playerBody = transform.parent;
        if (playerBody == null)
            playerBody = transform;

        // Блокируем курсор
        Cursor.lockState = CursorLockMode.Locked;

        // Устанавливаем позицию камеры
        transform.localPosition = offset;
        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (playerBody == null) return;

        // Ввод мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Вертикальный поворот камеры
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        // Применяем повороты
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Горизонтальный поворот персонажа
        playerBody.Rotate(Vector3.up * mouseX);

        // Следим за позицией персонажа (если камера не дочерняя)
        if (transform.parent == null && playerBody != null)
        {
            transform.position = playerBody.position + offset;
        }
    }
}