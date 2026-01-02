namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.TrackPlan.Domain;

public partial class TrackTemplateViewModel : ObservableObject
{
    public TrackTemplate Template { get; }

    public string ArticleCode => Template.ArticleCode;
    public string Name => Template.Name;

    public TrackTemplateViewModel(TrackTemplate template)
    {
        Template = template;
    }
}