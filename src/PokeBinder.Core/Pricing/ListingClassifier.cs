using System.Text.RegularExpressions;

namespace PokeBinder.Core.Pricing;

/// <summary>
/// Rule-based scorer per Phase 8. Pure function of its inputs - no DB, no I/O - so the 40+
/// table-driven tests can construct a ListingClassificationInput directly and assert on the
/// returned ClassificationResult with no fixture/database setup at all.
/// </summary>
public class ListingClassifier : IListingClassifier
{
    private static readonly (string Reason, Regex Pattern)[] KillPatterns =
    {
        ("bundle", new Regex(@"\bbundle\b", RegexOptions.IgnoreCase)),
        ("joblot", new Regex(@"\bjob\s?lot\b", RegexOptions.IgnoreCase)),
        ("lot of", new Regex(@"\blot\s+of\b", RegexOptions.IgnoreCase)),
        ("multiple quantity (xN)", new Regex(@"\bx\s?\d+\b", RegexOptions.IgnoreCase)),
        ("choose your card", new Regex(@"\bchoose\s+your\s+card\b", RegexOptions.IgnoreCase)),
        ("pick your card", new Regex(@"\bpick\s+your\s+card\b", RegexOptions.IgnoreCase)),
        ("proxy", new Regex(@"\bproxy\b", RegexOptions.IgnoreCase)),
        ("custom", new Regex(@"\bcustom\b", RegexOptions.IgnoreCase)),
        ("metal card", new Regex(@"\bmetal\b", RegexOptions.IgnoreCase)),
        ("orica", new Regex(@"\borica\b", RegexOptions.IgnoreCase)),
        ("repack", new Regex(@"\brepack\b", RegexOptions.IgnoreCase)),
        ("sealed", new Regex(@"\bsealed\b", RegexOptions.IgnoreCase)),
        ("empty (sleeve/box, not a card)", new Regex(@"\bempty\b", RegexOptions.IgnoreCase)),
        ("sleeve only", new Regex(@"\bsleeve\s+only\b", RegexOptions.IgnoreCase)),
    };

    private static readonly Regex AnySetNumberPattern = new(@"\b[A-Za-z]{0,4}\d+\s*/\s*[A-Za-z]{0,4}\d+\b", RegexOptions.IgnoreCase);

    private static readonly Regex GradedReadyPhrase = new(@"\b(PSA|BGS|CGC|ACE|TAG)\s+ready\b|\bworthy\s+of\s+grading\b", RegexOptions.IgnoreCase);
    private static readonly Regex GemMt10Phrase = new(@"\bGEM\s?MT\s?10\b", RegexOptions.IgnoreCase);
    // No \b between the grader letters and the grade digits: "psa10" has no boundary there at all
    // (letters and digits are both \w characters), so requiring one would reject the exact
    // no-space-lowercase form the spec explicitly calls out as a case that must parse.
    private static readonly Regex GraderGradePattern = new(@"\b(PSA|BGS|CGC|ACE|TAG)\s*-?\s*(\d{1,2}(?:\.\d)?)\b", RegexOptions.IgnoreCase);

    // Negative lookbehind for a digit so "180HP" (a Pokemon's HP stat) doesn't get misread as the
    // "Heavily Played" condition abbreviation - a real ambiguity in eBay titles the spec's literal
    // "HP" token doesn't itself resolve.
    private static readonly (RawConditionClassification Condition, Regex Pattern)[] ConditionPatterns =
    {
        (RawConditionClassification.NM, new Regex(@"\bnear\s+mint\b|\bpack\s+fresh\b", RegexOptions.IgnoreCase)),
        (RawConditionClassification.LP, new Regex(@"\blightly\s+played\b|\bLP\b", RegexOptions.IgnoreCase)),
        (RawConditionClassification.HP, new Regex(@"\bheavily\s+played\b|(?<!\d)\bHP\b", RegexOptions.IgnoreCase)),
        (RawConditionClassification.MP, new Regex(@"\bmoderately\s+played\b|\bplayed\b|\bMP\b", RegexOptions.IgnoreCase)),
        (RawConditionClassification.DMG, new Regex(@"\bdamaged\b|\bDMG\b", RegexOptions.IgnoreCase)),
        (RawConditionClassification.NM, new Regex(@"\bmint\b|\bNM\b", RegexOptions.IgnoreCase)),
    };

