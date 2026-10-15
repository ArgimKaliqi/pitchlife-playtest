using UnityEngine;

namespace Pitchlife.P01
{
    /// <summary>Deterministic swipe-to-shot math and a geometric goal crossing test.</summary>
    public static class ShotMath
    {
        public const float GoalWidth = 7.32f;
        public const float GoalHeight = 2.44f;
        public const float GoalLineZ = 12f;
        public const float BallRadius = 0.22f;

        public static bool TryCalculateVelocity(Vector2 start, Vector2 end,
            float screenWidth, float screenHeight, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            if (screenWidth <= 0 || screenHeight <= 0) return false;

            Vector2 delta = end - start;
            float upward = delta.y / screenHeight;
            if (upward < 0.035f) return false;

            float power = Mathf.Clamp01(Mathf.InverseLerp(0.04f, 0.4f, upward));
            float speed = Mathf.Lerp(15f, 25f, power);
            float yaw = Mathf.Clamp(delta.x / screenWidth * 75f, -24f, 24f) * Mathf.Deg2Rad;
            float elevation = Mathf.Lerp(8f, 14f, power) * Mathf.Deg2Rad;

            velocity = speed * new Vector3(
                Mathf.Sin(yaw) * Mathf.Cos(elevation),
                Mathf.Sin(elevation),
                Mathf.Cos(yaw) * Mathf.Cos(elevation));
            return true;
        }

        // The entire ball must cross: sample sphere centre at the goal line plus its radius.
        public static bool DidPassGoalLine(Vector3 previous, Vector3 current, float radius)
        {
            float crossingPlane = GoalLineZ + radius;
            return previous.z < crossingPlane && current.z >= crossingPlane;
        }

        public static bool IsGoalCrossing(Vector3 previous, Vector3 current,
            float radius, out Vector3 crossing)
        {
            crossing = Vector3.zero;
            if (!DidPassGoalLine(previous, current, radius)) return false;
            float zTravel = current.z - previous.z;
            if (zTravel <= 0f) return false;

            float t = (GoalLineZ + radius - previous.z) / zTravel;
            crossing = Vector3.Lerp(previous, current, t);

            bool insidePosts = Mathf.Abs(crossing.x) + radius <= GoalWidth * 0.5f;
            bool underBar = crossing.y + radius <= GoalHeight;
            bool aboveGround = crossing.y - radius >= -0.015f;
            return insidePosts && underBar && aboveGround;
        }
    }
}
