// Modello dati principale per il post-processing emozionale sui livelli generati con Edgar in Unity
// Questo file definisce:
//  - I tipi di emozione usati dal sistema
//  - Lo spazio di appraisal (dimensioni in stile Scherer)
//  - I pattern di appraisal (design pattern) e i loro delta numerici
//  - Le regioni target nello spazio di appraisal per ciascuna emozione
//  - I pesi usati per calcolare la distanza da un target

using System.Collections.Generic;
using UnityEngine;

namespace EmotionPCG
{
    /// <summary>
    /// Etichetta di emozione ad alto livello usata per configurare il livello.
    /// </summary>
    public enum EmotionType
    {
        Wonder,
        Fear,
        Joy
    }

    /// <summary>
    /// Agente percepito come responsabile dell'evento (sé, altri, ambiente, nessuno).
    /// </summary>
    public enum Agency
    {
        Self,
        Other,
        Env,
        Neutral
    }

    /// <summary>
    /// Punto continuo nello spazio di appraisal per una stanza / livello.
    /// Ogni campo codifica una dimensione di appraisal, normalizzata in un certo intervallo.
    /// </summary>
    [System.Serializable]
    public struct AppraisalProfile
    {
        // Dimensioni principali di appraisal (gli intervalli nei commenti sono convenzioni di design)
        public float Novelty;           // [0,1]  - quanto la situazione è nuova / inattesa
        public float Pleasantness;      // [-1,1] - valenza affettiva (da negativa a positiva)
        public float GoalConduciveness; // [-1,1] - quanto aiuta o ostacola gli obiettivi del giocatore
        public float Urgency;           // [0,1]  - quanto è necessario agire rapidamente
        public float Certainty;         // [-1,1] - quanto la situazione è chiara / prevedibile
        public float NegOutcomeProb;    // [0,1]  - probabilità di esiti negativi
        public float Controllability;   // [0,1]  - quanto il giocatore percepisce di poter influenzare gli eventi
        public float Power;             // [0,1]  - potere / risorse percepite rispetto alla situazione
        public float Adjustability;     // [0,1]  - quanto ci si può adattare / riorganizzare

        // Chi è percepito come agente dell'evento
        public Agency agency;

        /// <summary>
        /// Restituisce un punto neutro nello spazio di appraisal (baseline per le stanze).
        /// </summary>
        public static AppraisalProfile Neutral()
        {
            // Manteniamo alcune dimensioni lontane dallo zero esatto (es. controllability, power)
            // per evitare profili completamente piatti e aderire meglio alle bande emotive previste.
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

        /// <summary>
        /// Somma un delta di pattern al profilo corrente e limita il risultato agli intervalli validi.
        /// </summary>
        public void Add(AppraisalProfile delta)
        {
            // Somma componente per componente del delta
            Novelty += delta.Novelty;
            Pleasantness += delta.Pleasantness;
            GoalConduciveness += delta.GoalConduciveness;
            Urgency += delta.Urgency;
            Certainty += delta.Certainty;
            NegOutcomeProb += delta.NegOutcomeProb;
            Controllability += delta.Controllability;
            Power += delta.Power;
            Adjustability += delta.Adjustability;

            // L'agency è categoriale: la sovrascriviamo solo se il delta la imposta esplicitamente
            if (delta.agency != Agency.Neutral)
                agency = delta.agency;

            // Manteniamo il risultato entro gli intervalli previsti
            Clamp();
        }

        /// <summary>
        /// Limita tutte le dimensioni ai loro intervalli semantici per evitare derive fuori range.
        /// </summary>
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

        /// <summary>
        /// Somma due profili di appraisal applicando la logica di Add (inclusi clamp e override dell'agency).
        /// </summary>
        public static AppraisalProfile operator +(AppraisalProfile a, AppraisalProfile b)
        {
            var result = a;
            result.Add(b);
            return result;
        }

        /// <summary>
        /// Divide tutte le dimensioni continue per uno scalare (usato per calcolare medie).
        /// L'agency non viene modificata.
        /// </summary>
        public static AppraisalProfile operator /(AppraisalProfile a, float scalar)
        {
            // Evitiamo divisioni per zero in caso di errori logici nel chiamante
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
                // L'agency non viene scalata: manteniamo il valore categoriale
                agency = a.agency
            };
        }
    }

