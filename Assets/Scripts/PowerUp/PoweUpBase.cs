using UnityEngine;

public abstract class PoweUpBase : MonoBehaviour
{
    private void OnEnable()
    {
        ApplyPowerUp(Player.Instance.gameObject);
        enabled = false;
    }

    protected abstract void ApplyPowerUp(GameObject target);

    private void OnDisable()
    {
        Destroy(gameObject);
    }

}
