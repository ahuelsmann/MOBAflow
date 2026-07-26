// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using System.Text.RegularExpressions;

using global::Moba.Common.Multiplex;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.ViewModel;
using global::Moq;

internal sealed partial class TrackLayoutArchitectureTests
{
    private SignalBoxPropertiesViewModel _viewModel = null!;

    [SetUp]
    public void SetUpSignalBoxPropertiesViewModel()
    {
        var catalog = new Mock<ISignalArticleCatalog>();
        catalog
            .Setup(instance => instance.GetMainSignalOptions(It.IsAny<string>()))
            .Returns(
            [
                new("4046", "4046 - Ks exit signal"),
                new("4042", "4042 - Ks entry signal")
            ]);
        catalog
            .Setup(instance => instance.GetDistantSignalOptions(It.IsAny<string>()))
            .Returns([new("4040", "4040 - Ks distant signal")]);
        _viewModel = new SignalBoxPropertiesViewModel(
            catalog.Object,
            new DefaultMultiplexerProvider());
    }

    [Test]
    public void SelectedElementProjectsElementSpecificEditorState()
    {
        var detector = new SbDetector
        {
            Name = "B1",
            FeedbackAddress = 17
        };

        _viewModel.SelectedElement = detector;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.HasSelection, Is.True);
            Assert.That(_viewModel.IsAddressVisible, Is.True);
            Assert.That(_viewModel.IsSignalSelected, Is.False);
            Assert.That(_viewModel.IsSwitchSelected, Is.False);
            Assert.That(_viewModel.ElementName, Is.EqualTo("B1"));
            Assert.That(_viewModel.ElementAddress, Is.EqualTo(17));
            Assert.That(_viewModel.AddressHeader, Is.EqualTo("Feedback address"));
        }
    }

    [Test]
    public void ElementEditingMutatesModelAndRequestsPersistence()
    {
        var element = new SbSwitch
        {
            Name = "W1",
            Address = 3
        };
        var changes = new List<SignalBoxPropertyChangeEventArgs>();
        _viewModel.SelectedElement = element;
        _viewModel.ElementChanged += (_, args) => changes.Add(args);

        _viewModel.SetElementName("W2");
        _viewModel.SetElementAddress(4);
        _viewModel.RotateCommand.Execute(90);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(element.Name, Is.EqualTo("W2"));
            Assert.That(element.Address, Is.EqualTo(4));
            Assert.That(element.Rotation, Is.EqualTo(90));
            Assert.That(changes, Has.Count.EqualTo(3));
            Assert.That(changes.All(change => change.RequiresPersistence), Is.True);
        }
    }

    [Test]
    public void InvalidAddressAndRotationDoNotMutateOrPublish()
    {
        var element = new SbSwitch
        {
            Address = 3,
            Rotation = 90
        };
        var changeCount = 0;
        _viewModel.SelectedElement = element;
        _viewModel.ElementChanged += (_, _) => changeCount++;

        _viewModel.SetElementAddress(0);
        _viewModel.SetElementAddress(2049);
        _viewModel.RotateCommand.Execute(45);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(element.Address, Is.EqualTo(3));
            Assert.That(element.Rotation, Is.EqualTo(90));
            Assert.That(changeCount, Is.Zero);
        }
    }

    [Test]
    public void SelectMultiplexerInitializesArticlesAndSupportedAspects()
    {
        var signal = new SbSignal();
        var changes = new List<SignalBoxPropertyChangeEventArgs>();
        _viewModel.SelectedElement = signal;
        _viewModel.ElementChanged += (_, args) => changes.Add(args);

        _viewModel.SelectMultiplexer("5229");
        var change = changes.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signal.IsMultiplexed, Is.True);
            Assert.That(signal.MultiplexerArticleNumber, Is.EqualTo("5229"));
            Assert.That(signal.MainSignalArticleNumber, Is.EqualTo("4046"));
            Assert.That(signal.DistantSignalArticleNumber, Is.EqualTo("4040"));
            Assert.That(_viewModel.MainSignalOptions, Has.Count.EqualTo(2));
            Assert.That(_viewModel.DistantSignalOptions, Has.Count.EqualTo(1));
            Assert.That(_viewModel.IsSpeedIndicatorVisible, Is.True);
            Assert.That(_viewModel.IsAspectAvailable(SignalAspect.Zs1), Is.True);
            Assert.That(_viewModel.IsAspectAvailable(SignalAspect.Zs7), Is.False);
            Assert.That(change.RequiresVisualRefresh, Is.True);
            Assert.That(change.RequiresPersistence, Is.True);
        }
    }

    [Test]
    public void ResetMultiplexerClearsConfigurationAndRestoresAspectAvailability()
    {
        var signal = new SbSignal
        {
            IsMultiplexed = true,
            MultiplexerArticleNumber = "5229",
            MainSignalArticleNumber = "4046",
            DistantSignalArticleNumber = "4040"
        };
        _viewModel.SelectedElement = signal;

        _viewModel.SelectMultiplexer(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signal.IsMultiplexed, Is.False);
            Assert.That(signal.MultiplexerArticleNumber, Is.Null);
            Assert.That(signal.MainSignalArticleNumber, Is.Null);
            Assert.That(signal.DistantSignalArticleNumber, Is.Null);
            Assert.That(_viewModel.MainSignalOptions, Is.Empty);
            Assert.That(_viewModel.DistantSignalOptions, Is.Empty);
            Assert.That(
                Enum.GetValues<SignalAspect>().All(_viewModel.IsAspectAvailable),
                Is.True);
        }
    }

    [Test]
    public void SignalConfigurationUpdatesArticlesAddressAndSpeedIndicators()
    {
        var signal = new SbSignal();
        _viewModel.SelectedElement = signal;
        _viewModel.SelectMultiplexer("5229");

        _viewModel.SelectMainSignalArticle("4042");
        _viewModel.SelectDistantSignalArticle("4040");
        _viewModel.SetBaseAddress(101);
        _viewModel.SetTopSpeedIndicator(8);
        _viewModel.SetBottomSpeedIndicator(6);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signal.MainSignalArticleNumber, Is.EqualTo("4042"));
            Assert.That(signal.DistantSignalArticleNumber, Is.EqualTo("4040"));
            Assert.That(signal.BaseAddress, Is.EqualTo(101));
            Assert.That(signal.TopSpeedIndicator, Is.EqualTo("8"));
            Assert.That(signal.BottomSpeedIndicator, Is.EqualTo("6"));
            Assert.That(_viewModel.TopSpeedIndicator, Is.EqualTo(8));
            Assert.That(_viewModel.BottomSpeedIndicator, Is.EqualTo(6));
            Assert.That(_viewModel.IsSpeedIndicatorVisible, Is.False);
        }
    }

    [Test]
    public void SetSignalAspectWithValidConfigurationRequestsHardwareDispatch()
    {
        var signal = new SbSignal
        {
            BaseAddress = 101
        };
        var changes = new List<SignalBoxPropertyChangeEventArgs>();
        _viewModel.SelectedElement = signal;
        _viewModel.SelectMultiplexer("5229");
        _viewModel.ElementChanged += (_, args) => changes.Add(args);

        _viewModel.SetSignalAspectCommand.Execute(SignalAspect.Zs1);
        var change = changes.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signal.SignalAspect, Is.EqualTo(SignalAspect.Zs1));
            Assert.That(signal.ExtendedAccessoryValue, Is.EqualTo(1));
            Assert.That(change.RequiresVisualRefresh, Is.True);
            Assert.That(change.RequiresPersistence, Is.False);
            Assert.That(change.RequiresSignalCommand, Is.True);
        }
    }

    [Test]
    public void SetSignalAspectWithInvalidBaseAddressDoesNotRequestHardwareDispatch()
    {
        var signal = new SbSignal
        {
            BaseAddress = 100
        };
        var changes = new List<SignalBoxPropertyChangeEventArgs>();
        _viewModel.SelectedElement = signal;
        _viewModel.SelectMultiplexer("5229");
        _viewModel.ElementChanged += (_, args) => changes.Add(args);

        _viewModel.SetSignalAspectCommand.Execute(SignalAspect.Hp0);
        var change = changes.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signal.SignalAspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(change.RequiresSignalCommand, Is.False);
        }
    }

    [Test]
    public void SetSwitchPositionUpdatesRuntimeStateWithoutPersistence()
    {
        var sw = new SbSwitch();
        var changes = new List<SignalBoxPropertyChangeEventArgs>();
        _viewModel.SelectedElement = sw;
        _viewModel.ElementChanged += (_, args) => changes.Add(args);

        _viewModel.SetSwitchPositionCommand.Execute(SwitchPosition.DivergingLeft);
        var change = changes.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sw.SwitchPosition, Is.EqualTo(SwitchPosition.DivergingLeft));
            Assert.That(change.RequiresVisualRefresh, Is.True);
            Assert.That(change.RequiresPersistence, Is.False);
        }
    }

    [Test]
    public void DeleteSelectedElementRequestsDeletionWithoutOwningPlanMutation()
    {
        var element = new SbTrackStraight();
        var requests = new List<SignalBoxElementEventArgs>();
        _viewModel.SelectedElement = element;
        _viewModel.DeletionRequested += (_, args) => requests.Add(args);

        _viewModel.DeleteSelectedElementCommand.Execute(null);

        Assert.That(requests.Single().Element, Is.SameAs(element));
    }

    [Test]
    public void SignalBoxPropertiesControlCodeBehindDoesNotMutateDomainModels()
    {
        var repositoryRoot = FindRepositoryRoot();
        var codeBehind = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "MOBAflow",
            "Controls",
            "SignalBox",
            "SignalBoxPropertiesControl.xaml.cs"));
        string[] forbiddenDependencies =
        [
            "MultiplexerHelper",
            "SaveSolutionInternalAsync",
            "SetSignalAspectAsync",
            "ViessmannSignalService"
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                DirectMutationRegex().IsMatch(codeBehind),
                Is.False,
                "Signal-box domain mutation must remain in the shared ViewModel.");
            foreach (var forbiddenDependency in forbiddenDependencies)
            {
                Assert.That(
                    codeBehind,
                    Does.Not.Contain(forbiddenDependency),
                    $"SignalBoxPropertiesControl must not depend on {forbiddenDependency}.");
            }
        }
    }

    [GeneratedRegex(
        @"\b(?:signal|sw|detector|element|selectedElement)\." +
        @"(?:Name|Rotation|Address|FeedbackAddress|SwitchPosition|SignalAspect|" +
        @"MultiplexerArticleNumber|MainSignalArticleNumber|DistantSignalArticleNumber|" +
        @"BaseAddress|TopSpeedIndicator|BottomSpeedIndicator|IsMultiplexed|" +
        @"ExtendedAccessoryValue)\s*=(?!=)",
        RegexOptions.CultureInvariant)]
    private static partial Regex DirectMutationRegex();

}
