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
    [SerializeField] private InputAction attackAction_normal;
    [SerializeField] private InputAction attackAction_strong;

    // References
    private PlayerBulletSpawner playerBulletSpawner;

    //SpriteRenderer renderer = GetComponent<SpriteRenderer>();
    [SerializeField] Sprite spriteBasic;
    [SerializeField] Sprite spriteWhenDamaged;
    [SerializeField] Sprite spriteDeath1;
    [SerializeField] Sprite spriteDeath2;


    #region Inits             
    private void OnDisable()
    {
        if (!IsOwner) return;

        moveAction?.Disable();
        attackAction_normal?.Disable();
        attackAction_strong?.Disable();

        if (TimeManager != null)
            TimeManager.OnTick -= OnTick;
}

    private void Start()
    {
        playerBulletSpawner = gameObject.GetComponent<PlayerBulletSpawner>();
        StartCoroutine(DelayedIsOwner());
    }

    private IEnumerator DelayedIsOwner()
    {
        yield return null;  // n frame abwarten um zu schauen ob ownerhip gesetzt ist (stoppt hier bis die funktion wieder aufgerufen wird)
        if (IsOwner)
        {
            moveAction?.Enable();                                                           // ? = if not null
            attackAction_normal?.Enable();
            attackAction_strong?.Enable();

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
            //if (attackAction_normal.triggered)
            //    CheckForAttack();
            if (attackAction_normal.triggered)
                playerBulletSpawner.AttemptToFire();
            if (attackAction_strong.triggered)
                CheckForAttack();
                HandleInput();
        }
    }

    #region ReadyStateHandling
    [ServerRpc]
    public void SetReadyStateServerRpc(string name)
    {
        isReady.Value = !isReady.Value;

        //if (NetworkGameManager.Instance.round <= 1)
        //{
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
        //}
        //else
        {
            if (IsOwner)
            {
                moveAction?.Enable();                                                           // ? = if not null
                attackAction_normal?.Enable();
                attackAction_strong?.Enable();
            }
        }

        NetworkGameManager.Instance.DisableNameField(Owner, isReady.Value);
        NetworkGameManager.Instance.CheckAndStartGame();
    }

    [ServerRpc]
    public void SetUnreadyStateServerRpc()
    {
        isReady.Value = !isReady.Value;
        NetworkGameManager.Instance.Player1State.Value = " is not ready";
        NetworkGameManager.Instance.Player2State.Value = " is not ready";
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

    #region Attacking
    [Server]
    private void CheckForAttack()
     {
        Debug.Log("(Player:) stronger attack?");
        int iD = gameObject.GetComponent<PlayerData>().playerId;

        if (iD == 1)
        {
            Debug.Log(NetworkGameManager.Instance.Player1Score.Value);
            if (NetworkGameManager.Instance.player1Score != 0)
            {
                NetworkGameManager.Instance.PayForStrongAttack(iD);
                playerBulletSpawner.AttemptToFireStrong();
            }

        }
        if (iD == 2)
        {
            Debug.Log(NetworkGameManager.Instance.Player2Score.Value);
            if (NetworkGameManager.Instance.player2Score != 0)
            {
                NetworkGameManager.Instance.PayForStrongAttack(iD);
                playerBulletSpawner.AttemptToFireStrong();
            }

        }
        //PayForStrongAttack(iD);


    }
    #endregion

    #region Sprites/Animation
    [ServerRpc]
    public void ChangeSpriteTemp()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();       //syncvar?
        renderer.sprite = spriteWhenDamaged;

        StartCoroutine(ChangeSpriteTempBack(0.2f));
    }

    [Server]
    public IEnumerator ChangeSpriteTempBack(float delay)                                   
    {
        yield return new WaitForSeconds(delay);
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();       //syncvar?
            renderer.sprite = spriteBasic;
        }
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
    #endregion

    #region Dying
    [Server]
    public void AttemptToDie(float delay)
    {
        StartCoroutine(ChangeSpriteTemp(0.02f));
    }

    [Server][ServerRpc]
    public void Die()
    {
        Debug.Log("You're dead now! YAY.");

        if (IsOwner)
        {
            moveAction?.Disable();
            attackAction_normal?.Disable();
            attackAction_strong?.Disable();
        }
        isReady.Value = !isReady.Value;

        int iD = gameObject.GetComponent<PlayerData>().playerId;
        //UiManager.Instance.UiOnFinishedGame(iD);

        NetworkGameManager.Instance.gameState.Value = GameState.Finished;
    }
    #endregion
}
