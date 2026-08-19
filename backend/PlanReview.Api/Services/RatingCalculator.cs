using PlanReview.Api.Models;

namespace PlanReview.Api.Services;

/// <summary>
/// Computes each developer's weighted normalized rating and fits the cohort of finals
/// onto a normal curve (z-scores + performance bands).
/// </summary>
public static class RatingCalculator
{
    public record Components(double? Self, double? Peer, double? Manager1, double? Manager2, double? WeightedFinal);

    /// <summary>Per-review component scores and the normalized weighted final.</summary>
    public static Components Compute(Review review)
    {
        // Self score = average of the developer's skill self-ratings.
        double? self = review.SkillRatings.Count > 0
            ? review.SkillRatings.Average(s => (double)s.SelfRating)
            : null;

        var submitted = review.Assessments.Where(a => a.SubmittedAt != null).ToList();

        double? peer = submitted
            .Where(a => a.ReviewerType == ReviewerType.Peer)
            .Select(a => (double?)a.OverallRating)
            .FirstOrDefault();

        // Attribute each manager assessment to its slot by the reviewer's assigned weight.
        double? m1 = null, m2 = null;
        foreach (var a in submitted.Where(a => a.ReviewerType == ReviewerType.Manager))
        {
            var weight = review.Reviewers.FirstOrDefault(r => r.ReviewerId == a.ReviewerId)?.Weight ?? 0;
            if (weight >= (ReviewRules.Manager1Weight + ReviewRules.Manager2Weight) / 2) m2 = a.OverallRating;
            else m1 = a.OverallRating;
        }

        // Normalized weighted average across whichever components are present.
        var comps = new List<(double w, double s)>();
        if (self is not null) comps.Add((ReviewRules.SelfWeight, self.Value));
        if (peer is not null) comps.Add((ReviewRules.PeerWeight, peer.Value));
        if (m1 is not null) comps.Add((ReviewRules.Manager1Weight, m1.Value));
        if (m2 is not null) comps.Add((ReviewRules.Manager2Weight, m2.Value));

        double? final = comps.Count > 0
            ? Math.Round(comps.Sum(c => c.w * c.s) / comps.Sum(c => c.w), 2)
            : null;

        return new Components(self, peer, m1, m2, final);
    }

    /// <summary>Population mean and standard deviation.</summary>
    public static (double mean, double std) MeanStd(IReadOnlyList<double> xs)
    {
        if (xs.Count == 0) return (0, 0);
        var mean = xs.Average();
        var variance = xs.Sum(x => (x - mean) * (x - mean)) / xs.Count;
        return (mean, Math.Sqrt(variance));
    }

    /// <summary>Standard normal CDF Φ(z) via the Abramowitz &amp; Stegun erf approximation.</summary>
    public static double NormalCdf(double z)
    {
        // erf(x) approximation (max error ~1.5e-7).
        double x = z / Math.Sqrt(2);
        int sign = x < 0 ? -1 : 1;
        x = Math.Abs(x);
        double t = 1.0 / (1.0 + 0.3275911 * x);
        double y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
        double erf = sign * y;
        return 0.5 * (1.0 + erf);
    }

    /// <summary>Bell-curve performance band for a z-score.</summary>
    public static string Band(double z) => z switch
    {
        >= 1.5 => "Outstanding",
        >= 0.5 => "Exceeds",
        >= -0.5 => "Meets",
        >= -1.5 => "Below",
        _ => "Needs Improvement",
    };

    public static readonly (string Band, double Lower, double Upper)[] BandRanges =
    {
        ("Needs Improvement", double.NegativeInfinity, -1.5),
        ("Below", -1.5, -0.5),
        ("Meets", -0.5, 0.5),
        ("Exceeds", 0.5, 1.5),
        ("Outstanding", 1.5, double.PositiveInfinity),
    };
}
