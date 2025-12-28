using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmotionPCG
{
    /// <summary>
    /// Applica in scena i pattern emotivi assegnati alle stanze tramite <see cref="EmotionRoomMetadata"/>.
    /// </summary>
    /// <remarks>
    /// Questo componente è pensato per essere eseguito <b>dopo</b> la generazione del layout (Edgar) e dopo il post-processing
    /// che decide quali pattern assegnare alle stanze.
    ///
    /// Assunzioni/convenzioni usate da questo script:
    /// (1) Solo alcune stanze vengono processate, identificate via naming ("room*", "optional*", "end*", "deadend*").
    /// (2) Ogni stanza può esporre marker opzionali come child: "RoomCenter", "PointingOutTarget" e un trigger area (es. "CameraTrigger")
    ///     contenente un <see cref="BoxCollider2D"/> che definisce l'area sicura per spawnare oggetti/nemici.
    /// (3) Molte scelte sono stocastiche (Random): a parità di layout, l'istanziamento può variare tra run se non viene fissato un seed.
    /// </remarks>
    public class EmotionPatternApplier : MonoBehaviour
    {
        [Header("Conflict")]
        [SerializeField] private GameObject[] enemyPrefabs; // prefabs dei nemici possibili
        [SerializeField] private int baseEnemiesPerConflict = 2; // numero base di nemici per conflitto
        [SerializeField] private int extraEnemiesForFear = 1; // nemici aggiuntivi in caso di paura
        [SerializeField] private float enemySpawnRadius = 2.2f; // (non usato attualmente) raggio di spawn
        // Nota: questo valore era previsto per uno spawn radiale attorno al centro.
        // Al momento lo spawn avviene tramite griglia di spot liberi nella camera box (più stabile).
        [SerializeField] private float enemyCollisionRadius = 0.5f; // raggio usato per check OverlapCircle per posizioni libere
        [SerializeField] private LayerMask enemyBlockingLayers; // layer che bloccano lo spawn dei nemici

        [Header("Spawn area")]
        [SerializeField] private string cameraTriggerName = "CameraTrigger"; // nome del child che contiene il BoxCollider2D di area
        [SerializeField] private float spawnMarginFromBounds = 0.5f; // margine interno dall'area in cui spawnare oggetti

        [Header("Safe Haven")]
        [SerializeField] private GameObject safeHealPrefab; // prefab per il punto di cura in una safe haven
        [SerializeField] private GameObject safeStatuePrefab; // prefab della statua di safe haven

        [Header("Clear Signposting")]
        [SerializeField] private GameObject signpostPrefab; // prefab per il cartello di direzione
        [SerializeField] private float signpostDistanceFromCenter = 8f; // distanza dal centro stanza per posizionare il cartello
        [SerializeField] private bool arrowUsesUpAsForward = true; // se true usa up come forward locale per orientare la freccia

        [Header("Rewards")]
        [SerializeField] private GameObject rewardChestPrefab; // prefab del forziere ricompensa
        [SerializeField] private float rewardDistanceFromCenter = 1.5f; // distanza dal centro per posizionare il forziere

        [Header("Centering")]
        [SerializeField] private GameObject centeringPrefab; // prefab per indicare il centro quando non ci sono POI importanti

        [Header("Pointing Out")]
        [SerializeField] private GameObject pointingOutLightPrefab; // luce/usato per evidenziare un target esterno
        [SerializeField] private float pointingOutOffsetY = 0.5f; // offset verticale per il pointing out quando non c'è target

        [Header("Symmetry")]
        [SerializeField] private GameObject[] symmetryPropPrefabs; // props da posizionare simmetricamente
        [SerializeField] private float symmetryOffsetFromCenter = 2f; // offset rispetto al centro per la simmetria

        [Header("Appearance")]
        [SerializeField] private GameObject appearanceStatuePrefab; // prefab statue decorative
        [SerializeField] private GameObject[] appearanceBannerPrefabs; // prefab banner decorativi
        [SerializeField] private float appearanceCornerMargin = 1f; // margine dagli angoli per statue
        [SerializeField] private float appearanceBannerMargin = 0.5f; // margine per i banner
        [SerializeField] private float bannerWallOffsetY = 0.5f; // offset verticale per il controllo muro sopra il banner
        [SerializeField] private float minBannerSpacing = 1f; // spacing minimo tra banner
        [SerializeField] private float bannerWallCheckRadius = 0.2f; // raggio per OverlapCircle di verifica muro
        [SerializeField] private LayerMask wallLayerMask; // layer da considerare come muro per i banner
        [SerializeField] private int minBanners = 2; // numero minimo di banner da spawnare
        [SerializeField] private int maxBanners = 5; // numero massimo di banner da spawnare

        [Header("Content Density")]
        [SerializeField] private GameObject[] contentDensityPrefabs; // prefabs per riempire contenuto
        [SerializeField] private int propsPerContentDensity = 4; // numero di props da posizionare
        [SerializeField] private float contentMinSpacing = 0.9f; // distanza minima tra props
        [SerializeField] private int contentMaxAttemptsPerProp = 10; // tentativi per trovare una posizione valida per prop

        [Header("Occlusion")]
        [SerializeField] private AudioClip audioOcclusionPrefab; // clip o prefab audio per occlusion (usato da RoomGhostAudio)
        // Nota: il nome contiene 'Prefab' per eredità storica, ma qui il tipo è AudioClip (non GameObject).
        [SerializeField, Range(0f, 1f)] private float occlusionLightRemovalRatio = 0.4f; // percentuale massima di luci da disabilitare
        [SerializeField] private int occlusionMinLightsToKeep = 1; // minimo luci da mantenere
        [SerializeField] private float occlusionMinDelay = 2f; // ritardo minimo per suoni "ghost"
        [SerializeField] private float occlusionMaxDelay = 5f; // ritardo massimo per suoni "ghost"
        [SerializeField] private float occlusionVolume = 0.5f; // volume suoni occlusion

        [Header("Competence Gate")]
        [SerializeField] private GameObject competenceGatePrefab; // prefab per il "portale" di competenza

        [Header("Base Lighting")]
        [SerializeField] private GameObject wonderLightPrefab; // prefab luce per emozione Wonder
        [SerializeField] private GameObject fearLightPrefab; // prefab luce per emozione Fear
        [SerializeField] private GameObject relaxLightPrefab; // prefab luce per Relaxation

        [SerializeField] private int wonderMinLights = 2; // luci minime wonder
        [SerializeField] private int wonderMaxLights = 3; // luci massime wonder

        [SerializeField] private int fearMinLights = 1; // luci minime fear
        [SerializeField] private int fearMaxLights = 2; // luci massime fear

        [SerializeField] private int relaxMinLights = 3; // luci minime relax
        [SerializeField] private int relaxMaxLights = 4; // luci massime relax

        [Header("Level end")]
        [SerializeField] private GameObject endLevelStairsPrefab; // prefab scale fine livello
        [SerializeField] private Vector3 endLevelStairsOffset = Vector3.zero; // offset per posizionare le scale rispetto al centro stanza

        /// <summary>
        /// Esegue l'applicazione dei pattern su tutte le stanze presenti in scena.
        /// Legge i metadati (pattern assegnati + emozione) e istanzia gli elementi corrispondenti.
        /// </summary>
        public void ApplyAllPatternsInScene()
        {
            var rooms = FindObjectsOfType<EmotionRoomMetadata>();
            // Nota: FindObjectsOfType è relativamente costoso, ma qui viene usato una sola volta come passo di post-processing.
            // In runtime (Update) conviene evitare chiamate ripetute a FindObjectsOfType.

            // Itera sulle stanze e applica solo quelle compatibili con la convenzione di naming.
            foreach (var room in rooms)
            {
                // Evita di toccare oggetti/stanze non appartenenti al layout generato (es. prefab di test, camere tecniche, ecc.).
                if (!ShouldApplyPatternsToRoom(room))
                    continue;

                ApplyPatternsToRoom(room);
            }

            // Ultimo step: posiziona un marker/prefab di fine livello nella stanza end.
            PlaceEndLevelStairs();
        }

        /// <summary>
        /// Filtra le stanze che devono essere processate dal post-processing.
        /// </summary>
        /// <param name="room">Stanza candidata (con EmotionRoomMetadata).</param>
        /// <returns>True se la stanza rispetta le convenzioni di naming ed è considerata parte del dungeon da processare.</returns>
        private bool ShouldApplyPatternsToRoom(EmotionRoomMetadata room)
        {
            if (room == null)
                return false;

            string name = room.gameObject.name;
            // Per coerenza usiamo il nome del GameObject (come generato da Edgar).
            // Se in futuro vuoi usare un ID robusto, conviene spostare questa logica nei metadata.
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.StartsWith("room", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("optional", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("end", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("deadend", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        /// <summary>
        /// Applica tutti i pattern presenti nei metadati ad una singola stanza.
        /// </summary>
        /// <param name="metadata">Metadati della stanza, con lista di pattern e informazioni di navigazione (es. direzione del prossimo nodo critico).</param>
        /// <remarks>
        /// La luce base viene applicata sempre.
        /// Alcuni pattern vengono disabilitati nelle SafeHaven per evitare conflitti tra intenti progettuali (es. niente nemici).
        /// </remarks>
        private void ApplyPatternsToRoom(EmotionRoomMetadata metadata)
        {
            if (metadata == null)
                return;

            var roomTransform = metadata.transform;
            List<AppraisalPatternType> patterns = metadata.AppliedPatterns;
            // Lista dei pattern assegnati dal post-processing (ordine non garantito).
            // Nota: alcuni pattern sono incompatibili tra loro; la logica sotto gestisce priorità/minimi vincoli.

            Vector3 roomCenter = GetRoomCenter(roomTransform);
            // Luce base: applicata sempre per dare una "firma" emotiva di fondo alla stanza.
            ApplyBaseLighting(metadata, roomTransform);

            // Se la stanza è una SafeHaven, evitiamo pattern che introducono stress/conflitto o rumore (nemici, occlusione, ecc.).
            bool hasSafeHaven = patterns.Contains(AppraisalPatternType.SafeHaven);

            // Applichiamo pattern uno per uno: ogni case istanzia elementi e/o modifica l'ambiente della stanza.
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

        /// <summary>
        /// Applica il pattern Conflict: genera nemici in posizioni libere all'interno della camera box.
        /// </summary>
        /// <param name="metadata">Metadati della stanza (usati per modulare la quantità di nemici in base all'emozione).</param>
        /// <param name="roomTransform">Transform della stanza (parent per mantenere la gerarchia ordinata).</param>
        private void ApplyConflict(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            int enemies = baseEnemiesPerConflict;
            if (metadata.LevelEmotion == EmotionType.Fear)
                enemies += extraEnemiesForFear;

            enemies = Mathf.Max(1, enemies);

            // Per lo spawn nemici richiediamo una camera box: evita spawn dentro muri/porte e rende il comportamento più stabile.
            if (!TryGetCameraBox(roomTransform, out var cameraBox))
            {
                Debug.LogWarning(
                    $"[EmotionPatternApplier] Nessun camera box trovato in '{roomTransform.name}'. Nemici NON spawnati (fallback rimosso).");
                return;
            }

            // Spawn ripetuto: ogni nemico cerca uno spot libero. Se fallisce, si passa al successivo (nessun forcing).
            for (int i = 0; i < enemies; i++)
            {
                // Cerca uno spot libero basato su OverlapCircle con layer bloccanti (muri/nemici/ostacoli).
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
        /// Applica il pattern SafeHaven: istanzia punto cura e/o statue decorative vicino al centro.
        /// </summary>
        /// <param name="metadata">Metadati stanza (attualmente non modifica la logica, ma mantiene firma uniforme).</param>
        /// <param name="roomTransform">Transform della stanza (parent degli oggetti istanziati).</param>
        /// <param name="roomCenter">Centro stanza già calcolato (marker RoomCenter se presente).</param>
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

        /// <summary>
        /// Applica il pattern Rewards: posiziona un forziere in una direzione cardinale rispetto al centro.
        /// </summary>
        /// <param name="metadata">Metadati stanza (non usati direttamente; firma uniforme).</param>
        /// <param name="roomTransform">Transform della stanza (parent).</param>
        /// <param name="roomCenter">Centro stanza.</param>
        /// <remarks>
        /// Usare direzioni cardinali riduce la probabilità di spawn vicino ai bordi rispetto a direzioni completamente casuali.
        /// </remarks>
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

        /// <summary>
        /// Applica il pattern ClearSignposting: posiziona un cartello che indica il prossimo nodo critico (se disponibile).
        /// </summary>
        /// <param name="metadata">Metadati con HasNextCritical e NextCriticalDirection.</param>
        /// <param name="roomTransform">Transform della stanza (parent).</param>
        /// <param name="roomCenter">Centro stanza.</param>
        /// <param name="isSafeHavenRoom">True se la stanza è una SafeHaven: sposta il cartello per non interferire con i POI centrali.</param>
        private void ApplyClearSignposting(
            EmotionRoomMetadata metadata,
            Transform roomTransform,
            Vector3 roomCenter,
            bool isSafeHavenRoom)
        {
            if (signpostPrefab == null)
                return;

            // Direzione di default: se non sappiamo dove andare, puntiamo verso l'alto (convenzione).
            Vector3 dir = Vector3.up;

            // Se il post-processing ha calcolato la direzione del prossimo nodo critico, usiamola per guidare il giocatore (signposting).
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

            // Nota: la rotazione dipende dall'orientamento locale della freccia nel prefab.
            // Alcuni modelli "puntano" lungo up, altri lungo right.
            Vector3 localForwardAxis = arrowUsesUpAsForward ? Vector3.up : Vector3.right;
            Quaternion mainRot = Quaternion.FromToRotation(localForwardAxis, dir);

            Instantiate(signpostPrefab, mainPos, mainRot, roomTransform);
        }

        /// <summary>
        /// Applica il pattern PointingOut: evidenzia un target (marker 'PointingOutTarget') oppure il centro stanza.
        /// </summary>
        /// <param name="roomTransform">Transform della stanza.</param>
        /// <param name="roomCenter">Centro stanza (fallback se manca il target).</param>
        private void ApplyPointingOut(
            Transform roomTransform,
            Vector3 roomCenter)
        {
            if (pointingOutLightPrefab == null)
                return;

            // Target opzionale: se presente, la luce evidenzia un elemento specifico (es. porta, oggetto, landmark).
            // Se manca, usiamo il centro stanza come fallback.
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

        /// <summary>
        /// Applica il pattern Centering: evidenzia il centro della stanza.
        /// Se esiste già un POI importante al centro, usa una luce; altrimenti istanzia un prefab di centering.
        /// </summary>
        /// <param name="metadata">Metadati stanza (per leggere i pattern presenti).</param>
        /// <param name="roomTransform">Transform della stanza.</param>
        /// <param name="roomCenter">Centro stanza.</param>
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

        /// <summary>
        /// Applica il pattern Symmetry: posiziona due props simmetrici rispetto al centro della stanza.
        /// </summary>
        /// <param name="metadata">Metadati stanza (non usati direttamente).</param>
        /// <param name="roomTransform">Transform della stanza.</param>
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

        /// <summary>
        /// Applica il pattern AppearanceOfObjects: arricchisce la stanza con elementi decorativi.
        /// Sceglie tra statue e banner (se disponibili) e gestisce fallback se manca la camera box.
        /// </summary>
        /// <param name="metadata">Metadati stanza (non usati direttamente).</param>
        /// <param name="roomTransform">Transform della stanza.</param>
        private void ApplyAppOfObjects(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            bool hasStatues = appearanceStatuePrefab != null;
            bool hasBanners = appearanceBannerPrefabs != null && appearanceBannerPrefabs.Length > 0;

            if (!hasStatues && !hasBanners)
                return;

            if (!TryGetCameraBox(roomTransform, out var box))
            {
                // Se non c'è camera box usiamo fallback: posizioniamo una statua vicino al centro (se esistente)
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

        /// <summary>
        /// Spawna statue decorative in due angoli opposti della camera box (con margine), per creare bilanciamento visivo.
        /// </summary>
        /// <param name="box">Area di spawn (camera box).</param>
        /// <param name="roomTransform">Transform della stanza (parent).</param>
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

        /// <summary>
        /// Spawna banner decorativi lungo la parete superiore della camera box.
        /// Opzionalmente verifica la presenza di un muro con Physics2D.OverlapCircle.
        /// </summary>
        /// <param name="box">Area di spawn (camera box).</param>
        /// <param name="roomTransform">Transform della stanza (parent).</param>
        /// <remarks>
        /// La verifica del muro usa wallLayerMask: se è 0, la verifica viene saltata (comportamento intenzionale).
        /// </remarks>
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
                        // se non troviamo il muro sotto il banner, salta questa posizione
                        continue;
                    }
                }

                usedLocalXs.Add(x);

                var prefab = appearanceBannerPrefabs[UnityEngine.Random.Range(0, appearanceBannerPrefabs.Length)];
                Instantiate(prefab, world, Quaternion.identity, roomTransform);

                placed++;
            }
        }

        /// <summary>
        /// Applica il pattern ContentDensity: riempie la stanza con props evitando sovrapposizioni.
        /// </summary>
        /// <param name="metadata">Metadati stanza (non usati direttamente).</param>
        /// <param name="roomTransform">Transform della stanza.</param>
        private void ApplyContentDensity(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            if (contentDensityPrefabs == null || contentDensityPrefabs.Length == 0)
                return;

            int props = Mathf.Max(1, propsPerContentDensity);

            if (!TryGetCameraBox(roomTransform, out var box))
            {
                // fallback: posiziona attorno al centro della stanza
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

        /// <summary>
        /// Utility: verifica se una posizione candidata è troppo vicina a posizioni già occupate.
        /// </summary>
        /// <param name="candidate">Posizione candidata (world space).</param>
        /// <param name="existing">Lista di posizioni già piazzate (world space).</param>
        /// <param name="minDistance">Distanza minima accettabile.</param>
        /// <returns>True se il candidato viola la distanza minima.</returns>
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

        /// <summary>
        /// Applica il pattern OcclusionAudio: riduce alcune luci e attiva audio ambientali 'ghost' nella stanza.
        /// </summary>
        /// <param name="metadata">Metadati stanza (non usati direttamente).</param>
        /// <param name="roomTransform">Transform della stanza.</param>
        private void ApplyOcclusion(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            // Step 1: rende la stanza visivamente più "chiusa" riducendo alcune luci.
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

        /// <summary>
        /// Disabilita casualmente una parte delle luci Light2D figlie della stanza per simulare penombra/occlusione.
        /// </summary>
        /// <param name="roomTransform">Transform della stanza.</param>
        private void ReduceLightsForOcclusion(Transform roomTransform)
        {
            var lights = roomTransform.GetComponentsInChildren<Light2D>();
            if (lights == null || lights.Length == 0)
                return;

            int total = lights.Length;

            // Quante luci possiamo rimuovere al massimo, in base al ratio configurabile (0..1).
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

        /// <summary>
        /// Applica il pattern CompetenceGate: garantisce un conflitto e poi posiziona un gate in una posizione libera.
        /// </summary>
        /// <param name="metadata">Metadati stanza (usati per verificare se il pattern Conflict è già presente).</param>
        /// <param name="roomTransform">Transform della stanza.</param>
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
                // spawnPos definito dal TryFindFreeEnemySpotInCameraBox
            }
            else
            {
                // fallback al centro stanza
                spawnPos = GetRoomCenter(roomTransform);
            }

            Instantiate(competenceGatePrefab, spawnPos, Quaternion.identity, roomTransform);
        }

        /// <summary>
        /// Restituisce il centro logico della stanza.
        /// </summary>
        /// <param name="roomTransform">Transform della stanza.</param>
        /// <returns>Posizione del marker 'RoomCenter' se presente, altrimenti la posizione del transform della stanza.</returns>
        private Vector3 GetRoomCenter(Transform roomTransform)
        {
            Transform centerMarker = roomTransform.Find("RoomCenter");
            if (centerMarker != null)
                return centerMarker.position;

            return roomTransform.position;
        }

        /// <summary>
        /// Istanzia un numero di luci base in base all'emozione della stanza (Wonder/Fear/Relaxation).
        /// </summary>
        /// <param name="metadata">Metadati stanza (contiene LevelEmotion).</param>
        /// <param name="roomTransform">Transform della stanza.</param>
        /// <remarks>
        /// Quando esiste una camera box, le luci vengono distribuite su una griglia di celle per evitare clustering.
        /// In assenza di box, si usa un fallback circolare attorno al centro stanza.
        /// </remarks>
        private void ApplyBaseLighting(EmotionRoomMetadata metadata, Transform roomTransform)
        {
            GameObject lightPrefab = null;
            int minLights = 0;
            int maxLights = 0;

            // Mappa emozione -> prefab luce + range quantità.
            // Nota: i range sono parametri di tuning (euristici) e possono essere calibrati sperimentalmente.
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

                case EmotionType.Relaxation:
                    lightPrefab = relaxLightPrefab;
                    minLights = relaxMinLights;
                    maxLights = relaxMaxLights;
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
                // se esiste una camera box tentiamo di distribuire le luci su una griglia casuale all'interno dell'area
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
                // fallback: spawn circolare attorno al centro stanza
                Vector3 center = GetRoomCenter(roomTransform);

                for (int i = 0; i < lightsToSpawn; i++)
                {
                    Vector2 offsetCircle = UnityEngine.Random.insideUnitCircle * 2.0f;
                    Vector3 pos = center + new Vector3(offsetCircle.x, offsetCircle.y, 0f);

                    Instantiate(lightPrefab, pos, Quaternion.identity, roomTransform);
                }
            }
        }

        /// <summary>
        /// Posiziona le scale di fine livello nella prima stanza identificata come 'end'.
        /// </summary>
        /// <remarks>
        /// La scelta della 'prima' end room dipende dall'ordine di FindObjectsOfType.
        /// Se serve un comportamento più controllato, conviene selezionare esplicitamente la stanza end via metadata.
        /// </remarks>
        public void PlaceEndLevelStairs()
        {
            if (endLevelStairsPrefab == null)
            {
                Debug.LogWarning("[EmotionPCG] End level stairs prefab non assegnato in EmotionPatternApplier.");
                return;
            }

            // Recupera tutte le stanze e cerca la prima che sia 'end' secondo la convenzione.
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

        /// <summary>
        /// Determina se la stanza è una end room tramite convenzione di naming.
        /// </summary>
        /// <param name="room">Stanza da testare.</param>
        /// <returns>True se il nome della stanza inizia con 'end' (case-insensitive).</returns>
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

        /// <summary>
        /// Recupera il BoxCollider2D che definisce l'area di spawn della stanza.
        /// </summary>
        /// <param name="roomTransform">Transform della stanza.</param>
        /// <param name="box">(out) BoxCollider2D trovato.</param>
        /// <returns>True se il child cameraTriggerName esiste ed espone un BoxCollider2D.</returns>
        /// <remarks>
        /// Codifica una convenzione strutturale: un child con nome fisso che contiene l'area.
        /// Se il prefab delle stanze cambia, aggiornare cameraTriggerName o la struttura dei child.
        /// </remarks>
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

        /// <summary>
        /// Seleziona una posizione libera per spawn (nemico o gate) dentro la camera box.
        /// </summary>
        /// <param name="box">Area di spawn.</param>
        /// <param name="position">(out) Posizione world selezionata.</param>
        /// <returns>True se esiste almeno uno spot libero. False se non sono stati trovati spot: position viene comunque impostato al centro della box.</returns>
        /// <remarks>
        /// Il valore di ritorno è importante: alcuni chiamanti ignorano il fallback (es. Conflict), altri lo usano (es. CompetenceGate).
        /// </remarks>
        private bool TryFindFreeEnemySpotInCameraBox(BoxCollider2D box, out Vector3 position)
        {
            var freeSpots = ComputeFreeEnemySpots(box);

            // Nessuno spot valido trovato: ritorniamo false.
            // Impostiamo comunque una posizione di fallback (centro della box) per chiamanti che scelgono di usarla.
            if (freeSpots == null || freeSpots.Count == 0)
            {
                // fallback: centro della box se non ci sono spot liberi
                position = box.transform.TransformPoint(box.offset);
                return false;
            }

            int idx = UnityEngine.Random.Range(0, freeSpots.Count);
            position = freeSpots[idx];
            return true;
        }

        /// <summary>
        /// Calcola una lista di posizioni libere nella camera box evitando collisioni con layer bloccanti.
        /// </summary>
        /// <param name="box">Area di spawn (camera box).</param>
        /// <returns>Lista di posizioni in world space pronte per Instantiate.</returns>
        /// <remarks>
        /// La scansione avviene su una griglia con passo derivato da enemyCollisionRadius.
        /// Nota Unity: Collider2D eredita da UnityEngine.Object, che ha conversione implicita a bool; questo permette di scrivere:
        /// bool blocked = Physics2D.OverlapCircle(...) per testare trovato/non trovato.
        /// </remarks>
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

                    // Unity permette la conversione implicita di Collider2D a bool (true se non-null).
                    // Quindi questa riga vale: "c'è un collider bloccante entro enemyCollisionRadius?"
                    bool blocked = Physics2D.OverlapCircle(worldPoint, enemyCollisionRadius, enemyBlockingLayers);
                    if (!blocked)
                    {
                        result.Add(worldPoint);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Seleziona un prefab nemico da istanziare.
        /// </summary>
        /// <param name="metadata">Metadati stanza (attualmente non influenza la scelta; utile per estensioni future).</param>
        /// <returns>Un prefab scelto casualmente, oppure null se l'array è vuoto.</returns>
        private GameObject ChooseEnemyPrefab(EmotionRoomMetadata metadata)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                return null;

            int idx = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            return enemyPrefabs[idx];
        }
    }
}
