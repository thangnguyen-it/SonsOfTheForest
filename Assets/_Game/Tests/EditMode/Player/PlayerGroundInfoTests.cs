using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerGroundInfoTests
    {
        [Test]
        public void Airborne_IsNotGroundedOrWalkable()
        {
            PlayerGroundInfo info = PlayerGroundInfo.Airborne;

            Assert.That(info.IsGrounded, Is.False);
            Assert.That(info.IsWalkable, Is.False);
        }

        [Test]
        public void Constructor_PreservesGroundData()
        {
            var normal = new Vector3(0f, 0.8f, 0.2f).normalized;
            var info = new PlayerGroundInfo(
                true,
                normal,
                25f,
                true);

            Assert.That(info.IsGrounded, Is.True);
            Assert.That(info.Normal, Is.EqualTo(normal));
            Assert.That(info.SlopeAngle, Is.EqualTo(25f));
            Assert.That(info.IsWalkable, Is.True);
        }
    }
}
