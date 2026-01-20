using FishNet.Object;
using UnityEngine;

public class BallController : NetworkBehaviour
{
    private Rigidbody rb;
    private float roundTime;

    private Vector3 lastVelocity;
    private Vector3 goalVelocity;

    private void Start()
    {
        if (!IsServerInitialized) Destroy(this);                                                // wenn nicht Server, kein Zugriff aufs Skript

        rb = GetComponentInChildren<Rigidbody>();
        goalVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f), 0);        // bekommt nach Spawn zufällige Bewegungsrichtung (besser in CoRoutine für QoL)
    }

    private void FixedUpdate()
    {
        roundTime += Time.fixedDeltaTime;                                                       // Rundentimer wird 1 hochgezählt
        lastVelocity = rb.linearVelocity;                                                       // letzte Velocity wird just in case gespeichert
        rb.linearVelocity = goalVelocity.normalized * Mathf.Max(roundTime / 10f, 4f);           // wenn die Runde zu lange wird, wird der Ball schneller: nimmt größeren Wert der beiden
    }

    private void OnCollisionEnter(Collision col)
    {
        ContactPoint cp = col.contacts[0];
        goalVelocity = Vector3.Reflect(lastVelocity, cp.normal);
        Debug.Log("LV" + lastVelocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            //case "LeftGoal":
            //    NetworkGameManager.Instance.ScorePoint(0);
            //    break;
            //case "RightGoal":
            //    NetworkGameManager.Instance.ScorePoint(1);
            //    break;
            //default: return;
        }
        Despawn(DespawnType.Destroy);                                                           // vordefiniert von FishNet, @client
        Destroy(gameObject);                                                                    // info@server
    }
}
