using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] GameObject menuJoinServer;
    [SerializeField] GameObject Level0;
    [SerializeField] GameObject Level1;
    void Awake()
    {
        menuJoinServer.SetActive(false);
        Level0.SetActive(false);
        Level1.SetActive(false);
    }


}
