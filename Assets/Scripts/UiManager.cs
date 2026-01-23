using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [Header("Menus")]
    [SerializeField] GameObject MenuJoinServer;

    [Header("Screens")]
    [SerializeField] private GameObject LevelScreen;
    [SerializeField] private GameObject ReadyScreen;
    [SerializeField] private GameObject ScoreScreen;
    [SerializeField] private GameObject ConnectionScreen;
    [SerializeField] private GameObject FinishedScreen;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI MessageScore;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        UiOnAwake();
    }

    #region UiOn...
    private void UiOnAwake()
    {
        MenuJoinServer.SetActive(false);
        ReadyScreen.SetActive(false);
        LevelScreen.SetActive(false);
    }

    public void UiOnStartGame()
    {
        LevelScreen.SetActive(true);
        ReadyScreen.SetActive(false);
    }

    public void UiOnFinishedGame()
    {
        LevelScreen.SetActive(false);
        ScoreScreen.SetActive(true);
       // ShowScoreUi(playerId);
    }


    #endregion

    #region Click(...)
    public void ClickRestartGame()
    {
        RestartGameUi();
        NetworkGameManager.Instance.RestartGame();
    }
    public void ClickMainMenu()
    {
        FinishedScreen.SetActive(false);
        ConnectionScreen.SetActive(true);
    }

    public void ClickExit()
    {
        Debug.Log("I'll take you with me on a trip. Close your eyes. Imagine you're in a field of daisies. You can hear water nearby. You can feel the wind on your skin." +
            "Imagine the game is closed. You can open your eyes now.");
    }
    
    public void ClickOkScore()
    {
        ScoreScreen.SetActive(false);
        FinishedScreen.SetActive(true);
    }
    #endregion



    #region Finished?
    public void UiOnFinishedGameScore(string Player1Name, string Player1Score, string Player2Name, string Player2Score)
    {
        MessageScore.text = "Player " + Player1Name + " with " + Player1Score + "\nPlayer " + Player2Name + " with " + Player2Score;
    }
    private void RestartGameUi()
    {
        FinishedScreen.SetActive(false);
        LevelScreen.SetActive(false);
        ReadyScreen.SetActive(true);
    }
    #endregion

}