    /// <summary>
    /// Tipi di design pattern che possiamo applicare a una stanza.
    /// Ogni pattern è mappato a un delta in AppraisalPatternLibrary.
    /// </summary>
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
    /// Libreria dei delta di appraisal associati ai pattern.
    ///
    /// I valori numerici sono euristiche costruite a partire dagli intervalli
    /// di appraisal definiti per Wonder/Fear/Joy e ispirate al linguaggio di pattern
    /// di "Wonderful Design". Sono pensati come punto di partenza per la calibrazione
    /// negli esperimenti, non come costanti psicologicamente validate.
    /// </summary>
    public static class AppraisalPatternLibrary
    {
        /// <summary>
        /// Restituisce il delta di appraisal euristico associato a un dato pattern.
        /// </summary>
        public static AppraisalProfile GetDelta(AppraisalPatternType pattern)
        {
            switch (pattern)
            {
                case AppraisalPatternType.Centering:
                    // Fa sentire il giocatore più centrato, al sicuro e in controllo.
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
                    // La simmetria aumenta piacevolezza e ordine, riduce urgenza e minaccia.
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
                    // Apparizione improvvisa di oggetti: alta novità, certezza ambivalente.
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
                    // Forte guida: più conducibilità al goal, certezza e controllo.
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
                    // Conflitto diretto: più urgenza e minaccia, minore piacevolezza.
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
                    // Densità / occlusione fisica: più difficile controllare, più probabilità negative.
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
                    // Occlusione audio: nasconde informazione, aumenta incertezza e minaccia.
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
                    // Ricompense esplicite: valenza e conducibilità al goal fortemente positive.
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
                    // Sfida calibrata sulla competenza: aumenta controllo, potere e progressione positiva.
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
                    // Segnaletica chiara: riduce la probabilità di esiti negativi e aumenta la certezza.
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
                    // Zona sicura di riposo: molto positiva, bassa urgenza, riduce aspettative negative.
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
                    // Fallback in caso di pattern non gestito.
                    return AppraisalProfile.Neutral();
            }
        }
    }

    /// <summary>
    /// Nodo logico che rappresenta una stanza nel livello generato.
    /// Usato dallo strato di ottimizzazione; in seguito viene mappato alle stanze Unity.
    /// </summary>
    public class RoomNode
    {
        // Identificatore (di solito derivato dal nome della stanza / template in Edgar)
        public string Id;

        // Indica se la stanza è marcata come parte del percorso principale (critical path)
        public bool IsOnCriticalPath;

        // Informazioni opzionali di adiacenza, se vogliamo ragionare sul grafo
        public List<RoomNode> Neighbors = new List<RoomNode>();

        // Informazioni spaziali e di ordinamento lungo il critical path
        public Vector3 WorldPosition;      // posizione in spazio mondo (usata per calcolare direzioni)
        public int CriticalOrder = -1;    // indice lungo il critical path (-1 se non è sul path)
        public bool HasNextCritical;      // esiste una stanza successiva sul critical path?
        public Vector3 NextCriticalDirection; // direzione normalizzata verso il prossimo nodo critico

        // Stato di appraisal corrente della stanza (dopo l'applicazione dei pattern)
        public AppraisalProfile Appraisal;

        // Pattern applicati finora alla stanza (popolato dall'ottimizzatore)
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
    /// Intervallo e "centro" target nello spazio di appraisal per una data emozione.
    /// L'ottimizzatore cerca di portare la media globale vicino a Center.
    /// </summary>
    public struct EmotionTarget
    {
        public EmotionType Emotion;    // etichetta (Wonder/Fear/Joy)
        public AppraisalProfile Min;   // limite inferiore dell'intervallo desiderato
        public AppraisalProfile Max;   // limite superiore dell'intervallo desiderato
        public AppraisalProfile Center; // punto medio (usato come target dell'ottimizzazione)
    }

    /// <summary>
    /// Intervalli target predefiniti per Wonder, Fear e Joy.
    /// I valori sono tarati a mano per approssimare la fenomenologia desiderata.
    /// </summary>
    public static class EmotionTargets
    {
        public static readonly EmotionTarget Wonder;
        public static readonly EmotionTarget Fear;
        public static readonly EmotionTarget Joy;

