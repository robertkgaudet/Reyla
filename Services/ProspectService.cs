using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Reyla.Services.Models;

namespace Reyla.Services
{
    /// <summary>
    /// Orchestrates the "Create Prospect" flow for Reyla.
    ///
    /// Flow when user clicks "Add as Prospect":
    ///   1. Upsert the subject Property record (done by PropertySnapshotService)
    ///   2. Create or return existing Prospect record for this property + org
    ///   3. Redirect to Comps.aspx where the user reviews comps on a map
    ///      and selectively saves them to the database.
    ///
    /// Comparables are NOT saved here. Comp fetching and saving is handled
    /// on Comps.aspx so the user controls what gets stored.
    /// </summary>
    public class ProspectService
    {
        // ---------------------------------------------------------------
        // Result objects
        // ---------------------------------------------------------------

        public class CreateProspectResult
        {
            public bool   Success        { get; set; }
            public string ErrorMessage   { get; set; }
            public Guid   ProspectId     { get; set; }
            public bool   AlreadyExisted { get; set; }

            public static CreateProspectResult Fail(string message) =>
                new CreateProspectResult { Success = false, ErrorMessage = message };
        }

        public class SaveCompsResult
        {
            public bool   Success        { get; set; }
            public string ErrorMessage   { get; set; }
            public int    CompsInserted  { get; set; }
            public int    CompsRefreshed { get; set; }

            public static SaveCompsResult Fail(string message) =>
                new SaveCompsResult { Success = false, ErrorMessage = message };
        }

        public class ProspectSummary
        {
            public Guid     ProspectId    { get; set; }
            public Guid     PropertyId    { get; set; }
            public string   StreetAddress { get; set; }
            public string   CityNameRaw   { get; set; }
            public string   ZipCodeRaw    { get; set; }
            public string   Status        { get; set; }
            public DateTime CreatedAtUtc  { get; set; }
        }

        // ---------------------------------------------------------------
        // CreateProspect — saves subject property as a prospect only.
        // No comparables are fetched or written here.
        // ---------------------------------------------------------------

        public CreateProspectResult CreateProspect(
            Guid propertyId,
            Guid organizationId,
            Guid userId)
        {
            using (var db = new DCReyla())
            {
                // Check for any existing row — deleted or not
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.PropertyId     == propertyId     &&
                        p.OrganizationId == organizationId);

                if (prospect == null)
                {
                    // Brand new — insert
                    prospect = new Prospect
                    {
                        ProspectId     = Guid.NewGuid(),
                        PropertyId     = propertyId,
                        OrganizationId = organizationId,
                        UserId         = userId,
                        Status         = "New",
                        IsActive       = true,
                        IsDeleted      = false,
                        CreatedBy      = userId,
                        CreatedAtUtc   = DateTime.UtcNow
                    };
                    db.Prospects.InsertOnSubmit(prospect);
                    db.SubmitChanges();

                    // Resolve address for activity metadata
                    string address = null;
                    string clip    = null;
                    try
                    {
                        var prop = db.Properties.FirstOrDefault(p => p.PropertyId == propertyId);
                        if (prop != null)
                        {
                            address = (prop.StreetAddress ?? "") +
                                      (!string.IsNullOrEmpty(prop.CityNameRaw) ? ", " + prop.CityNameRaw : "");
                            clip = prop.Clip;
                        }
                    }
                    catch { /* non-fatal */ }

                    new ActivityService().Track(
                        ActivityType.ProspectCreated,
                        ActivityEntityType.Prospect,
                        prospect.ProspectId,
                        userId,
                        organizationId,
                        new { subjectAddress = address, clip = clip, prospectId = prospect.ProspectId.ToString() });

                    return new CreateProspectResult
                    {
                        Success        = true,
                        ProspectId     = prospect.ProspectId,
                        AlreadyExisted = false
                    };
                }

                if (!prospect.IsDeleted)
                {
                    // Already an active prospect
                    return new CreateProspectResult
                    {
                        Success        = true,
                        ProspectId     = prospect.ProspectId,
                        AlreadyExisted = true
                    };
                }

                // Soft-deleted row exists — reactivate it
                prospect.IsDeleted    = false;
                prospect.IsActive     = true;
                prospect.Status       = "New";
                prospect.UserId       = userId;
                prospect.UpdatedBy    = userId;
                prospect.UpdatedAtUtc = DateTime.UtcNow;
                db.SubmitChanges();

                string reactivatedAddress = null;
                string reactivatedClip    = null;
                try
                {
                    var prop = db.Properties.FirstOrDefault(p => p.PropertyId == propertyId);
                    if (prop != null)
                    {
                        reactivatedAddress = (prop.StreetAddress ?? "") +
                                             (!string.IsNullOrEmpty(prop.CityNameRaw) ? ", " + prop.CityNameRaw : "");
                        reactivatedClip = prop.Clip;
                    }
                }
                catch { /* non-fatal */ }

                new ActivityService().Track(
                    ActivityType.ProspectCreated,
                    ActivityEntityType.Prospect,
                    prospect.ProspectId,
                    userId,
                    organizationId,
                    new { subjectAddress = reactivatedAddress, clip = reactivatedClip, prospectId = prospect.ProspectId.ToString() });

                return new CreateProspectResult
                {
                    Success        = true,
                    ProspectId     = prospect.ProspectId,
                    AlreadyExisted = false
                };
            }
        }

