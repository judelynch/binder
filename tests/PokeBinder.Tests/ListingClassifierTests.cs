using PokeBinder.Core.Pricing;
using Xunit;

namespace PokeBinder.Tests;

/// <summary>
/// Table-driven coverage for IListingClassifier per Phase 8 section 12, plus the re-attribution
/// cases (this app's chosen behavior beyond the base spec) and explicit confidence-banding checks.
/// Every case uses a fixed Gengar/Fusion Strike/66-107 fixture unless otherwise noted, so titles
/// are real-shaped and the only thing varying is the one signal under test.
/// </summary>
public class ListingClassifierTests
{
    private static readonly Guid QueriedVariantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly ListingClassifier _classifier = new(new ClassifierOptions());

    private static ListingClassificationInput Input(
        string title,
        string target = "Normal",
        ListingFormat format = ListingFormat.BuyItNow,
        IReadOnlyList<SiblingVariant>? siblings = null,
        IReadOnlySet<Guid>? inScope = null) =>
        new(
            Title: title,
            ListingFormat: format,
            CardName: "Gengar",
            SetName: "Fusion Strike",
            SetNumber: "66/107",
            TargetVariantTypeName: target,
            SiblingVariants: siblings ?? Array.Empty<SiblingVariant>(),
            InScopeCardVariantIds: inScope ?? new HashSet<Guid>());

    private ClassificationResult Classify(ListingClassificationInput input) => _classifier.Classify(QueriedVariantId, input);

    // ---- Clean raw ----

