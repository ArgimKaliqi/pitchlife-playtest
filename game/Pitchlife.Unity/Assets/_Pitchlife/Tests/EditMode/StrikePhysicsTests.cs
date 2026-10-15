using NUnit.Framework;
using UnityEngine;

namespace Pitchlife.P01.Tests
{
    public class StrikePhysicsTests
    {
        private FootballTuning _tuning;
        private readonly Vector3 _aim = new Vector3(0, 4, 20);
        [SetUp] public void Setup() => _tuning = ScriptableObject.CreateInstance<FootballTuning>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(_tuning);

        [Test] public void CenterPreservesAimAndHasNoSidespin()
        {
            var kick = StrikePhysics.Calculate(_aim, Vector2.zero, _tuning);
            Assert.That(Vector3.Distance(kick.velocity, _aim), Is.LessThan(.001f));
            Assert.That(kick.angularVelocity.magnitude, Is.LessThan(.001f));
        }

        [Test] public void LowerContactProducesAChipAndBackspin()
        {
            var kick = StrikePhysics.Calculate(_aim, new Vector2(0, -.7f), _tuning);
            Assert.That(kick.velocity.y, Is.GreaterThan(_aim.y));
            Assert.That(kick.angularVelocity.x, Is.LessThan(0));
            Assert.That(StrikePhysics.MagnusAcceleration(kick.velocity, kick.angularVelocity, _tuning).y, Is.GreaterThan(0));
        }

        [Test] public void UpperContactProducesALowerDippingShot()
        {
            var kick = StrikePhysics.Calculate(_aim, new Vector2(0, .7f), _tuning);
            Assert.That(kick.velocity.y, Is.LessThan(_aim.y));
            Assert.That(kick.angularVelocity.x, Is.GreaterThan(0));
            Assert.That(StrikePhysics.MagnusAcceleration(kick.velocity, kick.angularVelocity, _tuning).y, Is.LessThan(0));
        }

        [Test] public void MirroredSideContactsProduceOppositeCurves()
        {
            var left = StrikePhysics.Calculate(_aim, new Vector2(-.7f, 0), _tuning);
            var right = StrikePhysics.Calculate(_aim, new Vector2(.7f, 0), _tuning);
            Assert.That(left.angularVelocity.y, Is.GreaterThan(0));
            Assert.That(right.angularVelocity.y, Is.LessThan(0));
            Assert.That(left.angularVelocity.y, Is.EqualTo(-right.angularVelocity.y).Within(.001f));
            Assert.That(StrikePhysics.MagnusAcceleration(left.velocity, left.angularVelocity, _tuning).x, Is.GreaterThan(0));
        }

        [Test] public void AerodynamicAccelerationStaysBounded()
        {
            Assert.That(StrikePhysics.MagnusAcceleration(Vector3.forward * 100, Vector3.up * 200, _tuning).magnitude,
                Is.LessThanOrEqualTo(_tuning.maxMagnusAcceleration + .001f));
        }

        [TestCase(720, 1280)] [TestCase(1920, 1080)] [TestCase(640, 480)]
        public void ScreenContactMappingHandlesPortraitAndLandscape(float width, float height)
        {
            float scale = Mathf.Clamp(Mathf.Min(width / 800, height / 1000), .25f, 2.5f);
            Rect ball = StrikePointOverlay.BallRect(width, height, scale);
            Vector2 center = new Vector2(ball.center.x * scale, height - ball.center.y * scale);
            Assert.IsTrue(StrikePointOverlay.TryContact(center, width, height, scale, out Vector2 point));
            Assert.That(point.magnitude, Is.LessThan(.001f));
            Vector2 low = center + Vector2.down * ball.height * scale * .35f;
            Assert.IsTrue(StrikePointOverlay.TryContact(low, width, height, scale, out point));
            Assert.That(point.y, Is.EqualTo(-.7f).Within(.001f));
            Assert.IsFalse(StrikePointOverlay.TryContact(Vector2.zero, width, height, scale, out _));
        }

        [Test] public void InvalidContactCannotBeConfirmed()
        {
            Assert.IsFalse(StrikePhysics.ValidContact(new Vector2(float.NaN, 0)));
            Assert.IsFalse(StrikePhysics.ValidContact(new Vector2(0, float.PositiveInfinity)));
            Assert.IsFalse(StrikePhysics.ValidContact(new Vector2(.9f, .9f)));
        }
    }
}
