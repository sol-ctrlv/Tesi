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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnNextClick()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
