using UnityEngine;

public class SnowmanFireTimer : FireTimer
{
    [SerializeField] AudioSource snowBallThrowAudioSource;

    protected override void OnFire()
    {
        myAnimator.SetTrigger("Attack");
    }

    public void PlayAttackAnimation()
    {
        Fire();
        snowBallThrowAudioSource.Play();
    }
}
