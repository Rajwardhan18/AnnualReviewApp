namespace PlanReview.Api.Models;

/// <summary>
/// An assigned reviewer for a review. Admin assigns 2 Managers and 1 Peer
/// (requirement 11). The Peer is normally the one the developer selected.
/// </summary>
public class ReviewReviewer
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review? Review { get; set; }

    public int ReviewerId { get; set; }
    public User? Reviewer { get; set; }

    public ReviewerType ReviewerType { get; set; }

    /// <summary>
    /// Weight of this reviewer in the normalized final rating.
    /// Peer = 0.20, Manager 1 = 0.30, Manager 2 = 0.40 (self-rating carries 0.10, held
    /// separately as it is not a reviewer). Managers are distinguished by their weight.
    /// </summary>
    public double Weight { get; set; }
}
