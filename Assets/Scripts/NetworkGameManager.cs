using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Dependencies.Utilities;
using JetBrains.Annotations;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private GameObject stateTextBox;
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private TMP_Text player1StateText;
    [SerializeField] private TMP_Text player2NameText;
    [SerializeField] private TMP_Text player2StateText;
    [SerializeField] private GameObject player1HealthBar;
    [SerializeField] private GameObject player2HealthBar;
    [SerializeField] private TMP_InputField PlayerNameField;
    [SerializeField] private Button ReadyButton;
    [SerializeField] private TMP_Text ReadyButtonText;
    [SerializeField] public TMP_Text playerIndexText;

    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private TextMeshProUGUI healthBarValueText;
    [SerializeField] private Slider healthBarSlider2;
    [SerializeField] private TextMeshProUGUI healthBarValueText2;

    [SerializeField] private GameObject startscreen;

    [SerializeField] private GameObject Level0;
    [SerializeField] private GameObject Level1;

    [SerializeField] private GameObject bulletSpawner;
    private GameObject spawnedBulletSpawner;

    public readonly SyncVar<string> Player1Name = new SyncVar<string>();
    public readonly SyncVar<string> Player2Name = new SyncVar<string>();
    public readonly SyncVar<string> Player1State = new SyncVar<string>();
    public readonly SyncVar<string> Player2State = new SyncVar<string>();
    public readonly SyncVar<float> Player1Health = new SyncVar<float>();
    public readonly SyncVar<float> Player2Health = new SyncVar<float>();

    //[SerializeField] private GameObject BulletSpawner1;

    //[Header("Score")]
    //private readonly SyncVar<int> scoreP1 = new SyncVar<int>();
    //private readonly SyncVar<int> scoreP2 = new SyncVar<int>();

    [Header("Game")]
    public readonly SyncVar<GameState> gameState = new SyncVar<GameState>();
    public GameState CurrentState => gameState.Value;
    [SerializeField] public float score = 0f;
    [SerializeField] public TextMeshProUGUI scoreText;


    private void Awake()
    {
        // Instance it
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Suscribe to changing things
        gameState.OnChange += OnStateChanged;
        //scoreP1.OnChange += (oldVal, newVal, asServer) => UpdateStateText();
        //scoreP2.OnChange += (oldVal, newVal, asServer) => UpdateStateText();

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
            //if (healthBarSlider != null)
            //    healthBarSlider.value = newVal;
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
        //Debug.Log("Server started Game Manager");
        gameState.Value = GameState.WaitingForPlayers;
        //scoreP1.Value = 0;
        //scoreP2.Value = 0;

        //healthBarSlider.value = 100f;
        //healthBarSlider2.value = 100f;

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
            Level1.SetActive(true);
            //Level0.SetActive(false);
            Despawn(Level0);                // yes this works
            gameState.Value = GameState.Playing;
            //StartCoroutine(BallSpawner.Instance.SpawnBall(5f));
            //BulletSpawner1.AttemptToFire();
            //BulletSpawner.Instance.Fire();

            SpawnBulletSpawner(40f);
        }
    }

    public void SetPlayerData()
    {
        // set up the players:
        // before starting the game, we want to set playerName, playerID, curerntHealth and maxHealth accorind to our settings

        // if we are having the player data with id 1
        //foreach (var playerData in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
        //    if (playerData.IsOwner && playerData.playerId == 1)
        //    {
        //        playerData.playerName = Player1Name.ToString();
        //        ChangeHealth1(playerData.currentHealth);
        //    }
        //    else
        //    {
        //        playerData.playerName = Player2Name.ToString();
        //        playerData.currentHealth = Player2Health.Value;
        //        ChangeHealth2(playerData.currentHealth);
        //    }
        foreach (var playerData in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            if (playerData.IsOwner && playerData.playerId == 1)
            {
                playerData.playerName = Player1Name.ToString();
                // ChangeHealth1(playerData.maxHealth);
                Player1Health.Value = playerData.maxHealth;
                //Debug.Log("MaxHealth for Player " + playerData.playerId + " is " + Player1Health.Value + 
                //" because the maxHealth was " + playerData.maxHealth + ".");
            }
            else
            {
                playerData.playerName = Player2Name.ToString();
                //ChangeHealth2(playerData.maxHealth);
                Player2Health.Value = playerData.maxHealth;
                //Debug.Log("MaxHealth for Player " + playerData.playerId + " is " + Player2Health.Value +
                //" because the maxHealth was " + playerData.maxHealth + ".");
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

                    //Set
                    //if (player.transform.position.x < 0) player1StateText.text = Player1Name.Value + " ist dabei.";
                    //else player2StateText.text = Player2Name.Value + " ist dabei.";

                }
                else
                {
                    ReadyButton.image.color = Color.white;
                    ReadyButtonText.text = "I AM READY!";

                    //if (player.transform.position.x < 0) player1StateText.text = Player1Name.Value + " ist nicht dabei.";
                    //else player2StateText.text = Player2Name.Value + " ist nicht dabei.";
                }
                player.SetReadyStateServerRpc(PlayerNameField.text);
                //player.SetPlayerIndex();

                //Debug.Log("Player is ready");
            }
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
                //stateText.text = $"{scoreP1.Value}:{scoreP2.Value}";
                startscreen.SetActive(false);
                stateTextBox.SetActive(true);
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
    private void SpawnBulletSpawner(float health)
    {
        spawnedBulletSpawner = Instantiate(bulletSpawner, this.transform.position, Quaternion.identity);
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
            }
            else if (other.gameObject.gameObject.GetComponent<PlayerData>().playerId == 2)
            {
                Player2Health.Value = Player1Health.Value - dmg;
            }
        }               
        else if (other.tag == "Enemy")
        {
            //other.GetComponent<BulletSpawner>().currentHealth = -dmg;
            other.gameObject.GetComponent<BulletSpawner>().SpawnerHealth.Value =
            other.gameObject.GetComponent<BulletSpawner>().SpawnerHealth.Value - dmg;
            Debug.Log("Damage to Enemy was done");
        }
    }

    public void TempGetPoints(float points)
    {
        score = score + points;
    }

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