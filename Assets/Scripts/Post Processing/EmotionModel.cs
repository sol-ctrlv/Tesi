// Core data model for emotion-driven post-processing over Edgar-generated levels in Unity

using System.Collections.Generic;
using UnityEngine;

namespace EmotionPCG
{
    public enum EmotionType
    {
        Wonder,
        Fear,
        Joy
    }

    public enum Agency
    {
        Self,
        Other,
        Env,
        Neutral
    }

    [System.Serializable]
    public struct AppraisalProfile
    {
        public float Novelty;           // [0,1]
        public float Pleasantness;      // [-1,1]
        public float GoalConduciveness; // [-1,1]
        public float Urgency;           // [0,1]
        public float Certainty;         // [-1,1]
        public float NegOutcomeProb;    // [0,1]
        public float Controllability;   // [0,1]
        public float Power;             // [0,1]
        public float Adjustability;     // [0,1]
        public Agency agency;

        public static AppraisalProfile Neutral()
        {
            return new AppraisalProfile
            {
                Novelty = 0.0f,
                Pleasantness = 0.0f,
                GoalConduciveness = 0.0f,
                Urgency = 0.0f,
                Certainty = 0.0f,
                NegOutcomeProb = 0.0f,
                Controllability = 0.5f,
                Power = 0.5f,
                Adjustability = 0.0f,
                agency = Agency.Neutral
            };
        }

        public void Add(AppraisalProfile delta)
        {
            Novelty += delta.Novelty;
            Pleasantness += delta.Pleasantness;
            GoalConduciveness += delta.GoalConduciveness;
            Urgency += delta.Urgency;
            Certainty += delta.Certainty;
            NegOutcomeProb += delta.NegOutcomeProb;
            Controllability += delta.Controllability;
            Power += delta.Power;
            Adjustability += delta.Adjustability;

            if (delta.agency != Agency.Neutral)
                agency = delta.agency;

            Clamp();
        }

        public void Clamp()
        {
            Novelty = Mathf.Clamp01(Novelty);
            Pleasantness = Mathf.Clamp(Pleasantness, -1f, 1f);
            GoalConduciveness = Mathf.Clamp(GoalConduciveness, -1f, 1f);
            Urgency = Mathf.Clamp01(Urgency);
            Certainty = Mathf.Clamp(Certainty, -1f, 1f);
            NegOutcomeProb = Mathf.Clamp01(NegOutcomeProb);
            Controllability = Mathf.Clamp01(Controllability);
            Power = Mathf.Clamp01(Power);
            Adjustability = Mathf.Clamp01(Adjustability);
        }

        public static AppraisalProfile operator +(AppraisalProfile a, AppraisalProfile b)
        {
            var result = a;
            result.Add(b);
            return result;
        }

        public static AppraisalProfile operator /(AppraisalProfile a, float scalar)
        {
            if (Mathf.Approximately(scalar, 0f))
            {
                Debug.LogError("Attempted to divide AppraisalProfile by zero scalar.");
                return a;
            }

            return new AppraisalProfile
            {
                Novelty = a.Novelty / scalar,
                Pleasantness = a.Pleasantness / scalar,
                GoalConduciveness = a.GoalConduciveness / scalar,
                Urgency = a.Urgency / scalar,
                Certainty = a.Certainty / scalar,
                NegOutcomeProb = a.NegOutcomeProb / scalar,
                Controllability = a.Controllability / scalar,
                Power = a.Power / scalar,
                Adjustability = a.Adjustability / scalar,
                agency = a.agency
            };
        }
    }

    public enum AppraisalPatternType
    {
        Centering,
        Symmetry,
        AppearanceOfObjects,
        PointingOut,
        Conflict,
        ContentDensity,
        OcclusionAudio,
        Rewards,
        CompetenceGate,
        ClearSignposting,
        SafeHaven
    }

