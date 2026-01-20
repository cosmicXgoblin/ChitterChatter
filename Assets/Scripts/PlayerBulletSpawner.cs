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
    public GameObject bullet;
    public float bulletLife = 1f;
    public float speed = 1f;

    private GameObject spawnedBullet;

    public override void OnStartServer()
    {
        if (!IsServerInitialized) return;

        if (Instance != null) Instance = this;
        //else Destroy(gameObject);
    }

    [Server]
    public void AttemptToFire()
    {
        if (!IsServerInitialized) //!IsOwner)
            return;

        spawnedBullet = Instantiate(bullet, this.transform.position, Quaternion.identity);
        // adopt it
        //spawnedBullet.transform.parent = transform;
        spawnedBullet.GetComponent<PlayerBullet>().speed = speed;
        spawnedBullet.GetComponent<PlayerBullet>().bulletLife = bulletLife;
        //spawnedBullet.transform.rotation = transform.rotation;
        // Spawn it on all clients (server authority)
        Spawn(spawnedBullet);

        Debug.Log("Sollte gefeuert haben");
    }
}



