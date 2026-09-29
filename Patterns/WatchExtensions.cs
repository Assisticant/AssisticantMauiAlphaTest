using Assisticant.Collections;
using Jinaga;
using Jinaga.Observers;

namespace AssisticantMauiAlphaTest.Patterns;

public static class WatchExtensions
{
    // Mirrors the results of a specification into an ObservableList, mapping each
    // projection to a list item. An item is added when its result appears and
    // removed when it no longer matches. Call on the UI thread: Watch captures
    // SynchronizationContext.Current and posts its callbacks back to it.
    public static IObserver WatchInto<TGiven, TProjection, TItem>(
        this JinagaClient j,
        ObservableList<TItem> list,
        Specification<TGiven, TProjection> specification,
        TGiven given,
        Func<TProjection, TItem> map)
        where TGiven : class
    {
        return j.Watch(specification, given, projection =>
        {
            var item = map(projection);
            list.Add(item);
            return () => list.Remove(item);
        });
    }

    // Mirrors a nested collection of a watched projection into an ObservableList.
    public static void MirrorInto<T>(this IObservableCollection<T> source, ObservableList<T> list)
    {
        source.OnAdded(item =>
        {
            list.Add(item);
            return () => list.Remove(item);
        });
    }
}
