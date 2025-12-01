using UnityEngine;

public class FireTimer : MonoBehaviour
{
    public float cooldown;
    BasicAttackBehaviour attackBehaviour;
    [SerializeField] private Timer fireTimer;
    [SerializeField] protected Animator myAnimator;
    [SerializeField] protected TargetDetection targetDetection;

    public void Init()
    {
        attackBehaviour = GetComponent<BasicAttackBehaviour>();
        fireTimer = new Timer(cooldown, false);
    }

    private void Update()
    {
        float counter = Time.deltaTime;
        if (targetDetection.TargetsInRange.Count > 0 && fireTimer.Tick(counter))
        {
            OnFire();
        }
    }


    protected virtual void OnFire()
    {
        myAnimator.SetTrigger("Attack");
        Fire();
    }

    protected void Fire()
    {
        attackBehaviour.Fire();
        fireTimer.Set(Random.Range(cooldown * 0.8f, cooldown * 1.2f));
        fireTimer.Reset();
    }
}
