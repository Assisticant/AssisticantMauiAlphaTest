namespace AssisticantMauiAlphaTest.Friends;

public partial class FriendEditPage : ContentPage
{
    private readonly FriendEditViewModel _viewModel;

    public FriendEditPage(FriendEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        await _viewModel.Save();
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
