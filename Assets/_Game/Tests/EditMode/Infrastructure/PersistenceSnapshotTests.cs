using NUnit.Framework;
using SonsOfTheForest.Infrastructure.Persistence;

namespace SonsOfTheForest.Tests.EditMode.Infrastructure
{
    public sealed class PersistenceSnapshotTests
    {
        [Test]
        public void CompleteSnapshot_IsValid()
        {
            var snapshot = new PersistenceSnapshot(
                new PersistentId("world.camp"),
                PersistenceScope.World,
                "1",
                "{\"value\":1}");

            Assert.That(snapshot.IsValid, Is.True);
        }

        [Test]
        public void InvalidId_MakesSnapshotInvalid()
        {
            var snapshot = new PersistenceSnapshot(
                new PersistentId(null),
                PersistenceScope.World,
                "1",
                "{}");

            Assert.That(snapshot.IsValid, Is.False);
        }

        [Test]
        public void ContractVersionAndJson_ArePreservedSafely()
        {
            var populated = new PersistenceSnapshot(
                new PersistentId("world.camp"),
                PersistenceScope.World,
                "2",
                "{\"ready\":true}");
            var empty = new PersistenceSnapshot(
                new PersistentId("world.camp"),
                PersistenceScope.World,
                null,
                null);

            Assert.That(populated.ContractVersion, Is.EqualTo("2"));
            Assert.That(populated.Json, Is.EqualTo("{\"ready\":true}"));
            Assert.That(empty.ContractVersion, Is.Empty);
            Assert.That(empty.Json, Is.Empty);
        }
    }
}
