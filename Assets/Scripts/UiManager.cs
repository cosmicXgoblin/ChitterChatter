using FishNet.Object;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] GameObject menuJoinServer;
    [SerializeField] GameObject Level1;

    [SerializeField] GameObject areUReadyScreen;

    [Header("Finished")]
    [SerializeField] private GameObject ConnectionScreen;
    [SerializeField] private GameObject FinishedScreen;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        UiOnAwake();
    }

    private void UiOnAwake()
    {
        menuJoinServer.SetActive(false);
        areUReadyScreen.SetActive(false);
        Level1.SetActive(false);
    }

    public void UiOnStartGame()
    {
        Level1.SetActive(true);
    }

    #region ClickTo(...)
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
    #endregion

    private void RestartGameUi()
    {
        FinishedScreen.SetActive(false);
        Level1.SetActive(false);
        areUReadyScreen.SetActive(true);
    }




}
