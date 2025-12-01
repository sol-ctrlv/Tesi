using UnityEngine;

public class AnimToActor : MonoBehaviour
{
    [SerializeField] Actor myActor;

    public void Die()
    {
        Destroy(myActor.gameObject);
    }
}
