using Steamworks;
using UnityEngine;

public class SteamworksManager : MonoBehaviour
{
    [SerializeField] private uint appId = 480;
    private void Awake()
    {
        if (!SteamClient.IsValid)
        {
            try
            {
                SteamClient.Init(appId, true);
                DontDestroyOnLoad(gameObject);
                Debug.Log("Steam initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Steam init failed: {e}");
            }
        }
    }

    private void Update()
    {
        if (SteamClient.IsValid)
            SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if(SteamClient.IsValid)
            SteamClient.Shutdown();
    }
}
