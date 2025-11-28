using UnityEngine;
using UnityEngine.UI;

public class PlayerHearts : MonoBehaviour
{
    [SerializeField] Image[] playerHeart;
    [SerializeField] Sprite fullHeart, emptyHeart;

    void OnDestroy()
    {
        Player.Instance.OnDamage -= EditUi;
    }

    private void Update()
    {
        if (Player.Instance == null)
            return;

        Player.Instance.OnDamage += EditUi;
        enabled = false;
    }

    private void EditUi(float damage, float currentHp, float maxHP)
    {
        for (int i = 0; i < maxHP; i++)
        {
            playerHeart[i].sprite = currentHp > i ? fullHeart : emptyHeart;
        }
    }

}
