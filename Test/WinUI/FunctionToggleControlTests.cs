// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS
namespace Moba.Test.WinUI;

using Moba.MOBAflow.Controls;

/// <summary>
/// A function tile without label or symbol must still be named for Narrator; the name composition is covered
/// here because WinUI controls cannot be created without a XAML runtime.
/// </summary>
[TestFixture]
internal sealed class FunctionToggleControlTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void BuildAutomationName_WithoutDescription_NamesTheFunctionSlot(string? description)
    {
        Assert.That(FunctionToggleControl.BuildAutomationName(3, description), Is.EqualTo("Function F3"));
    }

    [Test]
    public void BuildAutomationName_WithDescription_AppendsDescription()
    {
        Assert.That(FunctionToggleControl.BuildAutomationName(0, " Headlight "), Is.EqualTo("Function F0, Headlight"));
    }
}
#endif
