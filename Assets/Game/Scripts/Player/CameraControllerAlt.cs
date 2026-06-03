using System;
using UnityEngine;
using FishNet.Object;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CameraControllerAlt : NetworkBehaviour
{
    [Header("Settings")] 
    [SerializeField] private float sensitivity;
    [SerializeField] private float upAngleLimit;
    [SerializeField] private float downAngleLimit;
    
    [Header("References")]
    [SerializeField] private Camera _cameraPrefab;
    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private float _cameraOffset; 
    
    
    private Vector2 _lookAccum;
    private Vector3 _cameraPosition;
    private float _pitchY = 0f;
    private float _pitchX = 0f;
    
    public override void OnStartClient()
    {
        if (IsOwner)
        {
            _cameraPosition = new Vector3(_cameraPivot.position.x, _cameraPivot.position.y, _cameraPivot.position.z - _cameraOffset);
            Instantiate(_cameraPrefab, _cameraPosition, _cameraPivot.rotation, _cameraPivot);
        }

    }


    private void FixedUpdate()
    {
        Vector2 lookDeltaThisTick = _lookAccum;
        _lookAccum =  Vector2.zero;

        moveCameraVertical(lookDeltaThisTick);
    }


    public void GetLookInput(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;

        _lookAccum += context.ReadValue<Vector2>();
    }

    
    
    
    private void moveCameraVertical(Vector2 lookDelta)
    {
        if (!IsOwner)
            return;

  
        _pitchX -= lookDelta.y * sensitivity;
        _pitchX = Mathf.Clamp(_pitchX, downAngleLimit, upAngleLimit);
        
        _pitchY += lookDelta.x * sensitivity;

        _cameraPivot.localRotation = Quaternion.Euler(_pitchX, _pitchY, 0f);
        /*
        float rotDir = lookDelta.y * sensitivity;

        _cameraPivot.Rotate(-rotDir, 0f, 0f, Space.Self);
        */
        
        lookDelta = Vector2.zero;
    }
    
    
    
}
