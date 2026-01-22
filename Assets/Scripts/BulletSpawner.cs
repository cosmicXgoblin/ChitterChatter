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

 //enum SpawnerType { Straight, Spin }

public class BulletSpawner: NetworkBehaviour
{
    public enum SpawnerType { Straight, Spin }
    public static BulletSpawner Instance;

    [Header("Spawner Attributes")]
    public SpawnerType spawnerType;
    [SerializeField] public float firingRate;
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

    private bool strongerAttack = false;

    [SerializeField] Sprite spriteBasic;
    [SerializeField] Sprite spriteDeath1;
    [SerializeField] Sprite spriteDeath2;


    public readonly SyncVar<float> SpawnerHealth = new SyncVar<float>();
    public readonly SyncVar<Color> HealthPointColor = new SyncVar<Color>();

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
            UpdateHealthPoint();
        };
        HealthPointColor.OnChange += (oldVal, newVal, asServer) =>
        {
            SpriteRenderer renderer = HealthPoint.GetComponent<SpriteRenderer>();
            renderer.color = HealthPointColor.Value;
            //UpdateHealthPoint();
        };
        //SpriteRenderer renderer = HealthPoint.GetComponent<SpriteRenderer>();
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
        UpdateHealthPoint();
    }

    [Server]
    private void UpdateHealthPoint()
    {
        //SpriteRenderer renderer = HealthPoint.GetComponent<SpriteRenderer>();
        if (currentHealth == maxHealth)
        {
            //renderer.color = Color.green;
            HealthPointColor.Value = Color.green;
        }
        else
        {
            if (currentHealth <= maxHealth / 2)
            {
                //renderer.color = Color.yellow;                  // mach die color zu ner varsync | ok ne 
                HealthPointColor.Value = Color.yellow;
            }
            if (currentHealth <= maxHealth / 3)
            {
                //renderer.color = Color.red;
                HealthPointColor.Value = Color.red;
                strongerAttack = true;
            }
        }
    }

    public void Fire()
    {
        if (!IsServerInitialized)
            return;

        if (NetworkGameManager.Instance.CurrentState == GameState.Playing)
        {
            spawnedBullet = Instantiate(Bullet, this.gameObject.transform.position, Quaternion.identity);
            spawnedBullet.GetComponent<Bullet>().speed = speed;
            spawnedBullet.GetComponent<Bullet>().bulletLife = bulletLife;
            spawnedBullet.transform.rotation = transform.rotation;
                       
            if (strongerAttack) spawnedBullet.GetComponent<Bullet>().damage =+ 20;

            // Spawn it on all clients (server authority)
            Spawn(spawnedBullet);
            Debug.Log(spawnedBullet + ": bulletLife: " + bulletLife);
        }
    }

    [Server]
    public void AttemptToDie(float delay)
    {
        StartCoroutine(ChangeSpriteTemp(0.02f));

        //Die();
    }

    [Server]
    public IEnumerator ChangeSpriteTemp(float delay)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();       //syncvar?
        renderer.sprite = spriteDeath1;
        yield return new WaitForSeconds(delay);
        renderer.sprite = spriteDeath2;
        yield return new WaitForSeconds(delay);

        Die();
    }

    [Server]
    public void Die()
    {
        Debug.Log("BulletSpawner sagt byebye");
        Despawn(DespawnType.Destroy);
        Destroy(this.gameObject);
    }


    //[Server]
    //public IEnumerator ChangeSpriteTempBack(float delay)
    //{
    //    yield return new WaitForSeconds(delay);
    //    {
    //        SpriteRenderer renderer = GetComponent<SpriteRenderer>();       //syncvar?
    //        renderer.sprite = spriteBasic;
    //    }
    //}

}
