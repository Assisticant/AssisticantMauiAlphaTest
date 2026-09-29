namespace AssisticantMauiAlphaTest.Patterns;

// Marks a fact type as one link in a "prior" chain: a value of a property that
// changes over time. Implement it on the fact record, e.g.
//
//   public record FriendName(Friend friend, string value, FriendName[] prior)
//       : IMutableFact<FriendName, Friend, string>
//   {
//       string IMutableFact<FriendName, Friend, string>.Value => value;
//       public static FriendName Create(Friend owner, string value, FriendName[] prior) =>
//           new(owner, value, prior);
//   }
public interface IMutableFact<TSelf, TOwner, TValue>
    where TSelf : class, IMutableFact<TSelf, TOwner, TValue>
{
    TValue Value { get; }
    static abstract TSelf Create(TOwner owner, TValue value, TSelf[] prior);
}
