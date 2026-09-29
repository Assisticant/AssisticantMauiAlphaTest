using AssisticantMauiAlphaTest.Friends;
using Jinaga;
using Xunit;

namespace AssisticantMauiAlphaTest.Friends.Tests;

public class FriendListViewModelTests
{
    private static readonly DateTime May1 = new(1990, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static async Task<(FriendService service, FriendsModel model, FriendListViewModel list)> GivenNoFriends()
    {
        var service = new FriendService();
        var model = new FriendsModel(service);
        await model.Loaded();
        return (service, model, new FriendListViewModel(service, model));
    }

    private static async Task<(FriendService service, FriendsModel model, FriendViewModel row)> GivenFriend()
    {
        var (service, model, list) = await GivenNoFriends();
        await service.AddFriend("Bob", May1);
        await Settle(() => list.Friends.Count == 1 && list.Friends[0].BirthDate != null);
        return (service, model, list.Friends[0]);
    }

    [Fact]
    public async Task ModelMirrorsFriendsWithoutAnyViewModel()
    {
        var (service, model, _) = await GivenNoFriends();
        using var _m = model;

        var bob = await service.AddFriend("Bob", May1);
        await Settle(() => model.Friends.Count() == 1 && model.Friends.Single().BirthDate != null);

        var friend = model.Friends.Single();
        Assert.Equal("Bob", friend.Name);
        Assert.Equal(May1, friend.BirthDate);

        var edit = new FriendEditViewModel(service, friend.FreezeName(), friend.FreezeBirthDate());
        edit.Name = "Robert";
        await edit.Save();
        await Settle(() => friend.Name == "Robert");

        await service.RemoveFriend(bob);
        await Settle(() => !model.Friends.Any());
    }

    [Fact]
    public async Task ShowsAddedEditedAndRemovedFriends()
    {
        var (service, model, list) = await GivenNoFriends();
        using var _m = model;
        Assert.Empty(list.Friends);

        var bob = await service.AddFriend("Bob", May1);
        await service.AddFriend("Alice", new DateTime(1985, 1, 1));
        await Settle(() => list.Friends.Count == 2);
        Assert.Equal(new[] { "Alice", "Bob" }, list.Friends.Select(f => f.Name));

        var edit = list.Friends.Single(f => f.Name == "Bob").Edit();
        edit.Name = "Aaron";
        await edit.Save();
        await Settle(() => list.Friends.First().Name == "Aaron");
        Assert.Equal(new[] { "Aaron", "Alice" }, list.Friends.Select(f => f.Name));
        Assert.Equal(May1, list.Friends.First().BirthDate);

        await service.RemoveFriend(bob);
        await Settle(() => list.Friends.Count == 1);
        Assert.Equal("Alice", list.Friends.Single().Name);
    }

    [Fact]
    public async Task SavingAnUnchangedValueCreatesNoFact()
    {
        var (service, model, row) = await GivenFriend();
        using var _m = model;

        var edit = row.Edit();
        edit.BirthDate = May1.AddHours(13); // same calendar day once normalized
        await edit.Save();

        Assert.Single(await service.J.Query(AllBirthDates(), model.Friends.Single().Friend));
        Assert.False(row.Edit().HasBirthDateConflict);
    }

    [Fact]
    public async Task EditIsFrozenAndConcurrentEditForks()
    {
        var (service, model, row) = await GivenFriend();
        using var _m = model;

        var first = row.Edit();
        var second = row.Edit();      // both froze "Bob"
        first.Name = "Robert";
        second.Name = "Rob";
        await first.Save();
        await Settle(() => row.Name == "Robert");
        Assert.Equal("Bob", second.NameCandidates.Single()); // the open dialog did not shift
        await second.Save();                          // supersedes "Bob" too: a fork
        await Settle(() => row.Edit().HasNameConflict);

        Assert.Equal(new[] { "Rob", "Robert" }, row.Edit().NameCandidates);
    }

    [Fact]
    public async Task ResolvingAConflictCreatesAMergeEvenWithAnExistingValue()
    {
        var (service, model, row) = await GivenFriend();
        using var _m = model;
        var a = row.Edit();
        var b = row.Edit();
        a.Name = "Robert";
        b.Name = "Rob";
        await a.Save();
        await b.Save();
        await Settle(() => row.Edit().HasNameConflict);

        var resolve = row.Edit();
        Assert.Equal("Rob", resolve.Name);   // deterministic pick
        await resolve.Save();                // same value as one head, still a merge
        await Settle(() => !row.Edit().HasNameConflict);

        Assert.Equal("Rob", row.Name);
    }

    private static Specification<Friend, FriendBirthDate> AllBirthDates() =>
        Given<Friend>.Match((friend, facts) =>
            from birth in facts.OfType<FriendBirthDate>()
            where birth.friend == friend
            select birth);

    // Watch callbacks fire asynchronously after each save; poll briefly for the effect.
    private static async Task Settle(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);
        Assert.True(condition(), "view model did not settle");
    }
}
