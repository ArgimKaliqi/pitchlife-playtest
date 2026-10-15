using NUnit.Framework;
using UnityEngine;

namespace Pitchlife.P01.Tests
{
    public class FootballActionTests
    {
        private FootballTuning _tuning;
        [SetUp] public void Setup() => _tuning = ScriptableObject.CreateInstance<FootballTuning>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(_tuning);

        private Vector3 Velocity(FootballAction action, float power = 1, float elevation = 0)
        {
            Assert.IsTrue(FootballActionMath.TryVelocity(new Vector2(400, 200), new Vector2(400, 700),
                800, 1600, action, _tuning, power, elevation, out Vector3 velocity));
            return velocity;
        }

        [Test] public void LoftedPassHasHigherArcThanGroundPass()
        {
            Vector3 ground = Velocity(FootballAction.GroundPass), lofted = Velocity(FootballAction.LoftedPass);
            Assert.That(lofted.y / lofted.z, Is.GreaterThan(ground.y / ground.z + .3f));
        }
        [Test] public void PowerControlChangesSpeedWithoutChangingAim()
        {
            Vector3 low = Velocity(FootballAction.Shot, .6f), high = Velocity(FootballAction.Shot, 1.3f);
            Assert.That(high.magnitude, Is.GreaterThan(low.magnitude * 2));
            Assert.That(Vector3.Angle(low, high), Is.LessThan(.01f));
        }
        [Test] public void ElevationControlRaisesTheShot()
        {
            Assert.That(Velocity(FootballAction.Shot, 1, 15).y, Is.GreaterThan(Velocity(FootballAction.Shot).y));
        }
        [TestCase(0, 1600)] [TestCase(800, 0)]
        public void InvalidViewportCannotLaunch(float width, float height)
        {
            Assert.IsFalse(FootballActionMath.TryVelocity(Vector2.zero, new Vector2(400, 800), width, height,
                FootballAction.GroundPass, _tuning, 1, 0, out _));
        }
    }
}
