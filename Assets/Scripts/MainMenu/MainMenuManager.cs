using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MainMenuManager : MonoBehaviour
{
    public static int[] levelsToPlay;
    public static int currentLevelIndex = -1;

    private AudioSource _myAudioSource;

    private void Start()
    {
        _myAudioSource = GetComponent<AudioSource>();
    }

    public static void PlayNextLevel()
    {
        currentLevelIndex++;
        SceneManager.LoadScene(levelsToPlay[currentLevelIndex]);
    }


    private void ResumeTime()
    {
        Time.timeScale = 1f;
    }

    public void StartGame()
    {
        _myAudioSource.Play();

        if (Time.timeScale > 0f)
        {
            StartCoroutine(WaitOneSecondAndStartGame());
        }
        else
        {
            ResumeTime();
            levelsToPlay = GetRandom123();
            PlayNextLevel();
        }

    }

    public void QuitGame()
    {
        _myAudioSource.Play();

        if (Time.timeScale > 0f)
            StartCoroutine(WaitOneSecondAndQuitGame());
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public void MainMenu()
    {
        _myAudioSource.Play();

        Cursor.visible = true;

        if (Time.timeScale > 0f)
        {
            StartCoroutine(WaitOneSecondAndMainMenu());
        }
        else
        {
            ResumeTime();
            SceneManager.LoadScene(0);
        }
    }

    IEnumerator WaitOneSecondAndQuitGame()
    {
        yield return new WaitForSeconds(1.0f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator WaitOneSecondAndStartGame()
    {
        yield return new WaitForSeconds(1.0f);
        Cursor.lockState = CursorLockMode.Locked;
        ResumeTime();
        SceneManager.LoadScene(1);
    }

    IEnumerator WaitOneSecondAndMainMenu()
    {
        yield return new WaitForSeconds(2.0f);
        Cursor.lockState = CursorLockMode.None;
        ResumeTime();
        SceneManager.LoadScene(0);
    }

    IEnumerator WaitOneSecondAndIntroScene()
    {
        yield return new WaitForSeconds(2.0f);
        ResumeTime();
        SceneManager.LoadScene(2);
    }


    int[] GetRandom123()
    {
        int[] arr = { 1, 2, 3 };
        for (int i = 0; i < arr.Length; i++)
        {
            int r = UnityEngine.Random.Range(i, arr.Length);
            int temp = arr[i];
            arr[i] = arr[r];
            arr[r] = temp;
        }

        int[] result = new int[4];
        arr.CopyTo(result, 0);
        result[3] = 4;

        return arr;
    }

}