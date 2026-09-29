using Assisticant.Fields;

namespace AssisticantMauiAlphaTest;

// VIEWMODEL: derives Greeting from the injected Model, but also carries its
// own IsSelected - ViewModel-local UI state no Model would represent - and a
// SelectionLabel computed from it. [NotifyPropertyChanged] is what makes this
// bindable directly with MAUI XAML {Binding}.
[NotifyPropertyChanged]
public partial class PersonViewModel
{
    private readonly Person _person;

    public PersonViewModel(Person person)
    {
        _person = person;
    }

    [Observable] public partial bool IsSelected { get; set; }

    [Computed] private string ComputeGreeting() =>
        $"Hello, {_person.Name}! You are {_person.Age} years old.";

    [Computed] private string ComputeSelectionLabel() =>
        IsSelected ? "Selected" : "Not selected";

    [Command] private void Birthday() => _person.Age += 1;

    private bool CanBirthday() => _person.Age < 100;

    [Command] private void ToggleSelection() => IsSelected = !IsSelected;
}
