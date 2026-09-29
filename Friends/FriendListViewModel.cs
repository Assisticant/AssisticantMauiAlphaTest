using Assisticant.Fields;

namespace AssisticantMauiAlphaTest.Friends;

// VIEW MODEL: projects the friends model into a sorted list of rows. It holds no
// state of its own: rows are derived, and recycled by FriendViewModel equality.
[NotifyPropertyChanged]
public partial class FriendListViewModel
{
    private readonly FriendService _service;
    private readonly FriendsModel _model;

    public FriendListViewModel(FriendService service, FriendsModel model)
    {
        _service = service;
        _model = model;
    }

    // Collection-mode [Computed]: generates a live ObservableCollection<FriendViewModel>
    // named Friends, synchronized (not replaced) whenever a name or membership changes.
    [Computed] private IEnumerable<FriendViewModel> ComputeFriends() =>
        _model.Friends
            .Select(friend => new FriendViewModel(_service, friend))
            .OrderBy(row => row.Name);

    public FriendEditViewModel NewFriend() => new(_service);
}
