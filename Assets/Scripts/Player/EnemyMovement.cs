using UnityEngine;

public class EnemyMovement : ActorMovement
{
    [SerializeField] TargetDetection targetDetection;
    [SerializeField] float movementSpeed = 50f;
    [SerializeField] float repelForce = 8f;
    [SerializeField] bool stopOnPlayerInRange = true;

    public void SetMovementSpeed(float value)
    {
        movementSpeed = value;
    }

    private void Update()
    {
        if (stopOnPlayerInRange && targetDetection.TargetsInRange.Count > 0)
        {
            rb2d.linearVelocity = Vector2.zero;
            ManageAnimator(0f);
            return;
        }

        Movement();
        ManageAnimator(movementSpeed);
    }

    private void Movement()
    {
        Vector2 dir = (Player.position - (Vector2)transform.position).normalized;
        rb2d.linearVelocity = dir * movementSpeed * Time.fixedDeltaTime;
        movDir = dir;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            Vector2 pushDir = (transform.position - collision.transform.position).normalized;
            rb2d.AddForce(pushDir * repelForce, ForceMode2D.Force);
        }
    }
}
