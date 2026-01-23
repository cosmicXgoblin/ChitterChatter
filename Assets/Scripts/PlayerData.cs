using FishNet.Managing;
using FishNet.Object;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [Header("Data")]
    [SerializeField] public string playerName;
    [SerializeField] public int playerId;
    [SerializeField] public float currentHealth;
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public int currentDamage;
    [SerializeField] public int standardDamage;
    public float currentSpeed = 5f;
    public int standardSpeed;

    [Header("References")]
    private PlayerController playerController;
    private NetworkManager networkManager;

    public void Awake()
    {
        // referencing our NetworkManager & PlayerController
        if (networkManager == null) networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        playerController = gameObject.GetComponent<PlayerController>();
    }

    private void Update()
    {
        // updating the moveSpeed. shoudl be moved to NetworkGameManger, PlayerData ist just for HAVING the data, not doing things with it :3
        playerController.moveSpeed = currentSpeed;
    }
}




