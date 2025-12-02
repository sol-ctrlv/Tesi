using UnityEngine;
using UnityEngine.UI;

public class Ladder : MonoBehaviour, IInteractable
{
    [SerializeField] string NextLevel;
    [SerializeField] GameObject EndGameUI;
    [SerializeField] Button NextLvlBtn;

    public bool Interact(GameObject interactor)
    {
        EndGameUI.SetActive(true);
        NextLvlBtn.interactable = false;
        NextLvlBtn.onClick.AddListener(ChangeScene);
        Time.timeScale = 0f;
        return true;
    }

    public void SetInteractableNextLvlBtn()
    {
        NextLvlBtn.interactable = true;
    }

    private void ChangeScene()
    {
        Time.timeScale = 1f;
        MainMenuManager.PlayNextLevel();
    }

}
