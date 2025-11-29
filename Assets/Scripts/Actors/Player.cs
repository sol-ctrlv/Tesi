using UnityEngine;

public class Player : Actor
{
    static public Vector2 position => Instance ? Instance.gameObject.transform.position : Vector3.zero;
    static public Player Instance { get; private set; }

    [SerializeField] float healTimerSeconds = 10f;

    Timer healTimer;

    private void Awake()
    {
        Instance = this;
        Init(MaxHP);

        CameraManager.SetCameraPosition(transform.position);
        healTimer = new Timer(healTimerSeconds, true, false);

        OnDamage += HealAfterDamage;
    }

    private void OnDestroy()
    {
        OnDamage -= HealAfterDamage;
    }

    void HealAfterDamage(float a, float b, float c)
    {
        healTimer.SetShouldTick(true);
    }

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        Damage(1f);
    }

    public void Heal(float amount)
    {
        Damage(-amount);

    }

    private void Update()
    {
        if (healTimer.Tick(Time.deltaTime))
        {
            Heal(1);

            if (CurrentHP == MaxHP)
            {
                healTimer.SetShouldTick(false);
            }
        }
    }
}