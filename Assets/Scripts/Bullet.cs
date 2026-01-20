using FishNet.Object;
using FishNet.Component.Transforming.Beta;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class Bullet : NetworkBehaviour
{
    //public float bulletLife = 1f;
    public readonly SyncVar<float> bulletLifeSync = new SyncVar<float>();
    public float bulletLife;
    public float rotation = 0f;
    public float speed = 1f;

    public float damage = 5f;

    private Vector2 spawnPoint;
    private float timer = 0f;



    void Start()
    {
         bulletLifeSync.Value = bulletLife;
        // save our spawn point
        spawnPoint = new Vector2(transform.position.x,transform.position.y);
    }

    [Server]
    void Update()
    {
        // if the timer is greater as the bulletLife, we destroy it
        if (timer > bulletLife)
        {
            Destroy(this.gameObject);
        }
        // uptick of the timer
        timer += Time.deltaTime;
        // position update
        transform.position = Movement(timer);
    }

    private Vector2 Movement(float timer)
    {
        // moves right according to the bullets rotation
        float x = timer * speed * transform.right.x;
        float y = timer * speed * transform.right.y;
        // gives back the new position
        return new Vector2(x + spawnPoint.x, y + spawnPoint.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            NetworkGameManager.Instance.TakeDamage(other, damage);
            Despawn(DespawnType.Destroy);
            Destroy(this.gameObject);
        }
    }
}
