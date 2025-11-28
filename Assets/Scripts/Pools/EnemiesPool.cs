using UnityEngine;

public class EnemiesPool : ObjectsPool
{
    private void Awake()
    {
        InitialPoolSize = 100;
        Init(gameObject);
    }

    protected override void DecorateObject(GameObject gameObject)
    {
        //remove old weapon if any
        gameObject.transform.SetParent(transform);

        Transform EnemyGunHolder = gameObject.transform.GetChild(1);

        if (EnemyGunHolder.name != "GunHolder")
        {
            print("NOT FOUND GUN HOLDER");
            return;
        }

        for (int i = 0; i < EnemyGunHolder.childCount; i++)
        {
            Destroy(EnemyGunHolder.GetChild(i).gameObject);
        }
    }
}
