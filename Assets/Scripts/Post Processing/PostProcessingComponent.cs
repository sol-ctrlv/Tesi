// Orchestratore del post-processing emozionale (EmotionPCG)
//
// Obiettivo del file
//  - Tradurre il dungeon generato da Edgar (RoomInstances) in un grafo logico di RoomNode.
//  - Allocare AppraisalPattern alle stanze per avvicinare il profilo medio del livello al target emotivo (TargetEmotion).
//  - Scrivere i risultati su EmotionRoomMetadata e delegare lo spawn reale dei contenuti a EmotionPatternApplier.
//
// Note di design
//  - L'ottimizzazione è greedy: ad ogni iterazione sceglie la singola coppia (stanza, pattern) che migliora di più la distanza dal target.
//  - Se esiste un critical path, la metrica si concentra sulla sua media (fallback: media di tutte le stanze).
//  - L'appartenenza a critical/optional/pattern-room è determinata da convenzioni di naming (fragile ma pratica in prototipazione).

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Edgar.Unity;

namespace EmotionPCG
{
    /// <summary>
    /// Post-processing principale: assegna appraisal pattern alle stanze
    /// per avvicinare il profilo medio del livello al target emotivo scelto.
    /// Per ora si limita a:
    /// - creare RoomNode "logici"
    /// - scegliere pattern in modo greedy
    /// - scrivere i risultati su EmotionRoomMetadata per ogni stanza
    /// </summary>
    public class PostProcessingComponent : DungeonGeneratorPostProcessingComponentGrid2D
    {
        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            // 1) Costruzione dei RoomNode (uno per ogni room instance)
            var roomNodes = BuildRoomNodes(level);

            if (roomNodes.Count == 0)
            {
                Debug.LogWarning("[EmotionPCG] No rooms found in level – skipping emotion post-processing.");
                return;
            }

            // 2) Conteggio stanze dove si applicano i pattern
            int patternRoomCount = 0;
            foreach (var node in roomNodes)
            {
                if (IsPatternRoom(node))
                    patternRoomCount++;
            }

            // Se per qualche motivo non ne troviamo nessuna, usiamo roomNodes.Count per non rompere tutto
            if (patternRoomCount == 0)
                patternRoomCount = roomNodes.Count;

            // 3) Recupero target e pesi per l’emozione scelta
            var emotionTarget = GetEmotionTarget(TargetEmotion);
            var weights = EmotionWeights.Get(TargetEmotion);

            // 4) Ottimizzazione greedy dei pattern
            var patternBudget = CreatePatternBudget(TargetEmotion, patternRoomCount);
            RunGreedyOptimization(roomNodes, emotionTarget.Center, weights, patternBudget);
            FillOptional(roomNodes, patternBudget);

            // 5) Scrittura dei risultati sulle room template instance di Edgar
            ApplyMetadataToUnityRooms(level);

            var applier = FindObjectOfType<EmotionPatternApplier>();
            if (applier != null)
            {
                applier.ApplyAllPatternsInScene();
            }
            else
            {
                Debug.LogWarning("[EmotionPCG] No EmotionPatternApplier found in scene – patterns will not spawn any content.");
            }

#if UNITY_EDITOR
            // 6) Log di debug (facile da citare in tesi)
            LogSummaryStats(roomNodes, emotionTarget, weights);
#endif
        }

        [Header("Emozione target del livello")]
        public EmotionType TargetEmotion = EmotionType.Wonder;

        [Header("Impostazioni algoritmo")]
        [SerializeField] public int MaxPatternsPerRoom = 3;
        [SerializeField] private int referenceMaxPatternsPerRoom = 2;

        [Tooltip("Step massimi di ottimizzazione")]
        public int MaxIterations = 50;

        private readonly Dictionary<GameObject, RoomNode> _roomNodeByTemplate = new Dictionary<GameObject, RoomNode>();

        #region Room graph helpers

