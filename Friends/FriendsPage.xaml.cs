namespace AssisticantMauiAlphaTest.Friends;

public partial class FriendsPage : ContentPage
{
    private readonly FriendListViewModel _viewModel;

    // Resolved on the UI thread, so the FriendsModel singleton's Watch captures
    // this thread's synchronization context and posts its updates back to it.
    public FriendsPage(FriendListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(new FriendEditPage(_viewModel.NewFriend()));

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: FriendViewModel friend })
            await Navigation.PushModalAsync(new FriendEditPage(friend.Edit()));
    }

    private async void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: FriendViewModel friend })
            await friend.Remove();
    }
}
