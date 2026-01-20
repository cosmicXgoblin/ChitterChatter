using FishNet.Object;
using System.Collections;
using UnityEngine;

public class BallSpawner : NetworkBehaviour
{
    public static BallSpawner Instance;
    [SerializeField] private GameObject ballPrefab;

    void Start()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Server]
    public IEnumerator SpawnBall(float delay)                                       // es darf nur einen Spawner geben
    {
        yield return new WaitForSeconds(delay);
        if (NetworkGameManager.Instance.CurrentState == GameState.Playing)
        {
            GameObject ballInstance = Instantiate(ballPrefab);                    // Instance: 0,0,0
            Spawn(ballInstance);
        }
    }
}