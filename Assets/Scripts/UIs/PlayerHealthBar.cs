using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] Slider healthBarSlider;
    [SerializeField] Actor playerActor;

    private void OnEnable()
    {
        playerActor.OnDamage += UpdateHealthBar;
    }

    private void OnDisable()
    {
        playerActor.OnDamage -= UpdateHealthBar;
    }


    public void UpdateHealthBar(float damageAmount, float currentHealth, float maxHP)
    {
        healthBarSlider.maxValue = maxHP;
        healthBarSlider.value = currentHealth;
    }
}
