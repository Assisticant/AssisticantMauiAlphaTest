# ViewModelLocator

Use `ViewModelLocatorBase` to manage view model lifecycle and ensure view models are recreated when their dependencies change.

## Basic Pattern

```csharp
public class ViewModelLocator : ViewModelLocatorBase
{
    private Document document;
    private Selection selection;

    public ViewModelLocator()
    {
        if (DesignMode)
            document = LoadDesignModeDocument();
        else
            document = LoadDocument();
        selection = new Selection();
    }

    public object Main => ViewModel(() =>
        new MainViewModel(document, selection));

    public object Child => ViewModel(() =>
        selection.SelectedItem == null
            ? null
            : new ChildViewModel(selection.SelectedItem));

    private Document LoadDocument()
    {
        // TODO: Load your document here.
        Document document = new Document();
        return document;
    }

    private Document LoadDesignModeDocument()
    {
        // TODO: Load your design mode data here.
        Document document = new Document();
        return document;
    }
}
```

## App.xaml Configuration

Add the view model locator to application resources:

```xml
<Application
    x:Class="MyCoolApp.App"
    xmlns:vm="clr-namespace:MyCoolApp.ViewModels">

    <Application.Resources>
        <vm:ViewModelLocator x:Key="Locator"/>
    </Application.Resources>
</Application>
```

## View Binding

Bind view DataContext to locator property:

```xml
<UserControl
    x:Class="MyCoolApp.MainView"
    DataContext="{Binding Main, Source={StaticResource Locator}}">
</UserControl>
```

## How It Works

- **ViewModel method**: Takes a lambda that creates the view model
- **Caching**: View models are cached and reused when parameters don't change
- **Automatic recreation**: When parameters change (e.g., selection changes), view model is recreated
- **Null handling**: Can return `null` for conditional view models

## Selection Pattern

Common pattern for navigation based on selection:

```csharp
public class ViewModelLocator : ViewModelLocatorBase
{
    private Selection selection;

    public ViewModelLocator()
    {
        selection = new Selection();
    }

    public object Main => ViewModel(() =>
        new MainViewModel(selection));

    // Child view model only exists when something is selected
    public object Child => ViewModel(() =>
        selection.SelectedItem == null
            ? null
            : new ChildViewModel(selection.SelectedItem));
}
```

## Design Mode

Support design-time data:

```csharp
public ViewModelLocator()
{
    if (DesignMode)
    {
        document = LoadDesignModeDocument();
    }
    else
    {
        document = LoadDocument();
    }
}

private Document LoadDesignModeDocument()
{
    // Return sample data for design-time preview
    var document = new Document();
    document.AddSampleData();
    return document;
}
```

## Important Notes

- **Singleton**: ViewModelLocator is typically a singleton in application resources
- **ViewModel method**: Always use `ViewModel()` method, never directly instantiate
- **Lambda expressions**: Pass factory lambda, not instance
- **Dependency tracking**: Assisticant tracks dependencies in the lambda
- **State management**: Use selection objects to pass state between view models
