using System.Collections.Generic;
using UnityEngine;

public class ProjectilesPool : ObjectsPool
{
    private Sprite projectileSprite;

    public void Init(AttackSO data, GameObject owner)
    {
        projectileSprite = data.ProjectileSprite;
        Init(owner);
    }

    public void ReturnProjectile(GameObject proj)
    {
        proj.SetActive(false);
        pool.Enqueue(proj);
    }

    protected override void DecorateObject(GameObject proj)
    {
        proj.transform.SetParent(ProjectilesContainer.GetTransform(), true);
        proj.GetComponent<SpriteRenderer>().sprite = projectileSprite;

        PolygonCollider2D poly = proj.GetComponent<PolygonCollider2D>();
        poly.pathCount = projectileSprite.GetPhysicsShapeCount();
        List<Vector2> path = new List<Vector2>();
        for (int j = 0; j < poly.pathCount; j++)
        {
            projectileSprite.GetPhysicsShape(j, path);
            poly.SetPath(j, path.ToArray());
        }
    }
}
