using System.Collections.Generic;
using UnityEngine;

public class BasicAttackBehaviour : MonoBehaviour
{
    public Stats ScalingStat => data.Scaling;
    [SerializeField] private AttackSO data;
    private TargetSelector targetSelector;
    private ProjectilesPool projectilePool;
    private bool isInit = false;

    float damageMultiplier = 1f;

    public void Init(AttackSO atkSO)
    {
        if (isInit) return;

        targetSelector = GetComponent<TargetSelector>();
        projectilePool = GetComponent<ProjectilesPool>();

        data = atkSO;
        targetSelector.Init();
        projectilePool.Init(atkSO,transform.parent.gameObject); //gunHolder

        isInit = true;
    }

    public void Fire()
    {
        Vector2 shootDirection = targetSelector.GetShootDirection();

        if (shootDirection == Vector2.zero)
        {
            return;
        }

        GameObject proj = projectilePool.GetObject();
        proj.transform.position = (Vector2)transform.position + shootDirection.normalized * data.SpawnOffset;
        proj.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg);
        proj.SetActive(true);

        if (proj.TryGetComponent(out BaseProjectileBehaviour mover))
        {
            mover.Launch(shootDirection, data.ProjectileSpeed, data.Damage * damageMultiplier, data.ProjectileLifetime);
            //mover.SetOwner(this);
            mover.SetPool(projectilePool);
        }
    }

    public void SetNewDamageMultiplier(float value)
    {
        damageMultiplier = value;
    }
}