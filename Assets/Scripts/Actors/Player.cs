using UnityEngine;

public class Player : Actor
{
    static public Vector2 position => Instance ? Instance.gameObject.transform.position : Vector3.zero;
    static public Player Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Init(MaxHP);
    }

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        Damage(1f);
    }
}