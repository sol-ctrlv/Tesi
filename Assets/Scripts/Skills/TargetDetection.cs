using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetDetection : MonoBehaviour
{
    public string TargetTag;
    public List<GameObject> TargetsInRange;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(TargetTag) && !TargetsInRange.Contains(collision.gameObject))
        {
            TargetsInRange.Add(collision.gameObject);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(TargetTag) && TargetsInRange.Contains(collision.gameObject))
        {
            TargetsInRange.Remove(collision.gameObject);
        }
    }

    public GameObject GetClosestEnemy()
    {
        if (TargetsInRange.Count == 0) return null;

        return TargetsInRange.Where(obj => obj != null) // sempre bene proteggersi
        .OrderBy(obj => (obj.transform.position - transform.position).sqrMagnitude)
        .FirstOrDefault();
    }

    public GameObject GetRandomEnemy()
    {
        if (TargetsInRange.Count == 0) return null;

        int index = Random.Range(0, TargetsInRange.Count);
        return TargetsInRange[index];
    }

}
