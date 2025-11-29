using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class AttackSO : ScriptableObject
{
    [Header("Attack Variables")]
    public Sprite AttackSprite;
    public GameObject AttackPrefab;
    public bool HiddenAttack;

    [Header("Projectile Variables")] //move that inside projectile behaviour?
    public Sprite ProjectileSprite;
    public float Damage;
    public float ProjectileSpeed;
    public float ProjectileLifetime;
    public float SpawnOffset = 0.5f;
}

