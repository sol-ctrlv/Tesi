using System;
using UnityEngine;

public class EnemyCharacter : Actor
{
    public Action OnDie;

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
        Init(2f);

        //var fireTimer = attack.gameObject.GetComponent<FireTimer>();
        //fireTimer.Init(1);
    }

    protected override void Die()
    {
        OnDie?.Invoke();
        base.Die();
    }
}
