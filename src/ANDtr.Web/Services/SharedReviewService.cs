using ANDtr.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Services;

// Read-only customer view of shared reviews. Every query starts from OrganisationService.SharedWith,
// so a customer can only ever load reviews shared with their own organisation (A2).
public sealed class SharedReviewService(OrganisationService organisationService)
{
    public Task<List<SharedReviewListItem>> ListAsync(int organisationId) =>
        organisationService.SharedWith(organisationId)
            .AsNoTracking()
            .OrderByDescending(r => r.SharedAt)
            .Select(r => new SharedReviewListItem
            {
                Id = r.Id,
                Name = r.Name,
                SharedBy = r.Owner!.FullName,
                SharedAt = r.SharedAt!.Value,
                NoticeCount = r.ReviewNotices.Count,
                CommentCount = r.Comments.Count(c => !c.IsInternal),
            })
            .ToListAsync();

    // Null when the review does not exist or is not shared with this organisation - the caller shows
    // Access denied in both cases, so nobody can tell whether another organisation's review exists.
    public Task<SharedReviewDetails?> GetAsync(int organisationId, int reviewId) =>
        organisationService.SharedWith(organisationId)
            .AsNoTracking()
            .Where(r => r.Id == reviewId)
            .Select(r => new SharedReviewDetails
            {
                Id = r.Id,
                Name = r.Name,
                OrganisationName = r.Organisation!.Name,
                SharedBy = r.Owner!.FullName,
                SharedAt = r.SharedAt!.Value,
                Notices = r.ReviewNotices
                    .OrderBy(rn => rn.AddedAt)
                    .Select(rn => new SharedNoticeItem
                    {
                        RecallNumber = rn.Notice!.RecallNumber,
                        ProductCodeName = rn.Notice.ProductCodeName,
                        ProductDescription = rn.Notice.ProductDescription,
                        FirmName = rn.Notice.FirmName,
                        RecallClass = rn.Notice.RecallClass,
                        Reason = rn.Notice.ManufacturerRecallReason,
                        WebAddress = rn.Notice.WebAddress,
                    })
                    .ToList(),
                Comments = r.Comments
                    .Where(c => !c.IsInternal)
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new SharedCommentItem
                    {
                        Author = c.Author!.FullName,
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync();
}
