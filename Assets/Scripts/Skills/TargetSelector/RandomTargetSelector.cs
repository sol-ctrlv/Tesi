using UnityEngine;

public class RandomTargetSelector : TargetSelector
{
    public override Vector2 GetShootDirection()
    {
        if (targetDetection != null)
        {
            var target = targetDetection.GetRandomEnemy();
            if (target != null)
                return ((Vector2)(target.transform.position) - (Vector2)transform.position).normalized;
        }

        return Vector2.zero;
    }
}
