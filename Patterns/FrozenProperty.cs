using Jinaga;

namespace AssisticantMauiAlphaTest.Patterns;

// A snapshot of a mutable property's current facts, taken when an edit begins.
// The snapshot is what the edit supersedes: saving lists exactly these facts as
// prior, so an edit made while a concurrent change arrived forks (and shows as
// a conflict) instead of silently overwriting it.
public class FrozenProperty<TOwner, TFact, TValue>
    where TFact : class, IMutableFact<TFact, TOwner, TValue>
{
    private readonly TOwner _owner;
    private readonly TFact[] _heads;
    private readonly Func<TValue, TValue>? _normalize;

    internal FrozenProperty(TOwner owner, TFact[] heads, Func<TValue, TValue>? normalize)
    {
        _owner = owner;
        _heads = heads;
        _normalize = normalize;
    }

    public bool HasConflict => _heads.Length > 1;

    // Every distinct current value, so a conflict can be presented for resolution.
    public IReadOnlyList<TValue> Candidates =>
        _heads.Select(h => h.Value).Distinct().OrderBy(v => v).ToList();

    // The value to start editing from, or default if there is none yet.
    public TValue? Initial => _heads.Length == 0 ? default : Candidates[0];
    public bool HasInitial => _heads.Length > 0;

    // A single current value that equals the new one needs no new fact. Anything
    // else is a change (no heads or different value) or a conflict resolution
    // (several heads, even if the value matches one of them).
    public bool IsChange(TValue newValue) =>
        !(_heads.Length == 1 && Equals(Normalize(_heads[0].Value), Normalize(newValue)));

    // Creates the superseding fact and returns it, or returns null if nothing changed.
    public async Task<TFact?> Save(JinagaClient j, TValue newValue)
    {
        if (!IsChange(newValue))
            return null;
        return await j.Fact(TFact.Create(_owner, Normalize(newValue), _heads));
    }

    private TValue Normalize(TValue value) => _normalize == null ? value : _normalize(value);
}
