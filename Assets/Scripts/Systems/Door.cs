using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Collider2D myCollider;

    public void SetCollision(bool value)
    {
        myCollider.enabled = value;
    }
}
