using DotNut;

public class Counter : Dictionary<KeysetId, int>
{
    public Counter(IDictionary<KeysetId, int> dictionary) : base(dictionary) { }

    public Counter() {}

    public int GetCounterForId(KeysetId keysetId)
    {
        if (TryGetValue(keysetId, out var counter))
            return counter;

        return this[keysetId] = 0;
    }

    public int IncrementCounter(KeysetId keysetId, int bumpBy = 1)
    {
        var current = GetCounterForId(keysetId);
        var next = current + bumpBy;
        this[keysetId] = next;
        return next;
    }

    public void SetCounter(KeysetId keysetId, int counter) => this[keysetId] = counter;
    public Counter Clone() => new(this);
}