        // ---------------------------------------------------------------
        // SaveSelectedComps — called from Comps.aspx after the user
        // reviews the comp list/map and confirms their selection.
        // Only the clips the user checked are written to the database.
        // Also writes SelectedCompsJson to Prospect for instant valuation access.
        // ---------------------------------------------------------------

        public SaveCompsResult SaveSelectedComps(
            Guid                     prospectId,
            string                   subjectClip,
            decimal                  subjectLat,
            decimal                  subjectLng,
            string                   subjectAddress,
            decimal                  radiusMiles,
            List<string>             selectedClips,
            List<ComparableProperty> allComps,
            Guid                     organizationId,
            Guid                     userId)
        {
            using (var db = new DCReyla())
            {
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null)
                    return SaveCompsResult.Fail("Prospect not found.");

                // Audit record for this comp search
                var prospectSearch = new ProspectSearch
                {
                    ProspectSearchId = Guid.NewGuid(),
                    OrganizationId   = organizationId,
                    UserId           = userId,
                    OriginAddress    = subjectAddress,
                    OriginLat        = subjectLat,
                    OriginLng        = subjectLng,
                    RadiusMiles      = radiusMiles,
                    TotalResults     = selectedClips.Count,
                    CreatedAtUtc     = DateTime.UtcNow
                };
                db.ProspectSearches.InsertOnSubmit(prospectSearch);
                db.SubmitChanges();

                var snapshotService = new PropertySnapshotService();
                var selectedSet     = new HashSet<string>(selectedClips);
                var selectedComps   = new List<ComparableProperty>();
                int inserted        = 0;
                int refreshed       = 0;

                foreach (var comp in allComps)
                {
                    if (string.IsNullOrEmpty(comp.Clip))  continue;
                    if (comp.Clip == subjectClip)         continue;
                    if (!selectedSet.Contains(comp.Clip)) continue;

                    bool isNew = !db.Properties.Any(p => p.Clip == comp.Clip);

                    Guid compPropertyId = snapshotService.UpsertComparableProperty(db, comp);

                    if (isNew) inserted++;
                    else       refreshed++;

                    selectedComps.Add(comp);

                    db.ProspectSearchProperties.InsertOnSubmit(new ProspectSearchProperty
                    {
                        ProspectSearchPropertyId = Guid.NewGuid(),
                        ProspectSearchId         = prospectSearch.ProspectSearchId,
                        PropertyId               = compPropertyId,
                        CreatedAtUtc             = DateTime.UtcNow
                    });
                }

                // Persist the selected comp set on the prospect for instant valuation access
                prospect.SelectedCompsJson = JsonConvert.SerializeObject(selectedComps);
                prospect.UpdatedAtUtc      = DateTime.UtcNow;

                db.SubmitChanges();

                return new SaveCompsResult
                {
                    Success        = true,
                    CompsInserted  = inserted,
                    CompsRefreshed = refreshed
                };
            }
        }

        // ---------------------------------------------------------------
        // ToggleComp — adds or removes a single comp from SelectedCompsJson.
        // Called from PropertyDetail when the user clicks + / × on a comp row.
        // Automatically triggers valuation recalculation on next page load.
        // ---------------------------------------------------------------

        public bool ToggleComp(
            Guid   prospectId,
            string clipToToggle,
            bool   add,                      // true = add, false = remove
            Guid   organizationId)
        {
            using (var db = new DCReyla())
            {
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null) return false;

                // Deserialise current selected comps
                var current = new List<ComparableProperty>();
                if (!string.IsNullOrEmpty(prospect.SelectedCompsJson))
                {
                    try { current = JsonConvert.DeserializeObject<List<ComparableProperty>>(prospect.SelectedCompsJson)
                                    ?? new List<ComparableProperty>(); }
                    catch { current = new List<ComparableProperty>(); }
                }

                if (add)
                {
                    // Only add if not already present — pull data from ProspectCompsCache
                    if (current.Any(c => c.Clip == clipToToggle)) return true;

                    var cache = db.ProspectCompsCaches
                        .Where(c => c.ProspectId == prospectId)
                        .OrderByDescending(c => c.CreatedAtUtc)
                        .FirstOrDefault();

                    if (cache == null || string.IsNullOrEmpty(cache.CompsJson)) return false;

                    List<ComparableProperty> allCached;
                    try { allCached = JsonConvert.DeserializeObject<List<ComparableProperty>>(cache.CompsJson)
                                      ?? new List<ComparableProperty>(); }
                    catch { return false; }

                    var compToAdd = allCached.FirstOrDefault(c => c.Clip == clipToToggle);
                    if (compToAdd == null) return false;

                    current.Add(compToAdd);

                    // Also upsert into Property table if not already there
                    var snapshotService = new PropertySnapshotService();
                    snapshotService.UpsertComparableProperty(db, compToAdd);

                    prospect.SelectedCompsJson = JsonConvert.SerializeObject(current);
                    prospect.UpdatedAtUtc      = DateTime.UtcNow;
                    db.SubmitChanges();

                    // Resolve subject address for metadata
                    string addSubjectAddr = null;
                    string addSubjectClip = null;
                    try
                    {
                        var subjectProp = db.Properties.FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                        if (subjectProp != null)
                        {
                            addSubjectAddr = (subjectProp.StreetAddress ?? "") +
                                             (!string.IsNullOrEmpty(subjectProp.CityNameRaw) ? ", " + subjectProp.CityNameRaw : "");
                            addSubjectClip = subjectProp.Clip;
                        }
                    }
                    catch { /* non-fatal */ }

                    string compAddr = (compToAdd.StreetAddress ?? "") +
                                      (!string.IsNullOrEmpty(compToAdd.City) ? ", " + compToAdd.City : "");

                    new ActivityService().Track(
                        ActivityType.CompAdded,
                        ActivityEntityType.Prospect,
                        prospectId,
                        prospect.UserId,
                        organizationId,
                        new { subjectAddress = addSubjectAddr, compAddress = compAddr, clip = addSubjectClip, prospectId = prospectId.ToString() });

                    return true;
                }
                else
                {
                    // Remove
                    current = current.Where(c => c.Clip != clipToToggle).ToList();

                    prospect.SelectedCompsJson = JsonConvert.SerializeObject(current);
                    prospect.UpdatedAtUtc      = DateTime.UtcNow;
                    db.SubmitChanges();

                    // Resolve subject address for metadata
                    string removeSubjectAddr = null;
                    string removeSubjectClip = null;
                    try
                    {
                        var subjectProp = db.Properties.FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                        if (subjectProp != null)
                        {
                            removeSubjectAddr = (subjectProp.StreetAddress ?? "") +
                                                (!string.IsNullOrEmpty(subjectProp.CityNameRaw) ? ", " + subjectProp.CityNameRaw : "");
                            removeSubjectClip = subjectProp.Clip;
                        }
                    }
                    catch { /* non-fatal */ }

                    // Best-effort: get address of removed comp from cache
                    string removedCompAddr = clipToToggle;
                    try
                    {
                        var cache = db.ProspectCompsCaches
                            .Where(c => c.ProspectId == prospectId)
                            .OrderByDescending(c => c.CreatedAtUtc)
                            .FirstOrDefault();
                        if (cache != null && !string.IsNullOrEmpty(cache.CompsJson))
                        {
                            var allCachedForRemove = JsonConvert.DeserializeObject<List<ComparableProperty>>(cache.CompsJson)
                                                     ?? new List<ComparableProperty>();
                            var removed = allCachedForRemove.FirstOrDefault(c => c.Clip == clipToToggle);
                            if (removed != null)
                                removedCompAddr = (removed.StreetAddress ?? "") +
                                                  (!string.IsNullOrEmpty(removed.City) ? ", " + removed.City : "");
                        }
                    }
                    catch { /* non-fatal */ }

                    new ActivityService().Track(
                        ActivityType.CompRemoved,
                        ActivityEntityType.Prospect,
                        prospectId,
                        prospect.UserId,
                        organizationId,
                        new { subjectAddress = removeSubjectAddr, compAddress = removedCompAddr, clip = removeSubjectClip, prospectId = prospectId.ToString() });

                    return true;
                }
            }
        }

        // ---------------------------------------------------------------
        // ClearAllComps — removes all comps from SelectedCompsJson.
        // Valuation will show "no comps" state after this.
        // ---------------------------------------------------------------

        public bool ClearAllComps(Guid prospectId, Guid organizationId)
        {
            using (var db = new DCReyla())
            {
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null) return false;

                prospect.SelectedCompsJson = "[]";
                prospect.UpdatedAtUtc      = DateTime.UtcNow;
                db.SubmitChanges();

                string clearSubjectAddr = null;
                string clearSubjectClip = null;
                try
                {
                    var subjectProp = db.Properties.FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                    if (subjectProp != null)
                    {
                        clearSubjectAddr = (subjectProp.StreetAddress ?? "") +
                                           (!string.IsNullOrEmpty(subjectProp.CityNameRaw) ? ", " + subjectProp.CityNameRaw : "");
                        clearSubjectClip = subjectProp.Clip;
                    }
                }
                catch { /* non-fatal */ }

                new ActivityService().Track(
                    ActivityType.CompsCleared,
                    ActivityEntityType.Prospect,
                    prospectId,
                    prospect.UserId,
                    organizationId,
                    new { subjectAddress = clearSubjectAddr, clip = clearSubjectClip, prospectId = prospectId.ToString() });

                return true;
            }
        }

        // ---------------------------------------------------------------
        // GetSelectedComps — returns the user's curated comp list.
        // ---------------------------------------------------------------

        public List<ComparableProperty> GetSelectedComps(Guid prospectId, Guid organizationId)
        {
            using (var db = new DCReyla())
            {
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null || string.IsNullOrEmpty(prospect.SelectedCompsJson))
                    return new List<ComparableProperty>();

                try { return JsonConvert.DeserializeObject<List<ComparableProperty>>(prospect.SelectedCompsJson)
                             ?? new List<ComparableProperty>(); }
                catch { return new List<ComparableProperty>(); }
            }
        }

        // ---------------------------------------------------------------
        // UpdateStatus
        // ---------------------------------------------------------------

        public bool UpdateStatus(Guid prospectId, Guid organizationId, Guid userId, string status)
        {
            using (var db = new DCReyla())
            {
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null) return false;

                prospect.Status       = status;
                prospect.UpdatedBy    = userId;
                prospect.UpdatedAtUtc = DateTime.UtcNow;
                db.SubmitChanges();
                return true;
            }
        }

        // ---------------------------------------------------------------
        // DeleteProspect — soft delete
        // ---------------------------------------------------------------

        public bool DeleteProspect(Guid prospectId, Guid organizationId, Guid userId)
        {
            using (var db = new DCReyla())
            {
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null) return false;

                prospect.IsDeleted    = true;
                prospect.IsActive     = false;
                prospect.UpdatedBy    = userId;
                prospect.UpdatedAtUtc = DateTime.UtcNow;
                db.SubmitChanges();
                return true;
            }
        }

        // ---------------------------------------------------------------
        // GetProspectsForOrg — list page
        // ---------------------------------------------------------------

        public List<ProspectSummary> GetProspectsForOrg(Guid organizationId)
        {
            using (var db = new DCReyla())
            {
                return db.Prospects
                    .Where(p =>
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .Select(p => new ProspectSummary
                    {
                        ProspectId    = p.ProspectId,
                        PropertyId    = p.PropertyId,
                        StreetAddress = p.Property.StreetAddress,
                        CityNameRaw   = p.Property.CityNameRaw,
                        ZipCodeRaw    = p.Property.ZipCodeRaw,
                        Status        = p.Status,
                        CreatedAtUtc  = p.CreatedAtUtc
                    })
                    .ToList();
            }
        }
    }
}
