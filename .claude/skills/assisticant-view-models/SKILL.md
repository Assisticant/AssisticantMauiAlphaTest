---
name: assisticant-view-models
description: Guide for creating Assisticant view models with Observable, ObservableList, computed properties, collections, and validation. Use when building view models for WPF, .NET MAUI, or other XAML-based applications that need reactive data binding.
---

# Assisticant View Models

Use this skill when creating view models for XAML-based applications (.NET MAUI, WPF) that need reactive data binding. Assisticant provides observable properties, collections, computed values, and validation without requiring `INotifyPropertyChanged` implementations.

## Essential Steps

1. **Use `Observable<T>`** for single-value properties that need change notification
2. **Use `ObservableList<T>`** for collections that need change notification
3. **Expose computed properties** that automatically update when dependencies change
4. **Implement `IValidation`** for validation rules when needed
5. **Use `ViewModelLocatorBase`** for view model lifecycle management

## Prerequisites

```bash
dotnet add package Assisticant
```

```csharp
using Assisticant;
using Assisticant.Collections;
```

The latest published package (1.5.8, February 2022) ships only
`net45`/`MonoAndroid`/`Xamarin.iOS` binaries — no `net6.0`+ or
`netstandard2.0` target. Referencing it from a modern .NET project (MAUI,
net8.0+) restores via the .NET Framework compatibility shim, with an
`NU1701` warning, not a native match. Verified that far: `Observable<T>`,
`ObservableList<T>`, and `ComputedList<T>` do compile and run correctly
under the shim on a modern .NET runtime. Not verified: actual XAML data
binding or MAUI/WPF-specific behavior through the shim — that's a
different kind of concern than a plain C# compile-and-run check, and
should be confirmed against the real UI framework before relying on it.

## Quick Start

**Basic observable properties**: See [observable-properties.md](observable-properties.md) for single-value properties.

**Collections**: See [collections.md](collections.md) for working with lists and collections.

**Computed properties**: See [computed-properties.md](computed-properties.md) for derived values that update automatically.

**Validation**: See [validation.md](validation.md) for implementing validation rules.

**ViewModelLocator**: See [viewmodel-locator.md](viewmodel-locator.md) for managing view model lifecycle.

## Key Patterns

### Observable Properties

```csharp
public class PersonViewModel
{
    private Observable<string> firstName = new Observable<string>();

    public string FirstName
    {
        get => firstName;
        set => firstName.Value = value;
    }
}
```

### Observable Collections

```csharp
public class DocumentViewModel
{
    private ObservableList<Person> people = new ObservableList<Person>();

    public IEnumerable<Person> People => people;

    public Person NewPerson()
    {
        var person = new Person();
        people.Add(person);
        return person;
    }
}
```

### Computed Properties

Computed properties automatically update when their dependencies change:

```csharp
public class PersonViewModel
{
    private Observable<string> firstName = new Observable<string>();
    private Observable<string> lastName = new Observable<string>();

    public string FirstName { get => firstName; set => firstName.Value = value; }
    public string LastName { get => lastName; set => lastName.Value = value; }

    // Automatically recomputes when FirstName or LastName changes
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

### View Model Headers

For list items, create header view models that implement `Equals` and `GetHashCode`:

```csharp
public class PersonHeader
{
    private readonly Person person;

    public PersonHeader(Person person)
    {
        this.person = person;
    }

    public Person Person => person;
    public string Name => $"{person.LastName}, {person.FirstName}";

    public override bool Equals(object obj)
    {
        if (obj == this)
            return true;
        var that = obj as PersonHeader;
        if (that == null)
            return false;
        return object.Equals(this.person, that.person);
    }

    public override int GetHashCode() => person.GetHashCode();
}
```

## Important Notes

- **Private fields**: Observable fields should be private, exposed through public properties
- **Initialization**: Always initialize `Observable<T>` and `ObservableList<T>` fields
- **IEnumerable exposure**: Collections should be exposed as `IEnumerable<T>`, not the raw list
- **Equals/GetHashCode**: Header view models must implement these for proper collection management
- **No base class**: View models don't inherit from a base class or implement interfaces (except `IValidation` for validation)
- **Automatic updates**: Computed properties automatically track dependencies and update when they change

## Integration with Jinaga

When using Assisticant with Jinaga reactive view models:
- Use `ObservableList<T>` to store results from `j.Watch()`
- Computed properties automatically update when watched data changes
- No need to manually implement `INotifyPropertyChanged`

See [integrating-jinaga-reactive-viewmodels-dotnet](../integrating-jinaga-reactive-viewmodels-dotnet/SKILL.md) for combining Assisticant with Jinaga watches.
