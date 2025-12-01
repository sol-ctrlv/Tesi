using System;
using UnityEngine;

public class EnemyCharacter : Actor
{
    public Action<EnemyCharacter> OnDie;

    [SerializeField] Transform gunHolder;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] EnemyMovement enemyMovement;
    [SerializeField] BasicAttackBehaviour attack;
    [SerializeField] BoxCollider2D myCollider;

    private void Awake()
    {
        Init();
        OnDamage += HealAfterDamage;
    }

    private void OnDestroy()
    {
        OnDamage -= HealAfterDamage;
    }

    void HealAfterDamage(float a, float b, float c)
    {

    }

    //IEnumerator 

    public void Init()
    {
        Init(MaxHP);

        attack.Init();
        var fireTimer = attack.gameObject.GetComponent<FireTimer>();
        fireTimer.Init();
    }

    protected override void Die()
    {
        OnDie?.Invoke(this);
        enemyMovement.enabled = false;
        enemyMovement.rb2d.linearVelocity = Vector2.zero;
        animator.SetBool("Dead", true);
        Destroy(attack.gameObject);
        myCollider.enabled = false;
        //base.Die();
    }

    public void SetAIEnabled(bool value)
    {
        enemyMovement.enabled = value;
        attack.gameObject.SetActive(value);
        animator.SetBool("Moving", value);
        //enabled = value;

    }
}
