using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelecter : MonoBehaviour
{
    public static CharacterSO selectedCharacter;

    public void Select(CharacterSO character)
    {
        selectedCharacter = character;
        SceneManager.LoadSceneAsync("Arena");
    }
}
