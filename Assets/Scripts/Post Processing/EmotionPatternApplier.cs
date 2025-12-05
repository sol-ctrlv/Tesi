using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmotionPCG
{
    public class EmotionPatternApplier : MonoBehaviour
    {
        [Header("Conflict")]
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private int baseEnemiesPerConflict = 2;
        [SerializeField] private int extraEnemiesForFear = 1;
        [SerializeField] private float enemySpawnRadius = 2.2f;
        [SerializeField] private float enemyCollisionRadius = 0.5f;
        [SerializeField] private LayerMask enemyBlockingLayers;

        [Header("Spawn area")]
        [SerializeField] private string cameraTriggerName = "CameraTrigger";
        [SerializeField] private float spawnMarginFromBounds = 0.5f;

        [Header("Safe Haven")]
        [SerializeField] private GameObject safeHealPrefab;
        [SerializeField] private GameObject safeStatuePrefab;

        [Header("Clear Signposting")]
        [SerializeField] private GameObject signpostPrefab;
        [SerializeField] private float signpostDistanceFromCenter = 8f;
        [SerializeField] private bool arrowUsesUpAsForward = true;

        [Header("Rewards")]
        [SerializeField] private GameObject rewardChestPrefab;
        [SerializeField] private float rewardDistanceFromCenter = 1.5f;

        [Header("Centering")]
        [SerializeField] private GameObject centeringPrefab;

        [Header("Pointing Out")]
        [SerializeField] private GameObject pointingOutLightPrefab;
        [SerializeField] private float pointingOutOffsetY = 0.5f;

        [Header("Symmetry")]
        [SerializeField] private GameObject[] symmetryPropPrefabs;
        [SerializeField] private float symmetryOffsetFromCenter = 2f;

        [Header("Appearance")]
        [SerializeField] private GameObject appearanceStatuePrefab;
        [SerializeField] private GameObject[] appearanceBannerPrefabs;
        [SerializeField] private float appearanceCornerMargin = 1f;
        [SerializeField] private float appearanceBannerMargin = 0.5f;
        [SerializeField] private float bannerWallOffsetY = 0.5f;
        [SerializeField] private float minBannerSpacing = 1f;
        [SerializeField] private float bannerWallCheckRadius = 0.2f;
        [SerializeField] private LayerMask wallLayerMask;
        [SerializeField] private int minBanners = 2;
        [SerializeField] private int maxBanners = 5;

        [Header("Content Density")]
        [SerializeField] private GameObject[] contentDensityPrefabs;
        [SerializeField] private int propsPerContentDensity = 4;
        [SerializeField] private float contentMinSpacing = 0.9f;
        [SerializeField] private int contentMaxAttemptsPerProp = 10;

        [Header("Occlusion")]
        [SerializeField] private AudioClip audioOcclusionPrefab;
        [SerializeField, Range(0f, 1f)] private float occlusionLightRemovalRatio = 0.4f;
        [SerializeField] private int occlusionMinLightsToKeep = 1;
        [SerializeField] private float occlusionMinDelay = 2f;
        [SerializeField] private float occlusionMaxDelay = 5f;
        [SerializeField] private float occlusionVolume = 0.5f;


        [Header("Competence Gate")]
        [SerializeField] private GameObject competenceGatePrefab;

        [Header("Base Lighting")]
        [SerializeField] private GameObject wonderLightPrefab;
        [SerializeField] private GameObject fearLightPrefab;
        [SerializeField] private GameObject joyLightPrefab;

        [SerializeField] private int wonderMinLights = 2;
        [SerializeField] private int wonderMaxLights = 3;

        [SerializeField] private int fearMinLights = 1;
        [SerializeField] private int fearMaxLights = 2;

        [SerializeField] private int joyMinLights = 3;
        [SerializeField] private int joyMaxLights = 4;

        [Header("Level end")]
        [SerializeField] private GameObject endLevelStairsPrefab;
        [SerializeField] private Vector3 endLevelStairsOffset = Vector3.zero;

        public void ApplyAllPatternsInScene()
        {
            var rooms = FindObjectsOfType<EmotionRoomMetadata>();

            foreach (var room in rooms)
            {
                if (!ShouldApplyPatternsToRoom(room))
                    continue;

                ApplyPatternsToRoom(room);
            }

            PlaceEndLevelStairs();
        }

        private bool ShouldApplyPatternsToRoom(EmotionRoomMetadata room)
        {
            if (room == null)
                return false;

            string name = room.gameObject.name;
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.StartsWith("room", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("optional", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("end", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("deadend", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private void ApplyPatternsToRoom(EmotionRoomMetadata metadata)
        {
            if (metadata == null)
                return;

            var roomTransform = metadata.transform;
            List<AppraisalPatternType> patterns = metadata.AppliedPatterns;

            Vector3 roomCenter = GetRoomCenter(roomTransform);
            ApplyBaseLighting(metadata, roomTransform);

            bool hasSafeHaven = patterns.Contains(AppraisalPatternType.SafeHaven);

            foreach (var pattern in patterns)
            {
                switch (pattern)
                {
                    case AppraisalPatternType.Conflict:
                        if (!hasSafeHaven)
                            ApplyConflict(metadata, roomTransform);
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
                        ApplyPointingOut(roomTransform, roomCenter);
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
                            ApplyOcclusion(metadata, roomTransform);
                        break;

                    case AppraisalPatternType.CompetenceGate:
                        if (!hasSafeHaven)
                            ApplyCompetenceGate(metadata, roomTransform);
                        break;
                }
            }
        }

        private void ApplyConflict(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            int enemies = baseEnemiesPerConflict;
            if (metadata.LevelEmotion == EmotionType.Fear)
                enemies += extraEnemiesForFear;

            enemies = Mathf.Max(1, enemies);

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

                var prefab = ChooseEnemyPrefab(metadata);
                if (prefab == null) continue;
                Instantiate(prefab, spawnPos, Quaternion.identity, roomTransform);
            }
        }

        private void ApplySafeHaven(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            if (safeHealPrefab != null)
            {
                Instantiate(safeHealPrefab, roomCenter, Quaternion.identity, roomTransform);
            }

            if (safeStatuePrefab != null)
            {
                float sideOffset = 1.5f;
                Instantiate(safeStatuePrefab, roomCenter + new Vector3(-sideOffset, 0f, 0f),
                    Quaternion.identity, roomTransform);
                Instantiate(safeStatuePrefab, roomCenter + new Vector3(sideOffset, 0f, 0f),
                    Quaternion.identity, roomTransform);
            }
        }

        private void ApplyRewards(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            if (rewardChestPrefab == null)
                return;

            Vector3[] directions =
            {
                Vector3.up,
                Vector3.right,
                Vector3.down,
                Vector3.left
            };

            int index = UnityEngine.Random.Range(0, directions.Length);
            Vector3 dir = directions[index];

            Vector3 chestPos = roomCenter + dir * rewardDistanceFromCenter;

            Instantiate(rewardChestPrefab, chestPos, Quaternion.identity, roomTransform);
        }

        private void ApplyClearSignposting(
            EmotionRoomMetadata metadata,
            Transform roomTransform,
            Vector3 roomCenter,
            bool isSafeHavenRoom)
        {
            if (signpostPrefab == null)
                return;

            Vector3 dir = Vector3.up;

            if (metadata.HasNextCritical && metadata.NextCriticalDirection.sqrMagnitude > 0.0001f)
            {
                dir = metadata.NextCriticalDirection.normalized;
            }

            Vector3 mainPos;
            if (isSafeHavenRoom)
            {
                float safeOffsetTiles = 2f;
                mainPos = roomCenter + new Vector3(0f, safeOffsetTiles, 0f);
            }
            else
            {
                mainPos = roomCenter + dir * signpostDistanceFromCenter;
            }

            Vector3 localForwardAxis = arrowUsesUpAsForward ? Vector3.up : Vector3.right;
            Quaternion mainRot = Quaternion.FromToRotation(localForwardAxis, dir);

            Instantiate(signpostPrefab, mainPos, mainRot, roomTransform);
        }

        private void ApplyPointingOut(
            Transform roomTransform,
            Vector3 roomCenter)
        {
            if (pointingOutLightPrefab == null)
                return;

            Transform target = roomTransform.Find("PointingOutTarget");

            Vector3 targetPos;
            Transform parent;

            if (target != null)
            {
                targetPos = target.position;
                parent = target;
            }
            else
            {
                targetPos = roomCenter + new Vector3(0f, pointingOutOffsetY, 0f);
                parent = roomTransform;
            }

            Instantiate(pointingOutLightPrefab, targetPos, Quaternion.identity, parent);
        }

        private void ApplyCentering(
            EmotionRoomMetadata metadata,
            Transform roomTransform,
            Vector3 roomCenter)
        {
            var patterns = metadata.AppliedPatterns;

            bool hasImportantCenter =
                patterns.Contains(AppraisalPatternType.SafeHaven) ||
                patterns.Contains(AppraisalPatternType.Rewards) ||
                patterns.Contains(AppraisalPatternType.PointingOut);

            Transform centerAnchor = roomTransform.Find("RoomCenter");
            Vector3 pos = centerAnchor != null ? centerAnchor.position : roomCenter;
            Transform parentForLight = centerAnchor != null ? centerAnchor : roomTransform;

            if (hasImportantCenter)
            {
                if (pointingOutLightPrefab == null)
                    return;

                Instantiate(pointingOutLightPrefab, pos, Quaternion.identity, parentForLight);
            }
            else
            {
                if (centeringPrefab == null)
                    return;

                var poi = Instantiate(centeringPrefab, pos, Quaternion.identity, roomTransform);
            }
        }

        private void ApplySymmetry(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (symmetryPropPrefabs == null || symmetryPropPrefabs.Length == 0)
                return;

            Vector3 center = GetRoomCenter(roomTransform);
            float offset = symmetryOffsetFromCenter;

            var prefab = symmetryPropPrefabs[UnityEngine.Random.Range(0, symmetryPropPrefabs.Length)];

            Instantiate(prefab, center + new Vector3(-offset, 0f, 0f), Quaternion.identity, roomTransform);
            Instantiate(prefab, center + new Vector3(offset, 0f, 0f), Quaternion.identity, roomTransform);
        }

        private void ApplyAppOfObjects(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            bool hasStatues = appearanceStatuePrefab != null;
            bool hasBanners = appearanceBannerPrefabs != null && appearanceBannerPrefabs.Length > 0;

            if (!hasStatues && !hasBanners)
                return;

            if (!TryGetCameraBox(roomTransform, out var box))
            {
                if (appearanceStatuePrefab != null)
                {
                    Vector3 centerFallback = GetRoomCenter(roomTransform);
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 1.5f;
                    Vector3 pos = centerFallback + new Vector3(randomOffset.x, randomOffset.y, 0f);
                    Instantiate(appearanceStatuePrefab, pos, Quaternion.identity, roomTransform);
                }
                return;
            }

            if (hasStatues && hasBanners)
            {
                if (UnityEngine.Random.value < 0.5f)
                    SpawnAppearanceStatues(box, roomTransform);
                else
                    SpawnAppearanceBanners(box, roomTransform);
            }
            else if (hasStatues)
            {
                Vector3 centerFallback = GetRoomCenter(roomTransform);
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 1.5f;
                Vector3 pos = centerFallback + new Vector3(randomOffset.x, randomOffset.y, 0f);
                Instantiate(appearanceStatuePrefab, pos, Quaternion.identity, roomTransform);
            }
            else if (hasBanners)
            {
                SpawnAppearanceBanners(box, roomTransform);
            }
        }

        private void SpawnAppearanceStatues(BoxCollider2D box, Transform roomTransform)
        {
            if (appearanceStatuePrefab == null)
                return;

            var prefab = appearanceStatuePrefab;

            Vector2 halfSize = box.size * 0.5f;
            Vector2 offset = box.offset;
            float m = appearanceCornerMargin;

            Vector3 topLeft = new Vector3(-halfSize.x + m + offset.x, halfSize.y - m + offset.y, 0f);
            Vector3 topRight = new Vector3(halfSize.x - m + offset.x, halfSize.y - m + offset.y, 0f);
            Vector3 bottomLeft = new Vector3(-halfSize.x + m + offset.x, -halfSize.y + m + offset.y, 0f);
            Vector3 bottomRight = new Vector3(halfSize.x - m + offset.x, -halfSize.y + m + offset.y, 0f);

            Vector3 localA, localB;
            if (UnityEngine.Random.value < 0.5f)
            {
                localA = topLeft;
                localB = bottomRight;
            }
            else
            {
                localA = topRight;
                localB = bottomLeft;
            }

            Vector3 worldA = box.transform.TransformPoint(localA);
            Vector3 worldB = box.transform.TransformPoint(localB);

            Instantiate(prefab, worldA, Quaternion.identity, roomTransform);
            Instantiate(prefab, worldB, Quaternion.identity, roomTransform);
        }

        private void SpawnAppearanceBanners(BoxCollider2D box, Transform roomTransform)
        {
            if (appearanceBannerPrefabs == null || appearanceBannerPrefabs.Length == 0)
                return;

            Vector2 halfSize = box.size * 0.5f;
            Vector2 offset = box.offset;

            float minX = -halfSize.x + appearanceBannerMargin + offset.x;
            float maxX = halfSize.x - appearanceBannerMargin + offset.x;
            if (minX > maxX) (minX, maxX) = (maxX, minX);

            float topY = halfSize.y - appearanceBannerMargin + offset.y + bannerWallOffsetY;

            int min = Mathf.Max(1, minBanners);
            int max = Mathf.Max(min, maxBanners);
            int targetCount = UnityEngine.Random.Range(min, max + 1);

            List<float> usedLocalXs = new List<float>();

            int placed = 0;
            int maxGlobalAttempts = targetCount * 10;
            int attempts = 0;

            while (placed < targetCount && attempts < maxGlobalAttempts)
            {
                attempts++;

                float x = UnityEngine.Random.Range(minX, maxX);

                bool tooClose = false;
                foreach (var usedX in usedLocalXs)
                {
                    if (Mathf.Abs(x - usedX) < minBannerSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                Vector3 local = new Vector3(x, topY, 0f);
                Vector3 world = box.transform.TransformPoint(local);

                if (wallLayerMask.value != 0)
                {
                    var hit = Physics2D.OverlapCircle(world, bannerWallCheckRadius, wallLayerMask);
                    if (hit == null)
                    {
                        continue;
                    }
                }

                usedLocalXs.Add(x);

                var prefab = appearanceBannerPrefabs[UnityEngine.Random.Range(0, appearanceBannerPrefabs.Length)];
                Instantiate(prefab, world, Quaternion.identity, roomTransform);

                placed++;
            }
        }

        private void ApplyContentDensity(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (contentDensityPrefabs == null || contentDensityPrefabs.Length == 0)
                return;

            int props = Mathf.Max(1, propsPerContentDensity);

            if (!TryGetCameraBox(roomTransform, out var box))
            {
                Vector3 center = GetRoomCenter(roomTransform);
                List<Vector3> placed = new List<Vector3>();

                for (int i = 0; i < props; i++)
                {
                    bool placedThis = false;

                    for (int attempt = 0; attempt < contentMaxAttemptsPerProp && !placedThis; attempt++)
                    {
                        Vector2 offset = UnityEngine.Random.insideUnitCircle * 2.0f;
                        Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);

                        if (IsTooCloseToExisting(candidate, placed, contentMinSpacing))
                            continue;

                        var prefab = contentDensityPrefabs[UnityEngine.Random.Range(0, contentDensityPrefabs.Length)];
                        if (prefab == null)
                            break;

                        Instantiate(prefab, candidate, Quaternion.identity, roomTransform);
                        placed.Add(candidate);
                        placedThis = true;
                    }
                }

                return;
            }

            Vector2 halfSize = box.size * 0.5f;

            float minX = -halfSize.x + spawnMarginFromBounds;
            float maxX = halfSize.x - spawnMarginFromBounds;
            float minY = -halfSize.y + spawnMarginFromBounds;
            float maxY = halfSize.y - spawnMarginFromBounds;

            if (minX > maxX) (minX, maxX) = (maxX, minX);
            if (minY > maxY) (minY, maxY) = (maxY, minY);

            Vector2 offsetCenter = box.offset;

            List<Vector3> placedWorldPositions = new List<Vector3>();

            for (int i = 0; i < props; i++)
            {
                bool placedThis = false;

                for (int attempt = 0; attempt < contentMaxAttemptsPerProp && !placedThis; attempt++)
                {
                    float localX = UnityEngine.Random.Range(minX, maxX) + offsetCenter.x;
                    float localY = UnityEngine.Random.Range(minY, maxY) + offsetCenter.y;

                    Vector3 localPoint = new Vector3(localX, localY, 0f);
                    Vector3 worldPoint = box.transform.TransformPoint(localPoint);

                    if (IsTooCloseToExisting(worldPoint, placedWorldPositions, contentMinSpacing))
                        continue;

                    var prefab = contentDensityPrefabs[UnityEngine.Random.Range(0, contentDensityPrefabs.Length)];
                    if (prefab == null)
                        break;

                    Instantiate(prefab, worldPoint, Quaternion.identity, roomTransform);
                    placedWorldPositions.Add(worldPoint);
                    placedThis = true;
                }
            }
        }

        private bool IsTooCloseToExisting(Vector3 candidate, List<Vector3> existing, float minDistance)
        {
            float sqMin = minDistance * minDistance;

            for (int i = 0; i < existing.Count; i++)
            {
                if ((candidate - existing[i]).sqrMagnitude < sqMin)
                    return true;
            }

            return false;
        }

        private void ApplyOcclusion(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            ReduceLightsForOcclusion(roomTransform);

            if (audioOcclusionPrefab == null)
                return;

            // prendiamo il CameraTrigger della stanza
            if (!TryGetCameraBox(roomTransform, out var box))
            {
                Debug.LogWarning(
                    $"[EmotionPatternApplier] Nessun camera box trovato in '{roomTransform.name}' per OcclusionAudio.");
                return;
            }

            // aggiungiamo/riusiamo il componente SOLO su questa stanza
            var ghostAudio = box.GetComponent<RoomGhostAudio>();
            if (ghostAudio == null)
            {
                ghostAudio = box.gameObject.AddComponent<RoomGhostAudio>();
            }

            ghostAudio.Initialize(audioOcclusionPrefab, occlusionMinDelay, occlusionMaxDelay, occlusionVolume);
        }

        private void ReduceLightsForOcclusion(Transform roomTransform)
        {
            var lights = roomTransform.GetComponentsInChildren<Light2D>();
            if (lights == null || lights.Length == 0)
                return;

            int total = lights.Length;

            int maxToRemoveByRatio = Mathf.FloorToInt(total * occlusionLightRemovalRatio);
            int minKeep = Mathf.Clamp(occlusionMinLightsToKeep, 0, total);
            int maxRemovable = Mathf.Max(0, total - minKeep);
            int toRemove = Mathf.Min(maxToRemoveByRatio, maxRemovable);

            if (toRemove <= 0)
                return;

            var lightList = new List<Light2D>(lights);

            for (int i = 0; i < toRemove && lightList.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, lightList.Count);
                var l = lightList[idx];
                lightList.RemoveAt(idx);

                if (l != null)
                {
                    l.enabled = false;
                }
            }
        }

        private void ApplyCompetenceGate(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (competenceGatePrefab == null)
                return;

            var patterns = metadata.AppliedPatterns;
            if (!patterns.Contains(AppraisalPatternType.Conflict))
            {
                ApplyConflict(metadata, roomTransform);
            }

            Vector3 spawnPos;

            if (TryGetCameraBox(roomTransform, out var box)
                && TryFindFreeEnemySpotInCameraBox(box, out spawnPos))
            {
            }
            else
            {
                spawnPos = GetRoomCenter(roomTransform);
            }

            Instantiate(competenceGatePrefab, spawnPos, Quaternion.identity, roomTransform);
        }

        private Vector3 GetRoomCenter(Transform roomTransform)
        {
            Transform centerMarker = roomTransform.Find("RoomCenter");
            if (centerMarker != null)
                return centerMarker.position;

            return roomTransform.position;
        }

        private void ApplyBaseLighting(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            GameObject lightPrefab = null;
            int minLights = 0;
            int maxLights = 0;

            switch (metadata.LevelEmotion)
            {
                case EmotionType.Wonder:
                    lightPrefab = wonderLightPrefab;
                    minLights = wonderMinLights;
                    maxLights = wonderMaxLights;
                    break;

                case EmotionType.Fear:
                    lightPrefab = fearLightPrefab;
                    minLights = fearMinLights;
                    maxLights = fearMaxLights;
                    break;

                case EmotionType.Joy:
                    lightPrefab = joyLightPrefab;
                    minLights = joyMinLights;
                    maxLights = joyMaxLights;
                    break;
            }

            if (lightPrefab == null || maxLights <= 0)
                return;

            if (maxLights < minLights)
                maxLights = minLights;

            int lightsToSpawn = UnityEngine.Random.Range(minLights, maxLights + 1);
            if (lightsToSpawn <= 0)
                return;

            if (TryGetCameraBox(roomTransform, out var box))
            {
                Vector2 halfSize = box.size * 0.5f;
                Vector2 offset = box.offset;

                float minX = -halfSize.x + spawnMarginFromBounds + offset.x;
                float maxX = halfSize.x - spawnMarginFromBounds + offset.x;
                float minY = -halfSize.y + spawnMarginFromBounds + offset.y;
                float maxY = halfSize.y - spawnMarginFromBounds + offset.y;

                if (minX > maxX) (minX, maxX) = (maxX, minX);
                if (minY > maxY) (minY, maxY) = (maxY, minY);

                float width = maxX - minX;
                float height = maxY - minY;

                int cols = Mathf.CeilToInt(Mathf.Sqrt(lightsToSpawn));
                int rows = Mathf.CeilToInt((float)lightsToSpawn / cols);

                float cellWidth = width / cols;
                float cellHeight = height / rows;

                List<Vector2> candidateLocalPositions = new List<Vector2>();

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float cellMinX = minX + c * cellWidth;
                        float cellMinY = minY + r * cellHeight;

                        float x = UnityEngine.Random.Range(cellMinX + cellWidth * 0.2f,
                                               cellMinX + cellWidth * 0.8f);
                        float y = UnityEngine.Random.Range(cellMinY + cellHeight * 0.2f,
                                               cellMinY + cellHeight * 0.8f);

                        candidateLocalPositions.Add(new Vector2(x, y));
                    }
                }

                int placed = 0;
                int attempts = 0;
                int maxAttempts = candidateLocalPositions.Count * 2;

                while (placed < lightsToSpawn && candidateLocalPositions.Count > 0 && attempts < maxAttempts)
                {
                    attempts++;

                    int idx = UnityEngine.Random.Range(0, candidateLocalPositions.Count);
                    Vector2 local = candidateLocalPositions[idx];
                    candidateLocalPositions.RemoveAt(idx);

                    Vector3 localPoint = new Vector3(local.x, local.y, 0f);
                    Vector3 worldPoint = box.transform.TransformPoint(localPoint);

                    Instantiate(lightPrefab, worldPoint, Quaternion.identity, roomTransform);
                    placed++;
                }
            }
            else
            {
                Vector3 center = GetRoomCenter(roomTransform);

                for (int i = 0; i < lightsToSpawn; i++)
                {
                    Vector2 offsetCircle = UnityEngine.Random.insideUnitCircle * 2.0f;
                    Vector3 pos = center + new Vector3(offsetCircle.x, offsetCircle.y, 0f);

                    Instantiate(lightPrefab, pos, Quaternion.identity, roomTransform);
                }
            }
        }

        public void PlaceEndLevelStairs()
        {
            if (endLevelStairsPrefab == null)
            {
                Debug.LogWarning("[EmotionPCG] End level stairs prefab non assegnato in EmotionPatternApplier.");
                return;
            }

            var rooms = FindObjectsOfType<EmotionRoomMetadata>();

            EmotionRoomMetadata targetRoom = null;

            foreach (var room in rooms)
            {
                if (IsEndRoom(room))
                {
                    targetRoom = room;
                    break;
                }
            }

            if (targetRoom == null)
                return;

            Transform roomTransform = targetRoom.transform;

            Vector3 center = GetRoomCenter(roomTransform);

            Vector3 spawnPos = center + endLevelStairsOffset;

            Instantiate(endLevelStairsPrefab, spawnPos, Quaternion.identity, roomTransform);
        }

        private bool IsEndRoom(EmotionRoomMetadata room)
        {
            if (room == null)
                return false;

            string name = room.RoomName;
            if (string.IsNullOrEmpty(name))
                name = room.gameObject.name;

            if (!string.IsNullOrEmpty(name) && name.StartsWith("end", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private bool TryGetCameraBox(Transform roomTransform, out BoxCollider2D box)
        {
            box = null;

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

            return false;
        }

        private bool TryFindFreeEnemySpotInCameraBox(BoxCollider2D box, out Vector3 position)
        {
            var freeSpots = ComputeFreeEnemySpots(box);

            if (freeSpots == null || freeSpots.Count == 0)
            {
                position = box.transform.TransformPoint(box.offset);
                return false;
            }

            int idx = UnityEngine.Random.Range(0, freeSpots.Count);
            position = freeSpots[idx];
            return true;
        }

        private List<Vector3> ComputeFreeEnemySpots(BoxCollider2D box)
        {
            var result = new List<Vector3>();

            Vector2 halfSize = box.size * 0.5f;

            float minX = -halfSize.x + spawnMarginFromBounds;
            float maxX = halfSize.x - spawnMarginFromBounds;
            float minY = -halfSize.y + spawnMarginFromBounds;
            float maxY = halfSize.y - spawnMarginFromBounds;

            if (minX > maxX) (minX, maxX) = (maxX, minX);
            if (minY > maxY) (minY, maxY) = (maxY, minY);

            Vector2 offset = box.offset;

            float step = Mathf.Max(enemyCollisionRadius * 1.5f, 0.5f);

            for (float x = minX; x <= maxX; x += step)
            {
                for (float y = minY; y <= maxY; y += step)
                {
                    float localX = x + offset.x;
                    float localY = y + offset.y;

                    Vector3 localPoint = new Vector3(localX, localY, 0f);
                    Vector3 worldPoint = box.transform.TransformPoint(localPoint);

                    bool blocked = Physics2D.OverlapCircle(worldPoint, enemyCollisionRadius, enemyBlockingLayers);
                    if (!blocked)
                    {
                        result.Add(worldPoint);
                    }
                }
            }

            return result;
        }

        private GameObject ChooseEnemyPrefab(EmotionRoomMetadata metadata)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                return null;

            int idx = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            return enemyPrefabs[idx];
        }
    }
}
