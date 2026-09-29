using AssisticantMauiAlphaTest.Patterns;

namespace AssisticantMauiAlphaTest.Friends;

// MODEL: the current state of one friend, as mirrored from the LiveFriends watch.
// Reads are tracked by Assisticant, so view models can derive from them.
// Nothing here knows about display formatting, commands or UI.
public class FriendModel
{
    private readonly MutableProperty<Friend, FriendName, string> _name;
    private readonly MutableProperty<Friend, FriendBirthDate, DateTime> _birthDate;

    // Must be constructed synchronously inside the watch callback: the nested
    // collections in the projection are subscribed here, and results that were
    // announced before subscription would be missed.
    public FriendModel(FriendProjection projection)
    {
        Friend = projection.Friend;
        _name = new(Friend, projection.Names);
        _birthDate = new(Friend, projection.BirthDates, FriendBirthDate.AsCalendarDate);
    }

    public Friend Friend { get; }

    public string Name => _name.Value ?? "";
    public DateTime? BirthDate => _birthDate.HasValue ? _birthDate.Value : null;

    // Concurrent edits that nobody has merged yet.
    public bool HasNameConflict => _name.HasConflict;
    public bool HasBirthDateConflict => _birthDate.HasConflict;

    // Snapshots for an edit session: saving supersedes exactly these facts.
    public FrozenProperty<Friend, FriendName, string> FreezeName() => _name.Freeze();
    public FrozenProperty<Friend, FriendBirthDate, DateTime> FreezeBirthDate() => _birthDate.Freeze();

    public override bool Equals(object? obj) => obj is FriendModel that && Friend.Equals(that.Friend);
    public override int GetHashCode() => Friend.GetHashCode();
}
