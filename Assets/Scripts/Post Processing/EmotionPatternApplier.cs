using System;
using System.Collections.Generic;
using Edgar.Legacy.Core.MapLayouts;
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
        [SerializeField] private string cameraTriggerName = "CameraTrigger"; // nome del child con il BoxCollider2D
        [SerializeField] private float spawnMarginFromBounds = 0.5f;         // margine interno per non attaccarsi ai muri

        [Header("Safe Haven")]
        [SerializeField] private GameObject safeHealPrefab;     // tile che cura alla prima collisione
        [SerializeField] private GameObject safeStatuePrefab;   // statue laterali opzionali

        [Header("Clear Signposting")]
        [SerializeField] private GameObject signpostPrefab;
        [SerializeField] private float signpostDistanceFromCenter = 8f;
        [SerializeField] private bool arrowUsesUpAsForward = true;

        [Header("Rewards")]
        [SerializeField] private GameObject rewardChestPrefab;  // chest con buff (script sul prefab)
        [SerializeField] private float rewardDistanceFromCenter = 1.5f; // distanza fissa dal centro

        [Header("Centering")]
        [SerializeField] private GameObject centeringPrefab;

        [Header("Pointing Out")]
        [SerializeField] private GameObject pointingOutLightPrefab;  // luce + sfarfallio per Pointing Out
        [SerializeField] private float pointingOutOffsetY = 0.5f;

        [Header("Symmetry")]
        [SerializeField] private GameObject[] symmetryPropPrefabs;   // array di oggetti "simmetrici"
        [SerializeField] private float symmetryOffsetFromCenter = 2f; // offset fisso a sinistra/destra del centro

        [Header("Appearance")]
        [SerializeField] private GameObject appearanceStatuePrefab;     // statua per Appearance (angoli)
        [SerializeField] private GameObject[] appearanceBannerPrefabs;  // stendardi per Appearance (bordo alto)
        [SerializeField] private float appearanceCornerMargin = 1f;     // margine dagli angoli per le statue
        [SerializeField] private float appearanceBannerMargin = 0.5f;   // margine dai bordi per gli stendardi
        [SerializeField] private float bannerWallOffsetY = 0.5f;
        [SerializeField] private float minBannerSpacing = 1f;           // distanza minima tra stendardi
        [SerializeField] private float bannerWallCheckRadius = 0.2f;    // raggio per controllare il muro
        [SerializeField] private LayerMask wallLayerMask;               // layer dei muri (tilemap muro, ecc.)
        [SerializeField] private int minBanners = 2;                    // numero minimo di stendardi
        [SerializeField] private int maxBanners = 5;                    // numero massimo di stendardi

        [Header("Content Density / Physical Occlusion")]
        [SerializeField] private GameObject[] contentDensityPrefabs;
        [SerializeField] private int propsPerContentDensity = 4;

        [Header("Audio Occlusion")]
        [SerializeField] private GameObject audioOcclusionPrefab;
        [SerializeField, Range(0f, 1f)] private float occlusionLightRemovalRatio = 0.4f;
        [SerializeField] private int occlusionMinLightsToKeep = 1;
        [SerializeField] private float occlusionAudioOutsideOffset = 1.0f;

        [Header("Competence Gate")]
        [SerializeField] private GameObject competenceGatePrefab;

        [Header("Base Lighting")]
        [SerializeField] private GameObject wonderLightPrefab;
        [SerializeField] private GameObject fearLightPrefab;
        [SerializeField] private GameObject joyLightPrefab;

        // range di luci per stanza per ogni emozione
        [SerializeField] private int wonderMinLights = 2;
        [SerializeField] private int wonderMaxLights = 3;

        [SerializeField] private int fearMinLights = 1;
        [SerializeField] private int fearMaxLights = 2;

        [SerializeField] private int joyMinLights = 3;
        [SerializeField] private int joyMaxLights = 4;

        [Header("Level end")]
        [SerializeField] private GameObject endLevelStairsPrefab;
        [SerializeField] private Vector3 endLevelStairsOffset = Vector3.zero;

        /// <summary>
        /// Chiamato dal PostProcessing dopo aver scritto i metadata sulle stanze.
        /// </summary>
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

        /// <summary>
        /// Decide se applicare luci + pattern a questa stanza in base al nome.
        /// Valide: Room*, Optional*, End*, Deadend*.
        /// Corridoi, stanze speciali, ecc. vengono ignorate.
        /// </summary>
        private bool ShouldApplyPatternsToRoom(EmotionRoomMetadata room)
        {
            if (room == null)
                return false;

            string name = room.gameObject.name;
            if (string.IsNullOrEmpty(name))
                return false;

            // accettiamo prefissi, così Room_1, Optional_2, End_0, Deadend_3 funzionano
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


            Vector3 roomCenter = GetRoomCenter(roomTransform);  // centro calcolato con ancora "RoomCenter" se presente
            ApplyBaseLighting(metadata, roomTransform);         // luci di base in base all'emozione

            bool hasSafeHaven = patterns.Contains(AppraisalPatternType.SafeHaven);

            // SafeHaven “spegne” alcuni pattern ostili
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

                var prefab = ChooseEnemyPrefab(metadata);
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
        /// Reward: chest posizionata a distanza fissa dal centro stanza,
        /// lungo una delle 4 direzioni cardinali (up/right/down/left) scelta a caso.
        /// La logica di buff è nello script del prefab.
        /// </summary>
        private void ApplyRewards(EmotionRoomMetadata metadata, Transform roomTransform, Vector3 roomCenter)
        {
            if (rewardChestPrefab == null)
                return;

            // 4 direzioni perpendicolari, rispetto al centro stanza
            Vector3[] directions =
            {
                Vector3.up,
                Vector3.right,
                Vector3.down,
                Vector3.left
            };

            int index = UnityEngine.Random.Range(0, directions.Length);
            Vector3 dir = directions[index];

            // posizione finale: centro + direzione * distanza fissa
            Vector3 chestPos = roomCenter + dir * rewardDistanceFromCenter;

            Instantiate(rewardChestPrefab, chestPos, Quaternion.identity, roomTransform);
        }


        /// <summary>
        /// Clear signposting: cartello (freccia) che indica la direzione della prossima stanza
        /// sul critical path. Nelle stanze SafeHaven lo mettiamo due tile sopra il centro,
        /// così non collide con la healing tile.
        /// </summary>
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

            // 2) Posizione del cartello
            Vector3 mainPos;
            if (isSafeHavenRoom)
            {
                // nelle SafeHaven lo spostiamo verticalmente rispetto al centro/healing tile
                float safeOffsetTiles = 2f;
                mainPos = roomCenter + new Vector3(0f, safeOffsetTiles, 0f);
            }
            else
            {
                // nelle altre stanze resta spostato lungo la direzione del critical path
                mainPos = roomCenter + dir * signpostDistanceFromCenter;
            }

            // 3) Rotazione: mappa l'asse locale della freccia (up o right) alla direzione
            Vector3 localForwardAxis = arrowUsesUpAsForward ? Vector3.up : Vector3.right;
            Quaternion mainRot = Quaternion.FromToRotation(localForwardAxis, dir);

            Instantiate(signpostPrefab, mainPos, mainRot, roomTransform);
        }


        /// Pointing Out: evidenzia un punto di interesse con una luce 2D (e sfarfallio),
        /// posizionata direttamente sull'oggetto da "puntare".
        private void ApplyPointingOut(
            Transform roomTransform,
            Vector3 roomCenter)
        {
            if (pointingOutLightPrefab == null)
                return;

            // 1. Decidiamo l'oggetto/transform da evidenziare
            Transform target = roomTransform.Find("PointingOutTarget");

            Vector3 targetPos;
            Transform parent;

            if (target != null)
            {
                targetPos = target.position;
                parent = target; // la luce segue direttamente l'oggetto puntato
            }
            else
            {
                // fallback: centro stanza leggermente spostato in Y
                targetPos = roomCenter + new Vector3(0f, pointingOutOffsetY, 0f);
                parent = roomTransform;
            }

            // 2. Istanziamo solo la luce (con eventuale script di sfarfallio) come child del target
            Instantiate(pointingOutLightPrefab, targetPos, Quaternion.identity, parent);
        }


        /// <summary>
        /// Centering: se al centro c'è già qualcosa di importante (SafeHaven/Rewards/PointingOut)
        /// aggiunge solo una luce di PointingOut sul centro; altrimenti istanzia la runa
        /// come punto di interesse centrale.
        /// </summary>
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

            // proviamo a usare l’anchor RoomCenter se esiste
            Transform centerAnchor = roomTransform.Find("RoomCenter");
            Vector3 pos = centerAnchor != null ? centerAnchor.position : roomCenter;
            Transform parentForLight = centerAnchor != null ? centerAnchor : roomTransform;

            if (hasImportantCenter)
            {
                // caso 1: c'è già qualcosa di importante al centro → solo luce
                if (pointingOutLightPrefab == null)
                    return;

                Instantiate(pointingOutLightPrefab, pos, Quaternion.identity, parentForLight);
            }
            else
            {
                // caso 2: niente di importante → runa al centro
                if (centeringPrefab == null)
                    return;

                var poi = Instantiate(centeringPrefab, pos, Quaternion.identity, roomTransform);
            }
        }


        /// <summary>
        /// Symmetry: istanzia due props scelti da un array, speculari rispetto al centro stanza,
        /// con un offset fisso a sinistra e destra.
        /// </summary>
        private void ApplySymmetry(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (symmetryPropPrefabs == null || symmetryPropPrefabs.Length == 0)
                return;

            Vector3 center = GetRoomCenter(roomTransform);
            float offset = symmetryOffsetFromCenter;

            // scegliamo un prefab dall'array e lo replichiamo a sinistra e destra
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

            // Se non riusciamo a usare il camera trigger, fallback al comportamento semplice
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

            // Scegliamo quale modalità usare
            if (hasStatues && hasBanners)
            {
                if (UnityEngine.Random.value < 0.5f)
                    SpawnAppearanceStatues(box, roomTransform);
                else
                    SpawnAppearanceBanners(box, roomTransform);
            }
            else if (appearanceStatuePrefab != null)
            {
                Vector3 centerFallback = GetRoomCenter(roomTransform);
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 1.5f;
                Vector3 pos = centerFallback + new Vector3(randomOffset.x, randomOffset.y, 0f);
                Instantiate(appearanceStatuePrefab, pos, Quaternion.identity, roomTransform);
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

            // quattro angoli locali (con offset)
            Vector3 topLeft = new Vector3(-halfSize.x + m + offset.x, halfSize.y - m + offset.y, 0f);
            Vector3 topRight = new Vector3(halfSize.x - m + offset.x, halfSize.y - m + offset.y, 0f);
            Vector3 bottomLeft = new Vector3(-halfSize.x + m + offset.x, -halfSize.y + m + offset.y, 0f);
            Vector3 bottomRight = new Vector3(halfSize.x - m + offset.x, -halfSize.y + m + offset.y, 0f);

            // scegliamo casualmente una delle due diagonali: (TL, BR) oppure (TR, BL)
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

            // bordo alto del camera trigger + offset verso il muro
            float topY = halfSize.y - appearanceBannerMargin + offset.y + bannerWallOffsetY;

            int min = Mathf.Max(1, minBanners);
            int max = Mathf.Max(min, maxBanners);
            int targetCount = UnityEngine.Random.Range(min, max + 1);

            // teniamo traccia degli X (in spazio locale) già usati per applicare lo spacing
            List<float> usedLocalXs = new List<float>();

            int placed = 0;
            int maxGlobalAttempts = targetCount * 10;
            int attempts = 0;

            while (placed < targetCount && attempts < maxGlobalAttempts)
            {
                attempts++;

                // 1. scegliamo una X casuale nel range
                float x = UnityEngine.Random.Range(minX, maxX);

                // 2. controllo spacing: non troppo vicino ad altri banner
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
                    continue; // riprova con un'altra X

                // 3. calcoliamo posizione locale e mondo
                Vector3 local = new Vector3(x, topY, 0f);
                Vector3 world = box.transform.TransformPoint(local);

                // 4. controllo muro: vicino a questo punto deve esserci un collider di muro
                if (wallLayerMask.value != 0)
                {
                    var hit = Physics2D.OverlapCircle(world, bannerWallCheckRadius, wallLayerMask);
                    if (hit == null)
                    {
                        // nessun muro in zona -> probabilmente porta/buco, scartiamo
                        continue;
                    }
                }

                // se siamo qui, la posizione è buona: distanziata e con muro dietro
                usedLocalXs.Add(x);

                var prefab = appearanceBannerPrefabs[UnityEngine.Random.Range(0, appearanceBannerPrefabs.Length)];
                Instantiate(prefab, world, Quaternion.identity, roomTransform);

                placed++;
            }
        }


        /// Content Density: aggiunge props ambientali nella stanza, scegliendo
        /// casualmente il numero di elementi (5..propsPerContentDensity) e
        /// posizionandoli all'interno del CameraTrigger, se presente.
        private void ApplyContentDensity(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (contentDensityPrefabs == null || contentDensityPrefabs.Length == 0)
                return;

            // Numero di props casuale tra 5 e propsPerContentDensity (incluso)
            int maxProps = Mathf.Max(5, propsPerContentDensity);
            int totalProps = UnityEngine.Random.Range(5, maxProps + 1);


            if (TryGetCameraBox(roomTransform, out var box))
            {
                Vector2 halfSize = box.size * 0.5f;
                Vector2 offset = box.offset;

                float minX = -halfSize.x + spawnMarginFromBounds + offset.x;
                float maxX = halfSize.x - spawnMarginFromBounds + offset.x;
                float minY = -halfSize.y + spawnMarginFromBounds + offset.y;
                float maxY = halfSize.y - spawnMarginFromBounds + offset.y;

                for (int i = 0; i < totalProps; i++)
                {
                    var prefab = contentDensityPrefabs[UnityEngine.Random.Range(0, contentDensityPrefabs.Length)];

                    float localX = UnityEngine.Random.Range(minX, maxX);
                    float localY = UnityEngine.Random.Range(minY, maxY);

                    Vector3 localPoint = new Vector3(localX, localY, 0f);
                    Vector3 worldPoint = box.transform.TransformPoint(localPoint);

                    Instantiate(prefab, worldPoint, Quaternion.identity, roomTransform);
                }
            }

        }

        /// <summary>
        /// Occlusion Audio: rende la stanza un po' più buia rimuovendo alcune luci di base
        /// e istanzia un prefab audio "inquietante" appena fuori dal bordo alto della stanza.
        /// </summary>
        private void ApplyOcclusionAudio(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // 1) Rende la stanza leggermente più buia spegnendo una parte delle luci 2D
            ReduceLightsForOcclusion(roomTransform);

            // 2) Istanzia un audio "fuori" dalla stanza, se il prefab è assegnato
            if (audioOcclusionPrefab == null)
                return;

            // Se c'è un CameraTrigger, usiamo il suo BoxCollider2D per mettere l'audio appena oltre il bordo
            if (TryGetCameraBox(roomTransform, out var box))
            {
                Vector2 halfSize = box.size * 0.5f;
                Vector2 offset = box.offset;

                // punto casuale lungo il bordo superiore
                float minX = -halfSize.x + spawnMarginFromBounds + offset.x;
                float maxX = halfSize.x - spawnMarginFromBounds + offset.x;
                if (minX > maxX) (minX, maxX) = (maxX, minX);

                float xLocal = UnityEngine.Random.Range(minX, maxX);
                float yLocal = halfSize.y + occlusionAudioOutsideOffset + offset.y;

                Vector3 localPos = new Vector3(xLocal, yLocal, 0f);
                Vector3 worldPos = box.transform.TransformPoint(localPos);

                Instantiate(audioOcclusionPrefab, worldPos, Quaternion.identity, roomTransform);
            }
            else
            {
                // Fallback: sopra il centro stanza
                Vector3 center = GetRoomCenter(roomTransform);
                Vector3 pos = center + new Vector3(0f, 1.5f + occlusionAudioOutsideOffset, 0f);

                Instantiate(audioOcclusionPrefab, pos, Quaternion.identity, roomTransform);
            }
        }

        /// <summary>
        /// Spegne (disabilita) una parte delle luci 2D della stanza per rendere
        /// l'ambiente più buio quando viene applicato OcclusionAudio.
        /// </summary>
        private void ReduceLightsForOcclusion(Transform roomTransform)
        {
            // Recupera tutte le Light2D nella gerarchia della stanza
            var lights = roomTransform.GetComponentsInChildren<Light2D>();
            if (lights == null || lights.Length == 0)
                return;

            int total = lights.Length;

            // Calcola quante luci spegnere in base al ratio, ma tenendo almeno un certo numero di luci accese
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
                    // invece di distruggere la luce, la disabilitiamo per non rompere eventuali riferimenti
                    l.enabled = false;
                }
            }
        }

        /// <summary>
        /// Competence Gate: stanza che include un piccolo conflict + un nemico speciale
        /// (competenceGatePrefab) che funge da "test di competenza".
        /// Se la stanza ha già un Conflict tra i pattern, non richiamiamo ApplyConflict
        /// per evitare un raddoppio dei nemici.
        /// </summary>
        private void ApplyCompetenceGate(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // 1) Se non c'è un prefab speciale, non ha senso applicare il pattern
            if (competenceGatePrefab == null)
                return;

            // 2) Se la stanza NON ha già un Conflict tra i pattern, creiamo un normale conflict
            var patterns = metadata.AppliedPatterns;
            if (!patterns.Contains(AppraisalPatternType.Conflict))
            {
                ApplyConflict(metadata, roomTransform);
            }

            // 3) Spawn del nemico speciale di competenza, usando la stessa logica di spawn dei nemici
            Vector3 spawnPos;

            if (TryGetCameraBox(roomTransform, out var box)
                && TryFindFreeEnemySpotInCameraBox(box, out spawnPos))
            {
                // posizione valida trovata dentro il CameraTrigger
            }
            else
            {
                // fallback: centro stanza
                spawnPos = GetRoomCenter(roomTransform);
            }

            Instantiate(competenceGatePrefab, spawnPos, Quaternion.identity, roomTransform);
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
        /// Crea le luci "di base" nella stanza, in funzione dell'emozione del livello.
        /// Usa il CameraTrigger come area di spawn, con distribuzione stratificata
        /// per evitare grumi.
        /// </summary>
        private void ApplyBaseLighting(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // 1) Scegliamo prefab e range in base all'emozione del livello
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

            // 2) Se abbiamo un CameraTrigger, usiamo il BoxCollider2D come area
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

                // 2.1) Costruiamo una griglia 'quasi quadrata' in base al numero di luci
                int cols = Mathf.CeilToInt(Mathf.Sqrt(lightsToSpawn));
                int rows = Mathf.CeilToInt((float)lightsToSpawn / cols);

                float cellWidth = width / cols;
                float cellHeight = height / rows;

                // 2.2) Generiamo una lista di celle candidate con una posizione jitterata dentro
                List<Vector2> candidateLocalPositions = new List<Vector2>();

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float cellMinX = minX + c * cellWidth;
                        float cellMinY = minY + r * cellHeight;

                        // jitter interno alla cella (non mettiamo la luce esattamente al centro)
                        float x = UnityEngine.Random.Range(cellMinX + cellWidth * 0.2f,
                                               cellMinX + cellWidth * 0.8f);
                        float y = UnityEngine.Random.Range(cellMinY + cellHeight * 0.2f,
                                               cellMinY + cellHeight * 0.8f);

                        candidateLocalPositions.Add(new Vector2(x, y));
                    }
                }

                // 2.3) Peschiamo a caso tra le celle candidate finché abbiamo piazzato tutte le luci
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
                // 3) Fallback: intorno al centro stanza, un po' sparpagliate
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

            foreach (var room in rooms)
            {
                if (room.IsOnCriticalPath && !room.HasNextCritical)
                {
                    Transform roomTransform = room.transform;

                    // Usa lo stesso centro che stai già usando per gli altri pattern
                    Vector3 center = GetRoomCenter(roomTransform);

                    // Offset configurabile da Inspector
                    Vector3 spawnPos = center + endLevelStairsOffset;

                    Instantiate(endLevelStairsPrefab, spawnPos, Quaternion.identity, roomTransform);
                }
            }
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
                float localX = UnityEngine.Random.Range(minX, maxX) + offset.x;
                float localY = UnityEngine.Random.Range(minY, maxY) + offset.y;

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

        private GameObject ChooseEnemyPrefab(EmotionRoomMetadata metadata)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                return null;

            int idx = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            return enemyPrefabs[idx];
        }

        #endregion
    }
}