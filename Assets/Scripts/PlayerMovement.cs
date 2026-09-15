using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private bool pcInput;

    private Animator animator;

    private float HInput = 0, VInput = 0;
    private float tempH = 0, tempV = 0;
    [SerializeField]
    private float lerpSpeed = 10;
    [SerializeField]
    private float movementThreshold = .01f;
    private Vector3 velocity = Vector3.zero;
    public float speed = 1.2f;

    public float rotationStartSpeed = 250;
    public float slowRotationSpeed = 50;
    [HideInInspector]
    public float selfRotationSpeed;
    [HideInInspector]
    public float oponentRotationSpeed;
    public bool canSelfRotate = true;
    public bool canOponentRotate = true;

    [HideInInspector]
    public Vector3 jumpForce;
    public int jumpUpForce = 5;
    public int jumpDashForce = 5;
    public byte airSpeedMultiplier = 6;

    private Rigidbody rb;

    public Joystick joystick;

    public Transform oponent;

    [SerializeField]
    private float oponentDistance;

    public bool canMove = true;
    public bool isGrounded = true;
    public bool isJumping = false;

    public bool isSelfDefending = false;
    public bool isOponentDefending = false;

    private Vector3 direction;
    private Quaternion rotation;
    public float stopRotationWaitTime = .2f;

    void Start()
    {
        // Для не-локальных игроков отключаем управление
        if (!isLocalPlayer)
        {
            enabled = false;
            // Отключаем лишние компоненты для чужого игрока
            return;
        }

        selfRotationSpeed = rotationStartSpeed;
        Manager manager = FindObjectOfType<Manager>();
        if (manager != null)
            pcInput = manager.pcInput;

        if (!pcInput && manager != null && manager.joystick != null)
        {
            joystick = manager.joystick.GetComponent<Joystick>();
            if (joystick != null)
                joystick.gameObject.SetActive(true);
        }
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnDestroy()
    {
        if (!pcInput && joystick != null)
            joystick.gameObject.SetActive(false);
    }

    void Update()
    {
        // Двигаем ТОЛЬКО локального игрока
        if (!isLocalPlayer) return;

        if (pcInput)
            GetPcInput();
        else
            GetAndroidInput();

        SmoothInput();

        // Применяем движение
        MovePlayer();

        if (animator != null)
        {
            animator.SetFloat("VInput", VInput);
            animator.SetFloat("HInput", HInput);
        }
    }

    public void Jump()
    {
        // Прыгает ТОЛЬКО локальный игрок
        if (!isLocalPlayer) return;

        if (!isGrounded || isJumping || rb == null)
            return;
        isJumping = true;
        jumpForce = (new Vector3(HInput * jumpDashForce, jumpUpForce, VInput * jumpDashForce).normalized) * airSpeedMultiplier;
        if (animator != null)
            animator.applyRootMotion = false;
        if (animator != null)
            animator.SetBool("IsJumping", true);
        rb.AddRelativeForce(jumpForce, ForceMode.Impulse);
    }

    private void RotatePlayer()
    {
        return;
    }

    public void StopSelfRotation()
    {
        StartCoroutine(DisableRotation(true));
    }

    public void StopOponentRotation()
    {
        StartCoroutine(DisableRotation(false));
    }

    IEnumerator DisableRotation(bool isSelf)
    {
        yield return new WaitForSeconds(stopRotationWaitTime);
        if (isSelf)
            canSelfRotate = false;
        else
            canOponentRotate = false;
    }

    public void SlowSelfRotation()
    {
        selfRotationSpeed = slowRotationSpeed;
        canSelfRotate = true;
    }

    public void SlowOponentRotation()
    {
        oponentRotationSpeed = slowRotationSpeed;
        canOponentRotate = true;
    }

    public void ResetSelfRotation()
    {
        selfRotationSpeed = rotationStartSpeed;
        canSelfRotate = true;
    }

    public void ResetOponentRotation()
    {
        oponentRotationSpeed = rotationStartSpeed;
        canOponentRotate = true;
    }

    private void GetAndroidInput()
    {
        if (joystick != null)
        {
            tempH = joystick.Horizontal;
            if (tempH > .33) tempH = 1;
            else if (tempH < -.33) tempH = -1;
            else tempH = 0;
            tempV = joystick.Vertical;
            if (tempV > .33) tempV = 1;
            else if (tempV < -.33) tempV = -1;
            else tempV = 0;
        }
    }

    private void GetPcInput()
    {
        tempH = Input.GetAxisRaw("Horizontal");
        tempV = Input.GetAxisRaw("Vertical");
    }

    public void SmoothInput()
    {
        HInput = Mathf.Lerp(HInput, tempH, lerpSpeed * Time.deltaTime);
        VInput = Mathf.Lerp(VInput, tempV, lerpSpeed * Time.deltaTime);
        if (HInput < movementThreshold && HInput > -movementThreshold && tempH == 0)
            HInput = 0;

        if (VInput < movementThreshold && VInput > -movementThreshold && tempV == 0)
            VInput = 0;
    }

    private void MovePlayer()
    {
        if (!isLocalPlayer) return;

        Vector3 moveHorizontal = transform.right * HInput;
        Vector3 moveVertical = transform.forward * VInput;

        velocity = (moveHorizontal + moveVertical).normalized * speed;
        if (velocity != Vector3.zero && rb != null)
        {
            rb.MovePosition(rb.position + velocity * Time.deltaTime);
        }
    }

    public void FreezePosition()
    {
        if (rb != null && isLocalPlayer)
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;
    }

    public void ResetConstraints()
    {
        if (rb != null && isLocalPlayer)
            rb.constraints = RigidbodyConstraints.None;
    }
}