    [Fact]
    public void CleanRawNM_AutoAccepted()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike NM"));
        Assert.True(result.IdentityMatchStrong);
        Assert.Equal(GradedStatus.Raw, result.GradedStatus);
        Assert.Equal(RawConditionClassification.NM, result.RawCondition);
        Assert.Equal(ClassificationStatus.AutoAccepted, result.Status);
        Assert.Null(result.KillReason);
    }

    [Fact]
    public void CleanRaw_NoConditionStated_StillClassifiesAsUnspecified_NotKilled()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike"));
        Assert.Equal(RawConditionClassification.Unspecified, result.RawCondition);
        Assert.Null(result.KillReason);
        Assert.NotEqual(0, result.ConfidenceScore);
    }

    // ---- Grading ----

    [Fact]
    public void Psa10_Graded_AutoAccepted()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike PSA 10"));
        Assert.Equal(GradedStatus.Graded, result.GradedStatus);
        Assert.Equal("PSA", result.Grader);
        Assert.Equal(10m, result.Grade);
        Assert.Equal(ClassificationStatus.AutoAccepted, result.Status);
    }

    [Fact]
    public void Psa10Lowercase_NoSpace_ParsesAsGraded()
    {
        var result = Classify(Input("gengar 66/107 fusion strike psa10"));
        Assert.Equal(GradedStatus.Graded, result.GradedStatus);
        Assert.Equal("PSA", result.Grader);
        Assert.Equal(10m, result.Grade);
    }

    [Fact]
    public void Bgs95_Graded_ParsesDecimalGrade()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike BGS 9.5"));
        Assert.Equal(GradedStatus.Graded, result.GradedStatus);
        Assert.Equal("BGS", result.Grader);
        Assert.Equal(9.5m, result.Grade);
    }

    [Fact]
    public void Cgc9_Graded_NoDecimal()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike CGC 9"));
        Assert.Equal(GradedStatus.Graded, result.GradedStatus);
        Assert.Equal("CGC", result.Grader);
        Assert.Equal(9m, result.Grade);
    }

    [Fact]
    public void GemMt10Phrase_ParsesAsPsa10()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike GEM MT 10"));
        Assert.Equal(GradedStatus.Graded, result.GradedStatus);
        Assert.Equal("PSA", result.Grader);
        Assert.Equal(10m, result.Grade);
    }

    [Fact]
    public void PsaReady_MustBeRaw_NotGraded()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike PSA ready NM"));
        Assert.Equal(GradedStatus.Raw, result.GradedStatus);
        Assert.Null(result.Grader);
        Assert.Null(result.Grade);
    }

    [Fact]
    public void WorthyOfGrading_MustBeRaw_NotGraded()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike mint, worthy of grading"));
        Assert.Equal(GradedStatus.Raw, result.GradedStatus);
        Assert.Null(result.Grader);
    }

    // ---- Variant match ----

    [Fact]
    public void ReverseHolo_VariantConfirmed_WhenTargetIsReverseHolo()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike Reverse Holo", target: "Reverse Holo"));
        Assert.Equal(VariantMatch.Confirmed, result.VariantMatch);
        Assert.Equal(QueriedVariantId, result.ResolvedCardVariantId);
    }

    [Fact]
    public void FirstEdition_VariantConfirmed_WhenTargetIs1stEdition()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike 1st Edition", target: "1st Edition"));
        Assert.Equal(VariantMatch.Confirmed, result.VariantMatch);
    }

    [Fact]
    public void Shadowless_VariantConfirmed_WhenTargetIsShadowless()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike Shadowless", target: "Shadowless"));
        Assert.Equal(VariantMatch.Confirmed, result.VariantMatch);
    }

    [Fact]
    public void Holo_VariantConfirmed_WhenTargetIsHolo()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike Holo", target: "Holo"));
        Assert.Equal(VariantMatch.Confirmed, result.VariantMatch);
    }

    [Fact]
    public void NoVariantTokens_TargetNormal_ConfirmedByAbsence()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike", target: "Normal"));
        Assert.Equal(VariantMatch.Confirmed, result.VariantMatch);
    }

    [Fact]
    public void NoVariantTokens_TargetReverseHolo_Ambiguous()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike", target: "Reverse Holo"));
        Assert.Equal(VariantMatch.Ambiguous, result.VariantMatch);
        Assert.Null(result.KillReason);
    }

    [Fact]
    public void VariantMismatch_ReattachesToInScopeSibling()
    {
        var siblingId = Guid.NewGuid();
        var siblings = new[] { new SiblingVariant(siblingId, "Reverse Holo") };
        var inScope = new HashSet<Guid> { siblingId };

        var result = Classify(Input("Gengar 66/107 Fusion Strike Reverse Holo", target: "Normal", siblings: siblings, inScope: inScope));

        Assert.Equal(siblingId, result.ResolvedCardVariantId);
        Assert.Equal(VariantMatch.Confirmed, result.VariantMatch);
        Assert.Null(result.KillReason);
    }

    [Fact]
    public void VariantMismatch_NoSiblingExists_Killed()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike Reverse Holo", target: "Normal"));
        Assert.Equal("variant mismatch, no in-scope sibling", result.KillReason);
        Assert.Equal(ClassificationStatus.Rejected, result.Status);
        Assert.Equal(0, result.ConfidenceScore);
    }

    [Fact]
    public void VariantMismatch_SiblingExistsButOutOfScope_Killed()
    {
        var siblingId = Guid.NewGuid();
        var siblings = new[] { new SiblingVariant(siblingId, "Reverse Holo") };
        // sibling deliberately NOT added to inScope

        var result = Classify(Input("Gengar 66/107 Fusion Strike Reverse Holo", target: "Normal", siblings: siblings));

        Assert.Equal("variant mismatch, no in-scope sibling", result.KillReason);
        Assert.Equal(QueriedVariantId, result.ResolvedCardVariantId);
    }

    // ---- Language ----

    [Fact]
    public void Japanese_ExcludedLanguage_ClassifiedButNotEnglish()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike Japanese NM"));
        Assert.Equal("Japanese", result.Language);
        Assert.Null(result.KillReason);
    }

    [Fact]
    public void Korean_ExcludedLanguage()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike Korean"));
        Assert.Equal("Korean", result.Language);
    }

    [Fact]
    public void German_ExcludedLanguage()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike German NM"));
        Assert.Equal("German", result.Language);
    }

    // ---- Kill filters ----

    [Fact]
    public void Joblot_Killed()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike joblot bundle of cards"));
        Assert.NotNull(result.KillReason);
        Assert.Equal(ClassificationStatus.Rejected, result.Status);
    }

    [Fact]
    public void Bundle_Killed()
    {
        var result = Classify(Input("Pokemon card bundle Gengar 66/107"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void LotOf_Killed()
    {
        var result = Classify(Input("Lot of 10 Pokemon cards inc. Gengar 66/107"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void ChooseYourCard_Killed()
    {
        var result = Classify(Input("Pokemon cards - choose your card - Gengar 66/107 available"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void PickYourCard_Killed()
    {
        var result = Classify(Input("Pick your card! Gengar 66/107 and more"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void QuantityX4_Killed()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike x4"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void Sealed_Killed()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike sealed pack"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void Custom_Killed()
    {
        var result = Classify(Input("Gengar 66/107 custom art card"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void Orica_Killed()
    {
        var result = Classify(Input("Gengar 66/107 orica"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void Proxy_Killed()
    {
        var result = Classify(Input("Gengar 66/107 proxy card"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void Metal_Killed()
    {
        var result = Classify(Input("Gengar 66/107 metal card novelty"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void Repack_Killed()
    {
        var result = Classify(Input("Gengar 66/107 mystery repack"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void SleeveOnly_Killed()
    {
        var result = Classify(Input("Gengar 66/107 sleeve only, no card"));
        Assert.NotNull(result.KillReason);
    }

    [Fact]
    public void EmptyBoxOrSleeve_Killed()
    {
        var result = Classify(Input("Gengar 66/107 empty display box"));
        Assert.NotNull(result.KillReason);
    }

    // ---- Best offer ----

    [Fact]
    public void BestOfferAccepted_NotKilled_ButFlagged()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike NM", format: ListingFormat.BestOfferAccepted));
        Assert.Null(result.KillReason);
        Assert.True(result.BestOfferAccepted);
    }

    [Fact]
    public void Auction_NotFlaggedAsBestOffer()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike NM", format: ListingFormat.Auction));
        Assert.False(result.BestOfferAccepted);
    }

    // ---- Identity match ----

    [Fact]
    public void WrongSetNumber_Killed()
    {
        var result = Classify(Input("Gengar 20/102 Base Set"));
        Assert.Equal("wrong set number", result.KillReason);
    }

    [Fact]
    public void NoSetNumber_ButNameAndSetNamePresent_WeakMatch()
    {
        // Deliberately no variant tokens (e.g. "holo") in this title - target defaults to Normal,
        // so an accidental variant-mismatch kill would mask the identity assertion being tested here.
        var result = Classify(Input("Gengar Fusion Strike rare card"));
        Assert.Null(result.KillReason);
        Assert.False(result.IdentityMatchStrong);
    }

    [Fact]
    public void NoSetNumber_NoNameMatch_Killed()
    {
        var result = Classify(Input("Mystery Pokemon card holo rare"));
        Assert.Equal("no identity match", result.KillReason);
    }

    [Fact]
    public void SetNumberWithSpacesAroundSlash_StillMatchesStrong()
    {
        var result = Classify(Input("Gengar 66 / 107 Fusion Strike"));
        Assert.True(result.IdentityMatchStrong);
        Assert.Null(result.KillReason);
    }

    // ---- Condition mapping ----

    [Fact]
    public void PackFresh_MapsToNMCondition()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike pack fresh"));
        Assert.Equal(RawConditionClassification.NM, result.RawCondition);
    }

    [Fact]
    public void Damaged_MapsToDMGCondition()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike damaged corners"));
        Assert.Equal(RawConditionClassification.DMG, result.RawCondition);
    }

    [Fact]
    public void LightlyPlayed_MapsToLP()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike lightly played"));
        Assert.Equal(RawConditionClassification.LP, result.RawCondition);
    }

    [Fact]
    public void Played_MapsToMP()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike played condition"));
        Assert.Equal(RawConditionClassification.MP, result.RawCondition);
    }

    [Fact]
    public void Mint_MapsToNM()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike mint condition"));
        Assert.Equal(RawConditionClassification.NM, result.RawCondition);
    }

    [Fact]
    public void HeavilyPlayed_MapsToHP()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike heavily played"));
        Assert.Equal(RawConditionClassification.HP, result.RawCondition);
    }

    [Fact]
    public void HpStatInTitle_NotConfusedWithHeavilyPlayedCondition()
    {
        // "180HP" is the Pokemon's printed HP stat, not a condition abbreviation - must not be
        // misread as Heavily Played just because "HP" appears immediately after a number.
        var result = Classify(Input("Gengar 66/107 Fusion Strike 180HP"));
        Assert.Equal(RawConditionClassification.Unspecified, result.RawCondition);
    }

    // ---- Confidence banding ----

    [Fact]
    public void ConfidenceBanding_StrongIdentityConfirmedVariantStatedConditionEnglish_AutoAccepted()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike NM", target: "Normal"));
        Assert.True(result.ConfidenceScore >= 70);
        Assert.Equal(ClassificationStatus.AutoAccepted, result.Status);
    }

    [Fact]
    public void ConfidenceBanding_StrongIdentityAmbiguousVariantUnspecifiedCondition_Quarantined()
    {
        var result = Classify(Input("Gengar 66/107 Fusion Strike", target: "Reverse Holo"));
        Assert.InRange(result.ConfidenceScore, 40, 69);
        Assert.Equal(ClassificationStatus.Quarantined, result.Status);
    }

    [Fact]
    public void ConfidenceBanding_WeakIdentityAmbiguousVariantForeignLanguage_Rejected()
    {
        var result = Classify(Input("Gengar Fusion Strike Japanese", target: "Reverse Holo"));
        Assert.True(result.ConfidenceScore < 40);
        Assert.Equal(ClassificationStatus.Rejected, result.Status);
        Assert.Null(result.KillReason); // low-confidence reject, not a hard kill
    }
}
