using UnityEngine;

public class RandomDirectionTargetSelector : TargetSelector
{
    public override Vector2 GetShootDirection()
    {
        return Random.insideUnitCircle.normalized;
    }
}
