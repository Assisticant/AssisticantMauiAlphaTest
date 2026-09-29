using Assisticant.Fields;

namespace AssisticantMauiAlphaTest.Friends;

// VIEW MODEL: projects one FriendModel into what a row displays, plus the
// actions a row offers. Recreated on every recompute of the list, so equality
// must follow the model.
[NotifyPropertyChanged]
public partial class FriendViewModel
{
    private readonly FriendService _service;
    private readonly FriendModel _model;

    public FriendViewModel(FriendService service, FriendModel model)
    {
        _service = service;
        _model = model;
    }

    [Computed] private string ComputeName() => _model.Name;

    [Computed] private DateTime? ComputeBirthDate() => _model.BirthDate;

    [Computed] private string ComputeAge()
    {
        if (BirthDate is not { } birth) return "";
        var today = DateTime.Today;
        var age = today.Year - birth.Year;
        if (birth.Date > today.AddYears(-age)) age--;
        return age.ToString();
    }

    // Opening the editor freezes the current values; saving supersedes exactly those.
    public FriendEditViewModel Edit() =>
        new(_service, _model.FreezeName(), _model.FreezeBirthDate());

    public Task Remove() => _service.RemoveFriend(_model.Friend);

    public override bool Equals(object? obj) => obj is FriendViewModel that && _model.Equals(that._model);
    public override int GetHashCode() => _model.GetHashCode();
}
