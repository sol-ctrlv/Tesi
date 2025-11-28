using UnityEngine;

public class FireTimer : MonoBehaviour
{
    private float shootTimerSpeedMultiplier = 1f;
    public float cooldown;
    BasicAttackBehaviour attackBehaviour;
    [SerializeField] private Timer fireTimer;

    public void UpdateShootTimerMultiplier(float newTimerMultiplier)
    {
        shootTimerSpeedMultiplier = newTimerMultiplier;
    }

    public void Init(float baseShootTimerSpeedMultiplier)
    {
        attackBehaviour = GetComponent<BasicAttackBehaviour>();
        fireTimer = new Timer(cooldown, false);
        shootTimerSpeedMultiplier = baseShootTimerSpeedMultiplier;
    }

    private void Update()
    {
        float counter = Time.deltaTime * shootTimerSpeedMultiplier;
        if (fireTimer.Tick(counter))
        {
            attackBehaviour.Fire();
            fireTimer.Set(Random.Range(cooldown * 0.2f, cooldown));
            fireTimer.Reset();
        }
    }

}
