using System;
using System.Linq;
using Reyla.Services.Models;

namespace Reyla.Services
{
    /// <summary>
    /// Manages broker-maintained owner contact records (ProspectOwnerContact).
    ///
    /// One record per Property+Organization pair, created on demand.
    /// Pre-seeded from CoreLogic ownership data when first created so the
    /// broker isn't starting from a blank form.
    /// </summary>
    public class OwnerContactService
    {
        // ----------------------------------------------------------------
        // Result object
        // ----------------------------------------------------------------

        public class SaveContactResult
        {
            public bool   Success      { get; set; }
            public string ErrorMessage { get; set; }

            public static SaveContactResult Fail(string msg) =>
                new SaveContactResult { Success = false, ErrorMessage = msg };
        }

        // ----------------------------------------------------------------
        // GetOrCreate — returns existing record or seeds a new one.
        // Pass coreLogicOwnerName / coreLogicMailingAddress from the
        // already-loaded PropertyProfileResult so we avoid an extra API call.
        // ----------------------------------------------------------------

        public ProspectOwnerContact GetOrCreate(
            Guid   propertyId,
            Guid   organizationId,
            Guid   userId,
            string coreLogicOwnerName    = null,
            string coreLogicMailingAddr  = null)
        {
            using (var db = new DCReyla())
            {
                var existing = db.ProspectOwnerContacts
                    .FirstOrDefault(c =>
                        c.PropertyId     == propertyId &&
                        c.OrganizationId == organizationId);

                if (existing != null) return existing;

                // First visit — seed from CoreLogic data
                var record = new ProspectOwnerContact
                {
                    ProspectOwnerContactId = Guid.NewGuid(),
                    PropertyId             = propertyId,
                    OrganizationId         = organizationId,
                    ContactName            = TruncateTo(coreLogicOwnerName,  200),
                    MailingAddress         = TruncateTo(coreLogicMailingAddr, 500),
                    Phone                  = null,
                    Email                  = null,
                    PreferredContact       = null,
                    Notes                  = null,
                    CreatedAtUtc           = DateTime.UtcNow
                };

                db.ProspectOwnerContacts.InsertOnSubmit(record);
                db.SubmitChanges();

                return record;
            }
        }

        // ----------------------------------------------------------------
        // Save — updates all editable fields from form input.
        // Fires ActivityLog entry on success.
        // ----------------------------------------------------------------

        public SaveContactResult Save(
            Guid   propertyId,
            Guid   organizationId,
            Guid   userId,
            string contactName,
            string phone,
            string email,
            string preferredContact,
            string mailingAddress,
            string notes,
            string subjectAddress = null)   // for activity summary
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var record = db.ProspectOwnerContacts
                        .FirstOrDefault(c =>
                            c.PropertyId     == propertyId &&
                            c.OrganizationId == organizationId);

                    if (record == null)
                    {
                        // Shouldn't happen if GetOrCreate was called on page load,
                        // but handle gracefully
                        record = new ProspectOwnerContact
                        {
                            ProspectOwnerContactId = Guid.NewGuid(),
                            PropertyId             = propertyId,
                            OrganizationId         = organizationId,
                            CreatedAtUtc           = DateTime.UtcNow
                        };
                        db.ProspectOwnerContacts.InsertOnSubmit(record);
                    }

                    record.ContactName      = TruncateTo(contactName?.Trim(),      200);
                    record.Phone            = TruncateTo(phone?.Trim(),             50);
                    record.Email            = TruncateTo(email?.Trim(),            200);
                    record.PreferredContact = TruncateTo(preferredContact?.Trim(),  20);
                    record.MailingAddress   = TruncateTo(mailingAddress?.Trim(),   500);
                    record.Notes            = TruncateTo(notes?.Trim(),           1000);
                    record.UpdatedAtUtc     = DateTime.UtcNow;
                    record.UpdatedBy        = userId;

                    db.SubmitChanges();

                    // Look up CLIP and ProspectId for the activity link
                    string clip          = null;
                    string prospectIdStr = null;
                    try
                    {
                        var prop = db.Properties.FirstOrDefault(p => p.PropertyId == propertyId);
                        clip = prop?.Clip;

                        var prospect = db.Prospects.FirstOrDefault(p =>
                            p.PropertyId     == propertyId &&
                            p.OrganizationId == organizationId &&
                            p.IsDeleted      == false);
                        if (prospect != null)
                            prospectIdStr = prospect.ProspectId.ToString();
                    }
                    catch { /* non-fatal */ }

                    // Fire activity log inside using so clip/prospectId are in scope
                    new ActivityService().Track(
                        ActivityType.ContactInfoUpdated,
                        ActivityEntityType.Property,
                        propertyId,
                        userId,
                        organizationId,
                        new { subjectAddress = subjectAddress ?? "a property", clip = clip, prospectId = prospectIdStr });
                }

                return new SaveContactResult { Success = true };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "[OwnerContactService.Save] PropertyId={0}: {1}", propertyId, ex.Message);
                return SaveContactResult.Fail("Failed to save contact: " + ex.Message);
            }
        }

        // ----------------------------------------------------------------
        // Private helpers
        // ----------------------------------------------------------------

        private static string TruncateTo(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
