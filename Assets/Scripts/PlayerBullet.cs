using FishNet.Object;
using FishNet.Component.Transforming.Beta;
using UnityEngine;

public class PlayerBullet : NetworkBehaviour
{
    [Header("Bullet Attributes")]
    public float bulletLife = 1f;
    public float rotation = 0f;
    public float speed = 1f;
    public int owner;
    public float damage = 5f;

    private Vector2 spawnPoint;
    private float timer = 0f;

    #region Init
    [Server]
    void Start()
    {
        if (!IsServerInitialized) Destroy(this);

        // save the spawn point
        spawnPoint = new Vector2(transform.position.x, transform.position.y);
    }
    #endregion

    [Server]
    void Update()
    {
        // if the timer is greater as the bulletLife, we destroy it
        if (timer > bulletLife) Destroy(this.gameObject);
        // uptick of the timer
        timer += Time.deltaTime;
        // position update
        transform.position = Movement(timer);
    }

    [Server]
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
        // if it collides with an Enemy, it will registered as damage and destroyed
        //Debug.Log("Damage to Enemy was done");
        if (other.tag == "Enemy")
        {
            Debug.Log("Damage to Enemy will be done");

            NetworkGameManager.Instance.TakeDamageFromPlayer(other, damage, owner);

            Die();
        }
    }

    private void Die()
    {
        Despawn(DespawnType.Destroy);
        Destroy(this.gameObject);
        //Debug.Log("ByeBye PlayerBullet with the owner: " + owner);
    }
}
