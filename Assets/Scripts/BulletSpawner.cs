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
    private bool strongerAttack = false;

    [Header("Spawner Sprites")]
    [SerializeField] Sprite spriteBasic;
    [SerializeField] Sprite spriteDeath1;
    [SerializeField] Sprite spriteDeath2;

    [Header("Bullet Attributes")]
    public GameObject Bullet_normal;
    public GameObject Bullet_moreDmg;
    private float bulletLife = 10f;
    private float speed = 1f;

    [Header("Spawned Bullet")]
    public GameObject spawnedBullet;
    private float timer = 0f;

    [Header("SyncVars")]
    public readonly SyncVar<float> SpawnerHealth = new SyncVar<float>();
    public readonly SyncVar<Color> HealthPointColor = new SyncVar<Color>();

    #region Init
    public override void OnStartServer()
    {
        if (!IsServerInitialized) return;
    }

    private void Awake()
    {
        // hooking it up
        SpawnerHealth.OnChange += (oldVal, newVal, asServer) =>
        {
            currentHealth = newVal;
            UpdateHealthPoint();
        };
        HealthPointColor.OnChange += (oldVal, newVal, asServer) =>
        {
            SpriteRenderer renderer = HealthPoint.GetComponent<SpriteRenderer>();
            renderer.color = HealthPointColor.Value;
   
        };
    }
    #endregion

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
    }

    [Server]
    private void UpdateHealthPoint()
    {
        if (currentHealth == maxHealth)
        {
            HealthPointColor.Value = Color.green;
        }
        else
        {
            if (currentHealth <= maxHealth / 2)
            {
                HealthPointColor.Value = Color.yellow;
            }
            if (currentHealth <= maxHealth / 3)
            {
                HealthPointColor.Value = Color.red;
                strongerAttack = true;
            }
        }
    }

    #region Attacking
    public void Fire()
    {
        if (!IsServerInitialized)
            return;

        // looks if this is a stronger attack or a normal one and instiates the corresponding bullet
        // after that, the soon-to-be spawned bullet gets it's attributes from the spawner
        if (NetworkGameManager.Instance.CurrentState == GameState.Playing)
        {
            if (!strongerAttack)
                spawnedBullet = Instantiate(Bullet_normal, this.gameObject.transform.position, Quaternion.identity);
            if (strongerAttack)
                spawnedBullet = Instantiate(Bullet_moreDmg, this.gameObject.transform.position, Quaternion.identity);

            spawnedBullet.GetComponent<Bullet>().speed = speed;
            spawnedBullet.GetComponent<Bullet>().bulletLife = bulletLife;
            spawnedBullet.transform.rotation = transform.rotation;
            // Spawn it on all clients (server authority)
            Spawn(spawnedBullet);
        }
    }
    #endregion

    #region Animation/Sprites

    // a short animation for a destroyed bullet spawner
    [Server]
    public IEnumerator ChangeSpriteTemp(float delay)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); 
        renderer.sprite = spriteDeath1;
        yield return new WaitForSeconds(delay);
        renderer.sprite = spriteDeath2;
        yield return new WaitForSeconds(delay);

        Die();
    }
    #endregion

    #region Dying
    [Server]
    public void Die()
    {
        Despawn(DespawnType.Destroy);
        Destroy(this.gameObject);
    }

    [Server]
    public void AttemptToDie()
    {
        // with a little bit of delay so we are able to see the animation
        StartCoroutine(ChangeSpriteTemp(0.02f));
    }
    #endregion
}
