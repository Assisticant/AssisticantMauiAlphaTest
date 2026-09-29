# Validation

Assisticant supports validation using `INotifyDataErrorInfo` for WPF. Validation on other platforms (.NET MAUI) is not currently supported.

## Basic Pattern

```csharp
public class PersonViewModel : IValidation
{
    private Observable<string> name = new Observable<string>();

    public string Name
    {
        get => name;
        set => name.Value = value;
    }

    public IValidationRules Rules => Validator
        .For(() => Name)
            .NotNullOrWhitespace()
        .Build();
}
```

## XAML Binding

Enable validation in XAML bindings:

```xml
<TextBox Text="{Binding Name, ValidatesOnNotifyDataErrors=True}" />
```

## String Validation

### NotNullOrEmpty

Input must contain at least one character:

```csharp
public IValidationRules Rules => Validator
    .For(() => Name)
        .NotNullOrEmpty()
    .Build();
```

### NotNullOrWhitespace

Input must contain at least one non-whitespace character:

```csharp
public IValidationRules Rules => Validator
    .For(() => Name)
        .NotNullOrWhitespace()
    .Build();
```

### MaxLength

Input may contain no more than specified characters. Nulls are allowed:

```csharp
public IValidationRules Rules => Validator
    .For(() => Name)
        .MaxLength(50)
    .Build();
```

### Matches

Input must match regular expression. Nulls are allowed:

```csharp
public IValidationRules Rules => Validator
    .For(() => PhoneNumber)
        .Matches(@"^[0-9\-\(\)]*$")
    .Build();
```

To disallow nulls, combine with `NotNull`:

```csharp
public IValidationRules Rules => Validator
    .For(() => PhoneNumber)
        .NotNull()
        .Matches(@"^[0-9\-\(\)]*$")
    .Build();
```

## Numeric Validation

### GreaterThan

```csharp
public IValidationRules Rules => Validator
    .For(() => Age)
        .GreaterThan(0)
    .Build();
```

### Range

Combine rules for ranges:

```csharp
public IValidationRules Rules => Validator
    .For(() => Age)
        .GreaterThan(0)
        .LessThan(150)
    .Build();
```

### GreaterThanOrEqualTo

```csharp
public IValidationRules Rules => Validator
    .For(() => Price)
        .GreaterThanOrEqualTo(10.0m)
    .Build();
```

### LessThan

```csharp
public IValidationRules Rules => Validator
    .For(() => Speed)
        .LessThan(299792458.0)
    .Build();
```

### LessThanOrEqualTo

```csharp
public IValidationRules Rules => Validator
    .For(() => Code)
        .LessThanOrEqualTo((byte)0x7f)
    .Build();
```

## Multiple Properties

Validate multiple properties:

```csharp
public class PersonViewModel : IValidation
{
    private Observable<string> firstName = new Observable<string>();
    private Observable<string> lastName = new Observable<string>();

    public string FirstName { get => firstName; set => firstName.Value = value; }
    public string LastName { get => lastName; set => lastName.Value = value; }

    public IValidationRules Rules => Validator
        .For(() => FirstName)
            .NotNullOrWhitespace()
        .For(() => LastName)
            .NotNullOrWhitespace()
        .Build();
}
```

## Custom Messages

Provide custom validation messages:

```csharp
public IValidationRules Rules => Validator
    .For(() => Code)
        .LessThanOrEqualTo((byte)0x7f)
        .WithMessage(() => "The high bit of the code must be zero.")
    .Build();
```

## Custom Validation

Use `Where` for custom validation logic:

```csharp
public class PersonViewModel : IValidation
{
    private Observable<DateTime> birth = new Observable<DateTime>();
    private Observable<DateTime?> death = new Observable<DateTime?>();

    public DateTime Birth { get => birth; set => birth.Value = value; }
    public DateTime? Death { get => death; set => death.Value = value; }

    public IValidationRules Rules => Validator
        .For(() => Birth)
            .Where(v => v <= DateTime.Today)
            .WithMessage(() => "Birth date must not be in the future.")
        .For(() => Death)
            .Where(v => v == null || v > Birth)
            .WithMessage(() => "Death date must be after birth date.")
        .Build();
}
```

## Important Notes

- **WPF only**: Validation currently works only in WPF, not .NET MAUI
- **IValidation interface**: View model must implement `IValidation`
- **Rules property**: Must return `IValidationRules` from `Validator.Build()`
- **Lambda expressions**: Use `() => Property` syntax for property references
- **Chaining**: Chain multiple rules for a single property
- **Multiple For calls**: Use multiple `For()` calls for multiple properties
