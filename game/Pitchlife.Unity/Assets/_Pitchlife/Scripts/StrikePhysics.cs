using UnityEngine;

namespace Pitchlife.P01
{
    public struct StrikeSolution
    {
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Vector2 contact;
    }

    /// <summary>Rear-face contact: x is screen-right, y is up. Torque follows r cross impulse.</summary>
    public static class StrikePhysics
    {
        public static StrikeSolution Calculate(Vector3 aimedVelocity, Vector2 contact, FootballTuning tuning)
        {
            contact = Vector2.ClampMagnitude(contact, 1);
            Vector3 forward = Vector3.ProjectOnPlane(aimedVelocity, Vector3.up).normalized;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float baseAngle = Mathf.Atan2(aimedVelocity.y,
                Vector3.ProjectOnPlane(aimedVelocity, Vector3.up).magnitude) * Mathf.Rad2Deg;
            float elevation = Mathf.Clamp(baseAngle - contact.y * tuning.strikeLiftDegrees, 1, 52) * Mathf.Deg2Rad;
            float speed = aimedVelocity.magnitude * (1 - .12f * contact.sqrMagnitude);
            Vector3 velocity = (forward * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation)) * speed;
            // Use horizontal foot impulse for spin; a low strike produces backspin rather than artificial side force.
            Vector3 offset = (right * contact.x + Vector3.up * contact.y -
                forward * Mathf.Sqrt(Mathf.Max(0, 1 - contact.sqrMagnitude))) * ShotMath.BallRadius;
            Vector3 angular = Vector3.Cross(offset, forward * speed) *
                (tuning.strikeSpinCoupling / (.4f * ShotMath.BallRadius * ShotMath.BallRadius));
            return new StrikeSolution { velocity = velocity, angularVelocity = angular, contact = contact };
        }

        public static Vector3 MagnusAcceleration(Vector3 velocity, Vector3 angularVelocity, FootballTuning tuning) =>
            Vector3.ClampMagnitude(Vector3.Cross(angularVelocity, velocity) * tuning.magnusCoefficient,
                Mathf.Max(0, tuning.maxMagnusAcceleration));

        public static bool ValidContact(Vector2 point) =>
            !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsInfinity(point.x) &&
            !float.IsInfinity(point.y) && point.sqrMagnitude <= 1;

        public static string Describe(Vector2 contact)
        {
            string height = contact.y < -.25f ? "Low strike: chip + backspin" :
                contact.y > .25f ? "High strike: low shot + topspin" : "Center height: driven shot";
            string curve = contact.x < -.2f ? "curves right" : contact.x > .2f ? "curves left" : "straight";
            return height + "  |  " + curve;
        }
    }
}
