using FishNet.Example.ColliderRollbacks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerBulletSpawner : NetworkBehaviour
{
    public static PlayerBulletSpawner Instance;

    [Header("Bullet Attributes")]
    public GameObject bullet_normal;
    public GameObject bullet_strong;
    public float bulletLife = 1f;
    public float speed = 1f;

    private GameObject spawnedBullet;
    public int shootFromPlayer;

    #region Init
    public override void OnStartServer()
    {
        if (!IsServerInitialized) return;

        if (Instance != null) Instance = this;
        //else Destroy(gameObject);
    }

    private void Awake()
    {
        //shootFromPlayer = gameObject.GetComponent<PlayerData>().playerId;
        Debug.Log(shootFromPlayer);
    }
    #endregion

    #region Attacking
    [Server]
    public void AttemptToFire()
     {
        if (!IsServerInitialized) //!IsOwner)
            return;

        shootFromPlayer = gameObject.GetComponent<PlayerData>().playerId;
        spawnedBullet = Instantiate(bullet_normal, this.transform.position, Quaternion.identity);
 
        spawnedBullet.GetComponent<PlayerBullet>().speed = speed;
        spawnedBullet.GetComponent<PlayerBullet>().bulletLife = bulletLife;
        spawnedBullet.GetComponent<PlayerBullet>().owner = shootFromPlayer;
        spawnedBullet.transform.rotation = transform.rotation;

        // Spawn it on all clients (server authority)
        Spawn(spawnedBullet);

        Debug.Log("Sollte gefeuert haben");
    }

    public void AttemptToFireStrong()
    {
        if (!IsServerInitialized) //!IsOwner)
            return;

        shootFromPlayer = gameObject.GetComponent<PlayerData>().playerId;
        spawnedBullet = Instantiate(bullet_strong, this.transform.position, Quaternion.identity);

        spawnedBullet.GetComponent<PlayerBullet>().speed = speed;
        spawnedBullet.GetComponent<PlayerBullet>().bulletLife = bulletLife;
        spawnedBullet.GetComponent<PlayerBullet>().owner = shootFromPlayer;
        spawnedBullet.transform.rotation = transform.rotation;

        // Spawn it on all clients (server authority)
        Spawn(spawnedBullet);

        Debug.Log("Sollte gefeuert haben");
    }
    #endregion
}



