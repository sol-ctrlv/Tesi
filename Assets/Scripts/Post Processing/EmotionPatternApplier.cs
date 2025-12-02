using System.Collections.Generic;
using Edgar.Legacy.Core.MapLayouts;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmotionPCG
{
    public class EmotionPatternApplier : MonoBehaviour
    {
        [Header("Conflict (nemici)")]
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private int baseEnemiesPerConflict = 2;
        [SerializeField] private int extraEnemiesForFear = 1;
        [SerializeField] private float enemySpawnRadius = 2.2f;
        [SerializeField] private float enemyCollisionRadius = 0.5f;
        [SerializeField] private LayerMask enemyBlockingLayers;

        [Header("Spawn area")]
        [SerializeField] private string cameraTriggerName = "CameraTrigger"; // nome del child con il BoxCollider2D
        [SerializeField] private float spawnMarginFromBounds = 0.5f;         // margine interno per non attaccarsi ai muri

        [Header("Safe Haven")]
        [SerializeField] private GameObject safeHealPrefab;     // tile che cura alla prima collisione
        [SerializeField] private GameObject safeStatuePrefab;   // statue laterali opzionali

        [Header("Rewards")]
        [SerializeField] private GameObject rewardChestPrefab;  // chest con buff (script sul prefab)

        [Header("Pointing Out / Centering / Symmetry / Appearance")]
        [SerializeField] private GameObject pointOfInterestPrefab;
        [SerializeField] private float pointingOutOffsetY = 0.5f;
        [SerializeField] private GameObject symmetryPropPrefab;
        [SerializeField] private GameObject rareObjectPrefab;

        [Header("Content Density / Occlusion / Competence Gate")]
        [SerializeField] private GameObject[] occlusionPropPrefabs;
        [SerializeField] private int propsPerContentDensity = 4;
        [SerializeField] private GameObject audioOcclusionPrefab;
        [SerializeField] private GameObject competenceGatePrefab;

        [Header("Clear Signposting")]
        [SerializeField] private GameObject signpostPrefab;
        [SerializeField] private float signpostDistanceFromCenter = 1f;
        [SerializeField] private bool arrowUsesUpAsForward = true;

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
                        ApplyClearSignposting(metadata, roomTransform, roomCenter, hasSafeHaven);
                        break;

                    case AppraisalPatternType.PointingOut:
                        ApplyPointingOut(metadata, roomTransform, roomCenter);
                        break;

                    case AppraisalPatternType.Centering:
                        ApplyCentering(metadata, roomTransform, roomCenter);
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
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            int enemies = baseEnemiesPerConflict;
            if (metadata.LevelEmotion == EmotionType.Fear)
                enemies += extraEnemiesForFear;

            enemies = Mathf.Max(1, enemies);

            // prendiamo il BoxCollider2D del camera trigger
            if (!TryGetCameraBox(roomTransform, out var cameraBox))
            {
                Debug.LogWarning(
                    $"[EmotionPatternApplier] Nessun camera box trovato in '{roomTransform.name}'. Nemici NON spawnati (fallback rimosso).");
                return;
            }

            for (int i = 0; i < enemies; i++)
            {
                if (!TryFindFreeEnemySpotInCameraBox(cameraBox, out var spawnPos))
                {
                    Debug.LogWarning(
                        $"[EmotionPatternApplier] Nessuno spot libero per nemico {i + 1}/{enemies} in '{roomTransform.name}'.");
                    continue;
                }

                var prefab = ChooseEnemyPrefab(metadata, i);
                if (prefab == null) continue;
                Instantiate(prefab, spawnPos, Quaternion.identity, roomTransform);
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

        private void ApplyClearSignposting(
            EmotionRoomMetadata metadata,
            Transform roomTransform,
            Vector3 roomCenter,
            bool isSafeHavenRoom)
        {
            if (signpostPrefab == null)
                return;

            // 1) Direzione verso la prossima stanza sul critical path
            Vector3 dir = Vector3.up;

            if (metadata.HasNextCritical && metadata.NextCriticalDirection.sqrMagnitude > 0.0001f)
            {
                dir = metadata.NextCriticalDirection.normalized;
            }

            // 2) Posizione del cartello: spostato dal centro nella direzione scelta
            Vector3 mainPos = roomCenter + dir * signpostDistanceFromCenter;

            // 3) Rotazione: mappa l'asse locale della freccia (up o right) alla direzione
            Vector3 localForwardAxis = arrowUsesUpAsForward ? Vector3.up : Vector3.right;
            Quaternion mainRot = Quaternion.FromToRotation(localForwardAxis, dir);

            var sign = Instantiate(signpostPrefab, mainPos, mainRot, roomTransform);

            // 4) Cartello extra per safe haven (ad esempio fisso verso il centro)
            if (isSafeHavenRoom)
            {
                Vector3 safeDir = -dir; // ad esempio freccia che ributta verso il centro / sicurezza
                Vector3 safePos = roomCenter + safeDir * (signpostDistanceFromCenter * 0.7f);

                Quaternion safeRot = Quaternion.FromToRotation(localForwardAxis, safeDir);
                var safeSign = Instantiate(signpostPrefab, safePos, safeRot, roomTransform);
                // ConfigurePatternInstance(safeSign.transform);
            }
        }


        private void ApplyPointingOut(
            EmotionRoomMetadata metadata,
            Transform roomTransform,
            Vector3 roomCenter)
        {
            if (pointOfInterestPrefab == null)
                return;

            // 1. Decidiamo dove puntare
            // Se in futuro vuoi un marker esplicito, puoi aggiungere un child "PointingOutTarget" nel prefab stanza.
            Transform marker = roomTransform.Find("PointingOutTarget");

            Vector3 targetPos;
            if (marker != null)
            {
                targetPos = marker.position;
            }
            else
            {
                // fallback: sopra il centro stanza
                targetPos = roomCenter;
            }

            targetPos += new Vector3(0f, pointingOutOffsetY, 0f);

            // 2. Istanziamo il marker di Pointing Out (sprite + luce)
            Instantiate(pointOfInterestPrefab, targetPos, Quaternion.identity, roomTransform);

        }


        /// <summary>
        /// Oggetto centrato nella stanza, usato solo se non ci sono safe haven / rewards / pointing out.
        /// </summary>
        private void ApplyCentering(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
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
                Instantiate(pointOfInterestPrefab, roomCenter, Quaternion.identity, roomTransform);
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
        private bool TryGetCameraBox(Transform roomTransform, out BoxCollider2D box)
        {
            box = null;

            // 1. Cerca SOLO un child con quel nome
            if (!string.IsNullOrEmpty(cameraTriggerName))
            {
                var t = roomTransform.Find(cameraTriggerName);
                if (t == null)
                {
                    Debug.LogWarning(
                        $"[EmotionPatternApplier] Child '{cameraTriggerName}' non trovato in '{roomTransform.name}'.");
                    return false;
                }

                box = t.GetComponent<BoxCollider2D>();
                if (box == null)
                {
                    Debug.LogWarning(
                        $"[EmotionPatternApplier] Child '{cameraTriggerName}' in '{roomTransform.name}' non ha un BoxCollider2D.");
                    return false;
                }

                return true;
            }

            // (se vuoi proprio un fallback, puoi cercare il primo BoxCollider2D trigger, ma visto il casino, io per ora lo eviterei)
            return false;
        }

        private bool TryFindFreeEnemySpotInCameraBox(BoxCollider2D box, out Vector3 position)
        {
            const int maxTries = 30;

            // dimensioni locali della box (in local space)
            Vector2 halfSize = box.size * 0.5f;

            float minX = -halfSize.x + spawnMarginFromBounds;
            float maxX = halfSize.x - spawnMarginFromBounds;
            float minY = -halfSize.y + spawnMarginFromBounds;
            float maxY = halfSize.y - spawnMarginFromBounds;

            // sicurezza se margine esagerato
            if (minX > maxX) (minX, maxX) = (maxX, minX);
            if (minY > maxY) (minY, maxY) = (maxY, minY);

            Vector2 offset = box.offset; // il centro locale del collider

            for (int i = 0; i < maxTries; i++)
            {
                float localX = Random.Range(minX, maxX) + offset.x;
                float localY = Random.Range(minY, maxY) + offset.y;

                Vector3 localPoint = new Vector3(localX, localY, 0f);
                // trasformiamo da local (child del trigger) a world
                Vector3 worldPoint = box.transform.TransformPoint(localPoint);

                // check overlap con muri / props / altri nemici
                bool blocked = Physics2D.OverlapCircle(worldPoint, enemySpawnRadius, enemyBlockingLayers);
                if (!blocked)
                {
                    position = worldPoint;
                    return true;
                }
            }

            // fallback: centro della box
            position = box.transform.TransformPoint(box.offset);
            return false;
        }



        private GameObject ChooseEnemyPrefab(EmotionRoomMetadata metadata, int enemyIndex)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                return null;

            int idx = Random.Range(0, enemyPrefabs.Length);
            return enemyPrefabs[idx];

            #endregion
        }
    }
}
