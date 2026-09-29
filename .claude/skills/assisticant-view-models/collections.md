# Collections

Use `ObservableList<T>` for collections that need change notification in Assisticant view models.

## Basic Pattern

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

    public void DeletePerson(Person person)
    {
        people.Remove(person);
    }
}
```

## Key Points

1. **Declare as private field**: `ObservableList<T>` should be a private field
2. **Expose as IEnumerable**: Public property should be `IEnumerable<T>` to prevent external modification
3. **Control modifications**: Only the view model should modify the list (Add, Remove, Insert, etc.)
4. **Automatic updates**: Dependent properties automatically update when items are added/removed

## View Model Projection

Map source collections to view model collections using LINQ:

```csharp
public class MainViewModel
{
    private readonly Document document;

    public MainViewModel(Document document)
    {
        this.document = document;
    }

    public IEnumerable<PersonHeader> People =>
        from person in document.People
        select new PersonHeader(person);
}
```

## Sorting

```csharp
public IEnumerable<PersonHeader> People =>
    from person in document.People
    orderby person.LastName, person.FirstName
    select new PersonHeader(person);
```

## Filtering

```csharp
public class MainViewModel
{
    private Observable<string> searchTerm = new Observable<string>("");

    public string SearchTerm
    {
        get => searchTerm;
        set => searchTerm.Value = value;
    }

    public IEnumerable<PersonHeader> People =>
        from person in document.People
        where person.LastName.StartsWith(SearchTerm)
        select new PersonHeader(person);
}
```

## BindingList Support

If you add a method with signature `T NewItemIn{PropertyName}()`, Assisticant generates a `BindingList<T>`:

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

public class MainViewModel
{
    private readonly DocumentViewModel document;

    public IEnumerable<PersonHeader> People =>
        from person in document.People
        select new PersonHeader(person);

    // This method signature generates BindingList<T> for People property
    public PersonHeader NewItemInPeople()
    {
        return new PersonHeader(document.NewPerson());
    }
}
```

## Delete Support

Use `DeleteItemFrom{PropertyName}` convention for grid controls:

```csharp
public class MainViewModel
{
    // ...

    public void DeleteItemFromPeople(PersonHeader personHeader)
    {
        document.DeletePerson(personHeader.Person);
    }
}
```

## Object Recycling

Header view models must implement `Equals` and `GetHashCode` for proper object recycling:

```csharp
public class PersonHeader
{
    internal Person Person { get; }

    public PersonHeader(Person person)
    {
        Person = person;
    }

    public string Name => $"{Person.LastName}, {Person.FirstName}";

    public override bool Equals(object obj)
    {
        if (obj == this)
            return true;
        var that = obj as PersonHeader;
        if (that == null)
            return false;
        return object.Equals(this.Person, that.Person);
    }

    public override int GetHashCode() => Person.GetHashCode();
}
```

This allows Assisticant to:
- Preserve selection when list updates
- Preserve scroll position
- Only update changed items (better performance)
- Support entrance/exit animations

## VS Code Snippet

Use the "obslist" snippet to quickly insert an observable list property.
