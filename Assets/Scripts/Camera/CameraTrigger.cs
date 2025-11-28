using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraTrigger : MonoBehaviour
{
    [SerializeField] BoxCollider2D BoundingShape2D;
    [SerializeField] Tilemap wallRoomGrid;

    private void Awake()
    {
        if (!BoundingShape2D)
        {
            BoundingShape2D = GetComponent<BoxCollider2D>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            CinemachineCore.GetVirtualCamera(0).GetComponent<CinemachineConfiner2D>().BoundingShape2D = BoundingShape2D;
        }
    }

    public void SetTriggerSettings()
    {
        Tilemap[] tilemaps = transform.root.GetComponentsInChildren<Tilemap>();

        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i].name == "Walls")
            {
                wallRoomGrid = tilemaps[i];
                break;
            }
        }

        BoundingShape2D = GetComponent<BoxCollider2D>();
        transform.position = wallRoomGrid.transform.position;
        BoundingShape2D.size = new Vector2(wallRoomGrid.size.x, wallRoomGrid.size.y);
        BoundingShape2D.offset = wallRoomGrid.localBounds.center;

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
#endif
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(CameraTrigger))]
public class CameraTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraTrigger trigger = (CameraTrigger)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Reset"))
        {
            trigger.SetTriggerSettings();
        }
    }
}

#endif
