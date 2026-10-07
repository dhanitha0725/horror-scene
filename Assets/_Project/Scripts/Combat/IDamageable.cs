using UnityEngine;

namespace HorrorGame.Combat
{
    /// <summary>
    /// Information passed when an attack or collision inflicts damage.
    /// </summary>
    public struct DamageInfo
    {
        public int amount;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public Vector3 hitDirection;
        public float impactForce;
        public GameObject damageSource;

        public DamageInfo(int amount, Vector3 hitPoint, Vector3 hitNormal, Vector3 hitDirection, float impactForce = 1f, GameObject damageSource = null)
        {
            this.amount = amount;
            this.hitPoint = hitPoint;
            this.hitNormal = hitNormal;
            this.hitDirection = hitDirection;
            this.impactForce = impactForce;
            this.damageSource = damageSource;
        }
    }

    /// <summary>
    /// Interface for any object or character that can receive damage (e.g. Ghost, breakable props).
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
        bool IsDead { get; }
    }
}
