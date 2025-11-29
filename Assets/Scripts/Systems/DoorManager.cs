using UnityEngine;

public class DoorManager : MonoBehaviour
{
    private static Door[] doors;

    private void Awake()
    {
        doors = GameObject.FindObjectsByType<Door>(FindObjectsSortMode.None);
        SetDoorCollidable(false);
    }

    public static void SetDoorCollidable(bool value)
    {
        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].SetCollision(value);
        }
    }
}
