using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace Reyla.Services
{
    // ===================================================================
    // MODELS
    // ===================================================================

    public class ContactRow
    {
        public Guid             ContactId       { get; set; }
        public string           FirstName       { get; set; }
        public string           LastName        { get; set; }
        public string           FullName        => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
                                                    ? "(No Name)"
                                                    : $"{FirstName} {LastName}".Trim();
        public string           CompanyName     { get; set; }
        public string           Email           { get; set; }
        public string           Phone           { get; set; }
        public string           MobilePhone     { get; set; }
        public string           LinkedInUrl     { get; set; }
        public string           Notes           { get; set; }
        public bool             IsActive        { get; set; }
        public string           PreferredContact { get; set; }
        public DateTime         CreatedAtUtc    { get; set; }
        public DateTime?        UpdatedAtUtc    { get; set; }

        // Contact types assigned to this contact
        public List<string>     ContactTypes    { get; set; } = new List<string>();

        // Properties this contact is linked to
        public List<ContactPropertyLink> Properties { get; set; } = new List<ContactPropertyLink>();

        // Buyer score — calculated, never stored
        public int?             BuyerScore      { get; set; }
    }

    public class ContactPropertyLink
    {
        public Guid     PropertyId      { get; set; }
        public Guid     PropertyContactId { get; set; }
        public string   StreetAddress   { get; set; }
        public string   CityNameRaw     { get; set; }
        public string   ZipCodeRaw      { get; set; }
        public string   Clip            { get; set; }
        public bool     IsPrimary       { get; set; }

        // Snapshot data for buyer score and display
        public decimal? EstimatedValue      { get; set; }
        public decimal? EstimatedEquity     { get; set; }
        public decimal? PropensityScore     { get; set; }
        public bool     HasInvoluntaryLiens { get; set; }
        public bool     HasTaxDelinquency   { get; set; }
        public int      ActiveDealCount     { get; set; }
    }

    public class ContactSaveRequest
    {
        public Guid?            ContactId       { get; set; }   // null = new contact
        public string           FirstName       { get; set; }
        public string           LastName        { get; set; }
        public string           CompanyName     { get; set; }
        public string           Email           { get; set; }
        public string           Phone           { get; set; }
        public string           MobilePhone     { get; set; }
        public string           LinkedInUrl     { get; set; }
        public string           Notes           { get; set; }
        public List<Guid>       ContactTypeIds  { get; set; } = new List<Guid>();
    }

    public class ContactSaveResult
    {
        public bool     Success         { get; set; }
        public string   Error           { get; set; }
        public Guid?    ContactId       { get; set; }
        public bool     WasDuplicate    { get; set; }   // true if existing contact was returned
    }

    public class ContactTypeRow
    {
        public Guid     ContactTypeId   { get; set; }
        public string   Name            { get; set; }
        public int      SortOrder       { get; set; }
    }

    // ===================================================================
    // SERVICE
    // ===================================================================

    public class ContactService
    {
        // ---------------------------------------------------------------
        // Visibility check
        // Can the current user see all org contacts, or only their own?
        // ---------------------------------------------------------------
        public static bool CanSeeAllContacts(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;

            return Roles.IsUserInRole(username, "Administrator")
                || Roles.IsUserInRole(username, "OrganizationAdministrator")
                || Roles.IsUserInRole(username, "OrganizationContactAccess");
        }

        // ---------------------------------------------------------------
        // Get all contact types (for dropdowns / multi-select UI)
        // ---------------------------------------------------------------
        public List<ContactTypeRow> GetContactTypes()
        {
            using (var db = new DCReyla())
            {
                return db.ContactTypes
                    .Where(ct => ct.IsActive)
                    .OrderBy(ct => ct.SortOrder)
                    .Select(ct => new ContactTypeRow
                    {
                        ContactTypeId   = ct.ContactTypeId,
                        Name            = ct.Name,
                        SortOrder       = ct.SortOrder
                    })
                    .ToList();
            }
        }

        // ---------------------------------------------------------------
        // Get contacts for org — respects visibility rules
        // ---------------------------------------------------------------
        public List<ContactRow> GetContactsForOrg(Guid organizationId, Guid requestingUserId, string username)
        {
            using (var db = new DCReyla())
            {
                var query = db.Contacts
                    .Where(c => c.OrganizationId == organizationId && c.IsDeleted == false);

                // If user cannot see all contacts, limit to ones they created
                if (!CanSeeAllContacts(username))
                    query = query.Where(c => c.CreatedBy == requestingUserId);

                var contacts = query
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToList();

                return contacts.Select(c => MapToRow(db, c, includeProperties: false)).ToList();
            }
        }

        // ---------------------------------------------------------------
        // Get contacts for a specific property
        // ---------------------------------------------------------------
        public List<ContactRow> GetContactsForProperty(Guid propertyId, Guid organizationId)
        {
            using (var db = new DCReyla())
            {
                var links = db.PropertyContacts
                    .Where(pc =>
                        pc.PropertyId       == propertyId &&
                        pc.OrganizationId   == organizationId &&
                        pc.IsDeleted        == false)
                    .OrderByDescending(pc => pc.IsPrimary)
                    .ToList();

                var result = new List<ContactRow>();
                foreach (var link in links)
                {
                    var contact = db.Contacts
                        .FirstOrDefault(c => c.ContactId == link.ContactId && c.IsDeleted == false);
                    if (contact == null) continue;

                    var row = MapToRow(db, contact, includeProperties: false);
                    row.Properties.Add(new ContactPropertyLink
                    {
                        PropertyContactId   = link.PropertyContactId,
                        PropertyId          = link.PropertyId,
                        IsPrimary           = link.IsPrimary
                    });
                    result.Add(row);
                }
                return result;
            }
        }

        // ---------------------------------------------------------------
        // Get a single contact with full property list and buyer score
        // ---------------------------------------------------------------
        public ContactRow GetContact(Guid contactId, Guid organizationId, Guid requestingUserId, string username)
        {
            using (var db = new DCReyla())
            {
                var contact = db.Contacts
                    .FirstOrDefault(c =>
                        c.ContactId         == contactId &&
                        c.OrganizationId    == organizationId &&
                        c.IsDeleted         == false);

                if (contact == null) return null;

                // Visibility check
                if (!CanSeeAllContacts(username) && contact.CreatedBy != requestingUserId)
                    return null;

                var row = MapToRow(db, contact, includeProperties: true);
                row.BuyerScore = CalculateBuyerScore(row.Properties);
                return row;
            }
        }

        // ---------------------------------------------------------------
        // Dedup check — returns existing ContactId if match found
        // Matches on Email OR Phone within the same org
        // ---------------------------------------------------------------
        public Guid? FindDuplicate(string email, string phone, Guid organizationId, Guid? excludeContactId = null)
        {
            using (var db = new DCReyla())
            {
                var query = db.Contacts
                    .Where(c => c.OrganizationId == organizationId && c.IsDeleted == false);

                if (excludeContactId.HasValue)
                    query = query.Where(c => c.ContactId != excludeContactId.Value);

                Contact match = null;

                if (!string.IsNullOrWhiteSpace(email))
                    match = query.FirstOrDefault(c => c.Email == email.Trim());

                if (match == null && !string.IsNullOrWhiteSpace(phone))
                    match = query.FirstOrDefault(c => c.Phone == phone.Trim());

                return match?.ContactId;
            }
        }

        // ---------------------------------------------------------------
        // Save contact — create or update
        // Returns existing contact if dedup match found
        // ---------------------------------------------------------------
        public ContactSaveResult SaveContact(ContactSaveRequest request, Guid organizationId, Guid createdByUserId)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var now = DateTime.UtcNow;

                    // ── New contact ──────────────────────────────────────
                    if (!request.ContactId.HasValue)
                    {
                        // Dedup check before creating
                        var dupId = FindDuplicate(request.Email, request.Phone, organizationId);
                        if (dupId.HasValue)
                        {
                            return new ContactSaveResult
                            {
                                Success         = true,
                                ContactId       = dupId,
                                WasDuplicate    = true,
                                Error           = "A contact with this email or phone already exists. The existing contact has been returned."
                            };
                        }

                        var contact = new Contact
                        {
                            ContactId       = Guid.NewGuid(),
                            FirstName       = request.FirstName?.Trim(),
                            LastName        = request.LastName?.Trim(),
                            CompanyName     = request.CompanyName?.Trim(),
                            Email           = request.Email?.Trim(),
                            Phone           = request.Phone?.Trim(),
                            MobilePhone     = request.MobilePhone?.Trim(),
                            LinkedInUrl     = request.LinkedInUrl?.Trim(),
                            Notes           = request.Notes?.Trim(),
                            OrganizationId  = organizationId,
                            CreatedBy       = createdByUserId,
                            IsActive        = true,
                            IsDeleted       = false,
                            CreatedAtUtc    = now
                        };

                        db.Contacts.InsertOnSubmit(contact);
                        db.SubmitChanges();

                        // Save contact types
                        SaveContactTypes(db, contact.ContactId, request.ContactTypeIds, now);

                        return new ContactSaveResult
                        {
                            Success     = true,
                            ContactId   = contact.ContactId
                        };
                    }

                    // ── Update existing contact ──────────────────────────
                    var existing = db.Contacts
                        .FirstOrDefault(c =>
                            c.ContactId     == request.ContactId.Value &&
                            c.OrganizationId == organizationId &&
                            c.IsDeleted     == false);

                    if (existing == null)
                        return new ContactSaveResult { Success = false, Error = "Contact not found." };

                    // Dedup check excluding this contact
                    var dupIdOnUpdate = FindDuplicate(request.Email, request.Phone, organizationId, request.ContactId);
                    if (dupIdOnUpdate.HasValue)
                        return new ContactSaveResult
                        {
                            Success         = false,
                            Error           = "Another contact with this email or phone already exists.",
                            WasDuplicate    = true,
                            ContactId       = dupIdOnUpdate
                        };

                    existing.FirstName      = request.FirstName?.Trim();
                    existing.LastName       = request.LastName?.Trim();
                    existing.CompanyName    = request.CompanyName?.Trim();
                    existing.Email          = request.Email?.Trim();
                    existing.Phone          = request.Phone?.Trim();
                    existing.MobilePhone    = request.MobilePhone?.Trim();
                    existing.LinkedInUrl    = request.LinkedInUrl?.Trim();
                    existing.Notes          = request.Notes?.Trim();
                    existing.UpdatedBy      = createdByUserId;
                    existing.UpdatedAtUtc   = now;

                    db.SubmitChanges();

                    // Replace contact types
                    SaveContactTypes(db, existing.ContactId, request.ContactTypeIds, now);

                    return new ContactSaveResult
                    {
                        Success     = true,
                        ContactId   = existing.ContactId
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContactService.SaveContact] {0}", ex);
                return new ContactSaveResult { Success = false, Error = "An error occurred saving the contact." };
            }
        }

        // ---------------------------------------------------------------
        // Soft delete a contact
        // ---------------------------------------------------------------
        public bool DeleteContact(Guid contactId, Guid organizationId, Guid deletedByUserId)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var contact = db.Contacts
                        .FirstOrDefault(c =>
                            c.ContactId     == contactId &&
                            c.OrganizationId == organizationId &&
                            c.IsDeleted     == false);

                    if (contact == null) return false;

                    contact.IsDeleted       = true;
                    contact.IsActive        = false;
                    contact.UpdatedBy       = deletedByUserId;
                    contact.UpdatedAtUtc    = DateTime.UtcNow;
                    db.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContactService.DeleteContact] {0}", ex);
                return false;
            }
        }

        // ---------------------------------------------------------------
        // Link a contact to a property
        // ---------------------------------------------------------------
        public bool LinkContactToProperty(Guid contactId, Guid propertyId, Guid organizationId, Guid createdByUserId, bool isPrimary = false)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    // Check if already linked
                    var existing = db.PropertyContacts
                        .FirstOrDefault(pc =>
                            pc.ContactId        == contactId &&
                            pc.PropertyId       == propertyId &&
                            pc.OrganizationId   == organizationId &&
                            pc.IsDeleted        == false);

                    if (existing != null) return true; // already linked

                    if (isPrimary)
                        DemoteExistingPrimary(db, propertyId, organizationId);

                    db.PropertyContacts.InsertOnSubmit(new PropertyContact
                    {
                        PropertyContactId   = Guid.NewGuid(),
                        PropertyId          = propertyId,
                        ContactId           = contactId,
                        IsPrimary           = isPrimary,
                        OrganizationId      = organizationId,
                        CreatedBy           = createdByUserId,
                        IsActive            = true,
                        IsDeleted           = false,
                        CreatedAtUtc        = DateTime.UtcNow
                    });

                    db.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContactService.LinkContactToProperty] {0}", ex);
                return false;
            }
        }

        // ---------------------------------------------------------------
        // Unlink a contact from a property
        // ---------------------------------------------------------------
        public bool UnlinkContactFromProperty(Guid propertyContactId, Guid organizationId, Guid updatedByUserId)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var link = db.PropertyContacts
                        .FirstOrDefault(pc =>
                            pc.PropertyContactId    == propertyContactId &&
                            pc.OrganizationId       == organizationId &&
                            pc.IsDeleted            == false);

                    if (link == null) return false;

                    link.IsDeleted      = true;
                    link.IsActive       = false;
                    link.UpdatedBy      = updatedByUserId;
                    link.UpdatedAtUtc   = DateTime.UtcNow;
                    db.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContactService.UnlinkContactFromProperty] {0}", ex);
                return false;
            }
        }

        // ---------------------------------------------------------------
        // Set a contact as primary for a property
        // Demotes any existing primary first
        // ---------------------------------------------------------------
        public bool SetPrimaryContact(Guid propertyContactId, Guid propertyId, Guid organizationId, Guid updatedByUserId)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    // Demote current primary
                    DemoteExistingPrimary(db, propertyId, organizationId);

                    // Promote this one
                    var link = db.PropertyContacts
                        .FirstOrDefault(pc =>
                            pc.PropertyContactId    == propertyContactId &&
                            pc.OrganizationId       == organizationId &&
                            pc.IsDeleted            == false);

                    if (link == null) return false;

                    link.IsPrimary      = true;
                    link.UpdatedBy      = updatedByUserId;
                    link.UpdatedAtUtc   = DateTime.UtcNow;
                    db.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContactService.SetPrimaryContact] {0}", ex);
                return false;
            }
        }

        // ---------------------------------------------------------------
        // Buyer Score — calculated from all properties linked to contact
        // Never stored, always derived at query time
        // No paid API calls — uses only data already in our DB
        //
        // Formula:
        //   Propensity avg (0-100)          40% weight
        //   Estimated equity total          30% weight  (normalized 0-100)
        //   Active deal count               20% weight  (normalized 0-100, cap 5)
        //   Negative signals (liens/tax)    10% weight  (inverted — more = lower score)
        // ---------------------------------------------------------------
        public int CalculateBuyerScore(List<ContactPropertyLink> properties)
        {
            if (properties == null || !properties.Any()) return 0;

            // --- Component 1: Propensity score average (already 0-100) ---
            var propScores = properties
                .Where(p => p.PropensityScore.HasValue)
                .Select(p => (double)p.PropensityScore.Value)
                .ToList();
            double propComponent = propScores.Any() ? propScores.Average() : 0;

            // --- Component 2: Total equity normalized to 0-100 ---
            // Treat $5M+ total equity as 100, $0 or negative as 0
            double totalEquity = properties
                .Where(p => p.EstimatedEquity.HasValue && p.EstimatedEquity.Value > 0)
                .Sum(p => (double)p.EstimatedEquity.Value);
            double equityComponent = Math.Min(totalEquity / 5_000_000.0 * 100.0, 100.0);

            // --- Component 3: Active deal count normalized 0-100, cap at 5 deals ---
            int totalDeals = properties.Sum(p => p.ActiveDealCount);
            double dealComponent = Math.Min(totalDeals / 5.0 * 100.0, 100.0);

            // --- Component 4: Negative signals (inverted) ---
            // Count properties with liens or tax delinquency
            int negativeCount = properties.Count(p => p.HasInvoluntaryLiens || p.HasTaxDelinquency);
            int totalProps     = properties.Count;
            // More negative signals = lower score component
            double negativeRatio    = totalProps > 0 ? (double)negativeCount / totalProps : 0;
            double negativeComponent = (1.0 - negativeRatio) * 100.0;

            // --- Weighted composite ---
            double score =
                (propComponent    * 0.40) +
                (equityComponent  * 0.30) +
                (dealComponent    * 0.20) +
                (negativeComponent * 0.10);

            return (int)Math.Round(Math.Min(Math.Max(score, 0), 100));
        }

        // ---------------------------------------------------------------
        // Outreach email template builder
        // Plain template merge — no API call, no cost
        // Returns a ready-to-copy email string
        // ---------------------------------------------------------------
        public string BuildOutreachEmail(
            ContactRow contact,
            ContactPropertyLink property,
            string brokerFirstName,
            string brokerCompany,
            string brokerPhone,
            string brokerEmail)
        {
            string contactName  = !string.IsNullOrWhiteSpace(contact.FirstName)
                                    ? contact.FirstName
                                    : "Property Owner";

            string address      = !string.IsNullOrWhiteSpace(property?.StreetAddress)
                                    ? property.StreetAddress
                                    : "your property";

            // Build signal-aware body paragraph
            var signals = new List<string>();
            if (property != null)
            {
                if (property.HasTaxDelinquency)
                    signals.Add("recent tax activity on the property");
                if (property.HasInvoluntaryLiens)
                    signals.Add("recorded liens against the property");
                if (property.PropensityScore.HasValue && property.PropensityScore > 70)
                    signals.Add("strong market interest in properties like yours");
                if (property.EstimatedEquity.HasValue && property.EstimatedEquity > 0)
                    signals.Add("significant equity position in the current market");
            }

            string signalSentence = signals.Any()
                ? $"Based on our research, we've noted {string.Join(" and ", signals.Take(2))} and believe now may be an opportune time to explore your options."
                : "Based on our market research, we believe your property may be of interest to qualified buyers we currently represent.";

            string email = $@"Subject: Inquiry Regarding {address}

Dear {contactName},

My name is {brokerFirstName ?? "your broker"} with {brokerCompany ?? "our firm"}, and I specialize in commercial real estate transactions in your area.

I am reaching out regarding the property located at {address}. {signalSentence}

I would welcome the opportunity to have a brief conversation to discuss current market conditions and how they may relate to your property. There is no obligation, and I am happy to provide a complimentary market analysis at your convenience.

Please feel free to reach me at {brokerPhone ?? "[phone]"} or reply to this email at {brokerEmail ?? "[email]"}.

I look forward to hearing from you.

Best regards,
{brokerFirstName ?? "[Your Name]"}
{brokerCompany ?? "[Your Company]"}
{brokerPhone ?? "[Phone]"}
{brokerEmail ?? "[Email]"}";

            return email;
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private void DemoteExistingPrimary(DCReyla db, Guid propertyId, Guid organizationId)
        {
            var currentPrimary = db.PropertyContacts
                .FirstOrDefault(pc =>
                    pc.PropertyId       == propertyId &&
                    pc.OrganizationId   == organizationId &&
                    pc.IsPrimary        == true &&
                    pc.IsDeleted        == false);

            if (currentPrimary != null)
            {
                currentPrimary.IsPrimary    = false;
                currentPrimary.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        private void SaveContactTypes(DCReyla db, Guid contactId, List<Guid> typeIds, DateTime now)
        {
            // Remove existing type assignments
            var existing = db.ContactContactTypes
                .Where(ct => ct.ContactId == contactId)
                .ToList();
            db.ContactContactTypes.DeleteAllOnSubmit(existing);
            db.SubmitChanges();

            // Insert new ones
            foreach (var typeId in typeIds.Distinct())
            {
                db.ContactContactTypes.InsertOnSubmit(new ContactContactType
                {
                    ContactContactTypeId    = Guid.NewGuid(),
                    ContactId               = contactId,
                    ContactTypeId           = typeId,
                    CreatedAtUtc            = now
                });
            }
            db.SubmitChanges();
        }

        private ContactRow MapToRow(DCReyla db, Contact c, bool includeProperties)
        {
            // Load contact types
            var typeIds = db.ContactContactTypes
                .Where(ct => ct.ContactId == c.ContactId)
                .Select(ct => ct.ContactTypeId)
                .ToList();

            var typeNames = db.ContactTypes
                .Where(ct => typeIds.Contains(ct.ContactTypeId))
                .OrderBy(ct => ct.SortOrder)
                .Select(ct => ct.Name)
                .ToList();

            var row = new ContactRow
            {
                ContactId        = c.ContactId,
                FirstName        = c.FirstName,
                LastName         = c.LastName,
                CompanyName      = c.CompanyName,
                Email            = c.Email,
                Phone            = c.Phone,
                MobilePhone      = c.MobilePhone,
                LinkedInUrl      = c.LinkedInUrl,
                Notes            = c.Notes,
                IsActive         = c.IsActive,
                PreferredContact = c.PreferredContact,
                CreatedAtUtc     = c.CreatedAtUtc,
                UpdatedAtUtc     = c.UpdatedAtUtc,
                ContactTypes     = typeNames
            };

            if (!includeProperties) return row;

            // Load property links with snapshot data for buyer score
            var links = db.PropertyContacts
                .Where(pc =>
                    pc.ContactId    == c.ContactId &&
                    pc.IsDeleted    == false)
                .ToList();

            foreach (var link in links)
            {
                var prop = db.Properties
                    .FirstOrDefault(p => p.PropertyId == link.PropertyId);

                if (prop == null) continue;

                var propLink = new ContactPropertyLink
                {
                    PropertyContactId   = link.PropertyContactId,
                    PropertyId          = link.PropertyId,
                    IsPrimary           = link.IsPrimary,
                    StreetAddress       = prop.StreetAddress,
                    CityNameRaw         = prop.CityNameRaw,
                    ZipCodeRaw          = prop.ZipCodeRaw,
                    Clip                = prop.Clip
                };

                // Get latest non-expired snapshot for this property
                var now = DateTime.UtcNow;
                var snapshot = db.PropertySnapshots
                    .Where(s =>
                        s.PropertyId    == link.PropertyId &&
                        s.IsDeleted     == false &&
                        s.ExpiresAtUtc  > now)
                    .OrderByDescending(s => s.PulledAtUtc)
                    .FirstOrDefault();

                if (snapshot != null)
                {
                    var avm = db.PropertySnapshotAvms
                        .FirstOrDefault(a => a.PropertySnapshotId == snapshot.PropertySnapshotId);

                    var mortgage = db.PropertySnapshotMortgages
                        .Where(m => m.PropertySnapshotId == snapshot.PropertySnapshotId)
                        .ToList();

                    var hasLiens = db.PropertySnapshotInvoluntaryLiens
                        .Any(l => l.PropertySnapshotId == snapshot.PropertySnapshotId);

                    var hasTax = db.PropertySnapshotTaxAssessments
                        .Any(t =>
                            t.PropertySnapshotId    == snapshot.PropertySnapshotId &&
                            t.TaxDelinquentYear     != null);

                    // Deal → Prospect → Property (Deal has no direct PropertyId)
                    var prospectIds = db.Prospects
                        .Where(p =>
                            p.PropertyId    == link.PropertyId &&
                            p.IsDeleted     == false)
                        .Select(p => p.ProspectId)
                        .ToList();

                    var activeDealCount = prospectIds.Any()
                        ? db.Deals.Count(d =>
                            prospectIds.Contains(d.ProspectId.Value) &&
                            d.IsDeleted == false &&
                            d.Stage     != "Closed" &&
                            d.Stage     != "Dead")
                        : 0;

                    if (avm != null)
                    {
                        propLink.EstimatedValue = avm.EstimatedValue;

                        decimal totalDebt = mortgage
                            .Where(m => m.LoanAmount.HasValue)
                            .Sum(m => m.LoanAmount.Value);

                        propLink.EstimatedEquity = avm.EstimatedValue.HasValue
                            ? avm.EstimatedValue.Value - totalDebt
                            : (decimal?)null;
                    }

                    propLink.PropensityScore        = db.PropertySnapshotPropensities
                        .Where(p => p.PropertySnapshotId == snapshot.PropertySnapshotId)
                        .Select(p => p.PropensityScore)
                        .FirstOrDefault();

                    propLink.HasInvoluntaryLiens    = hasLiens;
                    propLink.HasTaxDelinquency      = hasTax;
                    propLink.ActiveDealCount        = activeDealCount;
                }

                row.Properties.Add(propLink);
            }

            return row;
        }
    }
}
