using FishNet.Example.ColliderRollbacks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.UIElements;

public class BulletSpawner: NetworkBehaviour
{
    enum SpawnerType { Straight, Spin }
    public static BulletSpawner Instance;

    [Header("Spawner Attributes")]
    [SerializeField] private SpawnerType spawnerType;
    [SerializeField] private float firingRate = 1f;
    [SerializeField] public float currentHealth;
    [SerializeField] public float maxHealth;
    [SerializeField] private GameObject HealthPoint;

    [Header("Bullet Attributes")]
    public GameObject Bullet;
    private float bulletLife = 10f;
    private float speed = 1f;

    [Header("Spawned Bullet")]
    public GameObject spawnedBullet;
    private float timer = 0f;



    public readonly SyncVar<float> SpawnerHealth = new SyncVar<float>();

    public override void OnStartServer()
    {
        if (!IsServerInitialized) return;

        //if (Instance == null) Instance = this;
        //else
        //{
        //    Destroy(gameObject);
        //    Debug.Log("multiple Instances of BulletSpawner found. DESTROY.");
        //}
    }

    private void Awake()
    {

        SpawnerHealth.OnChange += (oldVal, newVal, asServer) =>
        {
            currentHealth = newVal;
        };        
    }

    [Server]
    void Update()
    {
        timer += Time.deltaTime;

        //if it is type spin, spin it on the z - axis
        if (spawnerType == SpawnerType.Spin) transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z + 1f);

        if (timer >= firingRate)
        {
            Fire();
            timer = 0;
        }

        SpriteRenderer renderer = HealthPoint.GetComponent<SpriteRenderer>();
        if (currentHealth == maxHealth) renderer.color = Color.green;
        else
        {
            if (maxHealth / 2 <= currentHealth) renderer.color = Color.yellow;
            else renderer.color = Color.red;
        }

        //if (currentHealth == 0)
        //    Die();
    }

    public void Fire()
    {
        if (!IsServerInitialized)
            return;

        if (NetworkGameManager.Instance.CurrentState == GameState.Playing)
        {
            spawnedBullet = Instantiate(Bullet, this.gameObject.transform.position, Quaternion.identity);
            // spawnedBullet.transform.parent = transform;
            spawnedBullet.GetComponent<Bullet>().speed = speed;
            spawnedBullet.GetComponent<Bullet>().bulletLife = bulletLife;
            spawnedBullet.transform.rotation = transform.rotation;
            // Spawn it on all clients (server authority)
            Spawn(spawnedBullet);
            Debug.Log(spawnedBullet + ": bulletLife: " + bulletLife);
        }
    }

    public void Die()
    {
        Debug.Log("BulletSpawner sagt byebye");
        Despawn(DespawnType.Destroy);
        Destroy(this.gameObject);
    }

}
