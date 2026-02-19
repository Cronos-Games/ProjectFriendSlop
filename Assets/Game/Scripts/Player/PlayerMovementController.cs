using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;

public class PlayerMovementController : NetworkBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float sensitivity = 0.05f;

    [Header("Animation")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string sprintParam = "Speed";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private float animationDamping = 0.08f;
    [SerializeField] private float rootMotionMultiplier = 1f;

    [Header("Speed Values")]
    [SerializeField] private float walkSpeedValue = 0.5f;   // what your parent tree expects
    [SerializeField] private float sprintSpeedValue = 1.0f; // what your parent tree expects


    //references
    private Rigidbody rb;
    private Animator animator;

    //client side input
    private Vector2 moveInput;
    private Vector2 lookAccum;
    private bool sprintHeld;

    //server side latest input
    private Vector2 serverMoveInput;
    private Vector2 serverLookDelta;
    private bool serverSprintHeld;


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

        if (TryGetComponent<Rigidbody>(out Rigidbody rb)) //get rigidbody
        {
            this.rb = rb;
        } else
        {
            Debug.LogError("Player object does not have a Rigidbody.");
        }

        if (!IsServer) //set rigidbody to kinematic for client side, server does all the physics calc
        {
            rb.isKinematic = true;
        } else
        {
            rb.isKinematic = false;
        }

        if (TryGetComponent<Animator>(out Animator animator))
        {
            this.animator = animator;

        } else
        {
            Debug.LogError("Player object does not have an Animator.");
        }

        animator.updateMode = AnimatorUpdateMode.Fixed; //sest animator to tick with physics
        animator.applyRootMotion = IsServer; //only apply rootmotion for server
    }


    private void FixedUpdate()
    {
        if (IsOwner)
        {
            Vector2 clampedMove = moveInput;

            if (clampedMove.sqrMagnitude > 1f)
                clampedMove.Normalize();

            Vector2 lookDeltaThisTick = lookAccum;
            lookAccum = Vector2.zero;

            if (IsServer) //host player
            {
                serverMoveInput = moveInput;
                serverLookDelta = lookDeltaThisTick;
                serverSprintHeld = sprintHeld;
            }
            else
            {
                SendInputServerRpc(moveInput, lookDeltaThisTick, sprintHeld); //send input to server
            }
        }

        if (!IsServer)
            return;

        //set animator movement floats
        animator.SetFloat(moveXParam, serverMoveInput.x, animationDamping, Time.fixedDeltaTime);
        animator.SetFloat(moveYParam, serverMoveInput.y, animationDamping, Time.fixedDeltaTime);

        float mag = serverMoveInput.magnitude;
        float targetSpeed = mag < 0.01f ? 0f :
            (serverSprintHeld ? sprintSpeedValue : walkSpeedValue);

        animator.SetFloat(sprintParam, targetSpeed, animationDamping, Time.fixedDeltaTime);

        if (!string.IsNullOrWhiteSpace(isMovingParam))
            animator.SetBool(isMovingParam, serverMoveInput.sqrMagnitude > 0.001f);

        RotatePlayer_Server(); //rotate player
    }


    private void OnAnimatorMove()
    {
        if (!IsServer || rb == null || animator == null)
            return;

        Vector3 delta = animator.deltaPosition * rootMotionMultiplier;

        delta.y = 0f;

        rb.MovePosition(rb.position + delta);
    }


    private void RotatePlayer_Server()
    {
        float rotDir = serverLookDelta.x * sensitivity;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotDir, 0f));
        serverLookDelta = Vector2.zero; //consume delta
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

    public void GetSprintInput(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;

        sprintHeld = context.ReadValueAsButton();
    }


    [ServerRpc]
    private void SendInputServerRpc(Vector2 move, Vector2 lookDelta, bool sprint)
    {
        serverMoveInput = move;
        serverLookDelta = lookDelta;
        serverSprintHeld = sprint;
    }



    //private void MovePlayer_Server()
    //{
    //    Vector3 direction = transform.forward * serverMoveInput.y + transform.right * serverMoveInput.x;
    //    if(direction.sqrMagnitude > 1 ) //normalize vector
    //        direction.Normalize();

    //    Vector3 movement = direction * moveSpeed * Time.fixedDeltaTime; //get actual input location

    //    rb.MovePosition(rb.position + movement); //move player
    //}



}
