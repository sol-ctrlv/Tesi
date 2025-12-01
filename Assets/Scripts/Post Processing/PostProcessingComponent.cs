using System;
using System.Collections.Generic;
using UnityEngine;
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

            // 2) Recupero target e pesi per l’emozione scelta
            var emotionTarget = GetEmotionTarget(TargetEmotion);
            var weights = EmotionWeights.Get(TargetEmotion);

            // 3) Ottimizzazione greedy dei pattern
            var patternBudget = CreatePatternBudget(TargetEmotion, roomNodes.Count);
            RunGreedyOptimization(roomNodes, emotionTarget.Center, weights, patternBudget);

            // 4) Scrittura dei risultati sulle room template instance di Edgar
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
            // 5) Log di debug (facile da citare in tesi)
            LogSummaryStats(roomNodes, emotionTarget, weights);
#endif
        }

        [Header("Target emotion for this level")]
        public EmotionType TargetEmotion = EmotionType.Wonder;

        [Header("Algorithm settings")]
        [Range(1, 4)]
        public int MaxPatternsPerRoom = 2;

        [Tooltip("Max optimization steps (safety guard).")]
        public int MaxIterations = 50;

        private readonly Dictionary<GameObject, RoomNode> _roomNodeByTemplate = new Dictionary<GameObject, RoomNode>();

        #region Room graph helpers
        private List<RoomNode> BuildRoomNodes(DungeonGeneratorLevelGrid2D level)
        {
            var nodes = new List<RoomNode>();
            _roomNodeByTemplate.Clear();

            int index = 0;

            foreach (var roomInstance in level.RoomInstances)
            {
                // Prova a recuperare un nome "semantico" dal grafo di Edgar
                string sourceName = null;

                if (roomInstance.Room != null)
                {
                    sourceName = roomInstance.Room.GetDisplayName();
                }

                if (string.IsNullOrEmpty(sourceName) && roomInstance.RoomTemplateInstance != null)
                {
                    sourceName = roomInstance.RoomTemplateInstance.name;
                }

                if (string.IsNullOrEmpty(sourceName))
                {
                    sourceName = $"Room_{index}";
                }

                bool isCritical = IsCriticalByName(sourceName);

                var nodeId = $"{sourceName}_{index}";
                var node = new RoomNode(nodeId)
                {
                    IsOnCriticalPath = isCritical,
                    Appraisal = AppraisalProfile.Neutral()
                };

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

            return nodes;
        }

        private bool IsCriticalByName(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                return false;

            // Convenzione: tutte le stanze il cui nome inizia con
            // "Start", "Sword", "Room" o "End" sono considerate
            // parte del critical path.
            if (roomName.StartsWith("Start", StringComparison.OrdinalIgnoreCase)) return true;
            if (roomName.StartsWith("Sword", StringComparison.OrdinalIgnoreCase)) return true;
            if (roomName.StartsWith("Room", StringComparison.OrdinalIgnoreCase)) return true;
            if (roomName.StartsWith("End", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static EmotionTarget GetEmotionTarget(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Wonder: return EmotionTargets.Wonder;
                case EmotionType.Fear: return EmotionTargets.Fear;
                case EmotionType.Joy: return EmotionTargets.Joy;
                default: return EmotionTargets.Wonder;
            }
        }
        #endregion

        #region Optimization
        private Dictionary<AppraisalPatternType, int> CreatePatternBudget(EmotionType emotion, int roomCount)
        {
            // Heuristic budgets ispirati alle linee guida della sezione 6
            // del documento "Metriche-emozionali-gioco".
            var budget = new Dictionary<AppraisalPatternType, int>();

            void Set(AppraisalPatternType pattern, int count)
            {
                if (count > 0)
                    budget[pattern] = count;
            }

            // 1) Budget "desiderato" per emozione (in termini assoluti)
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

                case EmotionType.Joy:
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

            // 2) Capienza massima teorica: stanze × pattern per stanza
            int maxUsable = roomCount * MaxPatternsPerRoom;
            if (maxUsable <= 0 || budget.Count == 0)
                return budget;

            int totalDesired = 0;
            foreach (var kvp in budget)
                totalDesired += kvp.Value;

            // Se il budget desiderato sta già sotto il tetto, non tocchiamo nulla
            if (totalDesired <= maxUsable)
                return budget;

            // 3) Rescaling proporzionale: manteniamo le proporzioni
            // fra pattern, ma riduciamo i conteggi per rispettare maxUsable.
            var scaledBudget = new Dictionary<AppraisalPatternType, int>(budget.Count);
            var patternRemainders = new List<KeyValuePair<AppraisalPatternType, float>>(budget.Count);

            // Fattore di scala: quanto devo "schiacciare" il budget desiderato
            // per farlo rientrare nella capienza massima consentita.
            float scalingFactor = (float)maxUsable / totalDesired;

#if UNITY_EDITOR
            Debug.Log($"[EmotionPCG] Scaling pattern budget for {emotion}: totalDesired={totalDesired}, maxUsable={maxUsable}, factor={scalingFactor:F2}");
#endif

            int totalScaledCount = 0;

            foreach (var patternEntry in budget)
            {
                // Valore teorico dopo la scalatura (es. 2.7, 1.3, ...)
                float scaledExact = patternEntry.Value * scalingFactor;
                // Parte intera dei pattern che assegniamo subito
                int scaledFloorCount = Mathf.FloorToInt(scaledExact);
                // Parte frazionaria che useremo per distribuire gli slot residui
                float fractionalRemainder = scaledExact - scaledFloorCount;

                if (scaledFloorCount < 0)
                    scaledFloorCount = 0;

                scaledBudget[patternEntry.Key] = scaledFloorCount;
                totalScaledCount += scaledFloorCount;

                patternRemainders.Add(new KeyValuePair<AppraisalPatternType, float>(patternEntry.Key, fractionalRemainder));
            }

            int remainingSlots = maxUsable - totalScaledCount;
            if (remainingSlots > 0)
            {
                // Ordina per remainder decrescente: chi ha "perso" di più nella floor
                // riceve per primo gli slot residui.
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

            return scaledBudget;
        }

        private void RunGreedyOptimization(
            List<RoomNode> nodes,
            AppraisalProfile targetCenter,
            AppraisalWeights weights,
            Dictionary<AppraisalPatternType, int> patternBudget)
        {
            if (nodes.Count == 0)
                return;

            var currentAverage = ComputeAverageProfile(nodes);
            var currentDistance = AppraisalMath.WeightedSquaredDistance(currentAverage, targetCenter, weights);

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                float bestImprovement = 0f;
                float bestNewDistance = currentDistance;
                int bestRoomIndex = -1;
                AppraisalPatternType bestPattern = default;
                AppraisalProfile bestRoomProfile = default;
                AppraisalProfile bestAvgProfile = default;

                for (int roomIndex = 0; roomIndex < nodes.Count; roomIndex++)
                {
                    var node = nodes[roomIndex];

                    // Limite di pattern per stanza
                    if (node.AppliedPatterns.Count >= MaxPatternsPerRoom)
                        continue;

                    foreach (var patternEntry in patternBudget)
                    {
                        var pattern = patternEntry.Key;
                        int remaining = patternEntry.Value;

                        if (remaining <= 0)
                            continue;

                        // Evita di applicare lo stesso pattern due volte alla stessa stanza
                        if (node.AppliedPatterns.Contains(pattern))
                            continue;

                        var delta = AppraisalPatternLibrary.GetDelta(pattern);

                        // Profilo "ipotetico" della stanza con questo pattern in più
                        var candidateProfile = node.Appraisal;
                        candidateProfile.Add(delta);

                        // Media ipotetica del livello con la stanza modificata
                        var candidateAvg = ComputeAverageProfileWithCandidate(nodes, roomIndex, candidateProfile);

                        float newDistance = AppraisalMath.WeightedSquaredDistance(candidateAvg, targetCenter, weights);
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

                // Se nessuna combinazione migliora la distanza, ci fermiamo
                if (bestImprovement <= 0f || bestRoomIndex < 0)
                    break;

                // Commit della scelta migliore trovata in questa iterazione
                var bestNode = nodes[bestRoomIndex];
                bestNode.Appraisal = bestRoomProfile;
                bestNode.AppliedPatterns.Add(bestPattern);

                // Aggiorna budget e distanza corrente
                patternBudget[bestPattern]--;
                currentAverage = bestAvgProfile;
                currentDistance = bestNewDistance;
            }
        }

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

                sum.Novelty += p.Novelty;
                sum.Pleasantness += p.Pleasantness;
                sum.GoalConduciveness += p.GoalConduciveness;
                sum.Urgency += p.Urgency;
                sum.Certainty += p.Certainty;
                sum.NegOutcomeProb += p.NegOutcomeProb;
                sum.Controllability += p.Controllability;
                sum.Power += p.Power;
                sum.Adjustability += p.Adjustability;

                count++;
            }

            if (count > 0)
            {
                return sum / count;
            }

            // Fallback di sicurezza: se nessuna stanza è marcata come critical,
            // usiamo tutte le stanze per evitare divisioni per zero.
            sum = new AppraisalProfile();
            count = nodes.Count;

            for (int i = 0; i < nodes.Count; i++)
            {
                var p = nodes[i].Appraisal;

                sum.Novelty += p.Novelty;
                sum.Pleasantness += p.Pleasantness;
                sum.GoalConduciveness += p.GoalConduciveness;
                sum.Urgency += p.Urgency;
                sum.Certainty += p.Certainty;
                sum.NegOutcomeProb += p.NegOutcomeProb;
                sum.Controllability += p.Controllability;
                sum.Power += p.Power;
                sum.Adjustability += p.Adjustability;
            }

            return count > 0 ? sum / count : AppraisalProfile.Neutral();
        }

        private AppraisalProfile ComputeAverageProfileWithCandidate(
            List<RoomNode> nodes,
            int candidateIndex,
            AppraisalProfile candidateProfile)
        {
            var sum = new AppraisalProfile();
            int count = 0;

            // Media calcolata solo sulle stanze del critical path
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!node.IsOnCriticalPath)
                    continue;

                var p = (i == candidateIndex) ? candidateProfile : node.Appraisal;

                sum.Novelty += p.Novelty;
                sum.Pleasantness += p.Pleasantness;
                sum.GoalConduciveness += p.GoalConduciveness;
                sum.Urgency += p.Urgency;
                sum.Certainty += p.Certainty;
                sum.NegOutcomeProb += p.NegOutcomeProb;
                sum.Controllability += p.Controllability;
                sum.Power += p.Power;
                sum.Adjustability += p.Adjustability;

                count++;
            }

            if (count > 0)
            {
                return sum / count;
            }

            // Fallback di sicurezza: se nessuna stanza è marcata come critical,
            // consideriamo tutte le stanze.
            sum = new AppraisalProfile();
            count = nodes.Count;

            for (int i = 0; i < nodes.Count; i++)
            {
                var p = (i == candidateIndex) ? candidateProfile : nodes[i].Appraisal;

                sum.Novelty += p.Novelty;
                sum.Pleasantness += p.Pleasantness;
                sum.GoalConduciveness += p.GoalConduciveness;
                sum.Urgency += p.Urgency;
                sum.Certainty += p.Certainty;
                sum.NegOutcomeProb += p.NegOutcomeProb;
                sum.Controllability += p.Controllability;
                sum.Power += p.Power;
                sum.Adjustability += p.Adjustability;
            }

            return count > 0 ? sum / count : AppraisalProfile.Neutral();
        }
        #endregion

        #region Apply metadata back to Unity
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

                var metadata = go.GetComponent<EmotionRoomMetadata>();
                if (metadata == null)
                {
                    metadata = go.AddComponent<EmotionRoomMetadata>();
                }

                metadata.LevelEmotion = TargetEmotion;
                metadata.Appraisal = node.Appraisal;
                metadata.AppliedPatterns.Clear();
                metadata.AppliedPatterns.AddRange(node.AppliedPatterns);
            }
        }
        #endregion

#if UNITY_EDITOR
        private void LogSummaryStats(List<RoomNode> nodes, EmotionTarget target, AppraisalWeights weights)
        {
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

    /// <summary>
    /// Componente che porta sul GameObject stanza le informazioni calcolate
    /// dal post-processing (profilo di appraisal + pattern applicati).
    /// Altri sistemi (spawn nemici, props, luci) possono leggerle in seguito.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmotionRoomMetadata : MonoBehaviour
    {
        public EmotionType LevelEmotion;
        public AppraisalProfile Appraisal;
        public List<AppraisalPatternType> AppliedPatterns = new List<AppraisalPatternType>();
    }
}
