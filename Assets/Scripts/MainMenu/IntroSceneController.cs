using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSceneController : MonoBehaviour
{
    [Header("Tempi")]
    [SerializeField] private float holdTime = 10f;      // quanto resta visibile il testo
    [SerializeField] private string nextSceneName = "MainMenu";
    private void Start()
    {
        StartCoroutine(RunIntro());
    }

    private IEnumerator RunIntro()
    {
        float t = 0f;

        // Aspetta holdTime oppure uno skip
        while (t < holdTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Carica direttamente il menu principale
        SceneManager.LoadScene(nextSceneName);
    }
}
