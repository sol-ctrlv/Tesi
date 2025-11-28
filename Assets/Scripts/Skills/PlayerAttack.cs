using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Animator animator;
    [SerializeField] float resetAttackTime = .5f;
    [SerializeField] TargetDetection targetDetection;
    [SerializeField] float damage = 1f;
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
}
