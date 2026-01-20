using FishNet.Managing;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    // script is NOT inheriting from NetworkBehaviour bc it's active before and after the network is active
    [SerializeField] private NetworkManager _networkManager;

    void Start()
    {
        if (_networkManager == null)
        {
            _networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();

        }
    }

    public void StartHost()
    {
        StartServer();
        StartClient();
    }

    public void StartServer()
    {
        _networkManager.ServerManager.StartConnection();
    }

    public void StartClient()
    {
        _networkManager.ClientManager.StartConnection();
    }

    public void SetIPAdress(string text)
    {
        _networkManager.TransportManager.Transport.SetClientAddress(text);
    }
}
