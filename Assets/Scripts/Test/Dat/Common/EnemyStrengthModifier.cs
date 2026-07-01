using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Nhan buff suc manh tu Inquisitor (hoac nguon khac). Mac dinh x1.
    /// </summary>
    public class EnemyStrengthModifier : MonoBehaviour
    {
        public float DamageMultiplier { get; private set; } = 1f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;

        public void SetRoomBuff(float bonus)
        {
            float mul = 1f + Mathf.Max(0f, bonus);
            DamageMultiplier    = mul;
            MoveSpeedMultiplier = mul;
        }

        public void ClearBuff()
        {
            DamageMultiplier    = 1f;
            MoveSpeedMultiplier = 1f;
        }
    }
}