    /// <summary>
    /// Library of appraisal pattern deltas.
    ///
    /// The numeric values here are hand-tuned heuristics derived from the
    /// appraisal ranges defined for Wonder/Fear/Joy and inspired by the
    /// pattern language in "Wonderful Design". They are intended as
    /// a starting point for calibration in the experiments, not as
    /// psychologically validated constants.
    /// </summary>
    public static class AppraisalPatternLibrary
    {
        public static AppraisalProfile GetDelta(AppraisalPatternType pattern)
        {
            switch (pattern)
            {
                case AppraisalPatternType.Centering:
                    return new AppraisalProfile
                    {
                        Novelty = 0.15f,
                        Pleasantness = 0.20f,
                        GoalConduciveness = 0.20f,
                        Urgency = 0.00f,
                        Certainty = 0.25f,
                        NegOutcomeProb = 0.00f,
                        Controllability = 0.20f,
                        Power = 0.00f,
                        Adjustability = 0.10f,
                        agency = Agency.Self
                    };

                case AppraisalPatternType.Symmetry:
                    return new AppraisalProfile
                    {
                        Novelty = -0.10f,
                        Pleasantness = 0.30f,
                        GoalConduciveness = 0.10f,
                        Urgency = -0.20f,
                        Certainty = 0.20f,
                        NegOutcomeProb = -0.10f,
                        Controllability = 0.20f,
                        Power = 0.00f,
                        Adjustability = 0.10f,
                        agency = Agency.Neutral
                    };

                case AppraisalPatternType.AppearanceOfObjects:
                    return new AppraisalProfile
                    {
                        Novelty = 0.30f,
                        Pleasantness = 0.20f,
                        GoalConduciveness = 0.10f,
                        Urgency = 0.00f,
                        Certainty = -0.20f,
                        NegOutcomeProb = 0.00f,
                        Controllability = 0.10f,
                        Power = 0.00f,
                        Adjustability = 0.20f,
                        agency = Agency.Self
                    };

                case AppraisalPatternType.PointingOut:
                    return new AppraisalProfile
                    {
                        Novelty = 0.10f,
                        Pleasantness = 0.10f,
                        GoalConduciveness = 0.30f,
                        Urgency = 0.10f,
                        Certainty = 0.30f,
                        NegOutcomeProb = -0.10f,
                        Controllability = 0.20f,
                        Power = 0.00f,
                        Adjustability = 0.20f,
                        agency = Agency.Self
                    };

                case AppraisalPatternType.Conflict:
                    return new AppraisalProfile
                    {
                        Novelty = 0.10f,
                        Pleasantness = -0.30f,
                        GoalConduciveness = -0.30f,
                        Urgency = 0.30f,
                        Certainty = 0.00f,
                        NegOutcomeProb = 0.30f,
                        Controllability = -0.20f,
                        Power = -0.10f,
                        Adjustability = -0.20f,
                        agency = Agency.Other
                    };

                case AppraisalPatternType.ContentDensity:
                    return new AppraisalProfile
                    {
                        Novelty = 0.00f,
                        Pleasantness = -0.20f,
                        GoalConduciveness = -0.20f,
                        Urgency = 0.20f,
                        Certainty = -0.10f,
                        NegOutcomeProb = 0.20f,
                        Controllability = -0.30f,
                        Power = 0.00f,
                        Adjustability = -0.20f,
                        agency = Agency.Env
                    };

                case AppraisalPatternType.OcclusionAudio:
                    return new AppraisalProfile
                    {
                        Novelty = 0.10f,
                        Pleasantness = -0.20f,
                        GoalConduciveness = -0.10f,
                        Urgency = 0.30f,
                        Certainty = -0.30f,
                        NegOutcomeProb = 0.30f,
                        Controllability = -0.20f,
                        Power = -0.10f,
                        Adjustability = -0.10f,
                        agency = Agency.Other
                    };

                case AppraisalPatternType.Rewards:
                    return new AppraisalProfile
                    {
                        Novelty = 0.10f,
                        Pleasantness = 0.30f,
                        GoalConduciveness = 0.30f,
                        Urgency = -0.10f,
                        Certainty = 0.10f,
                        NegOutcomeProb = -0.30f,
                        Controllability = 0.20f,
                        Power = 0.30f,
                        Adjustability = 0.20f,
                        agency = Agency.Self
                    };

                case AppraisalPatternType.CompetenceGate:
                    return new AppraisalProfile
                    {
                        Novelty = 0.10f,
                        Pleasantness = 0.10f,
                        GoalConduciveness = 0.20f,
                        Urgency = 0.20f,
                        Certainty = 0.20f,
                        NegOutcomeProb = 0.10f,
                        Controllability = 0.30f,
                        Power = 0.20f,
                        Adjustability = -0.10f,
                        agency = Agency.Self
                    };

                case AppraisalPatternType.ClearSignposting:
                    return new AppraisalProfile
                    {
                        Novelty = 0.10f,
                        Pleasantness = 0.10f,
                        GoalConduciveness = 0.20f,
                        Urgency = 0.00f,
                        Certainty = 0.30f,
                        NegOutcomeProb = -0.20f,
                        Controllability = 0.30f,
                        Power = 0.00f,
                        Adjustability = 0.20f,
                        agency = Agency.Self
                    };

                case AppraisalPatternType.SafeHaven:
                    return new AppraisalProfile
                    {
                        Novelty = 0.10f,
                        Pleasantness = 0.30f,
                        GoalConduciveness = 0.20f,
                        Urgency = -0.30f,
                        Certainty = 0.20f,
                        NegOutcomeProb = -0.30f,
                        Controllability = 0.30f,
                        Power = 0.20f,
                        Adjustability = 0.30f,
                        agency = Agency.Self
                    };

                default:
                    return AppraisalProfile.Neutral();
            }
        }
    }

