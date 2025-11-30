using UnityEngine;

public class SnowmanFireTimer : FireTimer
{
    [SerializeField] Animator animator;

    protected override void OnFire()
    {
        animator.SetBool("Attack", true);
    }

    public void PlayAttackAnimation()
    {
        base.OnFire();
        animator.SetBool("Attack", false);
    }
}
