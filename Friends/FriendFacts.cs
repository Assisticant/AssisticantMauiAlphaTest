using AssisticantMauiAlphaTest.Patterns;
using Jinaga;

namespace AssisticantMauiAlphaTest.Friends;

[FactType("Friends.Friend")]
public record Friend(User owner, DateTime createdAt);

[FactType("Friends.Friend.Name")]
public record FriendName(Friend friend, string value, FriendName[] prior)
    : IMutableFact<FriendName, Friend, string>
{
    string IMutableFact<FriendName, Friend, string>.Value => value;

    public static FriendName Create(Friend owner, string value, FriendName[] prior) =>
        new(owner, value, prior);
}

[FactType("Friends.Friend.BirthDate")]
public record FriendBirthDate(Friend friend, DateTime value, FriendBirthDate[] prior)
    : IMutableFact<FriendBirthDate, Friend, DateTime>
{
    DateTime IMutableFact<FriendBirthDate, Friend, DateTime>.Value => value;

    public static FriendBirthDate Create(Friend owner, DateTime value, FriendBirthDate[] prior) =>
        new(owner, AsCalendarDate(value), prior);

    // Jinaga round-trips DateTime as UTC. Pinning a birth date to UTC midnight keeps
    // its calendar date the same in every time zone.
    public static DateTime AsCalendarDate(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
}

[FactType("Friends.Friend.Deletion")]
public record FriendDeletion(Friend friend, DateTime deletedAt);
