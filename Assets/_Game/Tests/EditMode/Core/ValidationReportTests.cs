using NUnit.Framework;
using SonsOfTheForest.Core;

namespace SonsOfTheForest.Tests.EditMode.Core
{
    public sealed class ValidationReportTests
    {
        [Test]
        public void NewReport_IsValid()
        {
            var report = new ValidationReport();

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.HasErrors, Is.False);
            Assert.That(report.Issues, Is.Empty);
        }

        [Test]
        public void Info_DoesNotMakeReportInvalid()
        {
            var report = new ValidationReport();

            report.AddInfo("info", "message");

            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Warning_DoesNotMakeReportInvalid()
        {
            var report = new ValidationReport();

            report.AddWarning("warning", "message");

            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Error_MakesReportInvalid()
        {
            var report = new ValidationReport();

            report.AddError("error", "message");

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void Issues_AreRecordedInOrder()
        {
            var report = new ValidationReport();

            report.AddInfo("first", "one");
            report.AddWarning("second", "two");
            report.AddError("third", "three");

            Assert.That(report.Issues.Count, Is.EqualTo(3));
            Assert.That(report.Issues[0].Code, Is.EqualTo("first"));
            Assert.That(report.Issues[1].Code, Is.EqualTo("second"));
            Assert.That(report.Issues[2].Code, Is.EqualTo("third"));
        }
    }
}
