using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using System.Runtime.CompilerServices;
using FishNet.Connection;
using FishNet.Component.Prediction;

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

    [Header("Server Snapshots (Owner Reconcile)")]
    [Tooltip("How often the server sends authoritative state to the owner client.")]
    [SerializeField] private float serverSnapshotRateHz = 15f;

    [Tooltip("If error exceeds these thresholds, the owner hard-snaps to server state.")]
    [SerializeField] private float hardSnapPositionError = 1.0f;
    [SerializeField] private float hardSnapRotationErrorDeg = 25f;

    [Tooltip("Soft correction strength per snapshot (0..1). Higher = faster correction.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float softCorrection = 0.12f;


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

    //client prediction
    private Vector3 accumulatedRootDelta;

    private float snapshotTimer;
    private bool simulates => IsServerInitialized || IsOwner;

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



        if (TryGetComponent<Animator>(out Animator animator))
        {
            this.animator = animator;

        } else
        {
            Debug.LogError("Player object does not have an Animator.");
        }
        
        if(rb != null)
            rb.isKinematic = !simulates; //simulate movement for owner and server

        animator.updateMode = AnimatorUpdateMode.Fixed; //sest animator to tick with physics
        animator.applyRootMotion = simulates; //apply rootmotion for client and server

        snapshotTimer = 0f;
    }


    private void FixedUpdate()
    {
        if (rb == null || animator == null)
            return;


        Vector2 currentMove = Vector2.zero;
        Vector2 currentLook = Vector2.zero;
        bool currentSprint = false;



        if (IsOwner)
        {
            //move
            currentMove = moveInput;
            if (currentMove.sqrMagnitude > 1f)
                currentMove.Normalize();

            //look
            currentLook = lookAccum;
            lookAccum = Vector2.zero;

            //sprint
            currentSprint = sprintHeld;

            if (IsServerInitialized) //host player
            {
                serverMoveInput = currentMove;
                serverLookDelta = currentLook;
                serverSprintHeld = currentSprint;
            }
            else
            {
                SendInputServerRpc(currentMove, currentLook, currentSprint); //send input to server
            }
        }


        if (simulates)
        {
            if (IsServerInitialized)
                SimulateMovement(Time.fixedDeltaTime, serverMoveInput, serverLookDelta, serverSprintHeld);
            else
                SimulateMovement(Time.fixedDeltaTime, currentMove, currentLook, currentSprint);

            ApplyRootMotion();
        }


        if (IsServerInitialized && !IsOwner)
        {
            float interval = (serverSnapshotRateHz <= 0f) ? 0.06f : (1f / serverSnapshotRateHz);
            snapshotTimer += Time.fixedDeltaTime;

            if (snapshotTimer >= interval)
            {
                snapshotTimer = 0f;

                SendStateToOwnerRpc(Owner, rb.GetState());
            }
        }

        if (IsServerInitialized)
        {
            serverLookDelta = Vector2.zero;
        }

    }



    private void SimulateMovement(float dt, Vector2 move, Vector2 look, bool sprint)
    {
        Vector2 clampedMove = move;

        if (clampedMove.sqrMagnitude > 1f)
            clampedMove.Normalize();

        Vector2 lookDeltaThisTick = look;


        //set animator movement floats
        animator.SetFloat(moveXParam, clampedMove.x, animationDamping, dt);
        animator.SetFloat(moveYParam, clampedMove.y, animationDamping, dt);

        float mag = clampedMove.magnitude;
        float targetSpeed = mag < 0.01f ? 0f :
            (sprint ? sprintSpeedValue : walkSpeedValue);

        animator.SetFloat(sprintParam, targetSpeed, animationDamping, dt);

        if (!string.IsNullOrWhiteSpace(isMovingParam))
            animator.SetBool(isMovingParam, clampedMove.sqrMagnitude > 0.001f);

        RotatePlayer(lookDeltaThisTick); //rotate player
        lookDeltaThisTick = Vector2.zero; //consume delta
    }


    private void OnAnimatorMove()
    {
        if (!simulates || rb == null || animator == null)
            return;

        Vector3 delta = animator.deltaPosition * rootMotionMultiplier;
        delta.y = 0f;

        accumulatedRootDelta += delta;
    }

    private void ApplyRootMotion()
    {
        if(accumulatedRootDelta.sqrMagnitude > 0f)
        {
            rb.MovePosition(rb.position + accumulatedRootDelta);
            accumulatedRootDelta = Vector3.zero;
        }
    }

    private void RotatePlayer(Vector2 lookDelta)
    {
        float rotDir = lookDelta.x * sensitivity;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotDir, 0f));
        lookDelta = Vector2.zero; //consume delta
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


    [TargetRpc]
    private void SendStateToOwnerRpc(NetworkConnection conn, RigidbodyState serverRbState)
    {
        if (!IsOwner || rb == null)
            return;

        float posErr = Vector3.Distance(rb.position, serverRbState.Position);
        float rotErr = Quaternion.Angle(rb.rotation, serverRbState.Rotation);

       
        //if difference is too big, hard snap back
        if (posErr > hardSnapPositionError)
        {
            rb.SetState(serverRbState);
            accumulatedRootDelta = Vector3.zero;
            return;
        }

        Vector3 posDelta = (serverRbState.Position - rb.position) * softCorrection;
        rb.MovePosition(rb.position + posDelta);

        //Quaternion rotDelta = Quaternion.Slerp(rb.rotation, serverRbState.Rotation, softCorrection);
        //rb.MoveRotation(rotDelta);

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, serverRbState.Velocity, softCorrection * 0.5f);
    }

}
