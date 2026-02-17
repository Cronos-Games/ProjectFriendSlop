using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks.Data;
using UnityEngine;

public class LobbyOverlayController : MonoBehaviour
{
    SteamLobbyManager lobbyManager;

    [Header("Player List")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LobbyMemberRow rowPrefab;

    // Cache avatars so opening the overlay is instant after first load
    private readonly Dictionary<SteamId, Texture2D> _avatarCache = new();

    private Lobby? _currentLobby;

    private void Awake()
    {
        if (!TryGetComponent<SteamLobbyManager>(out lobbyManager))
        {
            Debug.LogError("LobbyOverlayController could not find SteamLobbyManager");
        }
    }

    public void Show()
    {
        panel.SetActive(true);
        _ = Refresh();
    }

    public void Hide()
    {
        panel.SetActive(false);
    }


    private async Task Refresh()
    {
        _currentLobby = lobbyManager.GetLobby();

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        if (_currentLobby is not Lobby lobby)
            return;

        foreach (var member in lobby.Members)
        {
            var row = Instantiate(rowPrefab, contentRoot);


            if (member.Id == lobby.Owner.Id)
                row.SetName(member.Name + " (Host)");
            else
                row.SetName(member.Name);


            var texture = await GetAvatarTextureCached(member.Id, async () =>
            {
                var img = await member.GetMediumAvatarAsync();

                if (!img.HasValue)
                    return null; //set placeholder image?

                return ToTexture2D(img.Value);
            });

            row.SetAvatar(texture);

        }
    }

    private async Task<Texture2D> GetAvatarTextureCached(SteamId id, System.Func<Task<Texture2D>> factory)
    {
        if (_avatarCache.TryGetValue(id, out var cached) && cached != null)
            return cached;

        try
        {
            var texture = await factory();
            _avatarCache[id] = texture;
            return texture;
        }
        catch
        {
            return null;
        }
    }

    private Texture2D ToTexture2D(Steamworks.Data.Image img)
    {
        var texture = new Texture2D((int)img.Width, (int)img.Height, TextureFormat.RGBA32, false);

        texture.LoadRawTextureData(img.Data);
        texture.Apply();

        return texture;
    }
}
