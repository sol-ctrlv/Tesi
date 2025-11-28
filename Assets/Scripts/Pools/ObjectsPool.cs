using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectsPool : MonoBehaviour
{
    [SerializeField] GameObject GameObjectPrefab;

    protected int InitialPoolSize = 10;
    protected int layerID;
    protected Queue<GameObject> pool = new Queue<GameObject>();

    [SerializeField, Tooltip("Valore attuale di Count della coda (read-only)")]
    private int queueCount; // visibile in inspector

    void Update()
    {
        queueCount = pool.Count;
    }

    protected void Init(GameObject owner)
    {
        transform.SetParent(owner.transform);
        layerID = owner.layer; // get the layer of the owner
        SetupPool();
    }

    private void OnDestroy()
    {
        while (pool.Count > 0)
        {
            Destroy(pool.Dequeue());
        }
    }

    private void SetupPool()
    {
        for (int i = 0; i < InitialPoolSize; i++)
        {
            var proj = CreateNewObject(false);
            pool.Enqueue(proj);
        }
    }

    public GameObject GetObject()
    {
        if (pool.Count > 0)
        {
            var proj = pool.Dequeue();
            return proj;
        }
        else
        {
            return CreateNewObject(true);
        }
    }

    protected GameObject CreateNewObject(bool active = false)
    {
        GameObject gameObject = Instantiate(GameObjectPrefab);
        gameObject.name = string.Format("{0} {1}",gameObject.name,pool.Count);
        gameObject.layer = layerID;
        gameObject.SetActive(active);

        DecorateObject(gameObject);

        return gameObject;
    }

    protected abstract void DecorateObject(GameObject gameObject);
}
