using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomGhostAudio : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Clip fantasma")]
    [SerializeField] private AudioClip ghostClip;

    [Header("Tempo tra i versi (secondi)")]
    [SerializeField] private float minDelay = 3f;
    [SerializeField] private float maxDelay = 8f;

    [Header("Randomizzazione timbrica")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Volume base")]
    [SerializeField] private float baseVolume = 1f;

    private AudioSource _source;
    private Coroutine _loopRoutine;
    private int _playersInside;

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        if (_source != null)
            return;

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;   // 2D; cambia se vuoi 3D
        _source.volume = baseVolume;

        // opzionale: assicurati che il collider sia trigger
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    public void Initialize(AudioClip clip, float minDelaySec, float maxDelaySec, float volume)
    {
        // IMPORTANTISSIMO: ci assicuriamo che _source esista anche se Awake non è ancora passato
        EnsureAudioSource();

        ghostClip = clip;
        minDelay = minDelaySec;
        maxDelay = maxDelaySec;
        baseVolume = volume;

        _source.clip = ghostClip;
        _source.volume = baseVolume;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || ghostClip == null)
            return;

        _playersInside++;
        if (_playersInside == 1 && _loopRoutine == null)
        {
            _loopRoutine = StartCoroutine(GhostLoopRoutine());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        _playersInside = Mathf.Max(0, _playersInside - 1);
        if (_playersInside == 0 && _loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
            _source.Stop();
        }
    }

    private IEnumerator GhostLoopRoutine()
    {
        yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

        while (true)
        {
            _source.pitch = Random.Range(minPitch, maxPitch);
            _source.volume = baseVolume;

            _source.Play();

            yield return new WaitForSeconds(ghostClip.length);

            float wait = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(wait);
        }
    }
}
