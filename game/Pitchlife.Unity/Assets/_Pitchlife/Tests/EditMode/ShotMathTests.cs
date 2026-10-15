using NUnit.Framework;
using UnityEngine;

namespace Pitchlife.P01.Tests
{
    public class ShotMathTests
    {
        [Test]
        public void ShortSwipeIsRejected()
        {
            bool ok = ShotMath.TryCalculateVelocity(
                new Vector2(300,300), new Vector2(300,305),800,1600,
                out Vector3 velocity);
            Assert.IsFalse(ok);
            Assert.AreEqual(Vector3.zero,velocity);
        }

        [Test]
        public void VerticalSwipeLaunchesForward()
        {
            bool ok = ShotMath.TryCalculateVelocity(
                new Vector2(300,300),new Vector2(300,800),800,1600,
                out Vector3 velocity);
            Assert.IsTrue(ok);
            Assert.Greater(velocity.z,0f);
            Assert.Greater(velocity.y,0f);
            Assert.That(velocity.x,Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void BallEntirelyThroughGoalMouthScores()
        {
            bool goal = ShotMath.IsGoalCrossing(
                new Vector3(0,.8f,11.7f),new Vector3(0,.8f,12.5f),
                ShotMath.BallRadius,out Vector3 crossing);
            Assert.IsTrue(goal);
            Assert.That(crossing.z,Is.EqualTo(
                ShotMath.GoalLineZ+ShotMath.BallRadius).Within(.0001f));
        }

        [Test]
        public void WideShotDoesNotScore()
        {
            Assert.IsFalse(ShotMath.IsGoalCrossing(
                new Vector3(5,1,11),new Vector3(5,1,13),
                ShotMath.BallRadius,out _));
        }

        [Test]
        public void AboveCrossbarDoesNotScore()
        {
            Assert.IsFalse(ShotMath.IsGoalCrossing(
                new Vector3(0,2.4f,11),new Vector3(0,2.4f,13),
                ShotMath.BallRadius,out _));
        }

        [Test]
        public void BallTravelingBackwardDoesNotScore()
        {
            Assert.IsFalse(ShotMath.IsGoalCrossing(
                new Vector3(0,1,13),new Vector3(0,1,11),
                ShotMath.BallRadius,out _));
        }
    }
}
