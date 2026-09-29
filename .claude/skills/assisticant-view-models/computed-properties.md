# Computed Properties

Computed properties in Assisticant automatically track dependencies and update when they change. No need to manually implement `INotifyPropertyChanged` or call `OnPropertyChanged`.

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

    // Automatically recomputes when FirstName or LastName changes
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

## How It Works

Assisticant tracks which observable properties are accessed during property evaluation. When any of those dependencies change, the computed property automatically recomputes and notifies bindings.

## Common Patterns

### Combining Properties

```csharp
public string DisplayName => $"{LastName}, {FirstName}";
public string FullAddress => $"{Street}, {City}, {State} {ZipCode}";
```

### Default Values

```csharp
public string DisplayName => Name ?? "<New Person>";
public int TotalItems => Items?.Count() ?? 0;
```

### Conditional Logic

```csharp
public string Status => IsComplete ? "Complete" : "In Progress";
public bool CanSave => !string.IsNullOrWhiteSpace(Name) && Age > 0;
```

### Aggregations

```csharp
public decimal TotalPrice => Items.Sum(item => item.Price);
public int ItemCount => Items.Count();
public bool HasItems => Items.Any();
```

### Filtering and Projection

```csharp
public IEnumerable<Person> ActivePeople => 
    from person in People
    where person.IsActive
    select person;

public IEnumerable<string> Names => 
    from person in People
    select person.Name;
```

## Dependencies

Computed properties automatically track:
- Observable properties accessed during evaluation
- ObservableList collections accessed during evaluation
- Nested computed properties

```csharp
public class OrderViewModel
{
    private ObservableList<LineItem> items = new ObservableList<LineItem>();

    public IEnumerable<LineItem> Items => items;

    // Depends on Items collection
    public decimal Subtotal => Items.Sum(item => item.Price);

    // Depends on Subtotal (nested dependency)
    public decimal Tax => Subtotal * 0.08m;

    // Depends on both Subtotal and Tax
    public decimal Total => Subtotal + Tax;
}
```

## Performance

- Computed properties are evaluated lazily (only when accessed)
- Results are cached until dependencies change
- Only recomputes when a tracked dependency actually changes
- No manual invalidation needed

## Important Notes

- **Pure functions**: Computed properties should be pure (no side effects)
- **No parameters**: Computed properties cannot take parameters
- **Automatic tracking**: Dependencies are tracked automatically - no manual registration needed
- **Nested dependencies**: Can depend on other computed properties

## Computed Collections: `ComputedList<T>`

A property computed with `.Sum()`, `.Where()`, or similar LINQ over an
`ObservableList<T>` (as in [Dependencies](#dependencies) above) is fine for
a scalar result, but re-projecting a whole *collection* that way — `public
IEnumerable<X> Filtered => Items.Where(...)` — recomputes the entire
projection from scratch on every dependency change, discarding whatever
identity/order tracking the UI had for unchanged items. `ComputedList<T>`
exists for exactly this case: a derived collection that needs its own
incremental, observable identity, not just a plain computed property that
happens to return an `IEnumerable<T>`.

```csharp
using Assisticant.Collections;

public class OrderViewModel
{
    private ObservableList<LineItem> items = new ObservableList<LineItem>();
    public IEnumerable<LineItem> Items => items;

    private ComputedList<LineItem> discountedItems;
    public IEnumerable<LineItem> DiscountedItems => discountedItems;

    public OrderViewModel()
    {
        discountedItems = new ComputedList<LineItem>(() =>
            items.Where(item => item.Price > 100)
                 .OrderByDescending(item => item.Price));
    }
}
```

- **Construct once, in the constructor** — not inline on the property
  getter, unlike a plain computed property.
- **Automatic dependency tracking**: the lambda passed to `ComputedList<T>`
  is re-evaluated when anything it reads (an `ObservableList<T>`, another
  `ComputedList<T>`, an `Observable<T>`) changes — same tracking mechanism
  as a scalar computed property, just producing a collection instead of a
  single value.
- **Expose as `IEnumerable<T>`**, never the raw `ComputedList<T>` — same
  rule as `ObservableList<T>` (see [collections.md](collections.md)).
- **Use a plain computed property instead when the result is a single
  value** (a sum, a count, a boolean) — reserve `ComputedList<T>` for when
  the derived *shape* is itself a collection a view needs to bind to.
