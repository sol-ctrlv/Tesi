using System.Collections.Generic;
using UnityEngine;

namespace EmotionPCG
{
    /// <summary>
    /// Interpreta gli appraisal pattern applicati alle stanze e li traduce
    /// in modifiche concrete al contenuto (nemici, safe haven, ricompense, ecc.).
    /// Lavora leggendo i componenti EmotionRoomMetadata presenti nella scena
    /// dopo che il post-processing ha assegnato i pattern.
    /// </summary>
    public class EmotionPatternApplier : MonoBehaviour
    {
        [Header("Prefab di contenuto di base")]
        public GameObject enemyPrefab;
        public GameObject safeHealPrefab;
        public GameObject safeStatuePrefab;
        public GameObject rewardChestPrefab;
        public GameObject competenceGatePrefab;

        [Header("Prefab di segnaletica / punti di interesse")]
        public GameObject signpostPrefab;
        public GameObject pointOfInterestPrefab;

        [Header("Prefab estetici / ambientali")]
        public GameObject rareObjectPrefab;
        public GameObject symmetryPropPrefab;
        public GameObject[] occlusionPropPrefabs;

        [Header("Parametri di intensità (grezzi, per prototipo)")]
        public int baseEnemiesPerConflict = 2;
        public int extraEnemiesForFear = 1;
        public int propsPerContentDensity = 5;

        [Header("Spawn nemici (controllo collisioni)")]
        [SerializeField] private LayerMask enemyBlockingLayers;   // layer di muri + props + nemici
        [SerializeField] private float enemySpawnCheckRadius = 0.4f;
        [SerializeField] private int enemyMaxSpawnAttempts = 25;
        [SerializeField] private float enemySpawnRadius = 2f;

        /// <summary>
        /// Entry point da chiamare dopo la generazione del livello + post-processing.
        /// Può essere invocato da un altro componente manager o da un pulsante in editor.
        /// </summary>
        [ContextMenu("Apply patterns to all rooms in scene")]
        public void ApplyAllPatternsInScene()
        {
            var rooms = FindObjectsOfType<EmotionRoomMetadata>();

            foreach (var room in rooms)
            {
                ApplyPatternsToRoom(room);
            }
        }

