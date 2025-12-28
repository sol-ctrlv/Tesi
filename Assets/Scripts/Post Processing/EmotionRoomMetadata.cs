using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmotionPCG
{
    /// <summary>
    /// Contenitore dati associato al GameObject della stanza generata.
    /// Viene compilato dal post-processing e letto in seguito da sistemi di adattamento
    /// (spawn, props, luci, audio, signposting) per applicare variazioni stanza-specifiche.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmotionRoomMetadata : MonoBehaviour
    {
        // Nome della stanza (tipicamente derivato dal nome del RoomNode / prefab).
        // Utile per debug e, nel progetto, anche per convenzioni (es. critical path).
        public String RoomName;

        // Emozione target del livello corrente (Wonder/Fear/Calm), propagata dal controller del post-processing.
        public EmotionType LevelEmotion;

        // Profilo di appraisal "finale" della stanza dopo l'allocazione dei pattern (valori normalizzati 0..1).
        public AppraisalProfile Appraisal;

        // Pattern effettivamente assegnati a questa stanza dal post-processing (ordine di applicazione non garantito).
        public List<AppraisalPatternType> AppliedPatterns = new List<AppraisalPatternType>();

        // True se la stanza appartiene al critical path (percorso principale verso l'uscita/obiettivo).
        public bool IsOnCriticalPath;

        // True se esiste una stanza "successiva" sul critical path rispetto a questa.
        public bool HasNextCritical;

        // Vettore direzione (world-space) verso la prossima stanza del critical path.
        // Ha significato solo quando HasNextCritical è true.
        public Vector3 NextCriticalDirection;
    }
}
