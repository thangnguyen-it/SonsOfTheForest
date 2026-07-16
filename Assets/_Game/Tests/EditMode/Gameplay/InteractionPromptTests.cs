using NUnit.Framework;
using SonsOfTheForest.Gameplay.Interaction;

namespace SonsOfTheForest.Tests.EditMode.Gameplay
{
    public sealed class InteractionPromptTests
    {
        [Test]
        public void None_HasNoTextAndIsHidden()
        {
            InteractionPrompt prompt = InteractionPrompt.None;

            Assert.That(prompt.HasText, Is.False);
            Assert.That(prompt.Text, Is.Empty);
            Assert.That(prompt.Availability, Is.EqualTo(InteractionAvailability.Hidden));
        }

        [Test]
        public void Press_HasTextPressInputAndAvailableState()
        {
            InteractionPrompt prompt = InteractionPrompt.Press("Use");

            Assert.That(prompt.HasText, Is.True);
            Assert.That(prompt.Text, Is.EqualTo("Use"));
            Assert.That(prompt.InputKind, Is.EqualTo(InteractionInputKind.Press));
            Assert.That(prompt.Availability, Is.EqualTo(InteractionAvailability.Available));
        }

        [Test]
        public void Hold_HasTextHoldInputAndExpectedDuration()
        {
            InteractionPrompt prompt = InteractionPrompt.Hold("Open", 1.5f);

            Assert.That(prompt.HasText, Is.True);
            Assert.That(prompt.Text, Is.EqualTo("Open"));
            Assert.That(prompt.InputKind, Is.EqualTo(InteractionInputKind.Hold));
            Assert.That(prompt.HoldSeconds, Is.EqualTo(1.5f));
            Assert.That(prompt.Availability, Is.EqualTo(InteractionAvailability.Available));
        }
    }
}
