using UnityEngine;

public class RenderCollisionBox : MonoBehaviour
{
#if UNITY_EDITOR

    [SerializeField] Color boxColor = new Color(0f, 1f, 0f, 0.34f);

    private void OnDrawGizmos()
    {
        Gizmos.color = boxColor;

        // Convert the local coordinate values into world
        // coordinates for the matrix transformation.
        Gizmos.matrix = transform.localToWorldMatrix;

        var boxCol = GetComponent<BoxCollider2D>();
        var circleCol = GetComponent<CircleCollider2D>();

        if (boxCol)
        {
            Gizmos.DrawCube(boxCol.offset, boxCol.size);
        }
        else if (circleCol)
        {
            Gizmos.DrawSphere(circleCol.offset, circleCol.radius);
        }
        else
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
#endif
}