    private static readonly (string VariantTypeName, Regex Pattern)[] VariantPatterns =
    {
        ("Reverse Holo", new Regex(@"\breverse\s+holo\b|\brev\s?holo\b|\bRH\b", RegexOptions.IgnoreCase)),
        ("1st Edition", new Regex(@"\b1st\s+edition\b|\b1st\s+ed\b|\bfirst\s+edition\b", RegexOptions.IgnoreCase)),
        ("Shadowless", new Regex(@"\bshadowless\b", RegexOptions.IgnoreCase)),
        ("Holo", new Regex(@"\bholo\b", RegexOptions.IgnoreCase)),
    };

    private static readonly (string Language, Regex Pattern)[] LanguagePatterns =
    {
        ("Japanese", new Regex(@"\bjapanese\b|\bJPN\b|\bjap\b", RegexOptions.IgnoreCase)),
        ("Korean", new Regex(@"\bkorean\b|\bKOR\b", RegexOptions.IgnoreCase)),
        ("German", new Regex(@"\bgerman\b|\bdeutsch\b", RegexOptions.IgnoreCase)),
        ("French", new Regex(@"\bfrench\b|\bfrançais\b|\bfrancais\b", RegexOptions.IgnoreCase)),
        ("Italian", new Regex(@"\bitalian\b|\bitaliano\b", RegexOptions.IgnoreCase)),
        ("Spanish", new Regex(@"\bspanish\b|\bespañol\b|\bespanol\b", RegexOptions.IgnoreCase)),
        ("Chinese", new Regex(@"\bchinese\b", RegexOptions.IgnoreCase)),
    };

    private readonly ClassifierOptions _options;

    public ListingClassifier(ClassifierOptions options)
    {
        _options = options;
    }

    public ClassificationResult Classify(Guid queriedCardVariantId, ListingClassificationInput input)
    {
        var title = input.Title ?? string.Empty;

        foreach (var (reason, pattern) in KillPatterns)
        {
            if (pattern.IsMatch(title))
            {
                return Killed(queriedCardVariantId, reason);
            }
        }

        var (identityStrong, identityKillReason) = MatchIdentity(title, input.CardName, input.SetName, input.SetNumber);
        if (identityKillReason is not null)
        {
            return Killed(queriedCardVariantId, identityKillReason);
        }

        var (resolvedVariantId, variantMatch, variantKillReason) = MatchVariant(title, queriedCardVariantId, input.TargetVariantTypeName, input.SiblingVariants, input.InScopeCardVariantIds);
        if (variantKillReason is not null)
        {
            return Killed(queriedCardVariantId, variantKillReason);
        }

        var language = DetectLanguage(title);

        var (gradedStatus, grader, grade) = MatchGrading(title);

        var score = identityStrong ? _options.IdentityMatchStrong : _options.IdentityMatchWeak;
        score += variantMatch == VariantMatch.Confirmed ? _options.VariantMatchConfirmed : _options.VariantMatchAmbiguous;

        var rawCondition = RawConditionClassification.Unspecified;
        if (gradedStatus == GradedStatus.Graded)
        {
            score += _options.GradedConfident;
        }
        else
        {
            rawCondition = DetectCondition(title);
            score += rawCondition == RawConditionClassification.Unspecified ? _options.RawConditionUnspecified : _options.RawConditionStated;
        }

        if (language == "English")
        {
            score += _options.LanguageEnglish;
        }

        var status = score >= _options.AutoAcceptThreshold
            ? ClassificationStatus.AutoAccepted
            : score >= _options.QuarantineThreshold
                ? ClassificationStatus.Quarantined
                : ClassificationStatus.Rejected;

        return new ClassificationResult(
            ResolvedCardVariantId: resolvedVariantId,
            IdentityMatchStrong: identityStrong,
            GradedStatus: gradedStatus,
            Grader: grader,
            Grade: grade,
            RawCondition: rawCondition,
            VariantMatch: variantMatch,
            Language: language,
            BestOfferAccepted: input.ListingFormat == ListingFormat.BestOfferAccepted,
            KillReason: null,
            ConfidenceScore: score,
            Status: status);
    }

