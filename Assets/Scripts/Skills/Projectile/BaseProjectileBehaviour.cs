using UnityEngine;

public class BaseProjectileBehaviour : MonoBehaviour
{
    private float speed;
    private Vector2 direction;
    private float lifeTime = 5f;
    private float timer;
    private ProjectilesPool myPool;
    private float Damage;
    private Rigidbody2D rb2d;

    [SerializeField] Color hitParticleColor;

    public void Launch(Vector2 dir, float spd, float dmg, float lftm)
    {
        direction = dir.normalized;
        speed = spd;
        timer = 0f;
        Damage = dmg;
        lifeTime = lftm;
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.linearVelocity = (Vector3)(direction * speed);
    }

    public void SetPool(ProjectilesPool pool)
    {
        myPool = pool;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            ResetProjectile();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Actor otherActor = collision.gameObject.GetComponent<Actor>();

        if (otherActor != null) {

            otherActor.Damage(Damage);

            ResetProjectile();

            ParticleSystemSpawner.EmitOnPosition(otherActor.transform.position, ParticleSystemSpawner.ParticleType.Death, hitParticleColor, 50);
        }
    }

    private void ResetProjectile()
    {
        if (!gameObject.activeSelf)
            return;

        if (myPool)
        {
            myPool.ReturnProjectile(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
