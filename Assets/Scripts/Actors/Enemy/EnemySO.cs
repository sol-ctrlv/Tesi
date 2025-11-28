using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Scriptable Objects/Enemy")]
public class EnemySO : ScriptableObject
{
    public Sprite Sprite;
    public Color Color;
    public AttackSO DefaultAttack;
    public float HP;
    public float AttackRadius;
    public float MovementSpeed;
}