using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;




#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraTrigger : MonoBehaviour
{
    [SerializeField] BoxCollider2D BoundingShape2D;
    [SerializeField] Tilemap wallRoomGrid;
    [SerializeField] List<EnemyCharacter> enemiesInRoom;

    [SerializeField] float cameraLensOnEnter = 6f;

    int enemiesToKill = 0;

    bool activated = false;

    private void Awake()
    {
        if (!BoundingShape2D)
        {
            BoundingShape2D = GetComponent<BoxCollider2D>();
        }

        enemiesInRoom.Clear();

        var enemies = Physics2D.BoxCastAll(new Vector2(transform.position.x, transform.position.y) + BoundingShape2D.offset, BoundingShape2D.size, 0, Vector2.zero);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].collider.name == "TargetDetector")
                continue;

            var enemy = enemies[i].transform.GetComponent<EnemyCharacter>();
            if (enemy != null)
            {
                if (enemiesInRoom.Contains(enemy))
                    continue;

                enemiesInRoom.Add(enemy);
                if (enemy.gameObject.activeSelf)
                    enemiesToKill++;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            CameraManager.SetTargetPosition(BoundingShape2D.transform.position + (Vector3)BoundingShape2D.offset);
            CameraManager.SetLensOrtoSize(cameraLensOnEnter);

            if (!activated && enemiesToKill > 0)
            {
                activated = true;

                DoorManager.SetDoorCollidable(true);

                for (int i = 0; i < enemiesInRoom.Count; i++)
                {
                    if (enemiesInRoom[i] == null)
                        continue;

                    enemiesInRoom[i].SetAIEnabled(true);
                    enemiesInRoom[i].OnDie += CheckAllEnemiesDead;
                }
            }
        }
    }

    private void CheckAllEnemiesDead(EnemyCharacter enemy)
    {
        enemy.OnDie -= CheckAllEnemiesDead;
        enemiesToKill--;

        if (enemiesToKill < 1)
        {
            DoorManager.SetDoorCollidable(false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //must kill all enemies to leave room
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
        BoundingShape2D.size = new Vector2(wallRoomGrid.size.x, wallRoomGrid.size.y) - Vector2.one * 2f;
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
