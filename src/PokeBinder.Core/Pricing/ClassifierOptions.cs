namespace PokeBinder.Core.Pricing;

/// <summary>Scoring weights and acceptance thresholds - deliberately data, not code, so they can be tuned via configuration without a redeploy.</summary>
public class ClassifierOptions
{
    public int IdentityMatchStrong { get; set; } = 40;
    public int IdentityMatchWeak { get; set; } = 15;
    public int VariantMatchConfirmed { get; set; } = 15;
    public int VariantMatchAmbiguous { get; set; } = 5;
    public int GradedConfident { get; set; } = 25;
    public int RawConditionStated { get; set; } = 10;
    public int RawConditionUnspecified { get; set; } = 5;
    public int LanguageEnglish { get; set; } = 10;

    public int AutoAcceptThreshold { get; set; } = 70;
    public int QuarantineThreshold { get; set; } = 40;
}