        private void ApplyPatternsToRoom(EmotionRoomMetadata metadata)
        {
            if (metadata == null)
                return;

            var roomTransform = metadata.transform;
            var patterns = metadata.AppliedPatterns;
            var roomCenter = GetRoomCenter(roomTransform);

            bool hasSafeHaven = patterns.Contains(AppraisalPatternType.SafeHaven);

            // SafeHaven agisce come stanza "a sé stante" rispetto a minacce e ostacoli:
            // se presente, saltiamo Conflict, ContentDensity, OcclusionAudio e CompetenceGate.
            foreach (var pattern in patterns)
            {
                switch (pattern)
                {
                    case AppraisalPatternType.Conflict:
                        if (!hasSafeHaven)
                            ApplyConflict(metadata, roomTransform, roomCenter);
                        break;

                    case AppraisalPatternType.SafeHaven:
                        ApplySafeHaven(metadata, roomTransform, roomCenter);
                        break;

                    case AppraisalPatternType.Rewards:
                        ApplyRewards(metadata, roomTransform, roomCenter);
                        break;

                    case AppraisalPatternType.ClearSignposting:
                        ApplyClearSignposting(metadata, roomTransform, hasSafeHaven);
                        break;

                    case AppraisalPatternType.PointingOut:
                        ApplyPointingOut(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.Centering:
                        ApplyCentering(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.Symmetry:
                        ApplySymmetry(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.AppearanceOfObjects:
                        ApplyAppOfObjects(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.ContentDensity:
                        if (!hasSafeHaven)
                            ApplyContentDensity(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.OcclusionAudio:
                        if (!hasSafeHaven)
                            ApplyOcclusionAudio(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.CompetenceGate:
                        if (!hasSafeHaven)
                            ApplyCompetenceGate(metadata, roomTransform);
                        break;
                }
            }
        }

        #region Pattern handlers
        private void ApplyConflict(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            if (enemyPrefab == null)
                return;

            int enemies = baseEnemiesPerConflict;
            if (metadata.LevelEmotion == EmotionType.Fear)
                enemies += extraEnemiesForFear;

            enemies = Mathf.Max(1, enemies);

            Vector3 center = roomCenter;

            for (int i = 0; i < enemies; i++)
            {
                // allarghiamo un po' il raggio di ricerca per i nemici successivi
                float radiusForThisEnemy = enemySpawnRadius + i * 0.4f;

                if (TryFindFreeEnemySpot(center, radiusForThisEnemy, out var spawnPos))
                {
                    Instantiate(enemyPrefab, spawnPos, Quaternion.identity, roomTransform);
                }
                else
                {
                    Debug.LogWarning(
                        $"[EmotionPatternApplier] Nessuno spot libero per nemico {i + 1}/{enemies} in '{roomTransform.name}'.");
                }
            }
        }


        /// <summary>
        /// Safe haven centrato nella stanza.
        /// </summary>
        /// <summary>
        /// Safe haven come set di props: fuoco al centro + 2 statue ai lati
        /// + (opzionale) un segnale a indicare che è un luogo sicuro.
        /// </summary>
        private void ApplySafeHaven(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            Vector3 center = roomCenter;

            // 1. Fuoco da campo al centro
            if (safeHealPrefab != null)
            {
                Instantiate(safeHealPrefab, center, Quaternion.identity, roomTransform);
            }

            // 2. Due statue laterali (simmetriche)
            if (safeStatuePrefab != null)
            {
                float sideOffset = 1.5f; // distanza dal centro; tarala a gusto
                Instantiate(safeStatuePrefab, center + new Vector3(-sideOffset, 0f, 0f), Quaternion.identity, roomTransform);
                Instantiate(safeStatuePrefab, center + new Vector3(sideOffset, 0f, 0f), Quaternion.identity, roomTransform);
            }
        }


        /// <summary>
        /// Reward: piazza una chest leggermente sopra il centro stanza.
        /// La logica di buff è gestita dallo script sul prefab della chest.
        /// </summary>
        private void ApplyRewards(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            if (rewardChestPrefab == null)
                return;

            Vector3 center = roomCenter;
            // piccolo offset in su per non sovrapporla ad altri elementi centrali
            Vector3 pos = center + new Vector3(0f, 1.2f, 0f);

            Instantiate(rewardChestPrefab, pos, Quaternion.identity, roomTransform);
        }


        /// <summary>
        /// Cartello vicino alla porta del critical path (se marcata), altrimenti sopra il centro.
        /// </summary>
        private void ApplyClearSignposting(EmotionRoomMetadata metadata, Transform roomTransform, bool isSafeHavenRoom)
        {
            if (signpostPrefab == null)
                return;

            Vector3 mainPos;

            Transform criticalDoorMarker = roomTransform.Find("CriticalPathDoorMarker");
            if (criticalDoorMarker != null)
            {
                // Prendiamo l'orientamento della porta per mettere il cartello di lato.
                var forward = criticalDoorMarker.up.normalized;
                var right = new Vector3(forward.y, -forward.x, 0f); // vettore perpendicolare nel piano XY

                mainPos = criticalDoorMarker.position + right * 0.8f;
            }
            else
            {
                mainPos = roomTransform.position + new Vector3(0f, 2f, 0f);
            }

            Instantiate(signpostPrefab, mainPos, Quaternion.identity, roomTransform);

            if (isSafeHavenRoom)
            {
                var center = roomTransform.position;
                var safePos = center + new Vector3(0f, -1.5f, 0f);
                Instantiate(signpostPrefab, safePos, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Highlight diretto su un target marcato, oppure sopra il centro.
        /// </summary>
        private void ApplyPointingOut(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (pointOfInterestPrefab == null)
                return;

            Transform poiMarker = roomTransform.Find("PointingOutTarget");
            Vector3 pos;

            if (poiMarker != null)
            {
                pos = poiMarker.position + new Vector3(0f, 1.2f, 0f);
            }
            else
            {
                pos = roomTransform.position + new Vector3(0f, 1.2f, 0f);
            }

            Instantiate(pointOfInterestPrefab, pos, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Centering: oggetto raro al centro della stanza (es. statua, reliquia).
        /// </summary>
        private void ApplyCentering(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (rareObjectPrefab == null)
                return;

            var center = roomTransform.position;
            Instantiate(rareObjectPrefab, center, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Symmetry: duplica un prop marcatamente su un lato (per ora, semplice instanziazione simmetrica).
        /// </summary>
        private void ApplySymmetry(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (symmetryPropPrefab == null)
                return;

            var center = roomTransform.position;
            var leftPos = center + new Vector3(-2f, 0f, 0f);
            var rightPos = center + new Vector3(2f, 0f, 0f);

            Instantiate(symmetryPropPrefab, leftPos, Quaternion.identity, roomTransform);
            Instantiate(symmetryPropPrefab, rightPos, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Appearance of objects piacevoli: props decorativi attorno al centro.
        /// </summary>
        private void ApplyAppOfObjects(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (occlusionPropPrefabs == null || occlusionPropPrefabs.Length == 0)
                return;

            int props = Mathf.Max(1, propsPerContentDensity);
            float radius = 2.5f;
            var center = roomTransform.position;

            for (int i = 0; i < props; i++)
            {
                float angle = (Mathf.PI * 2f * i) / props;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                var prefab = occlusionPropPrefabs[Random.Range(0, occlusionPropPrefabs.Length)];
                if (prefab == null)
                    continue;

                var pos = center + offset;
                Instantiate(prefab, pos, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Content density: usa gli stessi occlusionPropPrefabs come riempitivo generico.
        /// </summary>
        private void ApplyContentDensity(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (occlusionPropPrefabs == null || occlusionPropPrefabs.Length == 0)
                return;

            int props = Mathf.Max(1, propsPerContentDensity);
            float radius = 3f;
            var center = roomTransform.position;

            for (int i = 0; i < props; i++)
            {
                float angle = (Mathf.PI * 2f * i) / props;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                var prefab = occlusionPropPrefabs[Random.Range(0, occlusionPropPrefabs.Length)];
                if (prefab == null)
                    continue;

                var pos = center + offset;
                Instantiate(prefab, pos, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Occlusion audio: props attorno al perimetro per simulare \"ostacoli\" acustici.
        /// </summary>
        private void ApplyOcclusionAudio(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (occlusionPropPrefabs == null || occlusionPropPrefabs.Length == 0)
                return;

            int props = Mathf.Max(1, propsPerContentDensity);
            float radius = 4f;
            var center = roomTransform.position;

            for (int i = 0; i < props; i++)
            {
                float angle = (Mathf.PI * 2f * i) / props;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                var prefab = occlusionPropPrefabs[Random.Range(0, occlusionPropPrefabs.Length)];
                if (prefab == null)
                    continue;

                var pos = center + offset;
                Instantiate(prefab, pos, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Competence gate: oggetto (es. porta bloccata) posto verso il lato
        /// della stanza considerato \"uscita principale\" (per ora semplice offset).
        /// </summary>
        private void ApplyCompetenceGate(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (competenceGatePrefab == null)
                return;

            var center = roomTransform.position;
            var pos = center + new Vector3(0f, 3f, 0f);
            Instantiate(competenceGatePrefab, pos, Quaternion.identity, roomTransform);
        }

        #endregion

        #region Helper per spawn nemici

        private bool TryFindFreeEnemySpot(Vector3 center, float radius, out Vector3 spawnPos)
        {
            int attempts = Mathf.Max(1, enemyMaxSpawnAttempts);

            for (int i = 0; i < attempts; i++)
            {
                // punto casuale dentro il cerchio di raggio 'radius'
                Vector2 offset2D = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(offset2D.x, offset2D.y, 0f);

                if (IsEnemySpotFree(candidate))
                {
                    spawnPos = candidate;
                    return true;
                }
            }

            spawnPos = center;
            return false;
        }


        private bool IsEnemySpotFree(Vector3 worldPos)
        {
            // Se non troviamo collider nel raggio, consideriamo lo spot libero.
            return !Physics2D.OverlapCircle(worldPos, enemySpawnCheckRadius, enemyBlockingLayers);
        }

        #endregion

        #region Helper generali

        private Vector3 GetRoomCenter(Transform roomTransform)
        {
            // 1. Se esiste un'ancora dedicata "RoomCenter" (tag o nome), usiamo quella.
            foreach (Transform child in roomTransform.GetComponentsInChildren<Transform>())
            {
                if (child == roomTransform)
                    continue;

                if (child.CompareTag("RoomCenter") || child.name == "RoomCenter")
                    return child.position;
            }

            // 2. Fallback: calcoliamo il bounding box di tutti i Renderer
            //    (TilemapRenderer, SpriteRenderer, ecc.) e usiamo il suo centro.
            var renderers = roomTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return roomTransform.position;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.center;
        }

        #endregion
    }
}
