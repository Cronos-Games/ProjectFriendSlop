using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using UnityEngine;

public enum ConnectionMode
{
    Steam,
    Local
}

public class TransportManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkManager networkManager;

    [Header("Transport Settings")]
    [SerializeField] private int steamTransportIndex = 0;
    [SerializeField] private int tugboatTransportIndex = 1;
    

    [Header("Local Settings")]
    [SerializeField] private string localAddress = "127.0.0.1";
    [SerializeField] private ushort localPort = 7770;
    [SerializeField] private string serverBindAddressIPv4 = "0.0.0.0";

    [SerializeField] private bool startServerOnAllTransports = true;
    public void StartHost(ConnectionMode mode)
    {
        ApplyMode(mode);

        if (startServerOnAllTransports)
        {
            networkManager.ServerManager.StartConnection();
        } else
        {
            GetMultipass().StartConnection(true, GetIndex(mode));
        }

        networkManager.ClientManager.StartConnection();
    }


    public void StartClient(ConnectionMode mode)
    {
        ApplyMode(mode);

        networkManager.ClientManager.StartConnection();
    }

    public void StartServer(ConnectionMode mode)
    {
        ApplyMode(mode);

        if (startServerOnAllTransports)
            networkManager.ServerManager.StartConnection();
        else
            GetMultipass().StartConnection(true, GetIndex(mode));
    }

    private void ApplyMode(ConnectionMode mode)
    {
        var mp = GetMultipass();

        // Configure Tugboat settings if Local.
        if (mode == ConnectionMode.Local)
        {
            var tugboat = mp.GetTransport<Tugboat>(); // direct access to the transport inside Multipass :contentReference[oaicite:4]{index=4}

            // These are the common Tugboat fields shown in docs. :contentReference[oaicite:5]{index=5}
            tugboat.SetClientAddress(localAddress);
            tugboat.SetPort(localPort);

            // Depending on FishNet version this property name can vary.
            // If this line errors, check Tugboat inspector/API for the exact field name for IPv4 bind.
            tugboat.SetServerBindAddress(serverBindAddressIPv4, IPAddressType.IPv4);
        }

        // Tell Multipass which transport the CLIENT should use.
        // Multipass supports setting the client transport; after that you start client normally. :contentReference[oaicite:6]{index=6}
        if (mode == ConnectionMode.Local)
        {
            mp.SetClientTransport(typeof(Tugboat));
        }
        else
        {
            // Use the configured Steam transport slot by grabbing its runtime type from Multipass list.
            // This avoids hard-coding FishySteamworks vs FishyFacepunch types.
            Transport steam = mp.Transports[steamTransportIndex];
            mp.SetClientTransport(steam.GetType());
        }
    }

    private void ApplyLocalConnectionInfoIfNeeded(ConnectionMode mode)
    {
        if (mode != ConnectionMode.Local)
            return;
    }

    private int GetIndex(ConnectionMode mode)
    => (mode == ConnectionMode.Local) ? tugboatTransportIndex : steamTransportIndex;

    private Multipass GetMultipass()
    => networkManager.TransportManager.GetTransport<Multipass>();
}
