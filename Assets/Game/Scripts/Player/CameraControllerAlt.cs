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
    private float _yaw = 0f;
    private float _pitch = 0f;
    
    public override void OnStartClient()
    {
        if (IsOwner)
        {
            _cameraPosition = new Vector3(_cameraPivot.position.x, _cameraPivot.position.y, _cameraPivot.position.z - _cameraOffset);
            Instantiate(_cameraPrefab, _cameraPosition, _cameraPivot.rotation, _cameraPivot);
        }

    }


    private void LateUpdate()
    {
        Vector2 lookDeltaThisTick = _lookAccum;
        _lookAccum =  Vector2.zero;

        MoveCamera(lookDeltaThisTick);
    }



    public void GetLookInput(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;

        _lookAccum += context.ReadValue<Vector2>();
    }

    
    
    
    private void MoveCamera(Vector2 lookDelta)
    {
        if (!IsOwner)
            return;

  
        _pitch -= lookDelta.y * sensitivity;
        _pitch = Mathf.Clamp(_pitch, downAngleLimit, upAngleLimit);
        
        _yaw += lookDelta.x * sensitivity;

        _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        
        lookDelta = Vector2.zero;
    }

    
}
