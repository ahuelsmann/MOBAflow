namespace Moba.WinUI.Behavior
{
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.Xaml.Interactivity;
    using System.Windows.Input;

    /// <summary>
    /// Custom behavior for ListView ItemClick that passes the clicked item directly.
    /// This avoids the issue where CommandParameter binding returns null.
    /// </summary>
    public sealed class ListViewItemClickBehavior : Behavior<ListView>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(ListViewItemClickBehavior),
                new PropertyMetadata(null));

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.ItemClick += OnListViewItemClick;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.ItemClick -= OnListViewItemClick;
            }
        }

        private void OnListViewItemClick(object sender, ItemClickEventArgs e)
        {
            Command?.Execute(e.ClickedItem);
        }
    }
}
