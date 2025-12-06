using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MainMenuManager : MonoBehaviour
{
    public static int[] levelsToPlay;
    public static int currentLevelIndex = 0;

    private AudioSource _myAudioSource;

    private void Start()
    {
        _myAudioSource = GetComponent<AudioSource>();
        levelsToPlay = GetRandomScenes();
    }

    public static void PlayNextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex == 3)
        {
            Application.OpenURL("https://forms.gle/VmJStR8b4iT1S4RA9");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        SceneManager.LoadScene(levelsToPlay[currentLevelIndex]);
    }

    private void ResumeTime()
    {
        Time.timeScale = 1f;
    }

    public void StartGame()
    {
        _myAudioSource.Play();
        StartCoroutine(WaitOneSecondAndStartGame());
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
        SceneManager.LoadScene(levelsToPlay[currentLevelIndex]);
    }

    IEnumerator WaitOneSecondAndMainMenu()
    {
        yield return new WaitForSeconds(2.0f);
        Cursor.lockState = CursorLockMode.None;
        ResumeTime();
        SceneManager.LoadScene(1);
    }

    int[] GetRandomScenes()
    {
        // 2, 3, 4 devono essere randomizzati
        // 5 deve rimanere sempre per ultimo
        int[] result = { 2, 3, 4, 5 };

        // Fisher–Yates solo sui primi 3 elementi (indici 0..2)
        for (int i = 0; i < 3; i++)
        {
            int r = UnityEngine.Random.Range(i, 3); // max è escluso, quindi [i, 2]
            int temp = result[i];
            result[i] = result[r];
            result[r] = temp;
        }

        // result[3] è sempre 5, mai toccato
        return result;
    }

}