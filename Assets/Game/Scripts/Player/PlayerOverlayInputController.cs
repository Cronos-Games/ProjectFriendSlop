using FishNet.Object;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerOverlayInputController : NetworkBehaviour
{
    private LobbyOverlayController _overlay;
    private PlayerInput _playerInput;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (TryGetComponent<PlayerInput>(out PlayerInput pi))
        {
            pi.enabled = IsOwner;
        }
        else
        {
            Debug.LogError("Player object does not have PlayerInput module.");
        }


        _overlay = Object.FindFirstObjectByType<LobbyOverlayController>();

        if (_overlay != null)
        {
            Debug.LogError("LobbyOverlayController not found, make sure it is present in the scene");
        }
    }

    public void OnScoreboard(InputAction.CallbackContext context)
    {
        if (!IsOwner || _overlay == null)
            return;

        if (context.started)
            _overlay.Show();

        if (context.canceled)
            _overlay.Hide();
    }
}
