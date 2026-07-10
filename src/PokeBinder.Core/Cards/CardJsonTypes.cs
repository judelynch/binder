namespace PokeBinder.Core.Cards;

public record Ability(string Name, string Text, string Type);

/// <summary>
/// Cost is IReadOnlyList&lt;string&gt;, an interface type, so the compiler-generated
/// record equality would compare it by reference (two freshly-deserialized lists with
/// identical content are never reference-equal), making every re-import look like a
/// change. Equals/GetHashCode are overridden below to compare Cost by content.
/// </summary>
public record Attack(string Name, IReadOnlyList<string> Cost, int ConvertedEnergyCost, string? Damage, string Text)
{
    public virtual bool Equals(Attack? other) =>
        other is not null
        && Name == other.Name
        && Cost.SequenceEqual(other.Cost)
        && ConvertedEnergyCost == other.ConvertedEnergyCost
        && Damage == other.Damage
        && Text == other.Text;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        foreach (var cost in Cost)
        {
            hash.Add(cost);
        }
        hash.Add(ConvertedEnergyCost);
        hash.Add(Damage);
        hash.Add(Text);
        return hash.ToHashCode();
    }
}

public record TypeEffect(string Type, string Value);
