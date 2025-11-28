using System;
using UnityEngine;

public class Actor : MonoBehaviour, IDamageable
{
    public Action<float, float, float> OnDamage;

    [SerializeField] protected float MaxHP = 100, CurrentHP;
    [SerializeField] bool isAlive = true;

    public void Damage(float amount)
    {
        CurrentHP -= amount;

        if (CurrentHP <= 0 && isAlive)
        {
            isAlive = false;
            Die();
            return;
        }

        OnDamage?.Invoke(amount, CurrentHP, MaxHP);
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    public virtual void Init(float maxHP)
    {
        MaxHP = maxHP;
        CurrentHP = MaxHP;
    }
}
