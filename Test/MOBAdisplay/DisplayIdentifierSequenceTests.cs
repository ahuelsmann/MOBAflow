// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

[TestFixture]
[Category("Unit")]
internal sealed class DisplayIdentifierSequenceTests
{
    [Test]
    public void Next_Should_AdvanceAfterSeed()
    {
        // Arrange
        var sequence = new DisplayIdentifierSequence(41);

        // Act
        var first = sequence.Next();
        var second = sequence.Next();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(42));
            Assert.That(second, Is.EqualTo(43));
        }
    }

    [Test]
    public void Next_Should_SkipZero_When_SequenceWraps()
    {
        // Arrange
        var sequence = new DisplayIdentifierSequence(uint.MaxValue);

        // Act
        var identifier = sequence.Next();

        // Assert
        Assert.That(identifier, Is.EqualTo(1));
    }

    [Test]
    public async Task Next_Should_ReturnDistinctValues_When_CalledConcurrently()
    {
        // Arrange
        var sequence = new DisplayIdentifierSequence();
        var callers = Enumerable.Range(0, 256)
            .Select(_ => Task.Run(sequence.Next))
            .ToArray();

        // Act
        var identifiers = await Task.WhenAll(callers);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(identifiers, Has.None.EqualTo(0));
            Assert.That(identifiers.Distinct().Count(), Is.EqualTo(identifiers.Length));
        }
    }

    [Test]
    public void Next_Should_UseOneProcessWideStream_For_DefaultSequences()
    {
        // Arrange
        var firstSequence = new DisplayIdentifierSequence();
        var secondSequence = new DisplayIdentifierSequence();

        // Act
        var identifiers = new[]
        {
            firstSequence.Next(),
            firstSequence.Next(),
            secondSequence.Next(),
            firstSequence.Next(),
            secondSequence.Next(),
        };

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(identifiers, Has.None.Zero);
            Assert.That(identifiers.Distinct().Count(), Is.EqualTo(identifiers.Length));
        }
    }
}