    public class RoomNode
    {
        public string Id;
        public bool IsOnCriticalPath;
        public List<RoomNode> Neighbors = new List<RoomNode>();

        // NUOVO: info spaziali / ordine sul critical path
        public Vector3 WorldPosition;
        public int CriticalOrder = -1;
        public bool HasNextCritical;
        public Vector3 NextCriticalDirection;

        public AppraisalProfile Appraisal;
        public List<AppraisalPatternType> AppliedPatterns = new List<AppraisalPatternType>();

        public RoomNode(string id)
        {
            Id = id;
            IsOnCriticalPath = false;
            Appraisal = AppraisalProfile.Neutral();
            WorldPosition = Vector3.zero;
            CriticalOrder = -1;
            HasNextCritical = false;
            NextCriticalDirection = Vector3.zero;
        }
    }


    /// <summary>
    /// Range e centro target per ciascuna emozione.
    /// </summary>
    public struct EmotionTarget
    {
        public EmotionType Emotion;
        public AppraisalProfile Min;
        public AppraisalProfile Max;
        public AppraisalProfile Center;
    }

    public static class EmotionTargets
    {
        public static readonly EmotionTarget Wonder;
        public static readonly EmotionTarget Fear;
        public static readonly EmotionTarget Joy;

        private static EmotionTarget CreateTarget(EmotionType emotion, AppraisalProfile min, AppraisalProfile max)
        {
            var center = (min + max) / 2f;
            center.agency = Agency.Neutral;

            return new EmotionTarget
            {
                Emotion = emotion,
                Min = min,
                Max = max,
                Center = center
            };
        }

        static EmotionTargets()
        {
            // WONDER ranges
            var wonderMin = new AppraisalProfile
            {
                Novelty = 0.6f,
                Pleasantness = 0.3f,
                GoalConduciveness = -0.1f,
                Urgency = 0.0f,
                Certainty = 0.2f,
                NegOutcomeProb = 0.0f,
                Controllability = 0.4f,
                Power = 0.3f,
                Adjustability = 0.3f,
                agency = Agency.Neutral
            };

            var wonderMax = new AppraisalProfile
            {
                Novelty = 0.9f,
                Pleasantness = 0.8f,
                GoalConduciveness = 0.4f,
                Urgency = 0.25f,
                Certainty = 0.5f,
                NegOutcomeProb = 0.3f,
                Controllability = 0.7f,
                Power = 0.6f,
                Adjustability = 0.7f,
                agency = Agency.Neutral
            };

            Wonder = CreateTarget(EmotionType.Wonder, wonderMin, wonderMax);

            // FEAR ranges
            var fearMin = new AppraisalProfile
            {
                Novelty = 0.5f,
                Pleasantness = -0.6f,
                GoalConduciveness = -0.7f,
                Urgency = 0.6f,
                Certainty = 0.1f,
                NegOutcomeProb = 0.6f,
                Controllability = 0.0f,
                Power = 0.0f,
                Adjustability = 0.0f,
                agency = Agency.Neutral
            };

            var fearMax = new AppraisalProfile
            {
                Novelty = 0.9f,
                Pleasantness = -0.2f,
                GoalConduciveness = -0.3f,
                Urgency = 1.0f,
                Certainty = 0.4f,
                NegOutcomeProb = 1.0f,
                Controllability = 0.4f,
                Power = 0.4f,
                Adjustability = 0.5f,
                agency = Agency.Neutral
            };

            Fear = CreateTarget(EmotionType.Fear, fearMin, fearMax);

            // JOY ranges
            var joyMin = new AppraisalProfile
            {
                Novelty = 0.5f,
                Pleasantness = 0.6f,
                GoalConduciveness = 0.5f,
                Urgency = 0.1f,
                Certainty = 0.5f,
                NegOutcomeProb = 0.0f,
                Controllability = 0.6f,
                Power = 0.6f,
                Adjustability = 0.6f,
                agency = Agency.Neutral
            };

            var joyMax = new AppraisalProfile
            {
                Novelty = 0.8f,
                Pleasantness = 1.0f,
                GoalConduciveness = 1.0f,
                Urgency = 0.45f,
                Certainty = 1.0f,
                NegOutcomeProb = 0.2f,
                Controllability = 1.0f,
                Power = 1.0f,
                Adjustability = 1.0f,
                agency = Agency.Neutral
            };

            Joy = CreateTarget(EmotionType.Joy, joyMin, joyMax);
        }
    }

