using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Dependencies.Utilities;
using JetBrains.Annotations;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static BulletSpawner;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private GameObject stateTextBox;
    [SerializeField] private TMP_InputField PlayerNameField;
    [SerializeField] private Button ReadyButton;
    [SerializeField] private TMP_Text ReadyButtonText;
    [SerializeField] public TMP_Text playerIndexText;

    [Header("UI Player1")]
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private GameObject player1StateBox;
    [SerializeField] private TMP_Text player1StateText;
    [SerializeField] private GameObject player1HealthBar;
    [SerializeField] private TMP_Text player1ScoreText;
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private TMP_Text healthBarValueText;

    [Header("UI Player2")]
    [SerializeField] private Transform player2SpawnPoint;
    [SerializeField] private TMP_Text player2NameText;
    [SerializeField] private GameObject player2StateBox;
    [SerializeField] private TMP_Text player2StateText;
    [SerializeField] private GameObject player2HealthBar;
    [SerializeField] private TMP_Text player2ScoreText;
    [SerializeField] private Slider healthBarSlider2;
    [SerializeField] private TMP_Text healthBarValueText2;

    [Header("UI LevelManagement")]
    [SerializeField] private GameObject startscreen;
    [SerializeField] private GameObject Level0;
    [SerializeField] private GameObject Level1;
    [SerializeField] private GameObject FinishedScreen;

    [Header("UI Spawner")]
    [SerializeField] private GameObject bulletSpawner;
    private GameObject spawnedBulletSpawner;

    [Header("Spawnpoints for Testing")]
    [SerializeField] private Transform Spawnpoint1;
    [SerializeField] private Transform Spawnpoint2;
    [SerializeField] private Transform Spawnpoint3;

    [Header("SyncVars Player")]
    public readonly SyncVar<string> Player1Name = new SyncVar<string>();
    public readonly SyncVar<string> Player2Name = new SyncVar<string>();
    public readonly SyncVar<string> Player1State = new SyncVar<string>();
    public readonly SyncVar<string> Player2State = new SyncVar<string>();
    public readonly SyncVar<float> Player1Health = new SyncVar<float>();
    public readonly SyncVar<float> Player2Health = new SyncVar<float>();
    public readonly SyncVar<int> Player1Score = new SyncVar<int>();
    public readonly SyncVar<int> Player2Score = new SyncVar<int>();

    public int player1Score => Player1Score.Value;
    public int player2Score => Player2Score.Value;

    [Header("Game")]
    public readonly SyncVar<GameState> gameState = new SyncVar<GameState>();
    public GameState CurrentState => gameState.Value;
    [SerializeField] public int round = 0;

    [Header("Manager")]
    private GameObject UIManager;

    private void Awake()
    {
        // Instance it
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Suscribe to changing things
        gameState.OnChange += OnStateChanged;

        Player1Score.OnChange += (oldVal, newVal, asServer) => UpdateScore();
        Player2Score.OnChange += (oldVal, newVal, asServer) => UpdateScore();

        Player1Name.OnChange += (oldVal, newVal, asServer) =>
        {
            if (player1NameText != null)
                player1NameText.text = newVal;
        };
        Player2Name.OnChange += (oldVal, newVal, asServer) =>
        {
            if (player2NameText != null)
                player2NameText.text = newVal;
        };

        Player1State.OnChange += (oldVal, newVal, asServer) =>
        {
            if (player1StateText != null)
                player1StateText.text = newVal;
        };
        Player2State.OnChange += (oldVal, newVal, asServer) =>
        {
            if (player2StateText != null)
                player2StateText.text = newVal;
        };
        Player1Health.OnChange += (oldVal, newVal, asServer) =>
        {
            foreach (var playerData in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (playerData.playerId == 1) playerData.currentHealth = newVal;
                healthBarSlider.value = newVal;
                healthBarValueText.text = newVal.ToString() + " / " + playerData.maxHealth;
            }
        };
        Player2Health.OnChange += (oldVal, newVal, asServer) =>
        {
            foreach (var playerData in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (playerData.playerId == 2) playerData.currentHealth = newVal;
                healthBarSlider2.value = newVal;
                healthBarValueText2.text = newVal.ToString() + " / " + playerData.maxHealth;
            }
        };
        // disable the statebox until we need it and activate it
        stateTextBox.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameState.Value = GameState.WaitingForPlayers;
    }

    #region State-Handling
    [Server]
    public void CheckAndStartGame()
    {
        if (CurrentState != GameState.WaitingForPlayers) return;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length >= 2 && players.All(p => p.IsReady))
        {
            SetPlayerData();
            UpdateScore();

            UiOnStartGameRpc();
            //LevelScreen.SetActive(true);
            //Despawn(Level0);

            gameState.Value = GameState.Playing;

            // for testing only
            SpawnBulletSpawner(15f, Spawnpoint1, 1f, true);
            SpawnBulletSpawner(30f, Spawnpoint2, 3f, false);
            SpawnBulletSpawner(45f, Spawnpoint3, 0.5f, true);
            Debug.Log("BulletSpawner should be spawned now");

            //UiOnStartGameRpc();
        }
    }

    public void SetPlayerReady()
    {

        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.IsOwner)
            {
                if (!player.IsReady)
                {
                    ReadyButton.image.color = Color.green;
                    ReadyButtonText.text = "i need a moment";
                }
                else
                {
                    ReadyButton.image.color = Color.white;
                    ReadyButtonText.text = "I AM READY!";
                }
                player.SetReadyStateServerRpc(PlayerNameField.text);
            }
        }
    }
    
    public void SetPlayerUnready()
    {
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            player.SetUnreadyStateServerRpc();

            ReadyButton.image.color = Color.white;
            ReadyButtonText.text = "I AM READY!";
            PlayerNameField.gameObject.SetActive(true);
        }
    }

    [TargetRpc]
    public void DisableNameField(NetworkConnection con, bool isOff)
    {
        PlayerNameField.gameObject.SetActive(!isOff);
    }

    private void OnStateChanged(GameState oldState, GameState newState, bool asServer)
    {
        UpdateStateText();
    }

    private void UpdateStateText()
    {
        if (stateText == null) return;

        switch (gameState.Value)
        {
            case GameState.WaitingForPlayers:
                stateText.text = "Waiting for players";
                stateTextBox.SetActive(false);
                break;
            case GameState.Playing:
                startscreen.SetActive(false);
                stateTextBox.SetActive(true);
                player1StateBox.SetActive(false);
                player2StateBox.SetActive(false);
                Level0.SetActive(false);
                Level1.SetActive(true);
                break;
            case GameState.Finished:
                stateText.text = "Finished";
                UiGameStateFinished();
                //stateTextBox.SetActive(false);
                //FinishedScreen.SetActive(true);
                foreach (var bulletSpawner in FindObjectsByType<BulletSpawner>(FindObjectsSortMode.None))
                {
                    Despawn(bulletSpawner);
                }
                    break;
        }
    }  

    #endregion

    #region Rpc-calls
    [ServerRpc]
    private void UiOnStartGameRpc()
    {
        UiManager.Instance.UiOnStartGame();
    }
    #endregion
   
    // So, long story short, i was so stressed out that i completly forgot everything i knew + the difference between NetworkGameManager & Game Manager.
    // The longer the project went on, the more i struggled with organizing.
    // If i find the time, i'll come back and organize it the right way, atm it's my personal bowl of spaghetti code.
    #region GameManager
    public void SetPlayerData()
    {
        Player1Score.Value = 0;
        Player2Score.Value = 0;

        foreach (var playerData in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            if (playerData.IsOwner && playerData.playerId == 1)
            {
                playerData.playerName = Player1Name.ToString();
                Player1Health.Value = playerData.maxHealth;
            }
            else
            {
                playerData.playerName = Player2Name.ToString();
                Player2Health.Value = playerData.maxHealth;
            }

        SetPlayerBulletSpawner();
    }

    [Server]
    public void ResetPlayerData()
    {
        foreach (var playerData in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            if (playerData.IsOwner && playerData.playerId == 1)
            {
                Player1Name.Value = null;
                Player1Health.Value = 0;
                Player1Score.Value = 0;
                playerData.gameObject.transform.position = player1SpawnPoint.transform.position;
            }
            else
            {
                Player2Name.Value = null;
                Player2Health.Value = 0;
                Player2Score.Value = 0;
                playerData.gameObject.transform.position = player2SpawnPoint.transform.position;
            }
    }

    public void TakeDamage(Collider2D other, float dmg)
    {
        // friendly fire is on atm - TODO: toggle it
        if (other.tag == "Player")
        {
            if (other.gameObject.gameObject.GetComponent<PlayerData>().playerId == 1)
            {
                Player1Health.Value = Player1Health.Value - dmg;
                other.gameObject.GetComponent<PlayerController>().ChangeSpriteTemp();

                if (Player1Health.Value <= 0)
                {
                    other.gameObject.GetComponent<PlayerController>().AttemptToDie(0.02f);
                }
            }
            else if (other.gameObject.gameObject.GetComponent<PlayerData>().playerId == 2)
            {
                Player2Health.Value = Player1Health.Value - dmg;
                other.gameObject.GetComponent<PlayerController>().ChangeSpriteTemp();

                if (Player1Health.Value <= 0)
                {
                    other.gameObject.GetComponent<PlayerController>().AttemptToDie(0.02f);
                }
            }
        }
        //else if (other.tag == "Enemy")
        //{
        //    //other.GetComponent<BulletSpawner>().currentHealth = -dmg;
        //    other.gameObject.GetComponent<BulletSpawner>().SpawnerHealth.Value =
        //        other.gameObject.GetComponent<BulletSpawner>().SpawnerHealth.Value - dmg;
        //    Debug.Log("Damage to Enemy was done");
        //}
    }
    public void TakeDamageFromPlayer(Collider2D other, float dmg, int owner)
    {
        if (other.tag == "Enemy")
        {
            //other.GetComponent<BulletSpawner>().currentHealth = -dmg;
            other.gameObject.GetComponent<BulletSpawner>().SpawnerHealth.Value =
                other.gameObject.GetComponent<BulletSpawner>().SpawnerHealth.Value - dmg;
            Debug.Log("Damage to Enemy was done by " + owner);
        }

        if (other.GetComponent<BulletSpawner>().currentHealth <= 0)
        {
            other.GetComponent<BulletSpawner>().AttemptToDie();

            if (owner == 1) Player1Score.Value++;
            if (owner == 2) Player2Score.Value++;
            else Debug.Log("This bullet was without parents, so no score for anybody. Sorry!");
            Debug.Log(Player1Score.Value.ToString() + " / " + Player2Score.Value.ToString());
        }
    }

    [Server]
    public void RestartGame()
    {
        ResetPlayerData();
        SetPlayerUnready();

        gameState.Value = GameState.WaitingForPlayers;
        round++;
    }
    
    public void PayForStrongAttack(int playerId)
    {
        //if (playerId == 1)
        //{
        //    if (Player1Score.Value >= 1)
        //        Player1Score.Value = Player1Score.Value - 1;
        //    playerBulletSpawner.AttemptToFire();
        //}
        //if (playerId == 2)
        //{
        //    if (Player2Score.Value >= 1)
        //        Player2Score.Value = Player1Score.Value - 1;
        //}
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
               if (playerId == 1)
                {
                    if (Player1Score.Value >= 1)
                        Player1Score.Value = Player1Score.Value - 1;
                }
                if (playerId == 2)
                {
                    if (Player2Score.Value >= 1)
                        Player2Score.Value = Player1Score.Value - 1;
                } 
            }



        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.IsOwner)
            {
                player.gameObject.GetComponent<PlayerBulletSpawner>().shootFromPlayer = player.gameObject.GetComponent<PlayerData>().playerId;
            }
        }
    }

    private void UiGameStateFinished()
    {
        UiManager.Instance.UiOnFinishedGame();
        UiManager.Instance.UiOnFinishedGameScore(Player1Name.Value.ToString(), Player1Score.Value.ToString(), 
                                                    Player2Name.Value.ToString(), Player2Score.Value.ToString());
    }

    //stateTextBox.SetActiv
    #endregion

    #region PlayerController
    private void SetPlayerBulletSpawner()
    {
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.IsOwner)
            {
                player.gameObject.GetComponent<PlayerBulletSpawner>().shootFromPlayer = player.gameObject.GetComponent<PlayerData>().playerId;
            }
        }
    }
    #endregion

    #region UiManager
    private void UpdateScore()
    {
        player1ScoreText.text = Player1Score.Value.ToString();
        player2ScoreText.text = Player2Score.Value.ToString();
    }
    #endregion

    #region SpawnManager
    [Server]
    private void SpawnBulletSpawner(float health, Transform spawn, float speed, bool spin)
    {
        spawnedBulletSpawner = Instantiate(bulletSpawner, this.transform.position, Quaternion.identity);
        spawnedBulletSpawner.transform.position = spawn.position;
        //spawnedBulletSpawner.gameObject.GetComponent<BulletSpawner>().firingRate = speed;
        spawnedBulletSpawner.gameObject.GetComponentInChildren<BulletSpawner>().firingRate = speed;

        if (spin)
            spawnedBulletSpawner.GetComponentInChildren<BulletSpawner>().spawnerType = SpawnerType.Spin;

        //if (spin) spawnerType.Spin.Value;

        spawnedBulletSpawner.GetComponentInChildren<BulletSpawner>().SpawnerHealth.Value = health;
        spawnedBulletSpawner.GetComponentInChildren<BulletSpawner>().currentHealth = health;
        spawnedBulletSpawner.GetComponentInChildren<BulletSpawner>().maxHealth = health;

        Spawn(spawnedBulletSpawner);
        // Debug.Log("Bullet Spawner should be spawned now");
    }
    #endregion
}

public enum GameState
{
    WaitingForPlayers,
    Playing,
    Finished
    // add: paused
}