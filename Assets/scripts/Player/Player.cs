using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    Vector2 movementVector;
    [SerializeField] float speed = 3f;
    Animator animator;
    PlayerInput playerInputActions;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        playerInputActions = new PlayerInput();
        playerInputActions.Player.Move.performed += MovePerformed;
        playerInputActions.Player.Move.canceled += OnMoveCanceled;

        playerInputActions.Player.Enable();
    }

    void OnDestroy()
    {
        playerInputActions.Player.Move.performed -= MovePerformed;
        playerInputActions.Player.Move.canceled -= OnMoveCanceled;

        playerInputActions.Player.Disable();
    }

    public void MovePerformed(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        movementVector.x = input.x;
        movementVector.y = input.y;
        movementVector *= speed;
        rb.velocity = movementVector;

        animator.SetFloat("LastXinput", movementVector.x);
        animator.SetFloat("LastYinput", movementVector.y);
        animator.SetFloat("Xinput", movementVector.x);
        animator.SetFloat("Yinput", movementVector.y);

    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        movementVector = Vector2.zero;
        rb.velocity = movementVector;

        animator.SetFloat("Xinput", 0);
        animator.SetFloat("Yinput", 0);

    }

    void FixedUpdate()
    {
        rb.velocity = movementVector;
    }
}
