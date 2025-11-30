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
        public GameObject safeHavenPrefab;
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
        public int extraEnemiesForFear    = 1;
        public int propsPerContentDensity = 5;

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

            // Per semplicità, accediamo direttamente al transform della stanza.
            // In un progetto più grande potresti voler passare da un RoomController dedicato.
            var roomTransform = metadata.transform;
            var patterns      = metadata.AppliedPatterns;

            bool hasSafeHaven = patterns.Contains(AppraisalPatternType.SafeHaven);

            // Applichiamo i pattern uno per uno.
            // SafeHaven agisce come stanza "a sé stante" rispetto a minacce e ostacoli:
            // se presente, saltiamo Conflict, ContentDensity, OcclusionAudio e CompetenceGate.
            foreach (var pattern in patterns)
            {
                switch (pattern)
                {
                    case AppraisalPatternType.Conflict:
                        if (!hasSafeHaven)
                            ApplyConflict(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.SafeHaven:
                        ApplySafeHaven(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.Rewards:
                        ApplyRewards(metadata, roomTransform);
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

                    case AppraisalPatternType.AppearancePleasant:
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

        private void ApplyConflict(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (enemyPrefab == null)
                return;

            // Numero grezzo di nemici: base + extra se il livello è Fear.
            int enemies = baseEnemiesPerConflict;
            if (metadata.LevelEmotion == EmotionType.Fear)
                enemies += extraEnemiesForFear;

            // Distribuzione a raggiera intorno al centro stanza (2D XY).
            float radius = 2f;
            for (int i = 0; i < enemies; i++)
            {
                float angle  = (Mathf.PI * 2f * i) / Mathf.Max(1, enemies);
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Instantiate(enemyPrefab, roomTransform.position + offset, Quaternion.identity, roomTransform);
            }
        }

        private void ApplySafeHaven(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // SafeHaven: nessun nemico aggiuntivo + un punto "sicuro" (es. fuoco da campo / heal).
            if (safeHavenPrefab != null)
            {
                Instantiate(safeHavenPrefab, roomTransform.position, Quaternion.identity, roomTransform);
            }
        }

        private void ApplyRewards(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (rewardChestPrefab != null)
            {
                Instantiate(rewardChestPrefab, roomTransform.position, Quaternion.identity, roomTransform);
            }
        }

        private void ApplyClearSignposting(EmotionRoomMetadata metadata, Transform roomTransform, bool isSafeHavenRoom)
        {
            if (signpostPrefab == null)
                return;

            // Proviamo a posizionare il segnale vicino a una porta dedicata, se esiste.
            Transform criticalDoorMarker = roomTransform.Find("CriticalPathDoorMarker");

            Vector3 mainPos;
            if (criticalDoorMarker != null)
            {
                // In 2D usiamo l'asse up come "direzione di uscita" della porta.
                mainPos = criticalDoorMarker.position - criticalDoorMarker.up * 0.5f;
            }
            else
            {
                // Fallback: spostato verso l'alto rispetto al centro stanza.
                mainPos = roomTransform.position + new Vector3(0f, 3f, 0f);
            }

            Instantiate(signpostPrefab, mainPos, Quaternion.identity, roomTransform);

            // Se la stanza è anche un SafeHaven, opzionalmente aggiungiamo un secondo segnale
            // vicino al centro per comunicare visivamente la zona sicura.
            if (isSafeHavenRoom)
            {
                Vector3 safePos = roomTransform.position + new Vector3(0f, -2f, 0f);
                Instantiate(signpostPrefab, safePos, Quaternion.identity, roomTransform);
            }
        }

        private void ApplyPointingOut(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (pointOfInterestPrefab == null)
                return;

            // Cerchiamo un bersaglio marcato nella stanza (es. GameObject con tag "PointingTarget").
            Transform target = null;
            foreach (var t in roomTransform.GetComponentsInChildren<Transform>())
            {
                if (t != roomTransform && t.CompareTag("PointingTarget"))
                {
                    target = t;
                    break;
                }
            }

            Vector3 pos = (target != null) ? target.position : roomTransform.position;
            var poi = Instantiate(pointOfInterestPrefab, pos, Quaternion.identity, roomTransform);

            // Se il prefab ha una luce, lo rendiamo un po' più evidente.
            var light = poi.GetComponentInChildren<Light>();
            if (light != null)
            {
                light.intensity *= 1.5f;
            }
        }

        private void ApplyCentering(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // Se la stanza ha già SafeHaven, Rewards o PointingOut,
            // assumiamo che ci sia già un forte focus centrale.
            var patterns = metadata.AppliedPatterns;
            if (patterns.Contains(AppraisalPatternType.SafeHaven) ||
                patterns.Contains(AppraisalPatternType.Rewards) ||
                patterns.Contains(AppraisalPatternType.PointingOut))
            {
                return;
            }

            // Altrimenti, usiamo il prefab di point of interest come elemento centrale generico.
            if (pointOfInterestPrefab != null)
            {
                Instantiate(pointOfInterestPrefab, roomTransform.position, Quaternion.identity, roomTransform);
            }
        }

        private void ApplySymmetry(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // Implementazione minimale: piazza due elementi simmetrici rispetto al centro stanza.
            if (symmetryPropPrefab == null)
                return;

            float distance = 2f;
            Vector3 leftPos  = roomTransform.position + new Vector3(-distance, 0f, 0f);
            Vector3 rightPos = roomTransform.position + new Vector3(distance, 0f, 0f);

            Instantiate(symmetryPropPrefab, leftPos, Quaternion.identity, roomTransform);
            Instantiate(symmetryPropPrefab, rightPos, Quaternion.identity, roomTransform);
        }

        private void ApplyAppOfObjects(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // Oggetti rari / insoliti che rompono la regolarità della stanza.
            if (rareObjectPrefab == null)
                return;

            Vector3 offset1 = new Vector3(1.5f, 0.5f, 0f);
            Vector3 offset2 = new Vector3(-1.0f, -0.5f, 0f);

            Instantiate(rareObjectPrefab, roomTransform.position + offset1, Quaternion.identity, roomTransform);
            Instantiate(rareObjectPrefab, roomTransform.position + offset2, Quaternion.identity, roomTransform);
        }

        private void ApplyContentDensity(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // ContentDensity: più elementi scenici / ostacoli nella stanza.
            if (occlusionPropPrefabs == null || occlusionPropPrefabs.Length == 0)
                return;

            int props  = Mathf.Max(1, propsPerContentDensity);
            float radius = 2.5f;

            for (int i = 0; i < props; i++)
            {
                float angle  = (Mathf.PI * 2f * i) / Mathf.Max(1, props);
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                var prefab = occlusionPropPrefabs[Random.Range(0, occlusionPropPrefabs.Length)];
                if (prefab == null)
                    continue;

                Instantiate(prefab, roomTransform.position + offset, Quaternion.identity, roomTransform);
            }
        }

        private void ApplyOcclusionAudio(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // Versione prototipale: scuriamo leggermente la stanza riducendo l'intensità delle luci.
            var lights = roomTransform.GetComponentsInChildren<Light>();
            foreach (var light in lights)
            {
                light.intensity *= 0.7f;
            }
        }

        private void ApplyCompetenceGate(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (competenceGatePrefab == null)
                return;

            Vector3 offset = new Vector3(0f, -1.5f, 0f);
            Instantiate(competenceGatePrefab, roomTransform.position + offset, Quaternion.identity, roomTransform);
        }

        #endregion
    }
}
