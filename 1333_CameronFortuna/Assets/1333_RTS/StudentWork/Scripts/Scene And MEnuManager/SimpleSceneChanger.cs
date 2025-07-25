using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneChanger : MonoBehaviour
{
    public string sceneName;


    public static SimpleSceneChanger SoundManager { get; private set; }
    private void Awake()
    {
        if (SoundManager == null)
        {
            SoundManager = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Use this when assigning from UI Button
    public void LoadScene()
    {
       
        LoadSceneByName(sceneName);
    }
    public void PlaySound()
    {
        SoundPracticePlayer.Instance.PlaySound(2, AudioSourceType.SFX);
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
