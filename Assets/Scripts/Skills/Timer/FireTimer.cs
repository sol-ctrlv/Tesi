using UnityEngine;

public class FireTimer : MonoBehaviour
{
    public float cooldown;
    BasicAttackBehaviour attackBehaviour;
    [SerializeField] private Timer fireTimer;

    public void Init()
    {
        attackBehaviour = GetComponent<BasicAttackBehaviour>();
        fireTimer = new Timer(cooldown, false);
    }

    private void Update()
    {
        float counter = Time.deltaTime;
        if (fireTimer.Tick(counter))
        {
            OnFire();
        }
    }


    protected virtual void OnFire()
    {
        attackBehaviour.Fire();
        fireTimer.Set(Random.Range(cooldown * 0.8f, cooldown * 1.2f));
        fireTimer.Reset();
    }
}