        /// <summary>
        /// Costruisce una lista di RoomNode a partire dalle RoomInstances generate da Edgar.
        /// Ogni nodo include: ID univoco, posizione nel mondo e (se applicabile) ordine sul critical path.
        /// Popola inoltre una mappa GameObject-&gt;RoomNode per collegare la stanza runtime al nodo logico.
        /// </summary>
        /// <remarks>
        /// L'appartenenza al critical path è stimata via convenzioni di naming (vedi IsCriticalByName()).
        /// </remarks>
        private List<RoomNode> BuildRoomNodes(DungeonGeneratorLevelGrid2D level)
        {
            var nodes = new List<RoomNode>();
            _roomNodeByTemplate.Clear();

            int index = 0;
            int criticalOrderCounter = 0;

            foreach (var roomInstance in level.RoomInstances)
            {
                // Prova a recuperare un nome "semantico" dal grafo di Edgar
                string sourceName = null;

                if (roomInstance.Room != null)
                {
                    sourceName = roomInstance.Room.GetDisplayName();
                }

                // Fallback sul nome del prefab della RoomTemplateInstance
                if (string.IsNullOrEmpty(sourceName) && roomInstance.RoomTemplateInstance != null)
                {
                    sourceName = roomInstance.RoomTemplateInstance.name;
                }

                bool isCritical = IsCriticalByName(sourceName);

                var nodeId = $"{sourceName}_{index}";
                var node = new RoomNode(nodeId)
                {
                    IsOnCriticalPath = isCritical,
                    Appraisal = AppraisalProfile.Neutral()
                };

                // Salviamo la posizione nel mondo della stanza
                if (roomInstance.RoomTemplateInstance != null)
                {
                    node.WorldPosition = roomInstance.RoomTemplateInstance.transform.position;
                }

                if (isCritical)
                {
                    node.CriticalOrder = criticalOrderCounter;
                    criticalOrderCounter++;
                }

                nodes.Add(node);

                // Usiamo il GameObject associato alla RoomTemplateInstance come chiave stabile.
                if (roomInstance.RoomTemplateInstance != null)
                {
                    var go = roomInstance.RoomTemplateInstance.gameObject;
                    if (!_roomNodeByTemplate.ContainsKey(go))
                    {
                        _roomNodeByTemplate.Add(go, node);
                    }
                }

                index++;
            }

            // Calcolo della direzione verso la prossima stanza sul critical path
            var criticalNodes = new List<RoomNode>();
            foreach (var node in nodes)
            {
                if (node.IsOnCriticalPath && node.CriticalOrder >= 0)
                {
                    criticalNodes.Add(node);
                }
            }

            // Ordiniamo per ordine lungo il critical path
            criticalNodes.Sort((a, b) => a.CriticalOrder.CompareTo(b.CriticalOrder));

            for (int i = 0; i < criticalNodes.Count - 1; i++)
            {
                var current = criticalNodes[i];
                var next = criticalNodes[i + 1];

                var dir = next.WorldPosition - current.WorldPosition;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    current.HasNextCritical = true;
                    current.NextCriticalDirection = dir;
                }
            }

