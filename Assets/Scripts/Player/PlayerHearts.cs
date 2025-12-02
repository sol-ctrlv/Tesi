using UnityEngine;
using UnityEngine.UI;

public class PlayerHearts : MonoBehaviour
{
    [SerializeField] Image[] playerHeart;
    [SerializeField] Sprite fullHeart, emptyHeart;

    [SerializeField] float currentMaxHP = 3;

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
        int difference = (int)(maxHP - currentMaxHP);

        if (difference != 0)
        {
            for (int i = 0; i < difference; i++)
            {
                playerHeart[(int)currentMaxHP + i].enabled = true;
            }

            currentMaxHP = maxHP;
        }

        for (int i = 0; i < maxHP; i++)
        {
            playerHeart[i].sprite = currentHp > i ? fullHeart : emptyHeart;
        }
    }

}
