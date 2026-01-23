using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : NetworkBehaviour
{           
    [Header("Multiplayer")]
    private readonly SyncVar<bool> isReady = new SyncVar<bool>();
    public bool IsReady => isReady.Value;

    [Header("Movement")]
    [SerializeField] public float moveSpeed = 0;

    [Header("Input System")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction attackAction_normal;
    [SerializeField] private InputAction attackAction_strong;

    [Header("References")]
    private PlayerBulletSpawner playerBulletSpawner;

    [Header("Sprites")]
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

    // used to wait a bit for ownership
    private IEnumerator DelayedIsOwner()
    {
        yield return null;
        if (IsOwner)
        {
            moveAction?.Enable();                                            
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

        if (isReady.Value)                                           
        {
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

            // didn't have the time to change it, so it's still ugly and.. well. functional-ish
            // if the player.position.x is less than 0, the player gets the ID 1, if not then the ID 2
            if (transform.position.x < 0)               
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
        {
            // enabling all the moves
            if (IsOwner)
            {
                moveAction?.Enable();                     
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

        // there was input
        if (inputX != 0 || inputY != 0)
        {
            Move(inputX, inputY);
            Rotate(inputX, inputY);
            //Debug.Log("inputX: " + inputX + "/ inputY: " + inputY);
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

        // (this is ugly and for testing purpose only. pls do not scream)
        // surprise it's permanent for now: it is rotating the player character in the direction we are moving
        if (inputX > 0) transform.eulerAngles = new Vector3(0, 0, 0);
        if (inputX < 0) transform.eulerAngles = new Vector3(0, 0, 180);
        if (inputY > 0) transform.eulerAngles = new Vector3(0, 0, 90);
        if (inputY < 0) transform.eulerAngles = new Vector3(0, 0, 270);

    }
    #endregion

    #region Attacking
    [Server]
    private void CheckForAttack()
    { 
        if (!IsOwner) return;
        Debug.Log("(Player:) stronger attack?");
        int iD = gameObject.GetComponent<PlayerData>().playerId;

        // via the ID we can check if the player is able to pay for the stronger attack; if so, it will fire
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
    }
    #endregion

    #region Sprites/Animation
    // if the player gets damaged, the sprite will change to a different one to highlight that the player got hurt
    [ServerRpc]
    public void ChangeSpriteTemp()

    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();    
        renderer.sprite = spriteWhenDamaged;

        if(gameObject.GetComponent<PlayerData>().currentHealth > 0)
            StartCoroutine(ChangeSpriteTempBack(0.2f));
    }

    [Server]
    public IEnumerator ChangeSpriteTempBack(float delay)                                   
    {
        yield return new WaitForSeconds(delay);
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();     
            renderer.sprite = spriteBasic;
        }
    }

    [Server]
    public IEnumerator ChangeSpriteDead(float delay)
    // mini two-part animation if the player is dead
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();  
        renderer.sprite = spriteDeath1;
        yield return new WaitForSeconds(delay);
        renderer.sprite = spriteDeath2;
        yield return new WaitForSeconds(delay+0.3f);

        Die();
    }
    #endregion

    #region Dying
    [Server]
    public void AttemptToDie(float delay)
    {
        StartCoroutine(ChangeSpriteDead(0.02f));
    }

    [Server]
    public void Die()
    {
        Debug.Log("You're dead now! YAY.");

        // disabling all of the moves
        if (IsOwner)
        {
            moveAction?.Disable();
            attackAction_normal?.Disable();
            attackAction_strong?.Disable();
        }
        // setting back the isReady
        isReady.Value = !isReady.Value;

        NetworkGameManager.Instance.gameState.Value = GameState.Finished;
    }
    #endregion
}
