using UnityEngine;
using UnityEngine.Rendering;

public class EnemyMovement : ActorMovement
{
    [SerializeField] TargetDetection targetDetection;
    [SerializeField] float movementSpeed = 50f;
    [SerializeField] bool stopOnPlayerInRange = true;

    public void SetMovementSpeed(float value)
    {
        movementSpeed = value;
    }

    private void Update()
    {
        if (stopOnPlayerInRange && targetDetection.TargetsInRange.Count > 0)
        {
            rb2d.angularVelocity = 0f;
            rb2d.linearVelocity = Vector2.zero;
            ManageAnimator(0f);
            return;
        }

        Movement();
        ManageAnimator(movementSpeed);
    }

    public void Movement()
    {
        Vector2 dirToPlayer = (Player.position - new Vector2(transform.position.x, transform.position.y)).normalized;
        rb2d.linearVelocity = dirToPlayer * movementSpeed * Time.fixedDeltaTime;

        movDir = dirToPlayer;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Push this enemy away
            Vector3 direction = transform.position - collision.transform.position;
            direction.Normalize();
            collision.transform.position += direction * Time.deltaTime * movementSpeed;
        }
    }
}
