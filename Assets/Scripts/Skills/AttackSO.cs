using UnityEngine;
public enum AtkType
{
    RandomTarget,
    ClosestTarget,
    RandomDirection,
    MovementDirection,
}

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class AttackSO : ScriptableObject
{
    [Header("Attack Variables")]
    public string AttackName;
    public Sprite AttackSprite;
    public AtkType Type;
    public Stats Scaling;
    public GameObject AttackPrefab;

    [Header("Projectile Variables")] //move that inside projectile behaviour?
    public Sprite ProjectileSprite;
    public float Damage;
    public float ProjectileSpeed;
    public float ProjectileLifetime;
    public float SpawnOffset = 0.5f;
}

