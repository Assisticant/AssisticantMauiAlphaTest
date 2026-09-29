# Observable Properties

Use `Observable<T>` for single-value properties that need change notification in Assisticant view models.

## Basic Pattern

```csharp
public class PersonViewModel
{
    private Observable<string> firstName = new Observable<string>();
    private Observable<string> lastName = new Observable<string>();

    public string FirstName
    {
        get => firstName;
        set => firstName.Value = value;
    }

    public string LastName
    {
        get => lastName;
        set => lastName.Value = value;
    }
}
```

## Key Points

1. **Declare as private field**: `Observable<T>` should be a private field
2. **Initialize immediately**: Always initialize to `new Observable<T>()` to avoid null reference exceptions
3. **Expose as raw type**: Property getter returns the Observable (automatically converts), setter assigns to `.Value`
4. **Initial values**: Pass initial value to constructor: `new Observable<string>("Default")`

## Common Types

### String Properties

```csharp
private Observable<string> name = new Observable<string>();

public string Name
{
    get => name;
    set => name.Value = value;
}
```

### Numeric Properties

```csharp
private Observable<int> age = new Observable<int>();
private Observable<decimal> price = new Observable<decimal>();

public int Age
{
    get => age;
    set => age.Value = value;
}

public decimal Price
{
    get => price;
    set => price.Value = value;
}
```

### Object Properties

```csharp
private Observable<Person> spouse = new Observable<Person>();

public Person Spouse
{
    get => spouse;
    set => spouse.Value = value;
}
```

### Nullable Properties

```csharp
private Observable<DateTime?> birthDate = new Observable<DateTime?>();

public DateTime? BirthDate
{
    get => birthDate;
    set => birthDate.Value = value;
}
```

## Default Values

Provide initial values in the constructor:

```csharp
private Observable<string> searchTerm = new Observable<string>("");

public string SearchTerm
{
    get => searchTerm;
    set => searchTerm.Value = value;
}
```

## VS Code Snippet

Use the "obs" snippet to quickly insert an observable property.
