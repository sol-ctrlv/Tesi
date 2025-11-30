using System;
using UnityEngine;

public class Actor : MonoBehaviour, IDamageable
{
    public Action<float, float, float> OnDamage;

    [SerializeField] protected float MaxHP = 100, CurrentHP;
    [SerializeField] bool isAlive = true;
    [SerializeField] protected Animator animator;

    public void Damage(float amount)
    {
        CurrentHP -= amount;
        OnDamage?.Invoke(amount, CurrentHP, MaxHP);

        if (CurrentHP <= 0 && isAlive)
        {
            isAlive = false;
            Die();
            return;
        }

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
