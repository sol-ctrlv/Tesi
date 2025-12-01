public class SnowmanFireTimer : FireTimer
{
    protected override void OnFire()
    {
        myAnimator.SetTrigger("Attack");
    }

    public void PlayAttackAnimation()
    {
        Fire();
    }
}
