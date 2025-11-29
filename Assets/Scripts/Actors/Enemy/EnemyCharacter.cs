using System;
using UnityEngine;

public class EnemyCharacter : Actor
{
    public Action<EnemyCharacter> OnDie;

    [SerializeField] Transform gunHolder;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] EnemyMovement enemyMovement;

    private BasicAttackBehaviour attack;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        Init(MaxHP);

        //var fireTimer = attack.gameObject.GetComponent<FireTimer>();
        //fireTimer.Init(1);
    }

    protected override void Die()
    {
        OnDie?.Invoke(this);
        base.Die();
    }

    public void SetAIEnabled(bool value)
    {
        enemyMovement.enabled = value;
        //enabled = value;
    }
}
