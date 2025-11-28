using UnityEngine;
using UnityEngine.Events;

public class AttackSpawner : MonoBehaviour
{
    public static AttackSpawner singleton;
    public UnityEvent<AttackSO,Transform> OnSpawnedAttack;
    private void Awake()
    {
        singleton = this;
    }

    public BasicAttackBehaviour Spawn(AttackSO attackToSpawn, Transform gunHolder)
    {
        GameObject go = Instantiate(attackToSpawn.AttackPrefab,gunHolder);
        go.name = attackToSpawn.AttackName;

        var behaviour = go.GetComponent<BasicAttackBehaviour>();
        if (behaviour != null)
        {
            behaviour.Init(attackToSpawn);
            OnSpawnedAttack.Invoke(attackToSpawn, gunHolder);
            return behaviour;
        }

        return null;
    }

    

}
