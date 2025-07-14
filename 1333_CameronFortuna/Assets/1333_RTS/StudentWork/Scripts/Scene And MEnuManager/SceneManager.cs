using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneChanger : MonoBehaviour
{
    public string sceneName;

    // Use this when assigning from UI Button
    public void LoadScene()
    {
        LoadSceneByName(sceneName);
    }

    // Use this from code to pass in a specific scene
    public void LoadSceneByName(string sceneToLoad)
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void LoadSceneAsync()
    {
        LoadYourAsyncScene();
    }
   
        IEnumerator LoadYourAsyncScene()
    {

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
