using NUnit.Framework;
using SonsOfTheForest.Core;

namespace SonsOfTheForest.Tests.EditMode.Core
{
    public sealed class GameResultTests
    {
        [Test]
        public void Success_HasExpectedState()
        {
            GameResult result = GameResult.Success();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failed, Is.False);
            Assert.That(result.Error, Is.Empty);
        }

        [Test]
        public void Failure_HasExpectedStateAndError()
        {
            const string error = "expected failure";

            GameResult result = GameResult.Failure(error);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Error, Is.EqualTo(error));
        }

        [Test]
        public void GenericSuccess_CarriesValue()
        {
            GameResult<int> result = GameResult<int>.Success(42);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failed, Is.False);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.Error, Is.Empty);
        }

        [Test]
        public void GenericFailure_DoesNotReportSuccess()
        {
            GameResult<int> result = GameResult<int>.Failure("expected");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Error, Is.EqualTo("expected"));
        }
    }
}
