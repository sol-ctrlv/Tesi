using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class LevelNameIntro : MonoBehaviour
{
    [Header("Tempi")]
    [SerializeField] private float holdTime = 3f;      // quanto resta visibile prima di iniziare il fade
    [SerializeField] private float fadeDuration = 2f; // durata del fade out

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // parte pienamente visibile
    }

    private void Start()
    {
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // 1) nome del livello visibile per un po'
        yield return new WaitForSeconds(holdTime);

        // 2) fade out
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            canvasGroup.alpha = 1f - k;
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // opzionale: spegni proprio tutto il pannello
        gameObject.SetActive(false);
    }
}
