using Assisticant.Collections;
using AssisticantMauiAlphaTest.Patterns;
using Jinaga;

namespace AssisticantMauiAlphaTest.Friends;

// MODEL: the friends the user has, mirrored from the LiveFriends specification.
// Owns the one watch behind the whole screen. Construct on the UI thread:
// Watch captures SynchronizationContext.Current and posts its callbacks to it.
public class FriendsModel : IDisposable
{
    private readonly ObservableList<FriendModel> _friends = new();
    private readonly IObserver _observer;

    public FriendsModel(FriendService service)
    {
        _observer = service.J.WatchInto(_friends, FriendSpecifications.LiveFriends(), service.Owner,
            projection => new FriendModel(projection));
    }

    public IEnumerable<FriendModel> Friends => _friends;

    public Task Loaded() => _observer.Loaded;

    public void Dispose() => _observer.Stop();
}
