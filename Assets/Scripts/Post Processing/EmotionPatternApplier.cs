using System.Collections.Generic;
using UnityEngine;

namespace EmotionPCG
{
    public class EmotionPatternApplier : MonoBehaviour
    {
        [Header("Conflict (nemici)")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int baseEnemiesPerConflict = 2;
        [SerializeField] private int extraEnemiesForFear = 1;
        [SerializeField] private float enemySpawnRadius = 2.2f;
        [SerializeField] private float enemyCollisionRadius = 0.5f;
        [SerializeField] private LayerMask enemyBlockingLayers;

        [Header("Safe Haven")]
        [SerializeField] private GameObject safeHealPrefab;     // tile che cura alla prima collisione
        [SerializeField] private GameObject safeStatuePrefab;   // statue laterali opzionali

        [Header("Rewards")]
        [SerializeField] private GameObject rewardChestPrefab;  // chest con buff (script sul prefab)

        [Header("Pointing Out / Centering / Symmetry / Appearance")]
        [SerializeField] private GameObject pointOfInterestPrefab;
        [SerializeField] private GameObject symmetryPropPrefab;
        [SerializeField] private GameObject rareObjectPrefab;

        [Header("Content Density / Occlusion / Competence Gate")]
        [SerializeField] private GameObject[] occlusionPropPrefabs;
        [SerializeField] private int propsPerContentDensity = 4;
        [SerializeField] private GameObject audioOcclusionPrefab;
        [SerializeField] private GameObject competenceGatePrefab;

        [Header("Clear Signposting")]
        [SerializeField] private GameObject signpostPrefab;

        /// <summary>
        /// Chiamato dal PostProcessing dopo aver scritto i metadata sulle stanze.
        /// </summary>
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
            List<AppraisalPatternType> patterns = metadata.AppliedPatterns;

            // centro calcolato con ancora "RoomCenter" se presente
            Vector3 roomCenter = GetRoomCenter(roomTransform);

            bool hasSafeHaven = patterns.Contains(AppraisalPatternType.SafeHaven);

            // SafeHaven “spegne” alcuni pattern ostili
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

        /// <summary>
        /// Nemici attorno al centro stanza, con controllo collisioni 2D.
        /// </summary>
        private void ApplyConflict(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            if (enemyPrefab == null)
                return;

            int enemies = baseEnemiesPerConflict;
            if (metadata.LevelEmotion == EmotionType.Fear)
                enemies += extraEnemiesForFear;

            enemies = Mathf.Max(1, enemies);

            for (int i = 0; i < enemies; i++)
            {
                // allarghiamo un po’ il raggio per i nemici successivi
                float radiusForThisEnemy = enemySpawnRadius + i * 0.4f;

                if (TryFindFreeEnemySpot(roomCenter, radiusForThisEnemy, out var spawnPos))
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
        /// Safe haven: tile di cura al centro + due statue laterali.
        /// </summary>
        private void ApplySafeHaven(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            // 1. tile di cura al centro
            if (safeHealPrefab != null)
            {
                Instantiate(safeHealPrefab, roomCenter, Quaternion.identity, roomTransform);
            }

            // 2. due statue simmetriche ai lati
            if (safeStatuePrefab != null)
            {
                float sideOffset = 1.5f;
                Instantiate(safeStatuePrefab, roomCenter + new Vector3(-sideOffset, 0f, 0f),
                    Quaternion.identity, roomTransform);
                Instantiate(safeStatuePrefab, roomCenter + new Vector3(sideOffset, 0f, 0f),
                    Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Reward: chest leggermente sopra il centro stanza.
        /// La logica di buff è nello script del prefab.
        /// </summary>
        private void ApplyRewards(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            if (rewardChestPrefab == null)
                return;

            // piccolo offset in su per non sovrapporla ad altri elementi centrali
            Vector3 pos = roomCenter + new Vector3(0f, 1.2f, 0f);
            Instantiate(rewardChestPrefab, pos, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Clear signposting:
        /// – se esiste un child "CriticalPathDoorMarker", piazza il cartello vicino alla porta;
        /// – altrimenti, fallback: cartello sopra il centro stanza.
        /// Se la stanza è anche SafeHaven, mette un secondo cartello in basso.
        /// </summary>
        private void ApplyClearSignposting(EmotionRoomMetadata metadata, Transform roomTransform, bool isSafeHavenRoom)
        {
            if (signpostPrefab == null)
                return;

            Vector3 mainPos;

            // 1) Caso "marker" esplicito nella stanza
            Transform doorMarker = roomTransform.Find("CriticalPathDoorMarker");
            if (doorMarker != null)
            {
                // prendiamo l’orientazione del marker per avere il cartello di lato
                Vector3 forward = doorMarker.up.normalized;
                Vector3 right = new Vector3(forward.y, -forward.x, 0f);

                mainPos = doorMarker.position + right * 0.8f;
            }
            else
            {
                // 2) Fallback: sopra il centro stanza
                mainPos = roomTransform.position + new Vector3(0f, 2f, 0f);
            }

            Instantiate(signpostPrefab, mainPos, Quaternion.identity, roomTransform);

            // cartello aggiuntivo nelle safe haven (es. "luogo sicuro")
            if (isSafeHavenRoom)
            {
                Vector3 center = roomTransform.position;
                Vector3 safePos = center + new Vector3(0f, -1.5f, 0f);
                Instantiate(signpostPrefab, safePos, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Evidenzia un punto di interesse (per ora: centro stanza o target marker).
        /// </summary>
        private void ApplyPointingOut(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (pointOfInterestPrefab == null)
                return;

            // se in futuro avrai un marker specifico (es. "POIMarker"), puoi usarlo qui
            Transform target = roomTransform.Find("POIMarker");

            Vector3 spawnPos;
            if (target != null)
            {
                spawnPos = target.position;
            }
            else
            {
                spawnPos = roomTransform.position + new Vector3(0f, 1.5f, 0f);
            }

            var poi = Instantiate(pointOfInterestPrefab, spawnPos, Quaternion.identity, roomTransform);

            var light = poi.GetComponentInChildren<Light>();
            if (light != null)
            {
                light.intensity *= 1.5f;
            }
        }

        /// <summary>
        /// Oggetto centrato nella stanza, usato solo se non ci sono safe haven / rewards / pointing out.
        /// </summary>
        private void ApplyCentering(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            var patterns = metadata.AppliedPatterns;

            if (patterns.Contains(AppraisalPatternType.SafeHaven) ||
                patterns.Contains(AppraisalPatternType.Rewards) ||
                patterns.Contains(AppraisalPatternType.PointingOut))
            {
                // già qualcosa di importante al centro: non aggiungiamo altro
                return;
            }

            if (pointOfInterestPrefab != null)
            {
                Vector3 center = roomTransform.position;
                Instantiate(pointOfInterestPrefab, center, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Due statue simmetriche a sinistra e destra del centro.
        /// </summary>
        private void ApplySymmetry(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (symmetryPropPrefab == null)
                return;

            float distance = 2f;
            Vector3 center = roomTransform.position;

            Vector3 leftPos = center + new Vector3(-distance, 0f, 0f);
            Vector3 rightPos = center + new Vector3(distance, 0f, 0f);

            Instantiate(symmetryPropPrefab, leftPos, Quaternion.identity, roomTransform);
            Instantiate(symmetryPropPrefab, rightPos, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Oggetti rari in diagonale rispetto al centro, per rompere la regolarità.
        /// </summary>
        private void ApplyAppOfObjects(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (rareObjectPrefab == null)
                return;

            Vector3 center = roomTransform.position;

            Vector3 offset1 = new Vector3(1.5f, 1.0f, 0f);
            Vector3 offset2 = new Vector3(-1.5f, -1.0f, 0f);

            Vector3 pos1 = center + offset1;
            Vector3 pos2 = center + offset2;

            Instantiate(rareObjectPrefab, pos1, Quaternion.identity, roomTransform);
            Instantiate(rareObjectPrefab, pos2, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Props distribuiti su un anello vicino alle pareti per dare densità senza occupare il centro.
        /// </summary>
        private void ApplyContentDensity(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (occlusionPropPrefabs == null || occlusionPropPrefabs.Length == 0)
                return;

            int props = Mathf.Max(1, propsPerContentDensity);
            float radius = 3f;
            Vector3 center = roomTransform.position;

            for (int i = 0; i < props; i++)
            {
                float angle = (Mathf.PI * 2f * i) / props;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Vector3 pos = center + offset;

                GameObject prefab = occlusionPropPrefabs[Random.Range(0, occlusionPropPrefabs.Length)];
                Instantiate(prefab, pos, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Elemento per l'occlusione audio (es. muro / barriera) in una zona specifica.
        /// </summary>
        private void ApplyOcclusionAudio(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (audioOcclusionPrefab == null)
                return;

            // per ora: mettiamo un solo elemento sopra la stanza
            Vector3 pos = roomTransform.position + new Vector3(0f, 3f, 0f);
            Instantiate(audioOcclusionPrefab, pos, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Competence gate: un ostacolo interattivo che blocca il passaggio (es. porta / barriera).
        /// </summary>
        private void ApplyCompetenceGate(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (competenceGatePrefab == null)
                return;

            // se esiste un marker, usalo; altrimenti centro
            Transform marker = roomTransform.Find("GateMarker");
            Vector3 pos = marker != null ? marker.position : roomTransform.position;

            Instantiate(competenceGatePrefab, pos, Quaternion.identity, roomTransform);
        }

        #endregion

        #region Helpers
        /// <summary>
        /// Ritorna il centro logico della stanza; se esiste un child "RoomCenter", usa quello.
        /// </summary>
        private Vector3 GetRoomCenter(Transform roomTransform)
        {
            Transform centerMarker = roomTransform.Find("RoomCenter");
            if (centerMarker != null)
                return centerMarker.position;

            return roomTransform.position;
        }

        /// <summary>
        /// Cerca uno spot libero in un cerchio attorno al centro, evitando overlap con i layer bloccanti.
        /// </summary>
        private bool TryFindFreeEnemySpot(Vector3 center, float radius, out Vector3 position)
        {
            const int maxTries = 20;

            for (int i = 0; i < maxTries; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(0f, radius);

                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;
                Vector3 candidate = center + offset;

                // check overlap con muri / props / altri nemici
                Collider2D hit = Physics2D.OverlapCircle(candidate, enemyCollisionRadius, enemyBlockingLayers);
                if (hit == null)
                {
                    position = candidate;
                    return true;
                }
            }

            // fallback: non trovato nulla
            position = center;
            return false;
        }

        #endregion
    }
}
