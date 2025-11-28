using UnityEngine;

public class ProjectilesContainer : MonoBehaviour
{
    private static Transform myself;

    private void Awake()
    {
        myself = this.transform;
    }

    public static Transform GetTransform() => myself;
}
