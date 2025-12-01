using System;
using UnityEngine;

public class Actor : MonoBehaviour, IDamageable
{
    public Action<float, float, float> OnDamage;

    [SerializeField] protected float MaxHP = 3, CurrentHP;
    [SerializeField] bool isAlive = true;
    [SerializeField] protected Animator animator;
    [SerializeField] protected AudioSource hurtAudioSource, dieAudioSource;

    public void Damage(float amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP - amount, 0, MaxHP);
        OnDamage?.Invoke(amount, CurrentHP, MaxHP);

        if (!(Mathf.Sign(amount) < 0))
        {
            hurtAudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
            hurtAudioSource.Play();
        }

        if (CurrentHP <= 0 && isAlive)
        {
            isAlive = false;
            Die();
            dieAudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
            dieAudioSource.Play();
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
