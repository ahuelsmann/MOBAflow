// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Model;
using Moba.SharedUI.Interface; // ✅ Factory interfaces

[TestFixture]
public class JourneyViewModelFactoryTests
{
    private class TestFactory : IJourneyViewModelFactory
    {
        public Moba.SharedUI.ViewModel.JourneyViewModel Create(Journey model)
            => new Moba.SharedUI.ViewModel.JourneyViewModel(model);
    }

    [Test]
    public void Create_ReturnsViewModel()
    {
        var model = new Journey();
        var factory = new TestFactory();

        var vm = factory.Create(model);

        Assert.That(vm, Is.Not.Null);
        Assert.That(vm.Model, Is.EqualTo(model));
    }
}
