# Building apps with Jinaga and Assisticant

This document records what was learned building the Friends sample in this repository: an in-memory
Jinaga store, Assisticant view models, and a .NET MAUI UI. It covers the architecture, how each
library behaves, the patterns that emerged, what is verified and what is not, and recommendations
for extending the libraries, a bridge library, and the skills.

Neither Jinaga nor Assisticant knows about the other, and that should stay true. Everything that
needs to know about both belongs in a separate bridge library (see "A library that knows both").

## 1. The layers

An application is a stack of descriptions. Each layer is written only in terms of the layer below
it and adds more abstract domain knowledge.

| Layer | Describes | Depends on | Friends example |
|---|---|---|---|
| Facts | The building blocks: what can be recorded | Jinaga | `Friend`, `FriendName`, `FriendBirthDate`, `FriendDeletion` |
| Specifications | How facts join together into useful shapes | Facts | `LiveFriends()` |
| Services | Common actions that record new facts | Facts, specifications | `FriendService.AddFriend`, `RemoveFriend` |
| Model | Current application state, mirrored from specifications by watches | Specifications | the friends and their current names and birth dates |
| View model | Model objects projected into properties and commands | Model | `FriendViewModel`, `FriendListViewModel`, `FriendEditViewModel` |
| View | The UI | View model | `FriendsPage`, `FriendEditPage` |

Rules that keep the layers honest:

- A layer never reaches past the layer directly below it. A view model does not query Jinaga. A
  view does not create facts.
- Facts are immutable and only grow. Everything above them is derived.
- Data flows up through watches, and changes flow down through services. The UI never edits model
  state directly. It asks a service to record a fact, and the watch brings the result back up.
- Each layer can be tested without the layers above it.

### The Friends sample, layer by layer

| Layer | Files | Responsibility |
|---|---|---|
| Facts | `FriendFacts.cs` | The four fact types. `FriendName` and `FriendBirthDate` implement `IMutableFact`. |
| Specifications | `FriendSpecifications.cs` | `LiveFriends()`: one projection per friend with live collections of its current name and birth date. |
| Services | `FriendService.cs` | `AddFriend`, `RemoveFriend`. Also holds the in-memory client and the local owner. |
| Model | `FriendsModel.cs`, `FriendModel.cs` | `FriendsModel` owns the one watch and an `ObservableList<FriendModel>`. `FriendModel` holds one friend's current `Name` and `BirthDate` as `MutableProperty` objects, and can freeze them for an edit. |
| View model | `FriendListViewModel.cs`, `FriendViewModel.cs`, `FriendEditViewModel.cs` | Derive display values (sorted rows, `Age`), and expose the edit session. They hold no mirrored state. |
| View | `FriendsPage`, `FriendEditPage` | XAML and thin code-behind. |

What moved when the model layer was separated:

- Only the model layer knows about `FriendProjection`, `MutableProperty` and the watch. View models
  never see a projection or an observer.
- `FriendsModel` is a singleton registered in DI. It is resolved on the UI thread when the page
  is first built, which is what lets its `Watch` capture the UI synchronization context.
- `FriendListViewModel` holds no state. Its `Friends` collection is a collection-mode `[Computed]`
  that wraps each `FriendModel` in a new `FriendViewModel` on every recompute and sorts them.
  Rows are recycled because `FriendViewModel.Equals` delegates to `FriendModel.Equals`, which
  delegates to the `Friend` fact.
- The model can be tested with no view model at all, and a test does.
- `FriendModel` must be constructed synchronously inside the watch callback. That is a model
  concern, so the constraint now lives in one place, with a comment.

## 2. Jinaga.NET: what was learned

### Facts

- A fact is a `record` with a `[FactType("Dotted.Name")]` attribute. Predecessors are constructor
  parameters. Nothing about a fact may change after creation.
- Identity is a hash of fields and predecessors. Two identical facts are one fact. An "anchor" fact
  such as `Friend` carries a `createdAt` so two friends created by the same user stay distinct.
- A value that changes over time is its own fact type with a `prior` array of the same type. The
  first value has `prior: []`. An edit lists what it supersedes. Concurrent edits fork, and a later
  fact that lists both heads merges them.
- Delete is a `FriendDeletion` fact pointing at the friend. `Jinaga.Patterns` has
  `WhereNotDeleted` and `WhereNotDeletedOrRestored` for it.
- **DateTime round-trips as UTC.** A local `1990-05-01` came back as `05:00Z`. There is no
  `DateOnly` support in the serializer (a search of `Jinaga/` found none). In zones east of UTC a
  local midnight would come back as the previous day. The sample pins birth dates to UTC midnight.

### Two specification styles

