using Jinaga;
using Jinaga.Observers;

namespace AssisticantMauiAlphaTest.Friends;

// One watch feeds a whole row: the friend, plus live collections of its
// current (un-superseded) name and birth date values.
public class FriendProjection
{
    public Friend Friend { get; set; } = null!;
    public IObservableCollection<FriendName> Names { get; set; } = null!;
    public IObservableCollection<FriendBirthDate> BirthDates { get; set; } = null!;
}

public static class FriendSpecifications
{
    public static Specification<User, FriendProjection> LiveFriends() =>
        Given<User>.Match((user, facts) =>
            from friend in facts.OfType<Friend>()
            where friend.owner == user
            where !facts.Any<FriendDeletion>(d => d.friend == friend)
            select new FriendProjection
            {
                Friend = friend,
                Names = facts.Observable(
                    from name in facts.OfType<FriendName>()
                    where name.friend == friend
                    where !facts.Any<FriendName>(next => next.prior.Contains(name))
                    select name),
                BirthDates = facts.Observable(
                    from birth in facts.OfType<FriendBirthDate>()
                    where birth.friend == friend
                    where !facts.Any<FriendBirthDate>(next => next.prior.Contains(birth))
                    select birth)
            });
}
