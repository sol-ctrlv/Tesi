using UnityEngine;

public class MovementDirectionTargetSelector : TargetSelector
{
    [SerializeField] ActorMovement actorMovement;

    public override void Init()
    {
        base.Init();

        if(actorMovement == null )
            // not transform.root because I don't want the enemies container but this enemy parent
            actorMovement = transform.parent.parent.GetComponentInChildren<ActorMovement>(); 
    }

    public override Vector2 GetShootDirection()
    {
        return actorMovement.getMovementDir();
    }
}
