using FishNet;
using FishNet.Managing.Scened;
using Unity.VisualScripting;
using UnityEngine;

/*  TO-DO
with every sceneswitch, a new bootstrapthingie is added. instance it, thank you sm */

public class BootstrapSceneManager : MonoBehaviour
{
    // not inherting from NetworkBehaviour bc it should be running at all times

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Update()
    {
        if (!InstanceFinder.IsServer)             // InstanceFinder: allows interacting with FishNet w/o being a NetworkBehaviour-Script
            return;

        //// for testing purpose: loading between the scenes
        //if(Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    LoadScene("Scene1");
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    LoadScene("Scene2");
        //}
    }

    public void LoadScene(string sceneName)
    {
        SceneLoadData sld = new SceneLoadData(sceneName);
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    void UnloadScene(string sceneName)
    {
        SceneUnloadData sld = new SceneUnloadData(sceneName);
        InstanceFinder.SceneManager.UnloadGlobalScenes(sld);
    }

    public void Load2()
    {
        LoadScene("GameSzene");
        UnloadScene("Scene1");
    }

    public void Load1()
    {
        LoadScene("Scene1");
        UnloadScene("GameSzene");
    }

}
