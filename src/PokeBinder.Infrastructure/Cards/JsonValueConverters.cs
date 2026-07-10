using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PokeBinder.Infrastructure.Cards;

/// <summary>Serializes an arbitrary value to a JSON string column and back. Used for fields we store but never filter on inside the JSON itself.</summary>
public class JsonValueConverter<T> : ValueConverter<T, string>
{
    public JsonValueConverter()
        : base(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<T>(json, (JsonSerializerOptions?)null)!)
    {
    }
}

/// <summary>Element-wise equality for JSON-converted list properties, so EF's change tracker doesn't treat every re-deserialized instance as modified.</summary>
public class JsonListValueComparer<TItem> : ValueComparer<IReadOnlyList<TItem>>
{
    public JsonListValueComparer()
        : base(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            v => v.ToList())
    {
    }
}

/// <summary>Content equality for JSON-converted dictionary properties (e.g. legalities), so EF's change tracker doesn't treat every re-deserialized instance as modified.</summary>
public class JsonDictionaryValueComparer<TKey, TValue> : ValueComparer<IReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
{
    public JsonDictionaryValueComparer()
        : base(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.Count == b.Count && a.OrderBy(kv => kv.Key).SequenceEqual(b.OrderBy(kv => kv.Key))),
            v => v.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
            v => v.ToDictionary(kv => kv.Key, kv => kv.Value))
    {
    }
}
