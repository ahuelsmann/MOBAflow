// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Service;

[TestFixture]
internal sealed class OperatorShortcutsTests
{
    [TestCase(false, false, false, false)]
    [TestCase(false, false, false, true)]
    [TestCase(true, true, true, true)]
    public void Pause_AlwaysStops(bool control, bool alt, bool shift, bool focusUsesSpace)
    {
        Assert.That(
            OperatorShortcuts.Resolve(OperatorShortcutKey.Pause, control, alt, shift, focusUsesSpace),
            Is.EqualTo(OperatorShortcutAction.EmergencyStop));
    }

    [Test]
    public void Space_StopsWhileTheFocusedElementDoesNotUseSpace()
    {
        Assert.That(
            OperatorShortcuts.Resolve(OperatorShortcutKey.Space, false, false, false, focusUsesSpace: false),
            Is.EqualTo(OperatorShortcutAction.EmergencyStop));
    }

    [TestCase(false, false, false, true)]
    [TestCase(true, false, false, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, false, true, false)]
    public void Space_KeepsItsNormalMeaningForSpaceControlsAndWithModifiers(bool control, bool alt, bool shift, bool focusUsesSpace)
    {
        Assert.That(
            OperatorShortcuts.Resolve(OperatorShortcutKey.Space, control, alt, shift, focusUsesSpace),
            Is.EqualTo(OperatorShortcutAction.None));
    }

    [TestCase(OperatorShortcutKey.P, true, false, false)]
    [TestCase(OperatorShortcutKey.F10, false, false, false)]
    public void CtrlPAndF10_ToggleTrackPowerEvenWhileATextInputHasFocus(OperatorShortcutKey key, bool control, bool alt, bool shift)
    {
        Assert.That(
            OperatorShortcuts.Resolve(key, control, alt, shift, focusUsesSpace: true),
            Is.EqualTo(OperatorShortcutAction.ToggleTrackPower));
    }

    [TestCase(OperatorShortcutKey.P, false, false, false)]
    [TestCase(OperatorShortcutKey.P, true, true, false)]
    [TestCase(OperatorShortcutKey.P, true, false, true)]
    [TestCase(OperatorShortcutKey.F10, false, false, true)]
    [TestCase(OperatorShortcutKey.F10, true, false, false)]
    [TestCase(OperatorShortcutKey.F10, false, true, false)]
    [TestCase(OperatorShortcutKey.Other, true, false, false)]
    public void OtherCombinations_AreNoShortcut(OperatorShortcutKey key, bool control, bool alt, bool shift)
    {
        Assert.That(OperatorShortcuts.Resolve(key, control, alt, shift, focusUsesSpace: false), Is.EqualTo(OperatorShortcutAction.None));
    }
}
