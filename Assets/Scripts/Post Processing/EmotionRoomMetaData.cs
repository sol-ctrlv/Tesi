using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmotionPCG
{
    /// <summary>
    /// Componente che porta sul GameObject stanza le informazioni calcolate
    /// dal post-processing (profilo di appraisal + pattern applicati).
    /// Altri sistemi (spawn nemici, props, luci) possono leggerle in seguito.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmotionRoomMetadata : MonoBehaviour
    {
        public String RoomName;
        public EmotionType LevelEmotion;
        public AppraisalProfile Appraisal;
        public List<AppraisalPatternType> AppliedPatterns = new List<AppraisalPatternType>();

        public bool IsOnCriticalPath;
        public bool HasNextCritical;
        public Vector3 NextCriticalDirection;
    }
}

