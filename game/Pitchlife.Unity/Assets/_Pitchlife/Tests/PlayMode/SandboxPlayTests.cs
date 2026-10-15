using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Pitchlife.P01.Tests
{
    public class SandboxPlayTests
    {
        private GameObject _root;
        private P01FootballSandbox _sandbox;
        private Rigidbody _ball;
        private InputDevice _testDevice;
        private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;
#endif
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            yield return SceneManager.LoadSceneAsync("Assets/_Pitchlife/Scenes/P01FootballSandbox.unity");
            yield return null;
            _root = GameObject.Find("P01 Sandbox");
            _sandbox = _root.GetComponent<P01FootballSandbox>();
            _ball = (Rigidbody)Field("_ball");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_testDevice != null) { InputSystem.RemoveDevice(_testDevice); _testDevice = null; }
            InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
#endif
            Object.Destroy(_root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (camera.name == "Main Camera") Object.Destroy(camera.gameObject);
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.name == "Sun") Object.Destroy(light.gameObject);
            yield return null;
        }

        [Test]
        public void BuildsPhysicalPitchBallAndGoal()
        {
            Assert.NotNull(_ball.GetComponent<SphereCollider>());
            Assert.That(_ball.mass, Is.EqualTo(.43f).Within(.001f));
            Assert.NotNull(_root.transform.Find("Pitch"));
            Assert.NotNull(_root.transform.Find("Left post"));
            Assert.NotNull(_root.transform.Find("Right post"));
            Assert.NotNull(_root.transform.Find("Crossbar"));
            Assert.NotNull(Camera.main);
        }

        [UnityTest]
        public IEnumerator RenderedPitchUsesConfiguredPipeline()
        {
            yield return null;
            Assert.IsInstanceOf<UniversalRenderPipelineAsset>(GraphicsSettings.defaultRenderPipeline);
            var target = new RenderTexture(720, 1280, 24);
            target.Create();
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                Camera.main.aspect = 720f / 1280;
                RenderPipeline.SubmitRenderRequest(Camera.main,
                    new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(720, 1280, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0);
                image.Apply();
                System.IO.Directory.CreateDirectory("Logs");
                System.IO.File.WriteAllBytes("Logs/P01-pitch-preview.png", image.EncodeToPNG());
                Color field = image.GetPixel(360, 600);
                Assert.That(field.g, Is.GreaterThan(field.r), "Pitch should render green rather than missing-shader pink.");
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.Destroy(target);
                if (image != null) Object.Destroy(image);
            }
        }

        [UnityTest]
        public IEnumerator ValidSwipeMovesBallForwardWithPhysics()
        {
            Vector3 before = _ball.position;
            Shoot();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(_ball.position.z, Is.GreaterThan(before.z));
            Assert.That(_ball.linearVelocity.z, Is.GreaterThan(10));
            Assert.That(Field("_shots"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MouseDragReachesTheShootingController()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            _testDevice = mouse;
            InputSystem.QueueStateEvent(mouse, new MouseState {
                position = new Vector2(Screen.width * .5f, Screen.height * .2f)
            }.WithButton(MouseButton.Left));
            yield return null;
            Assert.IsTrue(mouse.leftButton.isPressed, "Injected mouse press was not processed.");
            Assert.That(Field("_dragging"), Is.EqualTo(true), "Controller did not capture the injected mouse press.");
            InputSystem.QueueStateEvent(mouse, new MouseState {
                position = new Vector2(Screen.width * .65f, Screen.height * .5f)
            });
            yield return null;
            Assert.IsTrue(_sandbox.AwaitingStrikePoint, "Releasing the swipe should open contact selection.");
            Assert.That(Field("_shots"), Is.EqualTo(0));
            Rect mouseBall = ContactBallRect();
            Vector2 contactClick = ScreenPoint(mouseBall.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = contactClick }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = contactClick });
            yield return null;
            Assert.That(Field("_shots"), Is.EqualTo(1), "Mouse input at viewport " + Screen.width + "x" + Screen.height + ", dragging=" + Field("_dragging"));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(_ball.linearVelocity.x, Is.GreaterThan(0), "Sideways drag should aim to the right.");
        }

        [UnityTest]
        public IEnumerator TouchSwipeReachesTheShootingController()
        {
            var touch = InputSystem.AddDevice<Touchscreen>();
            _testDevice = touch;
            InputSystem.QueueStateEvent(touch, new TouchState {
                touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began,
                position = new Vector2(Screen.width * .5f, Screen.height * .2f)
            });
            yield return null;
            Assert.IsTrue(touch.primaryTouch.press.isPressed, "Injected touch press was not processed.");
            Assert.That(Field("_dragging"), Is.EqualTo(true), "Controller did not capture the injected touch press.");
            InputSystem.QueueStateEvent(touch, new TouchState {
                touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended,
                position = new Vector2(Screen.width * .35f, Screen.height * .5f)
            });
            yield return null;
            Assert.IsTrue(_sandbox.AwaitingStrikePoint, "Releasing the swipe should open contact selection.");
            Assert.That(Field("_shots"), Is.EqualTo(0));
            Vector2 contactTap = ScreenPoint(ContactBallRect().center);
            InputSystem.QueueStateEvent(touch, new TouchState {
                touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = contactTap
            });
            yield return null;
            InputSystem.QueueStateEvent(touch, new TouchState {
                touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = contactTap
            });
            yield return null;
            Assert.That(Field("_shots"), Is.EqualTo(1), "Touch input at viewport " + Screen.width + "x" + Screen.height + ", dragging=" + Field("_dragging"));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(_ball.linearVelocity.x, Is.LessThan(0), "Sideways swipe should aim to the left.");
        }

        [Test]
        public void ShortSwipeDoesNotCountAsShot()
        {
            Call("TryShoot", Vector2.zero, new Vector2(0, Screen.height * .01f));
            Assert.That(Field("_shots"), Is.EqualTo(0));
            Assert.That(Field("_state").ToString(), Is.EqualTo("Ready"));
        }

        [Test]
        public void GoalIsCountedOnceAcrossRepeatedPhysicsTicks()
        {
            Shoot();
            Set("_previous", new Vector3(0, 1, 11));
            _ball.position = new Vector3(0, 1, 13);
            Call("FixedUpdate");
            Call("FixedUpdate");
            Assert.That(Field("_goals"), Is.EqualTo(1));
            Assert.That(Field("_state").ToString(), Is.EqualTo("Resolved"));
            Assert.That(Field("_feedback"), Is.EqualTo("GOAL!"));
        }

        [Test]
        public void WideCrossingResolvesAsMissOnly()
        {
            Shoot();
            Set("_previous", new Vector3(5, 1, 11));
            _ball.position = new Vector3(5, 1, 13);
            Call("FixedUpdate");
            Assert.That(Field("_goals"), Is.EqualTo(0));
            Assert.That(Field("_feedback"), Is.EqualTo("Missed"));
        }

        [Test]
        public void ResetRestoresPositionAndClearsMomentum()
        {
            Vector3 spawn = _ball.position;
            Shoot();
            _ball.position = new Vector3(4, 2, 9);
            _ball.angularVelocity = Vector3.one;
            Call("ResetShot");
            Assert.That(Vector3.Distance(_ball.position, spawn), Is.LessThan(.001f));
            Assert.That(_ball.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(_ball.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(Field("_state").ToString(), Is.EqualTo("Ready"));
        }

        [UnityTest]
        public IEnumerator KeeperReactsAfterDelayAndMovesWithoutTeleporting()
        {
            var actors = (SandboxActors)Field("_actors");
            Vector3 start = actors.Keeper.position;
            _ball.position = new Vector3(5, .3f, 0);
            _ball.linearVelocity = new Vector3(0, 0, 10);
            actors.Tick(_ball, FootballAction.Shot, true, 0, Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(actors.Keeper.position, start), Is.LessThan(.001f));
            actors.Tick(_ball, FootballAction.Shot, true, 1, Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
            float distance = Vector3.Distance(actors.Keeper.position, start);
            Assert.That(distance, Is.GreaterThan(0));
            Assert.That(distance, Is.LessThanOrEqualTo(_sandbox.tuning.keeperSpeed * Time.fixedDeltaTime + .002f));
        }

        [UnityTest]
        public IEnumerator ReceiverRunsTowardALateralPass()
        {
            var actors = (SandboxActors)Field("_actors");
            _sandbox.SetOpposition(false);
            _ball.position = new Vector3(7, .3f, 1);
            _ball.linearVelocity = new Vector3(0, 0, 2);
            Vector3 start = actors.Receiver.position;
            actors.Tick(_ball, FootballAction.GroundPass, true, 1, Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
            Assert.That(actors.Receiver.position.x, Is.GreaterThan(start.x));
            Assert.That(Vector3.Distance(actors.Receiver.position, start),
                Is.LessThanOrEqualTo(_sandbox.tuning.receiverSpeed * Time.fixedDeltaTime + .002f));
        }

        [Test]
        public void RepeatedContactsPublishOnlyOneOutcome()
        {
            int events = 0;
            _sandbox.AttemptFinished += result => events++;
            Shoot();
            _sandbox.ActorContact(SandboxActorRole.Goalkeeper);
            _sandbox.ActorContact(SandboxActorRole.Defender);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(_sandbox.Results.Count, Is.EqualTo(1));
            Assert.That(_sandbox.Results[0].outcome, Is.EqualTo(AttemptOutcome.Blocked));
        }

        [UnityTest]
        public IEnumerator GroundPassCanActuallyReachTheReceiver()
        {
            _sandbox.SetOpposition(false);
            _sandbox.ChooseAction(FootballAction.GroundPass);
            Call("TryShoot", new Vector2(Screen.width * .5f, Screen.height * .2f),
                new Vector2(Screen.width * .78f, Screen.height * .42f));
            Assert.IsTrue(_sandbox.TryChooseStrike(Vector2.zero));
            for (int i = 0; i < 340 && _sandbox.Results.Count == 0; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(_sandbox.Results.Count, Is.EqualTo(1));
            Assert.That(_sandbox.Results[0].outcome, Is.EqualTo(AttemptOutcome.Received));
        }

        [UnityTest]
        public IEnumerator SpinCurvesAnInitiallyStraightShot()
        {
            StageShot();
            Assert.IsTrue(_sandbox.TryChooseStrike(new Vector2(-.75f, 0)));
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.That(_ball.linearVelocity.x, Is.GreaterThan(.1f));
        }

        [Test]
        public void ReplayRestoresInitialPositionsAndDoesNotInflateAttemptCount()
        {
            var actors = (SandboxActors)Field("_actors");
            Vector3 spawn = _ball.position, keeperSpawn = actors.Keeper.position;
            Shoot();
            _ball.position = new Vector3(4, 2, 9);
            actors.Keeper.position += Vector3.right * 2;
            _sandbox.ReplayLastAction();
            Assert.That(Vector3.Distance(_ball.position, spawn), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(actors.Keeper.position, keeperSpawn), Is.LessThan(.001f));
            Assert.That(Field("_shots"), Is.EqualTo(1));
            Assert.That(Field("_replay"), Is.EqualTo(true));
        }

        [Test]
        public void SwipeWaitsForContactWithoutLaunchingOrCounting()
        {
            StageShot();
            Assert.IsTrue(_sandbox.AwaitingStrikePoint);
            Assert.That(Field("_shots"), Is.EqualTo(0));
            Assert.That(_ball.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.IsFalse(_sandbox.TryChooseStrike(new Vector2(1, 1)));
            Assert.IsTrue(_sandbox.AwaitingStrikePoint);
            Call("ResetShot");
            Assert.IsFalse(_sandbox.AwaitingStrikePoint);
            Assert.IsFalse(_sandbox.TryChooseStrike(Vector2.zero));
            Assert.That(Field("_shots"), Is.EqualTo(0));
        }

        [Test]
        public void ContactIsCommittedOnceAndReplayRestoresItsSpin()
        {
            StageShot();
            Assert.IsTrue(_sandbox.TryChooseStrike(new Vector2(-.5f, -.5f)));
            Vector3 spin = _ball.angularVelocity;
            Assert.IsFalse(_sandbox.TryChooseStrike(Vector2.zero));
            _sandbox.ActorContact(SandboxActorRole.Goalkeeper);
            Assert.That(_sandbox.Results[0].strikePoint, Is.EqualTo(new Vector2(-.5f, -.5f)));
            Assert.That(_sandbox.Results[0].launchAngularVelocity, Is.EqualTo(spin));
            _sandbox.ReplayLastAction();
            Assert.That(Vector3.Distance(_ball.angularVelocity, spin), Is.LessThan(.001f));
            Assert.That(Field("_shots"), Is.EqualTo(1));
        }

        [Test]
        public void LostActorManagerRebindsBeforeContactWithoutDuplicatingOrMovingPlayers()
        {
            var actors = (SandboxActors)Field("_actors");
            Rigidbody receiver = actors.Receiver;
            receiver.position += Vector3.left;
            Vector3 before = receiver.position;
            StageShot();
            Set("_actors", null); // A managed, nonserialized field is lost during Unity's script reload.
            Assert.IsTrue(_sandbox.TryChooseStrike(Vector2.zero));
            var restored = (SandboxActors)Field("_actors");
            Assert.That(restored.Receiver, Is.SameAs(receiver));
            Assert.That(Vector3.Distance(restored.Receiver.position, before), Is.LessThan(.001f));
            Assert.That(_root.GetComponentsInChildren<SandboxActor>(true).Length, Is.EqualTo(3));
            Assert.That(Field("_state").ToString(), Is.EqualTo("InFlight"));
        }

        [Test]
        public void ReenableRestoresActorReferencesAndKeepsDisabledOpponentsDisabled()
        {
            _sandbox.SetOpposition(false);
            var actors = (SandboxActors)Field("_actors");
            Set("_actors", null);
            Call("OnEnable");
            var restored = (SandboxActors)Field("_actors");
            Assert.That(restored.Keeper, Is.SameAs(actors.Keeper));
            Assert.IsFalse(restored.Keeper.gameObject.activeSelf);
            Shoot();
            Set("_actors", null);
            Assert.DoesNotThrow(() => Call("FixedUpdate"));
        }

        private void StageShot() => Call("TryShoot", new Vector2(Screen.width * .5f, Screen.height * .2f),
            new Vector2(Screen.width * .5f, Screen.height * .5f));
        private void Shoot() { StageShot(); Assert.IsTrue(_sandbox.TryChooseStrike(Vector2.zero)); }
        private static float UiScale => Mathf.Clamp(Mathf.Min(Screen.width / 800f, Screen.height / 1000f), .25f, 2.5f);
        private static Rect ContactBallRect() => StrikePointOverlay.BallRect(Screen.width, Screen.height, UiScale);
        private static Vector2 ScreenPoint(Vector2 gui) => new Vector2(gui.x * UiScale, Screen.height - gui.y * UiScale);
        private object Field(string name) => typeof(P01FootballSandbox).GetField(name, Private).GetValue(_sandbox);
        private void Set(string name, object value) => typeof(P01FootballSandbox).GetField(name, Private).SetValue(_sandbox, value);
        private void Call(string name, params object[] args) => typeof(P01FootballSandbox).GetMethod(name, Private).Invoke(_sandbox, args);
    }
}
