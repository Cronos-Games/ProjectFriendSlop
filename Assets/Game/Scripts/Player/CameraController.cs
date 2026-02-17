using UnityEngine;
using FishNet.Object;

public class CameraController : NetworkBehaviour
{
    [SerializeField] private Camera _cameraPrefab;
    [SerializeField] private Transform _cameraPosition;

    public override void OnStartClient()
    {
        if (IsOwner)
            Instantiate(_cameraPrefab, _cameraPosition.position, _cameraPosition.rotation, _cameraPosition);
    }
}
