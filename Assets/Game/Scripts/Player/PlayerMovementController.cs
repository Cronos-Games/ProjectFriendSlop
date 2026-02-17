using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;

public class PlayerMovementController : NetworkBehaviour
{
    //settings
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float sensitivity = 0.05f;

    //references
    private Rigidbody rb;

    //client side input
    private Vector2 moveInput;
    private Vector2 lookAccum;

    //server side latest input
    private Vector2 serverMoveInput;
    private Vector2 serverLookDelta;


    public override void OnStartClient()
    {

        if (TryGetComponent<PlayerInput>(out PlayerInput pi))
        {
            pi.enabled = IsOwner;
        } else
        {
            Debug.LogError("Player object does not have PlayerInput module.");
        }

        if (IsOwner)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            this.rb = rb;
        } else
        {
            Debug.LogError("Player object does not have a Rigidbody.");
        }

        if (!IsServer)
        {
            rb.isKinematic = true;
        } else
        {
            rb.isKinematic = false;
        }
    }


    private void FixedUpdate()
    {
        if (IsOwner)
        {
            Vector2 lookDeltaThisTick = lookAccum;
            lookAccum = Vector2.zero;

            if (IsServer) //host player
            {
                serverMoveInput = moveInput;
                serverLookDelta = lookDeltaThisTick;
            } else
            {
                SendInputServerRpc(moveInput, lookDeltaThisTick);
            }

        }

        if (!IsServer)
            return;

        RotatePlayer_Server();
        MovePlayer_Server();
    }

    //get look input from action map
    public void GetLookInput(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;

        lookAccum += context.ReadValue<Vector2>();
    }


    //get move input from action map
    public void GetMoveInput(InputAction.CallbackContext context)
    {
        if(!IsOwner) 
            return;

        moveInput = context.ReadValue<Vector2>();
    }


    [ServerRpc]
    private void SendInputServerRpc(Vector2 move, Vector2 lookDelta)
    {
        serverMoveInput = move;
        serverLookDelta = lookDelta;
    }


    private void RotatePlayer_Server()
    {
        float rotDir = serverLookDelta.x * sensitivity;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotDir, 0f));
        serverLookDelta = Vector2.zero;
    }

    private void MovePlayer_Server()
    {
        Vector3 direction = transform.forward * serverMoveInput.y + transform.right * serverMoveInput.x;
        if(direction.sqrMagnitude > 1 ) //normalize vector
            direction.Normalize();

        Vector3 movement = direction * moveSpeed * Time.fixedDeltaTime; //get actual input location

        rb.MovePosition(rb.position + movement); //move player
    }



}
