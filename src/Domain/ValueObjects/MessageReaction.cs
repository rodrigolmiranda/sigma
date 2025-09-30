using Sigma.Domain.Common;

namespace Sigma.Domain.ValueObjects;

public class MessageReaction : ValueObject
{
    public string Key { get; private set; }
    public int Count { get; private set; }

    private MessageReaction()
    {
        Key = string.Empty;
    }

    public MessageReaction(string key, int count)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Count = count >= 0 ? count : 0;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Key;
        yield return Count;
    }
}