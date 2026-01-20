using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : NetworkBehaviour
{                                                                                       // beide sind privat damit spieler nicht drankommt: readonly = public get, private set 
    [Header("Multiplayer")]
    private readonly SyncVar<bool> isReady = new SyncVar<bool>();
    public bool IsReady => isReady.Value;

    [Header("Movement")]
    [SerializeField] public float moveSpeed = 0;

    [Header("Input System")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction attackAction;

    // References
    private PlayerBulletSpawner playerBulletSpawner;

    #region Inits             
    private void OnDisable()
    {
        if (!IsOwner) return;

        moveAction?.Disable();
        attackAction?.Disable();    

        if (TimeManager != null)
            TimeManager.OnTick -= OnTick;
}

    private void Start()
    {
        // playerBulletSpawner = this.gameObject.GetComponentInChildren<PlayerBulletSpawner>();
        playerBulletSpawner = gameObject.GetComponent<PlayerBulletSpawner>();
        StartCoroutine(DelayedIsOwner());
    }

    private IEnumerator DelayedIsOwner()
    {
        yield return null;  // n frame abwarten um zu schauen ob ownerhip gesetzt ist (stoppt hier bis die funktion wieder aufgerufen wird)
        if (IsOwner)
        {
            moveAction?.Enable();                                                           // ? = if not null
            attackAction?.Enable();

            if (TimeManager != null)
                TimeManager.OnTick += OnTick;
        }
    }
    #endregion

    private void OnTick()
    {
        if (!IsOwner) return;

        if (isReady.Value)                                                  // hübscher: if die eintritt als erstes
        {
            if (attackAction.triggered) CheckForAttack();
            HandleInput();
        }
    }

    #region ReadyStateHandling
    [ServerRpc]
    public void SetReadyStateServerRpc(string name)
    {
        isReady.Value = !isReady.Value;

        // TO-DO: pls, for the love of everything, change it
        if (transform.position.x < 0)                                                   // wenn der spieler links ist
        {
            NetworkGameManager.Instance.Player1Name.Value = name;
            this.GetComponent<PlayerData>().playerId = 1;

            if (IsReady) NetworkGameManager.Instance.Player1State.Value = " is ready";
            else NetworkGameManager.Instance.Player1State.Value = " is not ready";
        }
        else
        {
            NetworkGameManager.Instance.Player2Name.Value = name;
            this.GetComponent<PlayerData>().playerId = 2;

            if (IsReady) NetworkGameManager.Instance.Player2State.Value = " is ready";
            else NetworkGameManager.Instance.Player2State.Value = " is not ready";
        }

        NetworkGameManager.Instance.DisableNameField(Owner, isReady.Value);
        NetworkGameManager.Instance.CheckAndStartGame();
    }

    #endregion

    #region Movement
    private void HandleInput()
    {
        float inputX = moveAction.ReadValue<Vector2>().x;
        float inputY = moveAction.ReadValue<Vector2>().y;

        if (inputX != 0 || inputY != 0)
        {
            Move(inputX, inputY);                               // wenn kein input, dann nichts an server senden
            Rotate(inputX, inputY);
            Debug.Log("inputX: " + inputX + "/ inputY: " + inputY);
        }

    }

    [ServerRpc]
    private void Move(float inputX, float inputY)
    {
        float newX = transform.position.x + inputX * moveSpeed * (float)TimeManager.TickDelta;
        float newY = transform.position.y + inputY * moveSpeed * (float)TimeManager.TickDelta;

        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    [ServerRpc]
    private void Rotate(float inputX, float inputY)
    {
        //if (inputX > 0) Transform.Rotate.z = -180;

        // this is ugly and for testing purpose only. pls do not scream.
        if (inputX > 0) transform.eulerAngles = new Vector3(0, 0, 0);
        if (inputX < 0) transform.eulerAngles = new Vector3(0, 0, 180);
        if (inputY > 0) transform.eulerAngles = new Vector3(0, 0, 90);
        if (inputY < 0) transform.eulerAngles = new Vector3(0, 0, 270);
        //if (inputX != 0)
        //{
        //if (inputX > 0) transform.rotation = new Vector3(0, 0, 180);
        //if (inputX != 0) transform.rotation = Quaternion.Euler(0, 0, inputX);
        //if (inputX < 0) transform.rotation = Quaternion.Euler(0, 0, -180);
        //}
    }
    #endregion

    [ServerRpc]
    private void CheckForAttack()
    {
        //if(IsOwner)
        //{
            Debug.Log("(Player:) ATTACK!");
            playerBulletSpawner.AttemptToFire();
        //}

    }
}
