using System;
using UnityEngine;

public class EnemyCharacter : Actor
{
    public Action<EnemyCharacter> OnDie;

    [SerializeField] Transform gunHolder;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] EnemyMovement enemyMovement;

    [SerializeField] BasicAttackBehaviour attack;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        Init(MaxHP);

        attack.Init();
        var fireTimer = attack.gameObject.GetComponent<FireTimer>();
        fireTimer.Init(1);
    }

    protected override void Die()
    {
        OnDie?.Invoke(this);
        base.Die();
    }

    public void SetAIEnabled(bool value)
    {
        enemyMovement.enabled = value;
        attack.gameObject.SetActive(value);
        //enabled = value;
    }
}
