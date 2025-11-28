using UnityEngine;

public class ActorMovement : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer actorRenderer;
    Animator animator;
    protected Rigidbody2D rb2d;
    protected Vector2 movDir;
    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    protected void ManageAnimator(float velocity)
    {
        if (velocity > 0.01f)
        {
            animator.SetBool("Moving", true);

            if (movDir.x != 0)
                actorRenderer.flipX = movDir.x < 0;
        }
        else
        {
            animator.SetBool("Moving", false);
        }
    }

    public Vector2 getMovementDir()
    {
        return movDir;
    }
}
