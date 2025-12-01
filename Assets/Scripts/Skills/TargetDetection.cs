using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetDetection : MonoBehaviour
{
    public string TargetTag;
    public List<GameObject> TargetsInRange = new List<GameObject>();

    [Header("Range base")]
    [SerializeField] private CircleCollider2D detectionCollider;
    [SerializeField] private float baseRadius = 1.5f;

    public float RangeMultiplier { get; private set; } = 1f;

    /// <summary>
    /// Chiamalo dal player per applicare il buff al range.
    /// </summary>
    public void SetRangeMultiplier(float multiplier)
    {
        RangeMultiplier = multiplier;

        if (detectionCollider != null)
        {
            detectionCollider.radius = baseRadius * RangeMultiplier;
        }
    }

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

        return TargetsInRange
            .Where(obj => obj != null)
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
