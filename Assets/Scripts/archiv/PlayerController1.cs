using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController1 : NetworkBehaviour
{                                                                                       // beide sind privat damit spieler nicht drankommt: readonly = public get, private set 
    private readonly SyncVar<Color> playerColor = new SyncVar<Color>();
    private readonly SyncVar<bool> isReady = new SyncVar<bool>();
    public bool IsReady => isReady.Value;

    private Renderer playerRenderer;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;

    [Header("Input System")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction colorChangeAction;

    // Sachen werden gestartet / initialisiert oder eben nicht
    #region Inits             
    private void OnDisable()
    {
        playerColor.OnChange += OnColorChanged;
        if (!IsOwner) return;

        moveAction?.Disable();
        if (TimeManager != null)
            TimeManager.OnTick -= OnTick;
    }
    private void Start()
    {
        StartCoroutine(DelayedIsOwner());
    }

    private IEnumerator DelayedIsOwner()
    {
        playerColor.OnChange += OnColorChanged;
        playerRenderer = GetComponentInChildren<Renderer>();
        playerRenderer.material = new Material(playerRenderer.material);
        playerRenderer.material.color = playerColor.Value;
        yield return null;  // n frame abwarten um zu schauen ob ownerhip gesetzt ist (stoppt hier bis die funktion wieder aufgerufen wird)
        if (IsOwner)
        {
            ChangeColor(Random.value, Random.value, Random.value);

            moveAction?.Enable();                                                           // ? = if not null
            colorChangeAction?.Enable();
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
            HandleInput();
        }
        else
        {
            CheckForChangeColor();
        }
    }

      public void SetPlayerIndex()     // yeah , pls überarbeiten                                                        
      {
        //foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        //{
        //    if (IsOwner)
        //    {    
        if (transform.position.x < 0) NetworkGameManager.Instance.playerIndexText.text = "I am player 1";
        else NetworkGameManager.Instance.playerIndexText.text = "I am player 2";      
            //    }
            //}
      }


    #region ReadyStateHandling
    [ServerRpc]
    public void SetReadyStateServerRpc(string name)                         
    {
        isReady.Value = !isReady.Value;

        if (transform.position.x < 0)                                                   // wenn der spieler links ist
        {
            NetworkGameManager.Instance.Player1Name.Value = name;
            
            if (IsReady) NetworkGameManager.Instance.Player1State.Value = " is ready";
            else NetworkGameManager.Instance.Player1State.Value = " is not ready";
        }
        else
        {
            NetworkGameManager.Instance.Player2Name.Value = name;

            if (IsReady) NetworkGameManager.Instance.Player2State.Value = " is ready";
            else NetworkGameManager.Instance.Player2State.Value = " is not ready";
        }

        NetworkGameManager.Instance.DisableNameField(Owner, isReady.Value);         // namensfeld wird ausgeblendet
        NetworkGameManager.Instance.CheckAndStartGame();
    }

    //public void DisplayPlayerIndex(int playerIndex)
    //{
    //    foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
    //        if (player.IsOwner)
    //        {
    //            NetworkGameManager.Instance.playerIndexText.text = "I am player " + playerIndex + 1;
    //        }
    //}
    #endregion

    #region Movement
    private void HandleInput()
    {
        float input = moveAction.ReadValue<float>();
        if (input != 0) Move(input);                                // wenn kein input, dann nichts an server senden
    }

    [ServerRpc]

    private void Move(float input)
    {
        float newY = transform.position.y + input * moveSpeed * (float)TimeManager.TickDelta;
        newY = Mathf.Clamp(newY, minY, maxY);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    #endregion

    #region ColorChange
    private void CheckForChangeColor()                                 // habe ich die leertaste gedrückt um die farbe zu ändern?
    {
        if (!colorChangeAction.triggered) return;

        float r = Random.value;
        float g = Random.value;
        float b = Random.value;
        ChangeColor(r, g, b);                                          // anfrage @ server
    }

    [ServerRpc]
    private void ChangeColor(float r, float g, float b)                 // änderung auf server
    {
        playerColor.Value = new Color(r, g, b);
    }

    private void OnColorChanged(Color prevColor, Color newColor, bool asServer)     // rückmeldung von server
    {
        playerRenderer.material.color = newColor;
        // also possible: playerRenderer.material.color = playerColor.Value;
    }
    #endregion
}