| Style | Example | Notes |
|---|---|---|
| Successor style | `user.Successors().OfType<Friend>(f => f.owner).WhereNotDeleted(...)` | Compact. Has `WhereCurrent`, `WhereNotDeleted`. No nested live collections. |
| Facts style | `Given<User>.Match((user, facts) => from f in facts.OfType<Friend>() where ...)` | Verbose. The only style that offers `facts.Observable(...)`. |

`facts.Observable(...)` inside a projection produces an `IObservableCollection<T>`. That is the
only way to watch nested collections (a friend's current names) from a single `Watch`. The sample
uses the facts style for `LiveFriends()` for that reason, and it spells out "current" by hand:
`where !facts.Any<FriendName>(next => next.prior.Contains(name))`.

Specifications cannot sort, count, group or compare field values. Do that in C# after results
materialize, or in the view model.

### Watch

- `j.Watch(spec, given, item => () => removal)` calls back once per result and calls the returned
  action when the result stops matching.
- `Observer.Start` captures `SynchronizationContext.Current` at the moment `Watch` is called.
  Every add and remove callback is then posted to that context
  (`ObserverLocal.SynchronizeOperaton`). If there was no context, callbacks run on whatever thread
  finished the work. Consequence: **call `Watch` on the UI thread and Jinaga marshals for you.** A
  custom UI-thread helper is unnecessary, and it was removed from the sample.
- `IObserver.Loaded` is a `Task` property, not a method. `IObserver.Stop()` ends the watch.
- **Nested collections must be subscribed synchronously inside the parent's added callback.**
  `NotifyAdded` calls the parent handler first, then delivers nested results to whatever handlers
  the parent registered. Handlers registered after an `await` miss the initial values because each
  result is only announced once (`FirstTimeNotified`).
- Later arrivals, such as a rename, flow through the same registered handlers, matched by the
  parent's identity.

### In-memory store

`JinagaClient.Create()` with no options and no replicator keeps everything in memory. The sample
relies on it and its tests pass against it. Facts need a `User` to hang off. The sample uses
`new User("local-user")`, which is fine for a single-user in-memory app and says nothing about
real identity.

### Other findings

- `Jinaga.Maui` contains only navigation lifecycle helpers (`ILifecycleManaged`,
  `INavigationLifecycleManager`). It has no collection binding.
- The `BlogMaui` sample in the Jinaga repos uses the same manual `Watch` plus `ObservableCollection`
  code. That is the boilerplate this document proposes to remove.
- A `Watch` callback runs after a save, but the timing relative to the awaited `Fact` call was not
  characterized. The tests poll (`Settle`) instead of assuming.

## 3. Assisticant: what was learned

Versions used: `Assisticant 1.6.0-alpha.1` and `Assisticant.SourceGenerators 0.1.0-alpha.1`.

### Source generators

| Attribute | Use |
|---|---|
| `[Observable] public partial T Name { get; set; }` | Tracked property on a class. Needs C# 13 partial properties. Bare `{ get; set; }` only. |
| `[Computed] private T ComputeName()` | Read-only tracked property. Leading `Compute` or `Get` is stripped from the name. |
| `[Computed] private IEnumerable<T> ComputeItems()` | Collection mode. Generates a real `ObservableCollection<T>`, synchronized in place on each recompute. |
| `[NotifyPropertyChanged]` | Implements `INotifyPropertyChanged` on the class. Needed for MAUI `{Binding}`. |
| `[Command] private void DoThing()` | `ICommand` property `DoThingCommand`, with optional `CanDoThing()`. No parameters, `void` only. |

Points that matter:

- **Recycled view models need `Equals` and `GetHashCode`.** Collection mode matches old and new
  items by equality. Without overrides, every recompute rebuilds every row, and per-row state is
  lost silently. `FriendViewModel` overrides both on its `Friend` fact.
- **MAUI needs `UpdateScheduler.Initialize`.** Without it every notification is queued forever and
  nothing updates. `MauiProgram` calls it with `DispatchDelayed(TimeSpan.Zero, ...)`. It must
  queue, never run inline.
- **`ObservableList<T>` and `ComputedList<T>` live in `Assisticant.Collections`.** In this version
  `Assisticant.Fields.ObservableList<T>` is inaccessible and `ComputedList<T>` is not in
  `Fields`. The docs in the skills name the wrong namespace in places.
- Prefer collection-mode `[Computed]` over `ComputedList<T>` for anything bound to a MAUI
  `CollectionView`. MAUI does not reliably observe custom collection types.
- Reading an `ObservableList` inside a `[Computed]` method is what makes the computed reactive.
  That is how `Name` on `FriendViewModel` follows the mirrored list.
- `[Command]` cannot take parameters, so per-row actions (Edit, Remove) use `Clicked` handlers in
  the code-behind that read the row's `BindingContext`.
- No generator exists for a model's own collections (`ObservableList`, `ObservableDictionary`).
- `DateTime.Today` inside `[Computed]` is not observable, so an age does not roll over at
  midnight while the app is running. The sample accepts this.

## 4. Patterns that emerged

These sit between the two libraries. The parts that use only Jinaga and the parts that use only
Assisticant are called out, because they belong in different homes (section 6).

### 4.1 Mirror a watch into an observable list (`WatchInto`, `MirrorInto`)

The manual version is the same six lines everywhere: add on `added`, remove in the returned
action. Two extension methods remove it:

```csharp
// specification results to a list of mapped items
_observer = j.WatchInto(_friends, FriendSpecifications.LiveFriends(), owner,
    projection => new FriendViewModel(service, projection));

// a nested collection of a projection to a list
projection.Names.MirrorInto(_names);
```

Both must run on the UI thread (`WatchInto`) or synchronously inside the parent callback
(`MirrorInto`).

### 4.2 One watch per screen, nested collections per row

The first version watched each friend's name and birth date with its own `Watch`. Two observers
per row, two `Loaded()` tasks per row, and a `Dispose` per row. The final version projects
`FriendProjection { Friend, Names, BirthDates }` from one specification and has exactly one
observer for the whole list. Prefer this shape whenever a row has mutable properties.

### 4.3 The mutable property pattern

A mutable property has four jobs: mirror the current values, reduce them to one displayed value,
freeze them when an edit starts, and save an edit that supersedes exactly what was frozen.

```csharp
public record FriendName(Friend friend, string value, FriendName[] prior)
    : IMutableFact<FriendName, Friend, string>
{
    string IMutableFact<FriendName, Friend, string>.Value => value;
    public static FriendName Create(Friend owner, string value, FriendName[] prior) =>
        new(owner, value, prior);
}
```

- `IMutableFact<TSelf, TOwner, TValue>` marks the fact type and gives the pattern a static factory,
  so no lambda is needed at each use.
- `MutableProperty<TOwner, TFact, TValue>` mirrors the current facts and exposes `Value`,
  `HasValue`, `HasConflict`, and `Freeze()`. With a conflict, `Value` is the smallest candidate,
  which is the same on every client.
- `FrozenProperty<...>` is a snapshot. `Save(j, newValue)` lists the frozen facts as `prior`.
  - It returns `null` and creates nothing when exactly one fact was frozen and the normalized new
    value equals it.
  - Otherwise it creates a fact. That covers a first value, a change, and a conflict resolution
    (several heads, even if the new value matches one of them).
- An optional normalizer canonicalizes values before comparing and saving. Birth dates use it to
  pin the calendar day.

The edit dialog freezes when it opens. If someone else edits meanwhile, the dialog's `prior` is
still the frozen set, so the save forks and the list shows a conflict, instead of silently
overwriting. Tests cover the unchanged, forked and merged cases.

### 4.4 Edit sessions as view models

`FriendViewModel.Edit()` returns a `FriendEditViewModel` holding draft `[Observable]` values.
Drafts are ordinary view-model-local state. A second constructor covers "new friend", where there
is nothing to freeze and saving calls `FriendService.AddFriend`.

### 4.5 Sorted lists

`[Computed] IEnumerable<FriendViewModel> ComputeFriends() => _friends.OrderBy(f => f.Name)`
replaces a hand-managed `ComputedList`. A rename re-sorts in place.

## 5. Practical project findings

- The MAUI project's default globbing picks up `tests\**`. The `.csproj` removes it from `Compile`,
  `MauiXaml` and `None`.
- A test project can compile the MAUI project's `Friends\` and `Patterns\` folders directly on
  plain `net10.0`, as long as those folders contain no MAUI types. The page code-behind files
  are excluded with `Compile Remove`.
- `Assisticant.SourceGenerators` works from a plain `net10.0` test project.
- Tests drive view models through a real in-memory client, not mocks. Watch results arrive
  asynchronously, so tests poll with a bounded `Settle`.

## 6. Recommendations

### 6.1 A library that knows both

A small bridge package, for example `Jinaga.Assisticant`, that references both and owns:

| Piece | Purpose |
|---|---|
| `WatchInto`, `MirrorInto` | Mirror watches into `ObservableList<T>` |
| `MutableProperty`, `FrozenProperty` | The mutable property pattern's live and frozen sides |
| A model base or helper for "a watched root" | Owns an `IObserver`, its `Loaded`, and `Dispose`, so models are not hand-wired |
| Test helpers | A `Settled()` or `WaitUntil` that replaces ad hoc polling |
| Lifecycle adapter | Connects `ILifecycleManaged` (from `Jinaga.Maui`) to starting and stopping watches |

The dependency directions are important: the bridge depends on both libraries, and neither
depends on it or on each other.

### 6.2 Split the mutable property pattern

`IMutableFact`, `FrozenProperty` and the "no new fact if unchanged" rule use only Jinaga types
(`JinagaClient`, `IObservableCollection`). They belong in Jinaga.NET itself, next to
`WhereCurrent` in `Jinaga.Patterns`. Only the piece that mirrors into an `ObservableList` needs
Assisticant, so it stays in the bridge. A Jinaga-side type would expose the frozen heads as a
plain array, and the bridge would add the observable wrapper.

### 6.3 Jinaga.NET

- Offer `facts.Observable(...)` equivalents in the successor style, so nested live collections do
  not force the verbose style.
- Add first-class `DateOnly` support, or document the UTC round-trip prominently.
- Document that `Watch` captures `SynchronizationContext.Current`, and that nested `OnAdded` must
  be called synchronously in the parent callback.
- Make `IObserver` disposable so it works with `using` and with view model disposal patterns.
- Provide a test helper that completes when watches have delivered the results of an awaited save.
- An analyzer that warns when a mutable-property fact type is read without `WhereCurrent`.
- Consider a "current values" projection helper so `!facts.Any<T>(next => next.prior.Contains(x))`
  is not written by hand.

### 6.4 Assisticant

- Fix or document the `Assisticant.Fields` versus `Assisticant.Collections` namespaces for
  `ObservableList<T>` and `ComputedList<T>`.
- Ship `UpdateScheduler` initialization for MAUI so the first-run "nothing updates" trap goes away.
- Support async commands (`[Command] private async Task DoThing()`), since actions that record
  facts are asynchronous.
- Support command parameters, at least a single `object` parameter, so row-level Edit and Remove
  do not need code-behind handlers.
- Generate `Equals` and `GetHashCode` for recycled view models from the constructor's injected
  model, as the prototype notes already suggest.
- Add a generator for model-owned collections.
- Offer an observable way to read "now", so ages and relative times update.

### 6.5 Skills

Findings from using the current skills:

- **They disagree.** The Jinaga integration skill shows `ComputedList<T>` for derived collections.
  The Assisticant generator skill says never to use it directly for MAUI and to use collection-mode
  `[Computed]`. The view model skill still describes the old `Observable<T>` field style and the
  1.5.8 package. Align them on one recommended style per platform.
- **Namespaces.** State the correct namespaces for `ObservableList<T>` and friends.
- **Specification styles.** The specification skill only shows the successor style. Add the facts
  style and explain that `facts.Observable(...)` is required for nested live collections.
- **Threading.** The integration skill says to "dispatch explicitly". In fact Jinaga posts to the
  captured synchronization context, so the guidance should be "call `Watch` on the UI thread".
- **Mutable properties.** The facts skill covers the `prior` chain but not freezing, the no-op save
  rule, or conflict resolution. Add them, plus the `IMutableFact` pattern.
- **Dates.** Add the UTC round-trip caveat and the calendar-date pinning pattern.
- **Testing.** Add the bounded `Settle` pattern and warn that a nested subscription must be
  awaited as part of `Loaded`.
- **A new skill for structure.** Add a skill that teaches the layers in section 1: facts,
  specifications, services, model, view model, view, with what belongs in each and the rules for
  crossing layers. Reference the existing skills from it instead of duplicating them.
- **A skill for the bridge library** once it exists, covering `WatchInto`, `MutableProperty`,
  lifecycle and testing.

## 7. What is verified and what is not

Verified:

- The app builds for `net10.0-windows10.0.19041.0`.
- Five tests pass repeatedly against the in-memory store: the model alone mirroring add, edit and
  remove, the view model listing add, sort, rename and remove, the unchanged save creating no fact,
  the frozen dialog and fork, and conflict resolution creating a merge.
- The Jinaga source confirms the synchronization context capture and the nested handler ordering.

Not verified:

- The UI has never been launched. XAML bindings, the modal editor, `CollectionView` re-sorting on
  rename, and `UpdateScheduler` behavior on a real dispatcher are untested.
- Behavior with a persistent store or a replicator.
- Real user identity. The sample uses a fixed local `User`.
- The exact timing of watch callbacks relative to an awaited save.
- Restore-after-delete, which the sample does not expose.

## 8. Suggested next steps

1. Launch the app and exercise add, edit, conflict and remove in the real UI.
2. Extract `Patterns\` into the bridge package, and propose the Jinaga-only half upstream.
3. Update the skills as listed in 6.5, starting with the disagreements between them.
