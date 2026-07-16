using NUnit.Framework;
using SonsOfTheForest.Gameplay.Survival;

namespace SonsOfTheForest.Tests.EditMode.Gameplay
{
    public sealed class StatValueTests
    {
        [Test]
        public void NormalizedValue_IsCorrectForNormalRange()
        {
            var value = new StatValue(SurvivalStatKind.Health, 50f, 0f, 100f);

            Assert.That(value.Normalized, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [TestCase(1f, 1f)]
        [TestCase(2f, 1f)]
        public void Normalized_IsSafeWhenMaxIsNotGreaterThanMin(float min, float max)
        {
            var value = new StatValue(SurvivalStatKind.Health, 1f, min, max);

            Assert.That(value.Normalized, Is.EqualTo(0f));
        }

        [Test]
        public void ValuesAndKind_ArePreserved()
        {
            var value = new StatValue(SurvivalStatKind.Temperature, 12f, -20f, 40f);

            Assert.That(value.Kind, Is.EqualTo(SurvivalStatKind.Temperature));
            Assert.That(value.Current, Is.EqualTo(12f));
            Assert.That(value.Min, Is.EqualTo(-20f));
            Assert.That(value.Max, Is.EqualTo(40f));
        }
    }
}
