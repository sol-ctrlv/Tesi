using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Collider2D myCollider;
    [SerializeField] SpriteRenderer spriteRenderer;

    public void SetCollision(bool value)
    {
        myCollider.enabled = value;
        spriteRenderer.enabled = value;
    }
}
