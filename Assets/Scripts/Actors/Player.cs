using UnityEngine;

public class Player : Actor
{
    static public Vector2 position => instance.gameObject.transform.position;
    static private Player instance; //ew

    private void Awake()
    {
        instance = this;
    }

}