    private static ClassificationResult Killed(Guid queriedCardVariantId, string reason) => new(
        ResolvedCardVariantId: queriedCardVariantId,
        IdentityMatchStrong: false,
        GradedStatus: GradedStatus.Raw,
        Grader: null,
        Grade: null,
        RawCondition: RawConditionClassification.Unspecified,
        VariantMatch: VariantMatch.Mismatch,
        Language: "English",
        BestOfferAccepted: false,
        KillReason: reason,
        ConfidenceScore: 0,
        Status: ClassificationStatus.Rejected);

    private static (bool Strong, string? KillReason) MatchIdentity(string title, string cardName, string setName, string setNumber)
    {
        var normalizedExpected = NormalizeNumber(setNumber);
        var titleNumbers = AnySetNumberPattern.Matches(title).Select(m => NormalizeNumber(m.Value)).ToList();

        if (titleNumbers.Contains(normalizedExpected))
        {
            return (true, null);
        }

        if (titleNumbers.Count > 0)
        {
            // A set-number-shaped token is present but doesn't match this card's - likely a
            // different printing/card entirely, not just a vague listing.
            return (false, "wrong set number");
        }

        var hasName = title.Contains(cardName, StringComparison.OrdinalIgnoreCase);
        var hasSetName = !string.IsNullOrWhiteSpace(setName) && title.Contains(setName, StringComparison.OrdinalIgnoreCase);
        if (hasName && hasSetName)
        {
            return (false, null); // weak match, not killed - caller scores IdentityMatchWeak
        }

        return (false, "no identity match");
    }

    private static string NormalizeNumber(string value) =>
        Regex.Replace(value, @"\s+", string.Empty).ToUpperInvariant();

    private static (Guid ResolvedVariantId, VariantMatch Match, string? KillReason) MatchVariant(
        string title, Guid queriedCardVariantId, string targetVariantTypeName, IReadOnlyList<SiblingVariant> siblings, IReadOnlySet<Guid> inScope)
    {
        string? detected = null;
        foreach (var (variantTypeName, pattern) in VariantPatterns)
        {
            if (pattern.IsMatch(title))
            {
                detected = variantTypeName;
                break;
            }
        }

        if (detected is null)
        {
            // No special-variant token at all: for a Normal target this confirms it (Normal is
            // definitionally "none of the special markers"); for anything else, we simply can't
            // tell from the title.
            var match = targetVariantTypeName == "Normal" ? VariantMatch.Confirmed : VariantMatch.Ambiguous;
            return (queriedCardVariantId, match, null);
        }

        if (string.Equals(detected, targetVariantTypeName, StringComparison.OrdinalIgnoreCase))
        {
            return (queriedCardVariantId, VariantMatch.Confirmed, null);
        }

        var sibling = siblings.FirstOrDefault(s => string.Equals(s.VariantTypeName, detected, StringComparison.OrdinalIgnoreCase));
        if (sibling is not null && inScope.Contains(sibling.CardVariantId))
        {
            // Re-attribute: relative to the variant this listing actually resolves to, it's now a confirmed match.
            return (sibling.CardVariantId, VariantMatch.Confirmed, null);
        }

        return (queriedCardVariantId, VariantMatch.Mismatch, "variant mismatch, no in-scope sibling");
    }

    private static (GradedStatus Status, string? Grader, decimal? Grade) MatchGrading(string title)
    {
        if (GradedReadyPhrase.IsMatch(title))
        {
            return (GradedStatus.Raw, null, null);
        }

        if (GemMt10Phrase.IsMatch(title))
        {
            return (GradedStatus.Graded, "PSA", 10m);
        }

        var match = GraderGradePattern.Match(title);
        if (match.Success && decimal.TryParse(match.Groups[2].Value, out var grade))
        {
            return (GradedStatus.Graded, match.Groups[1].Value.ToUpperInvariant(), grade);
        }

        return (GradedStatus.Raw, null, null);
    }

    private static RawConditionClassification DetectCondition(string title)
    {
        foreach (var (condition, pattern) in ConditionPatterns)
        {
            if (pattern.IsMatch(title))
            {
                return condition;
            }
        }

        return RawConditionClassification.Unspecified;
    }

    private static string DetectLanguage(string title)
    {
        foreach (var (language, pattern) in LanguagePatterns)
        {
            if (pattern.IsMatch(title))
            {
                return language;
            }
        }

        return "English";
    }
}
