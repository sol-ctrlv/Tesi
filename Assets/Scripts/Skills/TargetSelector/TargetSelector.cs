using UnityEngine;

public abstract class TargetSelector : MonoBehaviour
{
    [SerializeField] protected TargetDetection targetDetection;

    private void Start()
    {
        Init();
    }

    public virtual void Init()
    {
        if (targetDetection == null)
        {
            // no transform.root because I don't want the enemies container but this enemy parent
            targetDetection = transform.parent.parent.GetComponentInChildren<TargetDetection>();

            if (targetDetection == null)
            {
                Debug.LogWarning($"No TargetDetection found near {name}");
            }
        }
    }

    public abstract Vector2 GetShootDirection();
}