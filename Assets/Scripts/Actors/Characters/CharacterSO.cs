using UnityEngine;

[CreateAssetMenu(fileName = "CharacterClass", menuName = "Scriptable Objects/Character")]
public class CharacterSO : ScriptableObject
{
    public Sprite Sprite;
    public Color Color;
    public AttackSO DefaultAttack;
    public float BaseAttackRadius;
    public float str;
    public float dex;
    public float cos;
    public float wis;
    public float intellect;
    public float cha;
}