        /// <summary>
        /// Helper che costruisce un EmotionTarget e ne calcola il centro come (min + max)/2.
        /// </summary>
        private static EmotionTarget CreateTarget(EmotionType emotion, AppraisalProfile min, AppraisalProfile max)
        {
            // Calcoliamo il punto medio per ogni dimensione; l'agency qui è neutrale per costruzione
            var center = new AppraisalProfile
            {
                Novelty = (min.Novelty + max.Novelty) * 0.5f,
                Pleasantness = (min.Pleasantness + max.Pleasantness) * 0.5f,
                GoalConduciveness = (min.GoalConduciveness + max.GoalConduciveness) * 0.5f,
                Urgency = (min.Urgency + max.Urgency) * 0.5f,
                Certainty = (min.Certainty + max.Certainty) * 0.5f,
                NegOutcomeProb = (min.NegOutcomeProb + max.NegOutcomeProb) * 0.5f,
                Controllability = (min.Controllability + max.Controllability) * 0.5f,
                Power = (min.Power + max.Power) * 0.5f,
                Adjustability = (min.Adjustability + max.Adjustability) * 0.5f,
                agency = Agency.Neutral
            };

            return new EmotionTarget
            {
                Emotion = emotion,
                Min = min,
                Max = max,
                Center = center
            };
        }

        // Il costruttore statico viene eseguito una volta e inizializza i target readonly
        static EmotionTargets()
        {
            // Intervalli per WONDER: alta novità e piacevolezza, controllo moderato.
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

            // Intervalli per FEAR: valenza negativa, alta urgenza e alta aspettativa di esiti negativi.
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

            // Intervalli per JOY: valenza molto positiva, buona conducibilità al goal e buon controllo.
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
    /// Pesi per ciascuna dimensione di appraisal nel calcolo della distanza da un target.
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

        /// <summary>
        /// Pesi di comodo con tutte le dimensioni ugualmente pesate (1).
        /// </summary>
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

    /// <summary>
    /// Pesi predefiniti per emozione: quali dimensioni contano di più per ciascuna emozione.
    /// </summary>
    public static class EmotionWeights
    {
        // Wonder enfatizza novità, piacevolezza e adattabilità.
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

        // Fear enfatizza urgenza, probabilità di esiti negativi e perdita di controllo.
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

        // Joy enfatizza piacevolezza, conducibilità al goal e senso di controllo/potere.
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

        /// <summary>
        /// Restituisce i pesi associati all'emozione data.
        /// </summary>
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

    /// <summary>
    /// Funzioni di utilità matematica per lavorare nello spazio di appraisal.
    /// </summary>
    public static class AppraisalMath
    {
        /// <summary>
        /// Restituisce la distanza quadratica euclidea pesata tra un profilo e un target.
        /// Peso zero significa che la dimensione viene ignorata; pesi più alti amplificano il contributo.
        /// </summary>
        public static float WeightedSquaredDistance(AppraisalProfile profile, AppraisalProfile target, AppraisalWeights w)
        {
            float distance = 0f;

            // Ogni dimensione contribuisce con weight * (value - target)^2 alla distanza totale.
            Accumulate(ref distance, profile.Novelty, target.Novelty, w.Novelty);
            Accumulate(ref distance, profile.Pleasantness, target.Pleasantness, w.Pleasantness);
            Accumulate(ref distance, profile.GoalConduciveness, target.GoalConduciveness, w.GoalConduciveness);
            Accumulate(ref distance, profile.Urgency, target.Urgency, w.Urgency);
            Accumulate(ref distance, profile.Certainty, target.Certainty, w.Certainty);
            Accumulate(ref distance, profile.NegOutcomeProb, target.NegOutcomeProb, w.NegOutcomeProb);
            Accumulate(ref distance, profile.Controllability, target.Controllability, w.Controllability);
            Accumulate(ref distance, profile.Power, target.Power, w.Power);
            Accumulate(ref distance, profile.Adjustability, target.Adjustability, w.Adjustability);

            return distance;
        }

        /// <summary>
        /// Aggiunge un singolo contributo di differenza quadratica pesata all'accumulatore di distanza.
        /// Tenuto piccolo e inlined per rendere più leggibile la funzione principale.
        /// </summary>
        private static void Accumulate(ref float distance, float value, float targetValue, float weight)
        {
            float delta = value - targetValue;        // differenza (con segno) tra valore corrente e target
            distance += weight * delta * delta;       // termine classico di errore quadratico pesato
        }
    }
}
