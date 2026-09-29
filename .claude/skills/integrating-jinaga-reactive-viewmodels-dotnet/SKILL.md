---
name: integrating-jinaga-reactive-viewmodels-dotnet
description: >
  Use when wiring a Jinaga.NET specification into a view model — populating
  a reactive collection from `j.Watch()`, exposing computed properties for
  data binding, or working through the design-first modeling workflow before
  implementing a screen.
---

# Integrating Jinaga into .NET reactive view models

Once a specification exists (`authoring-jinaga-specifications-dotnet`), a
view model's job is to keep a UI-bindable collection in sync with it via
`j.Watch()`, and to dispose of that subscription cleanly.

## Populate a collection from a watch

`j.Watch()` calls back once per matching fact with an add, and expects a
function back that runs on remove. Subscribe in the constructor — `Watch`
itself isn't async, so there's no reason to defer it to a separate `Load`
step:

```csharp
public class TaskListViewModel : IDisposable
{
    private readonly ObservableList<TaskProjection> _tasks = new();
    public IEnumerable<TaskProjection> Tasks => _tasks;

    private readonly IObserver _observer;

    public TaskListViewModel(JinagaClient j, Project project)
    {
        _observer = j.Watch(TasksInProjectQuery(), project, projection =>
        {
            _tasks.Add(projection);
            return () => _tasks.Remove(projection);
        });
    }

    public async Task Loaded()
    {
        await _observer.Loaded;
    }

    public void Dispose()
    {
        _observer.Stop();
    }
}
```

`IObserver.Loaded` is a `Task` **property**, not a method — `await
_observer.Loaded();` is a mistake that won't compile, not just a style
choice; the parens matter. Expose the view model's own `Loaded()` as an
`async Task` method wrapping it, per `testing-jinaga-dotnet` — a test
awaits *that*, not the observer directly, so a view model with more than
one observer can compose them (`Task.WhenAll(...)`) behind one call.

Expose derived values as computed properties rather than maintaining them by
hand. A single derived value is a plain computed property reading `_tasks`;
a derived *collection* — filtered, sorted, or reshaped from `_tasks` — is
Assisticant's `ComputedList<T>` instead, which tracks `_tasks` as a
dependency and recomputes only when it changes:

```csharp
private readonly ComputedList<TaskProjection> _overdueTasks;
public IEnumerable<TaskProjection> OverdueTasks => _overdueTasks;

// in the constructor, after _observer is set up:
_overdueTasks = new ComputedList<TaskProjection>(() =>
    _tasks.Where(t => t.DueDate < DateTime.UtcNow));
```

`ComputedList<T>` itself is a general Assisticant type, not a Jinaga one —
see `assisticant-view-models`'s
[computed-properties.md](../assisticant-view-models/computed-properties.md)
for the full API and when to reach for it over a plain computed property.
This is the Jinaga-specific half: the source it's computing over is data
`j.Watch()` populated.

## Marshal to the UI thread

`j.Watch()` callbacks can fire on a background thread. Anything that touches
UI-bound state — including an `ObservableList` a XAML binding is watching —
needs to run on the UI thread; dispatch explicitly rather than assuming the
callback already runs there.

Always implement `IDisposable` and stop the observer in it. A view model
that never stops its watch keeps its subscription (and everything it holds
a reference to) alive for the life of the `JinagaClient`, not the life of
the screen.

## Design-first: model, visualize, then implement

Before writing the view model — or the fact types, if the model is still in
flux — sketch the fact graph and confirm its shape against a couple of real
examples *before* wiring it into a view model and a XAML binding. A wrong
cardinality or a missing existential filter is much cheaper to notice at
this stage than after it's live in a screen. Two ways to do the "sketch
and confirm" part, combinable:

- **Diagram it** — draw the fact-type graph and a sample instance graph by
  hand, per `diagramming-historical-models`'s notation. (Polyglot Notebooks
  used to make this interactive via the `Jinaga.Notebooks` package's
  `Renderer`/`AsTable` helpers — that package's notebook-facing API is
  built on notebook-kernel display types and is dead weight without a
  notebook host, since Polyglot Notebooks is no longer supported. Its
  `Jinaga.Notebooks.Dot` namespace is a separate, plain-string API that
  still works standalone, though: `Dot.Renderer.RenderTypes(typeof(...))`
  and `j.RenderFacts(...)` (via `Dot.JinagaClientExtensions`) generate the
  same Graphviz notation from real fact types or fact instances, callable
  from a throwaway console app or test and piped through `dot -Tsvg` —
  useful if hand-drawing a graph with many fact types gets tedious, though
  hand-drawing is the default and needs nothing beyond the notation.)
- **Prove it with a test** — write a scratch `JinagaTest`-based test (per
  `testing-jinaga-dotnet`) that saves a couple of example facts and queries
  them back through the specification. This is the same "does the shape
  actually behave the way I think it does" check as the diagram, aimed at
  behavior instead of structure — cheap, disposable, and doesn't depend on
  any tooling beyond what's already needed to test the real thing.

Only once the shape is confirmed, copy the finished fact types and
specifications into the actual project and keep the test (or write a
proper one) rather than discarding it.
