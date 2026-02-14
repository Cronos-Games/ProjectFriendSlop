using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;

public class PlayerMovementController : NetworkBehaviour
{
    //settings
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float sensitivity = 5;

    //references
    private Rigidbody rb;

    private Vector2 moveInput;
    private Vector2 lookInput;


    public override void OnStartClient()
    {
        if (IsOwner)
            GetComponent<PlayerInput>().enabled = true;
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {

        if (!IsOwner)
            return;

        RotatePlayer();
        MovePlayer();
    }

    //get look input from action map
    public void GetLookInput(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }


    //get move input from action map
    public void GetMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }


    private void RotatePlayer()
    {
        float rotDir = lookInput.x * sensitivity;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotDir, 0f));
    }

    private void MovePlayer()
    {
        Vector3 direction = transform.forward * moveInput.y + transform.right * moveInput.x;
        if(direction.sqrMagnitude > 1 ) //normalize vector
            direction.Normalize();

        Vector3 movement = direction * moveSpeed * Time.fixedDeltaTime; //get actual input location

        rb.MovePosition(rb.position + movement); //move player
    }



}
