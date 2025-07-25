using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public string sceneName;
    public GameObject loadingUI;
    public Slider progressBar;
    public Image fadeImage;
    public float fadeDuration = 1f;

    public void LoadSceneWithFade()
    {
       
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        
        // Fade to black
        yield return StartCoroutine(Fade(0, 1));

        loadingUI.SetActive(true);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Progress Bar updates
        while (asyncLoad.progress < 0.9f)
        {
            progressBar.value = asyncLoad.progress;
            yield return null;
        }

        // Smooth finish to 100%
        while (progressBar.value < 1f)
        {
            progressBar.value += Time.deltaTime;
            yield return null;
        }

        // Wait a second and fade out (optional)
        yield return new WaitForSeconds(0.5f);

        // Allow scene activation
        asyncLoad.allowSceneActivation = true;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
    }
}