            return nodes;
        }

        /// <summary>
        /// Determina se una stanza deve essere trattata come parte del critical path in base al nome.
        /// </summary>
        private bool IsCriticalByName(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                return false;

            // Convenzione: tutte le stanze il cui nome inizia con
            // "Room" o "End" sono considerate parte del critical path.
            if (roomName.StartsWith("Room", StringComparison.OrdinalIgnoreCase)) return true;
            if (roomName.StartsWith("End", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        /// <summary>
        /// Restituisce l'EmotionTarget (target appraisal + pesi) corrispondente all'emozione selezionata.
        /// </summary>
        private static EmotionTarget GetEmotionTarget(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Wonder: return EmotionTargets.Wonder;
                case EmotionType.Fear: return EmotionTargets.Fear;
                case EmotionType.Relaxation: return EmotionTargets.Relaxation;
                default: return EmotionTargets.Wonder;
            }
        }
        #endregion

        #region Optimization
        /// <summary>
        /// Costruisce il budget iniziale ("desiderato") di pattern da allocare nel livello,
        /// in funzione dell'emozione target e del numero di stanze disponibili.
        /// </summary>
        /// <remarks>
        /// I numeri sono euristici (tuning) e vengono poi limitati alla capienza (roomCount * MaxPatternsPerRoom).
        /// </remarks>
        private Dictionary<AppraisalPatternType, int> CreatePatternBudget(EmotionType emotion, int roomCount)
        {
            // Heuristic budgets ispirati alle linee guida della sezione 6
            // del documento "Metriche-emozionali-gioco".
            // Mappa: pattern -> numero di occorrenze da distribuire nel livello.
            var budget = new Dictionary<AppraisalPatternType, int>();

            // Helper locale: evita di inserire chiavi con conteggio nullo/negativo.
            void Set(AppraisalPatternType pattern, int count)
            {
                if (count > 0)
                    budget[pattern] = count;
            }

            // 1) Budget "desiderato" per emozione
            switch (emotion)
            {
                case EmotionType.Wonder:
                    Set(AppraisalPatternType.Centering, 2);
                    Set(AppraisalPatternType.Symmetry, 2);
                    Set(AppraisalPatternType.AppearanceOfObjects, 2);
                    Set(AppraisalPatternType.PointingOut, 2);
                    Set(AppraisalPatternType.SafeHaven, 1);
                    Set(AppraisalPatternType.Rewards, 1);
                    Set(AppraisalPatternType.ClearSignposting, 1);
                    Set(AppraisalPatternType.Conflict, 2);
                    break;

                case EmotionType.Fear:
                    Set(AppraisalPatternType.Conflict, 3);
                    Set(AppraisalPatternType.ContentDensity, 3);
                    Set(AppraisalPatternType.OcclusionAudio, 2);
                    Set(AppraisalPatternType.ClearSignposting, 2);
                    Set(AppraisalPatternType.Centering, 1);
                    Set(AppraisalPatternType.Symmetry, 1);
                    Set(AppraisalPatternType.SafeHaven, 1);
                    Set(AppraisalPatternType.Rewards, 1);
                    Set(AppraisalPatternType.AppearanceOfObjects, 1);
                    Set(AppraisalPatternType.CompetenceGate, 1);
                    break;

                case EmotionType.Relaxation:
                    Set(AppraisalPatternType.Rewards, 3);
                    Set(AppraisalPatternType.CompetenceGate, 3);
                    Set(AppraisalPatternType.ClearSignposting, 2);
                    Set(AppraisalPatternType.SafeHaven, 2);
                    Set(AppraisalPatternType.PointingOut, 2);
                    Set(AppraisalPatternType.Centering, 1);
                    Set(AppraisalPatternType.Symmetry, 1);
                    Set(AppraisalPatternType.AppearanceOfObjects, 1);
                    Set(AppraisalPatternType.Conflict, 1);
                    break;
            }

            // I numeri sopra sono tarati assumendo un certo MaxPatternsPerRoom "di riferimento".
            // Se cambiamo quel parametro (es. da 2 a 3), riscalo proporzionalmente i conteggi.
            if (referenceMaxPatternsPerRoom > 0 &&
                MaxPatternsPerRoom > 0 &&
                MaxPatternsPerRoom != referenceMaxPatternsPerRoom)
            {
                float factor = (float)MaxPatternsPerRoom / referenceMaxPatternsPerRoom;

                // Moltiplichiamo tutti i budget per questo fattore e arrotondiamo a interi (serve un conteggio discreto).
                var keys = new List<AppraisalPatternType>(budget.Keys);
                foreach (var k in keys)
                {
                    int scaled = Mathf.RoundToInt(budget[k] * factor);
                    budget[k] = Mathf.Max(0, scaled);
                }
            }

            // 2) Capienza massima teorica: stanze × pattern per stanza
            // Capienza massima: ogni stanza eleggibile può ricevere al massimo MaxPatternsPerRoom pattern.
            int maxUsable = roomCount * MaxPatternsPerRoom;
            if (maxUsable <= 0 || budget.Count == 0)
                return budget;

            int totalDesired = 0;
            foreach (var kvp in budget)
                totalDesired += kvp.Value;

            // Se il budget desiderato sta già sotto il tetto, non tocchiamo nulla
            if (totalDesired <= maxUsable)
                return budget;

            return RescaleBudget(budget, maxUsable, emotion);
        }


        /// <summary>
        /// Riscalatura un budget di pattern per rispettare un tetto massimo di allocazioni (maxUsable).
        /// </summary>
        /// <remarks>
        /// Implementa una riscalatura proporzionale con arrotondamento verso il basso e redistribuzione
        /// dei resti (metodo del "largest remainder") per arrivare il più vicino possibile a maxUsable.
        /// </remarks>
        private static Dictionary<AppraisalPatternType, int> RescaleBudget(
            Dictionary<AppraisalPatternType, int> budget,
            int maxUsable,
            EmotionType emotion)
        {
            // Budget risultante dopo la riscalatura (conteggi interi).
            var scaledBudget = new Dictionary<AppraisalPatternType, int>(budget.Count);
            // Resti frazionari usati per la redistribuzione finale.
            var patternRemainders = new List<KeyValuePair<AppraisalPatternType, float>>(budget.Count);

            // Fattore di riscalatura proporzionale (<= 1 quando tagliamo il budget).
            float scalingFactor = (float)maxUsable / Mathf.Max(1, SumBudget(budget));

#if UNITY_EDITOR
            Debug.Log($"[EmotionPCG] Scaling pattern budget for {emotion}: totalDesired={SumBudget(budget)}, maxUsable={maxUsable}, factor={scalingFactor:F2}");
#endif

            int totalScaledCount = 0;

            // 1) Riscalatura proporzionale: prendo la parte intera (floor) e salvo il resto frazionario.
            foreach (var patternEntry in budget)
            {
                // Conteggio riscalato "continuo" (non intero).
                float scaledExact = patternEntry.Value * scalingFactor;
                // Parte intera: garantisce che non superiamo maxUsable.
                int scaledFloorCount = Mathf.FloorToInt(scaledExact);
                // Resto: servirà per assegnare gli ultimi slot ai pattern più "vicini" al prossimo intero.
                float fractionalRemainder = scaledExact - scaledFloorCount;

                if (scaledFloorCount < 0)
                    scaledFloorCount = 0;

                scaledBudget[patternEntry.Key] = scaledFloorCount;
                totalScaledCount += scaledFloorCount;

                patternRemainders.Add(new KeyValuePair<AppraisalPatternType, float>(patternEntry.Key, fractionalRemainder));
            }

            // 2) Redistribuzione dei resti: assegniamo gli slot rimanenti ai pattern con resto maggiore.
            int remainingSlots = maxUsable - totalScaledCount;
            if (remainingSlots > 0)
            {
                // Ordina per resto decrescente (largest remainder).
                patternRemainders.Sort((a, b) => b.Value.CompareTo(a.Value));

                int remainderIndex = 0;
                while (remainingSlots > 0 && remainderIndex < patternRemainders.Count)
                {
                    var pattern = patternRemainders[remainderIndex].Key;
                    scaledBudget[pattern] = scaledBudget[pattern] + 1;
                    remainingSlots--;
                    remainderIndex++;
                }
            }

            int finalTotal = SumBudget(scaledBudget);
            Assert.IsTrue(finalTotal <= maxUsable, $"Scaled budget exceeds maxUsable: {finalTotal} > {maxUsable}");

            return scaledBudget;
        }


        /// <summary>
        /// Somma i conteggi contenuti nel budget (numero totale di pattern da allocare).
        /// </summary>
        private static int SumBudget(Dictionary<AppraisalPatternType, int> budget)
        {
            int total = 0;
            // Somma semplice dei conteggi.
            foreach (var kvp in budget)
            {
                total += kvp.Value;
            }

            return total;
        }


        /// <summary>
        /// Ottimizza in modo greedy l'assegnazione dei pattern alle stanze per ridurre la distanza
        /// tra il profilo medio del livello e il target emozionale.
        /// </summary>
        /// <remarks>
        /// La metrica guarda principalmente il critical path: le stanze opzionali vengono trattate a parte
        /// (FillOptional) e non entrano nel processo di ottimizzazione vero e proprio.
        /// </remarks>
        private void RunGreedyOptimization(
            List<RoomNode> nodes,
            AppraisalProfile targetCenter,
            AppraisalWeights weights,
            Dictionary<AppraisalPatternType, int> patternBudget)
        {
            if (nodes.Count == 0)
                return;

            // Somme cumulative usate per aggiornare la media in O(1) durante l'ottimizzazione.
            var sumCritical = new AppraisalProfile();
            var sumAll = new AppraisalProfile();
            int countCritical = 0;
            int countAll = nodes.Count;

            // Stato iniziale: sommiamo i profili correnti (prima di applicare nuovi pattern).
            for (int i = 0; i < nodes.Count; i++)
            {
                var p = nodes[i].Appraisal;

                sumAll += p;

                if (nodes[i].IsOnCriticalPath)
                {
                    sumCritical += p;
                    countCritical++;
                }
            }

            bool hasCritical = countCritical > 0;
            // Media iniziale (critical path se disponibile, altrimenti tutte le stanze).
            var currentAverage = hasCritical ? sumCritical / countCritical : sumAll / Mathf.Max(1, countAll);
            // Distanza iniziale dal target: baseline per misurare i miglioramenti.
            var currentDistance = AppraisalMath.WeightedSquaredDistance(currentAverage, targetCenter, weights);
            // Track degli indici critici dove abbiamo già piazzato SafeHaven (per imporre anti-adiacenza).
            var lastSafeHavenIndex = new HashSet<int>();

            // Loop greedy: ad ogni iterazione scegliamo la singola mossa che riduce di più la distanza.
            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                // bestImprovement parte da 0: accettiamo solo mosse che migliorano davvero (improvement > 0).
                float bestImprovement = 0f;
                float bestNewDistance = currentDistance;
                int bestRoomIndex = -1;
                AppraisalPatternType bestPattern = default;
                AppraisalProfile bestRoomProfile = default;
                AppraisalProfile bestAvgProfile = default;

                for (int roomIndex = 0; roomIndex < nodes.Count; roomIndex++)
                {
                    var node = nodes[roomIndex];

                    // Skip stanze non eleggibili (es. start o stanze tecniche).
                    if (!IsPatternRoom(node))
                        continue;

                    // Limite massimo di pattern applicabili a questa stanza.
                    // (vincolo di capienza, per evitare accumuli eccessivi di modifiche)
                    // Limite di pattern per stanza
                    if (node.AppliedPatterns.Count >= MaxPatternsPerRoom)
                        continue;

                    foreach (var patternEntry in patternBudget)
                    {
                        var pattern = patternEntry.Key;
                        int remaining = patternEntry.Value;

                        // Budget esaurito per questo pattern.
                        if (remaining <= 0)
                            continue;

                        if (pattern == AppraisalPatternType.SafeHaven && node.IsOnCriticalPath
                        && node.CriticalOrder >= 0)
                        {
                            int order = node.CriticalOrder;
                            if (lastSafeHavenIndex.Contains(order - 1) || lastSafeHavenIndex.Contains(order + 1))
                            {
                                continue;
                            }
                        }

                        // Evita duplicati: lo stesso pattern non può essere applicato due volte alla stessa stanza.
                        if (node.AppliedPatterns.Contains(pattern))
                            continue;

                        // Regole di design per stanza/pattern (vincoli hard, indipendenti dalla metrica).
                        if (!IsPatternAllowedInRoom(node, pattern))
                            continue;

                        var delta = AppraisalPatternLibrary.GetDelta(pattern);

                        // Profilo "ipotetico" della stanza con questo pattern in più (what-if evaluation).
                        var candidateProfile = node.Appraisal;
                        candidateProfile.Add(delta);

                        AppraisalProfile candidateAvg;

                        if (hasCritical)
                        {
                            // Assunzione: la percezione globale è dominata dal critical path.
                            // Quindi, se stiamo modificando una stanza NON critica, la media (critical) non cambia.
                            if (node.IsOnCriticalPath)
                            {
                                candidateAvg = ComputeAverageFromSum(sumCritical, countCritical, node.Appraisal, candidateProfile);
                            }
                            else
                            {
                                candidateAvg = currentAverage;
                            }
                        }
                        else
                        {
                            // Fallback: se non esiste un critical path, ottimizziamo sulla media di tutte le stanze.
                            candidateAvg = ComputeAverageFromSum(sumAll, countAll, node.Appraisal, candidateProfile);
                        }

                        float newDistance = AppraisalMath.WeightedSquaredDistance(candidateAvg, targetCenter, weights);
                        // Miglioramento: quanto riduciamo la distanza rispetto allo stato corrente.
                        float improvement = currentDistance - newDistance;

                        if (improvement > bestImprovement)
                        {
                            bestImprovement = improvement;
                            bestNewDistance = newDistance;
                            bestRoomIndex = roomIndex;
                            bestPattern = pattern;
                            bestRoomProfile = candidateProfile;
                            bestAvgProfile = candidateAvg;
                        }
                    }
                }

                // Se nessuna combinazione migliora la distanza, siamo in un ottimo locale (per questa euristica).
                if (bestImprovement <= 0f || bestRoomIndex < 0)
                    break;

                var bestNode = nodes[bestRoomIndex];
                var oldProfile = bestNode.Appraisal;

                // Applica la migliore mossa trovata in questa iterazione.
                bestNode.Appraisal = bestRoomProfile;
                bestNode.AppliedPatterns.Add(bestPattern);

                // Consuma una unità di budget del pattern scelto.
                patternBudget[bestPattern]--;

                if (hasCritical)
                {
                    if (bestNode.IsOnCriticalPath)
                    {
                        UpdateSum(ref sumCritical, oldProfile, bestRoomProfile);
                    }
                }
                else
                {
                    UpdateSum(ref sumAll, oldProfile, bestRoomProfile);
                }

                // SE abbiamo messo SafeHaven su un nodo critico,
                // registriamo il suo ordine critico per le iterazioni successive
                if (bestPattern == AppraisalPatternType.SafeHaven &&
                    bestNode.IsOnCriticalPath &&
                    bestNode.CriticalOrder >= 0)
                {
                    lastSafeHavenIndex.Add(bestNode.CriticalOrder);
                }

                currentAverage = bestAvgProfile;
                currentDistance = bestNewDistance;
            }
        }


        /// <summary>
        /// Calcola la nuova media dopo una singola sostituzione (oldValue → newValue) senza
        /// ricostruire la somma completa: average = (sum - old + new) / count.
        /// </summary>
        private static AppraisalProfile ComputeAverageFromSum(
            AppraisalProfile baseSum,
            int count,
            AppraisalProfile oldValue,
            AppraisalProfile newValue)
        {
            if (count <= 0)
                return AppraisalProfile.Neutral();

            // Media aggiornata: (somma attuale - vecchio + nuovo) / count per ogni dimensione.
            var result = new AppraisalProfile
            {
                Novelty = (baseSum.Novelty - oldValue.Novelty + newValue.Novelty) / count,
                Pleasantness = (baseSum.Pleasantness - oldValue.Pleasantness + newValue.Pleasantness) / count,
                GoalConduciveness = (baseSum.GoalConduciveness - oldValue.GoalConduciveness + newValue.GoalConduciveness) / count,
                Urgency = (baseSum.Urgency - oldValue.Urgency + newValue.Urgency) / count,
                Certainty = (baseSum.Certainty - oldValue.Certainty + newValue.Certainty) / count,
                NegOutcomeProb = (baseSum.NegOutcomeProb - oldValue.NegOutcomeProb + newValue.NegOutcomeProb) / count,
                Controllability = (baseSum.Controllability - oldValue.Controllability + newValue.Controllability) / count,
                Power = (baseSum.Power - oldValue.Power + newValue.Power) / count,
                Adjustability = (baseSum.Adjustability - oldValue.Adjustability + newValue.Adjustability) / count,
                agency = baseSum.agency
            };

            return result;
        }


        /// <summary>
        /// Aggiorna una somma cumulativa applicando il delta (newValue - oldValue).
        /// Usato per mantenere sumCritical/sumAll coerenti dopo una modifica greedy.
        /// </summary>
        private static void UpdateSum(ref AppraisalProfile sum, AppraisalProfile oldValue, AppraisalProfile newValue)
        {
            // Aggiorniamo dimensione per dimensione per mantenere la somma coerente con il profilo corrente.
            sum.Novelty += newValue.Novelty - oldValue.Novelty;
            sum.Pleasantness += newValue.Pleasantness - oldValue.Pleasantness;
            sum.GoalConduciveness += newValue.GoalConduciveness - oldValue.GoalConduciveness;
            sum.Urgency += newValue.Urgency - oldValue.Urgency;
            sum.Certainty += newValue.Certainty - oldValue.Certainty;
            sum.NegOutcomeProb += newValue.NegOutcomeProb - oldValue.NegOutcomeProb;
            sum.Controllability += newValue.Controllability - oldValue.Controllability;
            sum.Power += newValue.Power - oldValue.Power;
            sum.Adjustability += newValue.Adjustability - oldValue.Adjustability;
        }


        /// <summary>
        /// Determina se una stanza è eleggibile per ricevere pattern, basandosi su una convenzione di naming.
        /// </summary>
        private bool IsPatternRoom(RoomNode node)
        {
            var name = node.Id;
            if (string.IsNullOrEmpty(name))
                return false;

            // Convenzione di naming: questi prefissi identificano stanze su cui il post-processing può agire.
            if (name.StartsWith("Room", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("End", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("Deadend", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("Optional", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }


        /// <summary>
        /// Verifica se un certo pattern è ammesso in questa stanza.
        /// Qui mettiamo le regole "di design": ad esempio niente Rewards / ClearSignposting
        /// nella stanza finale del critical path.
        /// </summary>
        private bool IsPatternAllowedInRoom(RoomNode node, AppraisalPatternType pattern)
        {
            if (node.Id.StartsWith("End", StringComparison.OrdinalIgnoreCase))
            {
                // Niente ricompense né segnaletica nella stanza finale
                if (pattern == AppraisalPatternType.Rewards ||
                    pattern == AppraisalPatternType.ClearSignposting ||
                    pattern == AppraisalPatternType.SafeHaven)
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Calcola la media del profilo di appraisal del livello, privilegiando il critical path.
        /// Se non esiste alcun nodo marcato come critical, usa tutte le stanze come fallback.
        /// </summary>
        private AppraisalProfile ComputeAverageProfile(List<RoomNode> nodes)
        {
            var sum = new AppraisalProfile();
            int count = 0;

            // Media calcolata solo sulle stanze del critical path
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!node.IsOnCriticalPath)
                    continue;

                var p = node.Appraisal;

                sum += p;
                count++;
            }

            if (count > 0)
            {
                return sum / count;
            }

            // Fallback di sicurezza: se nessuna stanza è marcata come critical (es. naming cambiato),
            // usiamo tutte le stanze per evitare divisioni per zero.
            sum = new AppraisalProfile();
            count = nodes.Count;

            for (int i = 0; i < nodes.Count; i++)
            {
                var p = nodes[i].Appraisal;

                sum += p;
            }

            return count > 0 ? sum / count : AppraisalProfile.Neutral();
        }


        /// <summary>
        /// Riempie le stanze opzionali (Optional/Deadend) con pattern residui in modo non ottimizzato (random).
        /// Serve a utilizzare il budget rimanente senza alterare la logica strutturale del critical path.
        /// </summary>
        private void FillOptional(
            List<RoomNode> nodes,
            Dictionary<AppraisalPatternType, int> patternBudget)
        {
            if (patternBudget == null || patternBudget.Count == 0)
                return;

            // Stanze candidate: Optional + Deadend
            var targetRooms = new List<RoomNode>();
            foreach (var node in nodes)
            {
                if (IsOptional(node))
                    targetRooms.Add(node);
            }

            if (targetRooms.Count == 0)
                return;

            // Pattern "riempitivi": evitiamo quelli troppo strutturali/teleologici (es. signposting).
            var fillablePatterns = new List<AppraisalPatternType>();
            foreach (var kvp in patternBudget)
            {
                var p = kvp.Key;

                if (p == AppraisalPatternType.ClearSignposting)
                    continue;

                fillablePatterns.Add(p);
            }

            if (fillablePatterns.Count == 0)
                return;

            // Loop semplice: non ottimizziamo la metrica; distribuiamo finché troviamo pattern applicabili.
            // 'safety' evita loop potenzialmente infiniti se le combinazioni diventano impossibili.
            bool placedSomething = true;
            int safety = 0;

            while (placedSomething && safety < 1000)
            {
                placedSomething = false;
                safety++;

                foreach (var node in targetRooms)
                {
                    if (node.AppliedPatterns.Count >= MaxPatternsPerRoom)
                        continue;

                    // Pattern ancora disponibili, non già applicati, e ammessi in questa stanza
                    var localCandidates = new List<AppraisalPatternType>();
                    foreach (var p in fillablePatterns)
                    {
                        if (!patternBudget.TryGetValue(p, out int remaining) || remaining <= 0)
                            continue;

                        if (node.AppliedPatterns.Contains(p))
                            continue;

                        if (!IsPatternAllowedInRoom(node, p))
                            continue;

                        localCandidates.Add(p);
                    }

                    if (localCandidates.Count == 0)
                        continue;

                    // Scelta casuale tra i pattern compatibili: riduce bias deterministico sulle optional.
                    var chosen = localCandidates[UnityEngine.Random.Range(0, localCandidates.Count)];

                    // Applichiamo davvero il pattern: aggiorna lista + profilo appraisal.
                    node.AppliedPatterns.Add(chosen);
                    var delta = AppraisalPatternLibrary.GetDelta(chosen);
                    node.Appraisal.Add(delta);

                    // Consuma budget e segnala che in questo giro abbiamo piazzato almeno qualcosa.
                    patternBudget[chosen]--;
                    placedSomething = true;
                }
            }
        }


        /// <summary>
        /// Identifica le stanze opzionali (Optional/Deadend) tramite naming convention.
        /// </summary>
        private static bool IsOptional(RoomNode node)
        {
            var baseName = node.Id;
            if (string.IsNullOrEmpty(baseName))
                return false;

            // Queste due categorie vengono trattate come "non critiche" e riempite in modo più leggero.
            if (baseName.StartsWith("Optional", StringComparison.OrdinalIgnoreCase)) return true;
            if (baseName.StartsWith("Deadend", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        #endregion

        #region Apply metadata to Unity

        /// <summary>
        /// Propaga i risultati dell'ottimizzazione (Appraisal e pattern assegnati) nei GameObject Unity
        /// tramite il componente EmotionRoomMetadata, che verrà poi letto dagli applier a runtime.
        /// </summary>
        private void ApplyMetadataToUnityRooms(DungeonGeneratorLevelGrid2D level)
        {
            foreach (var roomInstance in level.RoomInstances)
            {
                if (roomInstance.RoomTemplateInstance == null)
                    continue;

                var go = roomInstance.RoomTemplateInstance.gameObject;

                // Invece di basarci sull'indice, usiamo la mappa costruita in BuildRoomNodes.
                if (!_roomNodeByTemplate.TryGetValue(go, out var node))
                    continue;

                // Recupera/crea il componente di metadata: è il "contratto" tra post-processing e runtime.
                var metadata = go.GetComponent<EmotionRoomMetadata>();
                if (metadata == null)
                {
                    metadata = go.AddComponent<EmotionRoomMetadata>();
                }

                string roomName = null;
                if (roomInstance.Room != null)
                {
                    roomName = roomInstance.Room.GetDisplayName();
                }

                if (string.IsNullOrEmpty(roomName))
                {
                    roomName = go.name;
                }

                // Copiamo nel metadata i risultati dell'ottimizzazione per questa stanza.
                metadata.RoomName = roomName;
                metadata.LevelEmotion = TargetEmotion;
                metadata.Appraisal = node.Appraisal;
                metadata.AppliedPatterns.Clear();
                metadata.AppliedPatterns.AddRange(node.AppliedPatterns);

                // Info sul critical path utili per pattern come ClearSignposting
                metadata.IsOnCriticalPath = node.IsOnCriticalPath;

                // La stanza finale non ha un "next" critico: utile per pattern basati su direzione (signposting).
                bool isEndRoom = !string.IsNullOrEmpty(roomName) && roomName.StartsWith("End", StringComparison.OrdinalIgnoreCase);

                if (isEndRoom)
                {
                    metadata.HasNextCritical = false;
                    metadata.NextCriticalDirection = Vector3.zero;
                }
                else
                {
                    metadata.HasNextCritical = node.HasNextCritical;
                    metadata.NextCriticalDirection = node.NextCriticalDirection;
                }
            }
        }

        #endregion

#if UNITY_EDITOR

        /// <summary>
        /// Logga in editor alcune statistiche finali (profilo medio e distanza dal target) utili per tuning/debug.
        /// </summary>
        private void LogSummaryStats(List<RoomNode> nodes, EmotionTarget target, AppraisalWeights weights)
        {
            // Calcolo della media (preferibilmente sul critical path) e della distanza dal target.
            var avg = ComputeAverageProfile(nodes);
            var dist = AppraisalMath.WeightedSquaredDistance(avg, target.Center, weights);

            Debug.Log(
                $"[EmotionPCG] Level emotion={TargetEmotion} | " +
                $"distance from target center = {dist:F3}\n" +
                $"Avg Novelty={avg.Novelty:F2}, Pleasantness={avg.Pleasantness:F2}, " +
                $"Conduciveness={avg.GoalConduciveness:F2}, Urgency={avg.Urgency:F2}, " +
                $"Certainty={avg.Certainty:F2}, NegOutcomeProb={avg.NegOutcomeProb:F2}, " +
                $"Control={avg.Controllability:F2}, Power={avg.Power:F2}, Adjust={avg.Adjustability:F2}");
        }
#endif
    }
}
