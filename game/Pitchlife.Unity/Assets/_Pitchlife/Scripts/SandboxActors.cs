using UnityEngine;

namespace Pitchlife.P01
{
    public enum SandboxActorRole { Receiver, Defender, Goalkeeper }
    public sealed class SandboxActor : MonoBehaviour
    {
        public SandboxActorRole role;
    }

    public sealed class BallContact : MonoBehaviour
    {
        public P01FootballSandbox sandbox;
        private void OnCollisionEnter(Collision collision)
        {
            var actor = collision.collider.GetComponent<SandboxActor>();
            if (actor != null) sandbox.ActorContact(actor.role);
        }
    }

    public sealed class SandboxActors
    {
        public Rigidbody Receiver { get; private set; }
        public Rigidbody Defender { get; private set; }
        public Rigidbody Keeper { get; private set; }
        private readonly FootballTuning _tuning;
        private readonly Vector3 _receiverStart = new Vector3(4, .95f, 3);
        private readonly Vector3 _defenderStart = new Vector3(2.7f, .95f, 5);
        private readonly Vector3 _keeperStart = new Vector3(0, .95f, 10.8f);
        private readonly Transform _target;
        private bool _opposition;
        public bool IsValid => Receiver != null && Defender != null && Keeper != null && _target != null;

        public SandboxActors(Transform parent, FootballTuning tuning)
        {
            _tuning = tuning;
            Receiver = Create(parent, "Receiving Teammate", SandboxActorRole.Receiver, _receiverStart,
                new Color(.2f, .45f, .95f));
            Defender = Create(parent, "Defender", SandboxActorRole.Defender, _defenderStart,
                new Color(.9f, .22f, .15f));
            Keeper = Create(parent, "Goalkeeper", SandboxActorRole.Goalkeeper, _keeperStart,
                new Color(1, .72f, .05f));
            _target = parent.Find("Pass Target");
            if (_target == null)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "Pass Target";
                marker.transform.SetParent(parent);
                marker.transform.localScale = new Vector3(1.3f, .015f, 1.3f);
                Object.Destroy(marker.GetComponent<Collider>());
                marker.GetComponent<Renderer>().sharedMaterial = Material(new Color(.3f, .75f, 1), _tuning.litShader);
                _target = marker.transform;
                _target.position = new Vector3(_receiverStart.x, .035f, _receiverStart.z);
            }
            // Rebinding after a script reload must preserve existing positions and opponent visibility.
            _opposition = Defender.gameObject.activeSelf;
        }

        public void Reset(bool opposition)
        {
            Receiver.position = _receiverStart;
            Defender.position = _defenderStart;
            Keeper.position = _keeperStart;
            _opposition = opposition;
            Defender.gameObject.SetActive(opposition);
            Keeper.gameObject.SetActive(opposition);
            _target.position = new Vector3(_receiverStart.x, .035f, _receiverStart.z);
        }

        public void Tick(Rigidbody ball, FootballAction action, bool inFlight, float elapsed, float dt)
        {
            if (!inFlight) return;
            if (action != FootballAction.Shot)
            {
                // Chase only a reachable landing area; do not chase the ball into the goal or off the pitch.
                float fallTime = (ball.linearVelocity.y + Mathf.Sqrt(ball.linearVelocity.y * ball.linearVelocity.y +
                    2 * 9.81f * Mathf.Max(0, ball.position.y - ShotMath.BallRadius))) / 9.81f;
                Vector3 intercept = ball.position + ball.linearVelocity * Mathf.Clamp(fallTime, 0, 1.3f);
                intercept.x = Mathf.Clamp(intercept.x, -9, 9);
                intercept.z = Mathf.Clamp(intercept.z, -1, 8);
                intercept.y = .95f;
                Move(Receiver, intercept, _tuning.receiverSpeed, dt);
                _target.position = new Vector3(intercept.x, .035f, intercept.z);
            }
            if (!_opposition) return;
            if (ball.position.z < 6.5f)
                Move(Defender, new Vector3(Mathf.Clamp(ball.position.x, -6, 6), .95f, 5), _tuning.defenderSpeed, dt);
            if (elapsed < _tuning.keeperReactionTime) return;
            float ahead = ball.linearVelocity.z > .1f ? Mathf.Clamp((10.8f - ball.position.z) / ball.linearVelocity.z, 0, 1) : 0;
            float x = Mathf.Clamp(ball.position.x + ball.linearVelocity.x * ahead, -3, 3);
            Move(Keeper, new Vector3(x, .95f, 10.8f), _tuning.keeperSpeed, dt);
        }

        public bool CanReceive(Rigidbody ball)
        {
            Vector3 delta = ball.position - Receiver.position;
            return ball.position.y < 1.6f && new Vector2(delta.x, delta.z).magnitude < 1;
        }

        private static void Move(Rigidbody body, Vector3 target, float speed, float dt) =>
            body.MovePosition(Vector3.MoveTowards(body.position, target, Mathf.Max(0, speed) * dt));

        private Rigidbody Create(Transform parent, string name, SandboxActorRole role, Vector3 position, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent<Rigidbody>(out var existingBody)) return existingBody;
            var actor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            actor.name = name;
            actor.transform.SetParent(parent);
            actor.transform.position = position;
            actor.transform.localScale = new Vector3(.65f, .95f, .65f);
            actor.GetComponent<Renderer>().sharedMaterial = Material(color, _tuning.litShader);
            actor.AddComponent<SandboxActor>().role = role;
            var body = actor.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            return body;
        }

        private static Material Material(Color color, Shader litShader)
        {
            var material = new Material(litShader != null ? litShader : Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
