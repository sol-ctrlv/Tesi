using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb2D;
    [SerializeField] SpriteRenderer spriteRender;

    [SerializeField] PlayerInput playerInput;
    [SerializeField] Animator animator;
    [SerializeField] float resetAttackTime = .5f;
    [SerializeField] TargetDetection targetDetection;
    [SerializeField] float damage = 1f;
    [SerializeField] float recoilForce = 10f;
    InputAction attackAction;

    bool canAttack = true;

    private void Start()
    {
        attackAction = playerInput.actions.actionMaps[0].FindAction("Attack");
        attackAction.performed += Attack;
    }

    private void OnDestroy()
    {
        attackAction.performed -= Attack;
    }

    void Attack(InputAction.CallbackContext context)
    {
        if (!canAttack)
            return;

        StartCoroutine(BigPush(rb2D, spriteRender.flipX ? Vector2.right : Vector2.left, 1000f, .5f));

        canAttack = false;
        playerInput.enabled = false;
        animator.SetBool("IsAttacking", !canAttack);
        StartCoroutine(ResetAttack());

        for (int i = 0; i < targetDetection.TargetsInRange.Count; i++)
        {
            IDamageable damageable = targetDetection.TargetsInRange[i].GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.Damage(damage);
            }
        }
    }

    IEnumerator ResetAttack()
    {
        yield return new WaitForSeconds(resetAttackTime);
        canAttack = true;
        playerInput.enabled = true;
        animator.SetBool("IsAttacking", !canAttack);
    }

    public IEnumerator BigPush(Rigidbody2D rb, Vector2 dir, float strength, float duration)
    {
        // piccolo burst iniziale
        rb.AddForce(dir.normalized * (strength * 0.4f), ForceMode2D.Impulse);

        // poi spingi il resto spalmandolo in tempo
        float t = 0f;
        while (t < duration)
        {
            rb.AddForce(dir.normalized * (strength * 0.6f) * Time.deltaTime, ForceMode2D.Force);
            t += Time.deltaTime;
            yield return null;
        }
    }

}