    /// <summary>
    /// Pesi per le metriche quando si calcola la distanza dal target.
    /// </summary>
    [System.Serializable]
    public struct AppraisalWeights
    {
        public float Novelty;
        public float Pleasantness;
        public float GoalConduciveness;
        public float Urgency;
        public float Certainty;
        public float NegOutcomeProb;
        public float Controllability;
        public float Power;
        public float Adjustability;

        public static AppraisalWeights Ones => new AppraisalWeights
        {
            Novelty = 1f,
            Pleasantness = 1f,
            GoalConduciveness = 1f,
            Urgency = 1f,
            Certainty = 1f,
            NegOutcomeProb = 1f,
            Controllability = 1f,
            Power = 1f,
            Adjustability = 1f
        };
    }

    public static class EmotionWeights
    {
        public static readonly AppraisalWeights Wonder = new AppraisalWeights
        {
            Novelty = 1.5f,
            Pleasantness = 1.2f,
            GoalConduciveness = 0.7f,
            Urgency = 0.5f,
            Certainty = 1.0f,
            NegOutcomeProb = 0.5f,
            Controllability = 1.0f,
            Power = 0.8f,
            Adjustability = 1.2f
        };

        public static readonly AppraisalWeights Fear = new AppraisalWeights
        {
            Novelty = 0.8f,
            Pleasantness = 1.2f,
            GoalConduciveness = 1.2f,
            Urgency = 1.5f,
            Certainty = 1.0f,
            NegOutcomeProb = 1.5f,
            Controllability = 1.3f,
            Power = 0.7f,
            Adjustability = 0.7f
        };

        public static readonly AppraisalWeights Joy = new AppraisalWeights
        {
            Novelty = 0.8f,
            Pleasantness = 1.5f,
            GoalConduciveness = 1.5f,
            Urgency = 0.8f,
            Certainty = 1.2f,
            NegOutcomeProb = 1.2f,
            Controllability = 1.2f,
            Power = 1.2f,
            Adjustability = 1.2f
        };

        public static AppraisalWeights Get(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Wonder: return Wonder;
                case EmotionType.Fear: return Fear;
                case EmotionType.Joy: return Joy;
                default: return AppraisalWeights.Ones;
            }
        }
    }

    public static class AppraisalMath
    {
        /// <summary>
        /// Ritorna la distanza quadratica pesata tra un profilo e un target.
        /// </summary>
        public static float WeightedSquaredDistance(AppraisalProfile profile, AppraisalProfile target, AppraisalWeights w)
        {
            float distance = 0f;

            float deltaNovelty = profile.Novelty - target.Novelty;
            distance += w.Novelty * deltaNovelty * deltaNovelty;

            float deltaPleasantness = profile.Pleasantness - target.Pleasantness;
            distance += w.Pleasantness * deltaPleasantness * deltaPleasantness;

            float deltaGoalConduciveness = profile.GoalConduciveness - target.GoalConduciveness;
            distance += w.GoalConduciveness * deltaGoalConduciveness * deltaGoalConduciveness;

            float deltaUrgency = profile.Urgency - target.Urgency;
            distance += w.Urgency * deltaUrgency * deltaUrgency;

            float deltaCertainty = profile.Certainty - target.Certainty;
            distance += w.Certainty * deltaCertainty * deltaCertainty;

            float deltaNegOutcomeProb = profile.NegOutcomeProb - target.NegOutcomeProb;
            distance += w.NegOutcomeProb * deltaNegOutcomeProb * deltaNegOutcomeProb;

            float deltaControllability = profile.Controllability - target.Controllability;
            distance += w.Controllability * deltaControllability * deltaControllability;

            float deltaPower = profile.Power - target.Power;
            distance += w.Power * deltaPower * deltaPower;

            float deltaAdjustability = profile.Adjustability - target.Adjustability;
            distance += w.Adjustability * deltaAdjustability * deltaAdjustability;

            return distance;
        }
    }
}
