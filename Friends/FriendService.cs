using Jinaga;

namespace AssisticantMauiAlphaTest.Friends;

// In-memory Jinaga store: JinagaClient.Create with no options and no
// replicator endpoint keeps all facts in memory for the app's lifetime.
public class FriendService
{
    public JinagaClient J => _j;
    public User Owner => _owner;

    private readonly JinagaClient _j = JinagaClient.Create();
    private readonly User _owner = new User("local-user");

    public async Task<Friend> AddFriend(string name, DateTime birthDate)
    {
        var friend = await _j.Fact(new Friend(_owner, DateTime.UtcNow));
        await _j.Fact(new FriendName(friend, name, []));
        await _j.Fact(FriendBirthDate.Create(friend, birthDate, []));
        return friend;
    }

    public async Task RemoveFriend(Friend friend) =>
        await _j.Fact(new FriendDeletion(friend, DateTime.UtcNow));
}
