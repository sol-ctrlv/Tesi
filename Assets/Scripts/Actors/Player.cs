using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : Actor
{
    static public Vector2 position => Instance ? Instance.gameObject.transform.position : Vector3.zero;
    static public Player Instance { get; private set; }

    [SerializeField] AudioSource healAudioSource;
    [SerializeField] float healCooldownSeconds = 15f;
    float nextAllowedHealTime = 0f;
    public bool IsHealOnCooldown => Time.time < nextAllowedHealTime;


    //[SerializeField] float healTimerSeconds = 10f;
    //Timer healTimer;

    private void Awake()
    {
        Instance = this;
        Init(MaxHP);

        CameraManager.SetCameraPosition(transform.position);
        //healTimer = new Timer(healTimerSeconds, true, false);

        //OnDamage += HealAfterDamage;
    }

    protected override void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //private void OnDestroy()
    //{
    //    OnDamage -= HealAfterDamage;
    //}

    //void HealAfterDamage(float a, float b, float c)
    //{
    //    healTimer.SetShouldTick(true);
    //}

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        Damage(1f);
    }

    public void Heal(float amount, bool bypassCooldown = false)
    {
        // niente heal se amount non è positivo
        if (amount <= 0f)
            return;

        if (CurrentHP >= MaxHP)
            return;

        // se non vogliamo bypassare e siamo ancora in cooldown, esci
        if (!bypassCooldown && Time.time < nextAllowedHealTime)
            return;

        // applichiamo la cura (Damage negativo = heal)
        Damage(-amount);

        if (healAudioSource != null)
        {
            healAudioSource.pitch = Random.Range(0.8f, 1.2f);
            healAudioSource.Play();
        }

        // se questa è una heal “normale” (es. life-steal), fa partire il cooldown
        if (!bypassCooldown)
        {
            nextAllowedHealTime = Time.time + healCooldownSeconds;
        }
    }


    //private void Update()
    //{
    //    if (healTimer.Tick(Time.deltaTime))
    //    {
    //        Heal(1);

    //        if (CurrentHP == MaxHP)
    //        {
    //            healTimer.SetShouldTick(false);
    //        }
    //    }
    //}

    public void AddMaxHealth()
    {
        MaxHP += 1;
        Heal(420f, true);
    }
}