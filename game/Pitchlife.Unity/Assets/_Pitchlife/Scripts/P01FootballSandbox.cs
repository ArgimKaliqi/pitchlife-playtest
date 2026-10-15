using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Pitchlife.P01
{
    /// <summary>
    /// P01 shooting, passing and opponent-response practice.
    /// Scene creation: Tools > PITCHLIFE > Create P01 Football Sandbox Scene.
    /// </summary>
    public sealed class P01FootballSandbox : MonoBehaviour
    {
        private enum State { Ready, SelectingStrike, InFlight, Resolved }
        private State _state;
        private Rigidbody _ball;
        private Vector3 _spawn, _previous;
        private Vector2 _dragStart;
        private bool _dragging, _touchOwned;
        private bool _guiMouseDragging;
        private Vector2 _guiMouseStart;
        private float _shotAt, _resetAt;
        private int _shots, _goals;
        private string _feedback = "Swipe up to shoot";
        public FootballTuning tuning;
        public FootballAction Action { get; private set; }
        public bool Opposition { get; private set; } = true;
        public IReadOnlyList<AttemptResult> Results => _results;
        public event System.Action<AttemptResult> AttemptFinished;
        private readonly List<AttemptResult> _results = new List<AttemptResult>();
        private SandboxActors _actors;
        private LineRenderer _aim;
        private float _powerScale = 1;
        private Vector3 _pendingVelocity, _lastAngularVelocity;
        private Vector2 _lastContact, _hoverContact;
        private bool _hoveringContact;
        public bool AwaitingStrikePoint => _state == State.SelectingStrike;
        private Vector3 _lastVelocity;
        private FootballAction _lastAction;
        private float _lastSpin;
        private bool _hasLastAction, _replay, _ownsTuning;
        private bool _lastOpposition;
        private Vector3 _lastBallPosition, _lastReceiverPosition, _lastDefenderPosition, _lastKeeperPosition;
        private Quaternion _lastBallRotation;

        private void OnEnable()
        {
            if (Application.isPlaying && _ball != null) EnsureActors();
        }

        // Unity restores component references during script reloads, but not this plain C# manager.
        private void EnsureActors()
        {
            if (_actors == null || !_actors.IsValid) _actors = new SandboxActors(transform, tuning);
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            if (tuning == null) { tuning = ScriptableObject.CreateInstance<FootballTuning>(); _ownsTuning = true; }
            _ball = SandboxWorldBuilder.Build(transform, tuning.litShader);
            _actors = new SandboxActors(transform, tuning);
            _ball.gameObject.AddComponent<BallContact>().sandbox = this;
            var aimObject = new GameObject("Trajectory Preview");
            aimObject.transform.SetParent(transform);
            _aim = aimObject.AddComponent<LineRenderer>();
            _aim.sharedMaterial = new Material(tuning.unlitShader != null ? tuning.unlitShader : Shader.Find("Universal Render Pipeline/Unlit"));
            _aim.sharedMaterial.SetColor("_BaseColor", new Color(1, .87f, .2f));
            _aim.startWidth = .055f;
            _aim.endWidth = .025f;
            _spawn = _ball.position;
            ResetShot();
        }

        private void Update()
        {
            if (_ball == null) return;

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                ResetShot();

            if (_state == State.Resolved && Time.time >= _resetAt)
                ResetShot();

            if (_state == State.Ready)
            {
                CaptureGesture();
                UpdateAim();
            }
            else if (_state == State.SelectingStrike) CaptureStrikeSelection();
        }

        private void FixedUpdate()
        {
            if (_ball == null || _state != State.InFlight) return;
            EnsureActors();
            _actors.Tick(_ball, Action, true, Time.time - _shotAt, Time.fixedDeltaTime);
            _ball.AddForce(StrikePhysics.MagnusAcceleration(_ball.linearVelocity, _ball.angularVelocity, tuning),
                ForceMode.Acceleration);
            if (Action != FootballAction.Shot && _actors.CanReceive(_ball))
            {
                _ball.linearVelocity = Vector3.zero;
                _ball.angularVelocity = Vector3.zero;
                Finish("Pass received", AttemptOutcome.Received);
                return;
            }

            Vector3 now = _ball.position;
            if (ShotMath.IsGoalCrossing(_previous,now,ShotMath.BallRadius,out _))
            {
                if (!_replay) _goals++;
                Finish("GOAL!", AttemptOutcome.Goal);
            }
            else if (ShotMath.DidPassGoalLine(_previous,now,ShotMath.BallRadius))
            {
                Finish("Missed", AttemptOutcome.Miss);
            }
            else if (Time.time - _shotAt > 6f || now.y < -2f || Mathf.Abs(now.x) > 20f)
                Finish("Missed", AttemptOutcome.Miss);

            _previous = now;
        }

        private void CaptureGesture()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                if (OverControls(touchscreen.primaryTouch.position.ReadValue())) return;
                _dragging = true;
                _touchOwned = true;
                _dragStart = touchscreen.primaryTouch.position.ReadValue();
            }
            else if (_dragging && _touchOwned && touchscreen != null &&
                     touchscreen.primaryTouch.press.wasReleasedThisFrame)
            {
                Vector2 end = touchscreen.primaryTouch.position.ReadValue();
                _dragging = false;
                if (touchscreen.primaryTouch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Canceled)
                    TryShoot(_dragStart,end);
            }

            Mouse mouse = Mouse.current;
            if (!_touchOwned && mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (OverControls(mouse.position.ReadValue())) return;
                    _dragging = true;
                    _dragStart = mouse.position.ReadValue();
                }
                else if (_dragging && mouse.leftButton.wasReleasedThisFrame)
                {
                    _dragging = false;
                    TryShoot(_dragStart,mouse.position.ReadValue());
                }
            }
            if (!_dragging) _touchOwned = false;
        }

        private void TryShoot(Vector2 start,Vector2 end)
        {
            if (_state != State.Ready) return;
            if (!FootballActionMath.TryVelocity(start,end,Screen.width,Screen.height,Action,tuning,_powerScale,0,
                out Vector3 velocity))
            {
                _feedback = "Swipe UP further";
                return;
            }

            _pendingVelocity = velocity;
            _state = State.SelectingStrike;
            _touchOwned = false;
            _aim.positionCount = 0;
            _hoverContact = Vector2.zero;
            _hoveringContact = false;
            _feedback = "Choose where to strike the ball";
        }

        private void CaptureStrikeSelection()
        {
            var touch = Touchscreen.current;
            var mouse = Mouse.current;
            Vector2 pointer = touch != null && touch.primaryTouch.press.isPressed
                ? touch.primaryTouch.position.ReadValue() : mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            _hoveringContact = StrikePointOverlay.TryContact(pointer, Screen.width, Screen.height, InterfaceScale, out _hoverContact);
            bool pressed = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
            if (!pressed && mouse != null) pressed = mouse.leftButton.wasPressedThisFrame;
            if (pressed && _hoveringContact) TryChooseStrike(_hoverContact);
        }

        public bool TryChooseStrike(Vector2 contact)
        {
            if (_state != State.SelectingStrike || !StrikePhysics.ValidContact(contact)) return false;
            EnsureActors();
            StrikeSolution strike = StrikePhysics.Calculate(_pendingVelocity, contact, tuning);
            _lastVelocity = strike.velocity;
            _lastAngularVelocity = strike.angularVelocity;
            _lastContact = strike.contact;
            _lastAction = Action;
            _lastSpin = Vector3.Dot(strike.angularVelocity, Vector3.up) / 18;
            _lastOpposition = Opposition;
            _lastBallPosition = _ball.position;
            _lastBallRotation = _ball.rotation;
            _lastReceiverPosition = _actors.Receiver.position;
            _lastDefenderPosition = _actors.Defender.position;
            _lastKeeperPosition = _actors.Keeper.position;
            _hasLastAction = true;
            Launch(strike.velocity, strike.angularVelocity, false);
            return true;
        }

        private void Launch(Vector3 velocity, Vector3 angularVelocity, bool replay)
        {
            _replay = replay;
            if (!replay) _shots++;
            _state = State.InFlight;
            _feedback = replay ? "Replaying last action..." : "Ball in flight...";
            _aim.positionCount = 0;
            _previous = _ball.position;
            _shotAt = Time.time;
            _ball.WakeUp();
            _ball.AddForce(velocity * _ball.mass,ForceMode.Impulse);
            _ball.angularVelocity = angularVelocity;
        }

        private void Finish(string message, AttemptOutcome outcome)
        {
            if (_state != State.InFlight) return;
            _state = State.Resolved;
            _feedback = message;
            _resetAt = Time.time + 2f;
            if (outcome == AttemptOutcome.Received)
            {
                _ball.linearVelocity = Vector3.zero;
                _ball.angularVelocity = Vector3.zero;
                _ball.Sleep();
            }
            var result = new AttemptResult {
                attempt = _shots, action = Action, outcome = outcome, launchVelocity = _lastVelocity,
                spin = _lastSpin, launchAngularVelocity = _lastAngularVelocity, strikePoint = _lastContact,
                endPosition = _ball.position, duration = Time.time - _shotAt, replay = _replay
            };
            _results.Add(result);
            AttemptFinished?.Invoke(result);
        }

        public void ActorContact(SandboxActorRole role)
        {
            if (_state != State.InFlight) return;
            if (role == SandboxActorRole.Receiver && Action != FootballAction.Shot)
                Finish("Pass received", AttemptOutcome.Received);
            else if (role != SandboxActorRole.Receiver)
                Finish(role == SandboxActorRole.Goalkeeper ? "Saved!" : "Blocked!", AttemptOutcome.Blocked);
        }

        public void ChooseAction(FootballAction action)
        {
            if (_state != State.Ready) return;
            Action = action;
            _feedback = action == FootballAction.Shot ? "Swipe up to shoot" : "Swipe toward the blue teammate";
        }

        public void SetOpposition(bool enabled)
        {
            if (_state != State.Ready) return;
            Opposition = enabled;
            EnsureActors();
            _actors.Reset(enabled);
        }

        public void ReplayLastAction()
        {
            if (!_hasLastAction) return;
            EnsureActors();
            Opposition = _lastOpposition;
            ResetShot();
            _ball.position = _lastBallPosition;
            _ball.rotation = _lastBallRotation;
            _actors.Receiver.position = _lastReceiverPosition;
            _actors.Defender.position = _lastDefenderPosition;
            _actors.Keeper.position = _lastKeeperPosition;
            Action = _lastAction;
            Launch(_lastVelocity, _lastAngularVelocity, true);
        }

        private void ResetShot()
        {
            _guiMouseDragging = false;
            _dragging = false;
            _touchOwned = false;
            _state = State.Ready;
            _pendingVelocity = Vector3.zero;
            _replay = false;
            _feedback = Action == FootballAction.Shot ? "Swipe up, then choose ball contact" : "Swipe toward teammate, then choose ball contact";
            _ball.position = _spawn;
            _ball.rotation = Quaternion.identity;
            _ball.linearVelocity = Vector3.zero;
            _ball.angularVelocity = Vector3.zero;
            _ball.Sleep();
            _previous = _spawn;
            EnsureActors();
            _actors.Reset(Opposition);
            if (_aim != null) _aim.positionCount = 0;
        }

        private static bool OverControls(Vector2 position)
        {
            float scale = InterfaceScale;
            return position.y > Screen.height - 230 * scale || position.y < 88 * scale;
        }

        private static float InterfaceScale => Mathf.Clamp(Mathf.Min(Screen.width / 800f, Screen.height / 1000f), .25f, 2.5f);

        private void UpdateAim()
        {
            Vector2 end = _touchOwned && Touchscreen.current != null
                ? Touchscreen.current.primaryTouch.position.ReadValue()
                : Mouse.current != null ? Mouse.current.position.ReadValue() : _dragStart;
            if (!_dragging || !FootballActionMath.TryVelocity(_dragStart, end, Screen.width, Screen.height,
                Action, tuning, _powerScale, 0, out Vector3 velocity))
            { _aim.positionCount = 0; return; }
            _aim.positionCount = 24;
            for (int i = 0; i < 24; i++)
            {
                float t = i * .055f;
                Vector3 pos = _ball.position + velocity * t + Physics.gravity * (.5f * t * t);
                pos.y = Mathf.Max(ShotMath.BallRadius, pos.y);
                _aim.SetPosition(i, pos);
            }
        }

        private void OnDestroy()
        {
            if (_ownsTuning && tuning != null) Destroy(tuning);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            _dragging = false;
            _guiMouseDragging = false;
            _touchOwned = false;
            if (_aim != null) _aim.positionCount = 0;
        }

        private void OnGUI()
        {
            if (_ball == null) return;
            CaptureNativeMouseGesture();
            float scale = InterfaceScale;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1));
            float w = Screen.width/scale, h = Screen.height/scale;

            if (_state == State.SelectingStrike)
            {
                if (StrikePointOverlay.Draw(Screen.width, Screen.height, scale, _hoverContact, _hoveringContact, _pendingVelocity.magnitude))
                    ResetShot();
                GUI.matrix = old;
                return;
            }

            GUI.skin.label.fontSize = 18;
            GUI.skin.button.fontSize = 18;
            GUI.skin.box.fontSize = 18;
            GUI.Box(new Rect(14,14,w-28,210),"PITCHLIFE - P01 SANDBOX");
            GUI.Label(new Rect(30,48,w-60,25),"Attempts: "+_shots+"   Goals: "+_goals+"   "+_feedback);
            float buttonWidth = (w - 70) / 3;
            GUI.enabled = _state == State.Ready;
            if (GUI.Button(new Rect(30,80,buttonWidth,36), Action == FootballAction.Shot ? "[ Shot ]" : "Shot")) ChooseAction(FootballAction.Shot);
            if (GUI.Button(new Rect(35+buttonWidth,80,buttonWidth,36), Action == FootballAction.GroundPass ? "[ Ground pass ]" : "Ground pass")) ChooseAction(FootballAction.GroundPass);
            if (GUI.Button(new Rect(40+2*buttonWidth,80,buttonWidth,36), Action == FootballAction.LoftedPass ? "[ Lofted pass ]" : "Lofted pass")) ChooseAction(FootballAction.LoftedPass);
            GUI.Label(new Rect(30,127,180,26),"Power " + _powerScale.ToString("0.0") + "x");
            _powerScale = GUI.HorizontalSlider(new Rect(220,138,w-260,20), _powerScale, .6f, 1.3f);
            if (GUI.Button(new Rect(30,172,buttonWidth,38), Opposition ? "Opponents: ON" : "Opponents: OFF")) SetOpposition(!Opposition);
            GUI.enabled = true;
            if (GUI.Button(new Rect(35+buttonWidth,172,buttonWidth,38),"RESET (R)")) ResetShot();
            GUI.enabled = _hasLastAction;
            if (GUI.Button(new Rect(40+2*buttonWidth,172,buttonWidth,38),"Replay last")) ReplayLastAction();
            GUI.enabled = true;

            GUI.Box(new Rect(14,h-88,Mathf.Min(w-28,510),68),
                "1. Swipe to aim + power   2. Tap the ball to strike\nCenter = straight   Low = chip   Sides = curve");
            GUI.matrix = old;
        }

        // Native GUI events retain down/up positions even when a very fast drag is coalesced into one input frame.
        private void CaptureNativeMouseGesture()
        {
            Event input = Event.current;
            if (_state != State.Ready) { _guiMouseDragging = false; return; }
            if (input.button != 0) return;
            Vector2 point = new Vector2(input.mousePosition.x, Screen.height - input.mousePosition.y);
            if (input.type == EventType.MouseDown && !OverControls(point))
            {
                _guiMouseStart = point;
                _guiMouseDragging = true;
                _dragging = true;
                _dragStart = point;
            }
            else if (input.type == EventType.MouseUp && _guiMouseDragging)
            {
                _guiMouseDragging = false;
                _dragging = false;
                TryShoot(_guiMouseStart, point);
            }
        }
    }
}
