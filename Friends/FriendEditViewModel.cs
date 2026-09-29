using Assisticant.Fields;
using AssisticantMauiAlphaTest.Patterns;

namespace AssisticantMauiAlphaTest.Friends;

// The friend dialog. The current facts were frozen when it opened, so nothing
// here shifts under the user. Draft values are local UI state; Save supersedes
// the frozen facts, and only for the properties that actually changed.
[NotifyPropertyChanged]
public partial class FriendEditViewModel
{
    private readonly FriendService _service;
    private readonly FrozenProperty<Friend, FriendName, string>? _name;
    private readonly FrozenProperty<Friend, FriendBirthDate, DateTime>? _birthDate;

    // Editing an existing friend: supersedes the frozen facts on save.
    public FriendEditViewModel(
        FriendService service,
        FrozenProperty<Friend, FriendName, string> name,
        FrozenProperty<Friend, FriendBirthDate, DateTime> birthDate)
    {
        _service = service;
        _name = name;
        _birthDate = birthDate;
        Title = "Edit friend";
        Name = name.Initial ?? "";
        BirthDate = birthDate.HasInitial ? birthDate.Initial : DateTime.Today;
    }

    // Adding a new friend: nothing to freeze, saving creates the friend.
    public FriendEditViewModel(FriendService service)
    {
        _service = service;
        Title = "New friend";
        Name = "";
        BirthDate = DateTime.Today;
    }

    public string Title { get; }

    [Observable] public partial string Name { get; set; }
    [Observable] public partial DateTime BirthDate { get; set; }

    // Concurrent edits left several values; list them so the user can pick one.
    public bool HasNameConflict => _name?.HasConflict ?? false;
    public IReadOnlyList<string> NameCandidates => _name?.Candidates ?? [];
    public string NameConflict => string.Join(", ", NameCandidates);
    public bool HasBirthDateConflict => _birthDate?.HasConflict ?? false;
    public IReadOnlyList<DateTime> BirthDateCandidates => _birthDate?.Candidates ?? [];
    public string BirthDateConflict =>
        string.Join(", ", BirthDateCandidates.Select(d => d.ToString("d")));

    [Computed] private bool ComputeCanSave() => !string.IsNullOrWhiteSpace(Name);

    public async Task Save()
    {
        if (_name == null || _birthDate == null)
        {
            await _service.AddFriend(Name.Trim(), BirthDate);
            return;
        }
        await _name.Save(_service.J, Name.Trim());
        await _birthDate.Save(_service.J, BirthDate);
    }
}
