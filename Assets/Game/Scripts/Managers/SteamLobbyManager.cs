using FishNet.Managing;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using UnityEngine;
using FishyFacepunch;

public class SteamLobbyManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private bool friendsOnly = true;
    [SerializeField] private GameObject inviteButton;

    private Lobby? _currentLobby;

    private void OnEnable()
    {
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void OnDisable()
    {
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }

    public async void CreateLobbyAndHost(int maxPlayers = 4)
    {
        _currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);

        if (_currentLobby == null)
        {
            Debug.LogError("Failed to create steam lobby.");
            return;
        }

        var lobby = _currentLobby.Value;

        if (friendsOnly)
        {
            lobby.SetFriendsOnly();
        } else
        {
            lobby.SetPublic();
        }

        lobby.SetJoinable(true);
        lobby.SetData("HostSteamId", SteamClient.SteamId.ToString());

        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();

        Debug.Log($"Lobby created: {lobby.Id} host = {SteamClient.SteamId}");

        inviteButton.SetActive(true);
    }

    public void OpenInviteOverlay()
    {
        if (_currentLobby == null)
            return;

        SteamFriends.OpenGameInviteOverlay(_currentLobby.Value.Id);
    }


    private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
    {
        Debug.Log($"Invite join requested. Lobby={lobby.Id} from friend={friendId}");
        await JoinLobbyAndConnect(lobby.Id);
    }

    public async Task JoinLobbyAndConnect(SteamId lobbyId)
    {
        var lobby = new Lobby(lobbyId);
        var enter = await lobby.Join();

        if(enter != RoomEnter.Success)
        {
            Debug.LogError($"Failed to join lobby: {enter}");
            return;
        }

        var hostSteamIdStr = lobby.GetData("HostSteamId");

        ulong rawSteamId;
        SteamId hostSteamId;

        if (ulong.TryParse(hostSteamIdStr, out rawSteamId))
        {
            hostSteamId = rawSteamId;
            if (!hostSteamId.IsValid)
            {
                hostSteamId = lobby.Owner.Id;
            }
        } else
        {
            hostSteamId = lobby.Owner.Id;
        }

            Debug.Log($"Joined lobby. Connecting to host steamId={hostSteamId}");

        var transport = networkManager.GetComponent<FishyFacepunch.FishyFacepunch>();
        transport.SetClientAddress(hostSteamId.ToString());

        networkManager.ClientManager.StartConnection();
    }


    public static void SetLobbyGrouping(Lobby lobby)
    {
        SteamFriends.SetRichPresence("steam_player_group", lobby.Id.ToString());

        SteamFriends.SetRichPresence("steam_player_group_size", lobby.MemberCount.ToString());
    }
}
