using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collision other)
    {
        if(other.GameObject.CompareTag("Player"))
        {
            //Camera.Main
        }
    }
}
