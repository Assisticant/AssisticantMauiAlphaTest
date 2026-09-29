using Assisticant.Collections;
using Jinaga.Observers;

namespace AssisticantMauiAlphaTest.Patterns;

// The live side of a mutable property. Mirrors the current (un-superseded) facts
// of a watched projection and reduces them to one displayable value. Reads are
// tracked by Assisticant, so use them from [Computed] members.
//
// Editing goes through Freeze(): see FrozenProperty.
public class MutableProperty<TOwner, TFact, TValue>
    where TFact : class, IMutableFact<TFact, TOwner, TValue>
{
    private readonly TOwner _owner;
    private readonly Func<TValue, TValue>? _normalize;
    private readonly ObservableList<TFact> _heads = new();

    // normalize canonicalizes a value before it is compared or saved
    // (for example, pinning a date to a calendar day).
    public MutableProperty(TOwner owner, IObservableCollection<TFact> heads, Func<TValue, TValue>? normalize = null)
    {
        _owner = owner;
        _normalize = normalize;
        heads.MirrorInto(_heads);
    }

    public bool HasValue => _heads.Count > 0;

    // More than one current value means concurrent edits that nobody has merged yet.
    public bool HasConflict => _heads.Count > 1;

    // With a conflict, every candidate is current; show the smallest so the
    // choice is deterministic on every client.
    public TValue? Value => HasValue
        ? _heads.Select(h => h.Value).Min()
        : default;

    // Captures the current facts. Later changes do not affect the snapshot.
    public FrozenProperty<TOwner, TFact, TValue> Freeze() =>
        new(_owner, _heads.ToArray(), _normalize);
}
