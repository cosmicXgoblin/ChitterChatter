using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Dependencies.Utilities;
using JetBrains.Annotations;
using System;
using System.Linq;
using TMPro;
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
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private GameObject player1StateBox;
    [SerializeField] private TMP_Text player1StateText;
    [SerializeField] private GameObject player1HealthBar;
    [SerializeField] private TMP_Text player1Score;
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private TMP_Text healthBarValueText;

    [Header("UI Player1")]
    [SerializeField] private TMP_Text player2NameText;
    [SerializeField] private GameObject player2StateBox;
    [SerializeField] private TMP_Text player2StateText;
    [SerializeField] private GameObject player2HealthBar;
    [SerializeField] private TMP_Text player2Score;
    [SerializeField] private Slider healthBarSlider2;
    [SerializeField] private TMP_Text healthBarValueText2;

    [Header("UI LevelManagement")]
    [SerializeField] private GameObject startscreen;
    [SerializeField] private GameObject Level0;
    [SerializeField] private GameObject Level1;

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
    private readonly SyncVar<int> Player1Score = new SyncVar<int>();
    private readonly SyncVar<int> Player2Score = new SyncVar<int>();

    [Header("Game")]
    public readonly SyncVar<GameState> gameState = new SyncVar<GameState>();
    public GameState CurrentState => gameState.Value;

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
            Player1Score.Value = 0;
            Player2Score.Value = 0;
            UpdateScore();

            Level1.SetActive(true);
            Despawn(Level0);                // yes this works
            gameState.Value = GameState.Playing;

            // for testing only
            SpawnBulletSpawner(15f, Spawnpoint1, 1f, true);
            SpawnBulletSpawner(30f, Spawnpoint2, 3f, false);
            SpawnBulletSpawner(45f, Spawnpoint3, 0.5f, true);
        }
    }

    public void SetPlayerData()
    {
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
    
    private void UpdateScore()
    {
        player1Score.text = Player1Score.Value.ToString();
        player2Score.text = Player2Score.Value.ToString();

        //player1Score.text = Player1Score.Value.ToString();
        //player2Score.text = "This is a test HELP";

        Debug.Log("Score was updated");
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
                break;
            case GameState.Finished:
                stateText.text = "Finished";
                stateTextBox.SetActive(false);
                break;
        }
    }

    //public void UpdateHealthBar(Collider2D other)
    //{
    //    other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth.ToString();

    //    if (other.gameObject.gameObject.GetComponent<PlayerData>().playerId == 1)
    //    {
    //        //healthBarValueText.text = other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth.ToString()
    //        //                        + "/" + other.gameObject.gameObject.GetComponent<PlayerData>().maxHealth.ToString();

    //        //healthBarSlider.value = other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth;
    //        //healthBarSlider.maxValue = other.gameObject.gameObject.GetComponent<PlayerData>().maxHealth;

    //        //Player1Health.Value = other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth;

    //    }
    //    if (other.gameObject.gameObject.GetComponent<PlayerData>().playerId == 2)
    //    {
    //        healthBarValueText2.text = other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth.ToString()
    //                                + "/" + other.gameObject.gameObject.GetComponent<PlayerData>().maxHealth.ToString();

    //        healthBarSlider2.value = other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth;
    //        healthBarSlider2.maxValue = other.gameObject.gameObject.GetComponent<PlayerData>().maxHealth;

    //        Player2Health.Value = other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth;
    //    }

    [Server]
    private void SpawnBulletSpawner(float health, Transform spawn, float speed, bool spin)
    {
        spawnedBulletSpawner = Instantiate(bulletSpawner, this.transform.position, Quaternion.identity);
        spawnedBulletSpawner.transform.position = spawn.position;
        spawnedBulletSpawner.GetComponent<BulletSpawner>().firingRate = speed;

        if (spin) 
            spawnedBulletSpawner.GetComponent<BulletSpawner>().spawnerType = SpawnerType.Spin;

        //if (spin) spawnerType.Spin.Value;

        spawnedBulletSpawner.GetComponent<BulletSpawner>().SpawnerHealth.Value = health;
        spawnedBulletSpawner.GetComponent<BulletSpawner>().currentHealth = health;
        spawnedBulletSpawner.GetComponent<BulletSpawner>().maxHealth = health;
        
        Spawn(spawnedBulletSpawner);
        // Debug.Log("Bullet Spawner should be spawned now");
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

    //[Server]
    //public void Score(int owner)
    //{
    //    if (owner == 1) Player1Score.Value = 22;
    //    else Player2Score.Value =+ 1;
    //}

    //public void TempGetPoints(float points)
    //{
    //    score = score + points;
    //}

    //public void FireServer(GameObject bullet, Vector3 GameObject.transform.position, Quaternion.identity))
    //{
    //    FireServer(bullet, this.transform.position, Quaternion.identity);
    //    spawnedBullet = Instantiate(bullet, this.transform.position, Quaternion.identity);
    //    spawnedBullet.GetComponent<PlayerBullet>().speed = speed;
    //    spawnedBullet.GetComponent<PlayerBullet>().bulletLife = bulletLife;
    //    spawnedBullet.transform.rotation = transform.rotation;
    //}


    //public void TakeDamage(Collider2D other, float dmg)
    //{
    //    foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
    //    {
    //           if (player.IsOwner && other.gameObject.gameObject.GetComponent<PlayerData>().playerId == 1)
    //            {
    //                ChangeHealth1(other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth - dmg);
    //            }
    //            if (player.IsOwner && other.gameObject.gameObject.GetComponent<PlayerData>().playerId == 2)
    //            {
    //                ChangeHealth2(other.gameObject.gameObject.GetComponent<PlayerData>().currentHealth - dmg);
    //            }
    //    }
    //}







    //healthBarValueText.text = currentHealth.ToString() + "/" + maxHealth.ToString();

    //healthBarSlider.value = currentHealth;
    //healthBarSlider.maxValue = maxHealth;
}


    #endregion
    #region Scoring
    //[Server]
    //public void ScorePoint(int playerIndex)
    //{
    //    if (gameState.Value != GameState.Playing) return;
    //    if (playerIndex == 0)
    //        scoreP1.Value++;
    //    else if (playerIndex == 1)
    //        scoreP2.Value++;
    //    // check for win condition
    //    if (scoreP1.Value >= 10 || scoreP2.Value >= 10)
    //    {
    //        gameState.Value = GameState.Finished;
    //    }
    //    else
    //    {
    //        StartCoroutine(BallSpawner.Instance.SpawnBall(6f));
    //    }
    //}
    #endregion
//}


public enum GameState
{
    WaitingForPlayers,
    Playing,
    Finished
    // add: paused
}