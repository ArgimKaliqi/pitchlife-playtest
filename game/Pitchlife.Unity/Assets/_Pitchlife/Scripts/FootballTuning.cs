using UnityEngine;

namespace Pitchlife.P01
{
    public enum FootballAction { Shot, GroundPass, LoftedPass }
    public enum AttemptOutcome { Goal, Miss, Received, Blocked }

    [System.Serializable]
    public struct AttemptResult
    {
        public int attempt;
        public FootballAction action;
        public AttemptOutcome outcome;
        public Vector3 launchVelocity;
        public Vector3 launchAngularVelocity;
        public Vector2 strikePoint;
        public Vector3 endPosition;
        public float spin;
        public float duration;
        public bool replay;
    }

    [CreateAssetMenu(menuName = "PITCHLIFE/Football Tuning")]
    public sealed class FootballTuning : ScriptableObject
    {
        [Header("Build shader references")]
        public Shader litShader;
        public Shader unlitShader;
        [Header("Shot")]
        public float shotMinSpeed = 15;
        public float shotMaxSpeed = 25;
        [Header("Ball contact")]
        public float strikeLiftDegrees = 30;
        public float strikeSpinCoupling = .16f;
        public float magnusCoefficient = .012f;
        public float maxMagnusAcceleration = 6;
        [Header("Passing")]
        public float groundPassMinSpeed = 9;
        public float groundPassMaxSpeed = 17;
        public float loftedPassMinSpeed = 13;
        public float loftedPassMaxSpeed = 20;
        [Header("Curve and movement")]
        public float curveAcceleration = 3;
        public float receiverSpeed = 4.5f;
        public float defenderSpeed = 2.7f;
        public float keeperSpeed = 3;
        public float keeperReactionTime = .3f;
    }

    public static class FootballActionMath
    {
        public static bool TryVelocity(Vector2 start, Vector2 end, float width, float height,
            FootballAction action, FootballTuning tuning, float powerScale, float elevationOffset,
            out Vector3 velocity)
        {
            velocity = Vector3.zero;
            if (width <= 0 || height <= 0 || (end.y - start.y) / height < .035f) return false;
            float power = Mathf.Clamp01(Mathf.InverseLerp(.04f, .4f, (end.y - start.y) / height));
            float min = tuning.shotMinSpeed, max = tuning.shotMaxSpeed;
            float angle = Mathf.Lerp(8, 14, power);
            if (action == FootballAction.GroundPass)
            {
                min = tuning.groundPassMinSpeed; max = tuning.groundPassMaxSpeed; angle = 1;
            }
            else if (action == FootballAction.LoftedPass)
            {
                min = tuning.loftedPassMinSpeed; max = tuning.loftedPassMaxSpeed;
                angle = Mathf.Lerp(28, 40, power);
            }
            angle = Mathf.Clamp(angle + elevationOffset, 1, 48) * Mathf.Deg2Rad;
            float yaw = Mathf.Clamp((end.x - start.x) / width * 75, -32, 32) * Mathf.Deg2Rad;
            float speed = Mathf.Lerp(min, max, power) * Mathf.Clamp(powerScale, .6f, 1.3f);
            velocity = speed * new Vector3(Mathf.Sin(yaw) * Mathf.Cos(angle), Mathf.Sin(angle),
                Mathf.Cos(yaw) * Mathf.Cos(angle));
            return true;
        }
    }
}
