using Moba.SharedUI.ViewModel;
using System.ComponentModel;

namespace Moba.Test.SharedUI;

public class PropertyViewModelTests
{
    private class TestTarget : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _text = "hello";
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value) { _text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); }
            }
        }

        private bool _flag;
        public bool Flag
        {
            get => _flag;
            set
            {
                if (_flag != value) { _flag = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Flag))); }
            }
        }

        public TestEnum Mode { get; set; } = TestEnum.A;

        public Workflow? RefWorkflow { get; set; }
    }

    private enum TestEnum { A, B, C }

    private class Workflow { public string? Name { get; set; } }

    [Test]
    public void Value_SetString_UpdatesTarget()
    {
        var target = new TestTarget();
        var prop = typeof(TestTarget).GetProperty("Text")!;
        var vm = new PropertyViewModel(prop, target);

        vm.Value = "world";

        Assert.That(target.Text, Is.EqualTo("world"));
        Assert.That(vm.Value, Is.EqualTo("world"));
    }

    [Test]
    public void BoolValue_TogglesTarget()
    {
        var target = new TestTarget();
        var prop = typeof(TestTarget).GetProperty("Flag")!;
        var vm = new PropertyViewModel(prop, target);

        vm.BoolValue = true;
        Assert.That(target.Flag, Is.True);

        vm.BoolValue = false;
        Assert.That(target.Flag, Is.False);
    }

    [Test]
    public void EnumValue_UpdatesTarget()
    {
        var target = new TestTarget();
        var prop = typeof(TestTarget).GetProperty("Mode")!;
        var vm = new PropertyViewModel(prop, target);

        vm.EnumValue = TestEnum.B;
        Assert.That(target.Mode, Is.EqualTo(TestEnum.B));
    }

    [Test]
    public void ReferenceValueName_SetsReferenceByName()
    {
        var target = new TestTarget();
        var prop = typeof(TestTarget).GetProperty("RefWorkflow")!;
        var vm = new PropertyViewModel(prop, target);

        var w1 = new Workflow { Name = "One" };
        var w2 = new Workflow { Name = "Two" };
        vm.ReferenceValues = new object?[] { null, w1, w2 };

        vm.ReferenceValueName = "Two";

        Assert.That(target.RefWorkflow, Is.SameAs(w2));
        Assert.That(vm.ReferenceValueName, Is.EqualTo("Two"));
    }
}
