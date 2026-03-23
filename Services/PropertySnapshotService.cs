using System;
using System.Collections.Generic;
using System.Linq;
using Reyla.Services.Models;

namespace Reyla.Services
{
    /// <summary>
    /// Orchestrates the full motivated-seller profile for a property.
    ///
    /// Flow:
    ///   1. Search CoreLogic for the address to resolve the Clip identifier.
    ///   2. Upsert the shared Property record (one row per Clip, no org ownership).
    ///   3. Check for a fresh PropertySnapshot belonging to the caller's Organization.
    ///      If one exists and has not expired, load and return it from the database.
    ///   4. If no fresh snapshot exists, call all 13 CoreLogic API endpoints
    ///      sequentially and persist the full profile in one transaction.
    ///
    /// Snapshot ownership is at the Organization level — any user in the same org
    /// will share the cached snapshot, avoiding redundant API calls.
    ///
    /// Individual endpoint failures are swallowed by TryGet() so one bad endpoint
    /// never prevents the rest of the profile from being returned.
    /// </summary>
    public class PropertySnapshotService
    {
        private const int SnapshotExpiryDays = 30;

        private readonly CoreLogicPropertyService _api;

        public PropertySnapshotService()
        {
            _api = new CoreLogicPropertyService();
        }

        // ---------------------------------------------------------------
        // Public entry point
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns a full motivated-seller profile for the given address.
        /// Pulls from the org's DB cache if fresh; otherwise calls CoreLogic and persists.
        /// </summary>
        /// <param name="streetAddress">Street address to search.</param>
        /// <param name="city">City name.</param>
        /// <param name="state">Two-letter state code.</param>
        /// <param name="zipCode">Zip code (optional but improves match accuracy).</param>
        /// <param name="organizationId">The calling user's OrganizationId — snapshot is cached at this level.</param>
        /// <param name="userId">The calling user's UserId — recorded on PropertySearch for history.</param>
        public PropertyProfileResult GetProfile(
            string streetAddress,
            string city,
            string state,
            string zipCode,
            Guid   organizationId,
            Guid   userId)
        {
            // Step 1 — Resolve address to Clip via CoreLogic search
            // Step 1 — Resolve address to Clip + coordinates via geocode search
            // Uses /search/geocode instead of /search so lat/lng are always populated.
            var searchResult = _api.SearchByAddressWithGeocode(streetAddress, city, state, zipCode);

            if (searchResult?.Items == null || !searchResult.Items.Any())
                return PropertyProfileResult.NotFound();

            var match = searchResult.Items.First();
            string clip = match.Clip;

            using (var db = new DCReyla())
            {
                // Step 2 — Upsert shared Property record (no org/user ownership)
                var property = db.Properties.FirstOrDefault(p => p.Clip == clip);

                if (property == null)
                {
                    property = CreatePropertyRecord(db, match);
                    db.Properties.InsertOnSubmit(property);
                    db.SubmitChanges();
                }
                else
                {
                    // Back-fill FipsCode / APN on existing rows that predate the fix.
                    // These come from the search result which is the only reliable FIPS source.
                    bool dirty = false;
                    if (string.IsNullOrWhiteSpace(property.FipsCode)
                        && !string.IsNullOrWhiteSpace(match.PropertyApn?.FipsCode))
                    {
                        property.FipsCode    = match.PropertyApn.FipsCode;
                        dirty = true;
                    }
                    if (string.IsNullOrWhiteSpace(property.Apn)
                        && !string.IsNullOrWhiteSpace(match.PropertyApn?.Apn))
                    {
                        property.Apn         = match.PropertyApn.Apn;
                        dirty = true;
                    }
                    if (dirty)
                    {
                        property.UpdatedAtUtc = DateTime.UtcNow;
                        db.SubmitChanges();
                    }
                }

                // Step 3 — Record this individual user's search
                RecordPropertySearch(db, property.PropertyId, organizationId, userId,
                    $"{streetAddress}, {city}, {state} {zipCode}".Trim());

                // Step 4 — Check for a fresh org-level snapshot
                var now = DateTime.UtcNow;
                var existingSnapshot = db.PropertySnapshots
                    .FirstOrDefault(s =>
                        s.PropertyId     == property.PropertyId &&
                        s.OrganizationId == organizationId      &&
                        s.ExpiresAtUtc   >  now                 &&
                        s.IsDeleted      == false);

                if (existingSnapshot != null)
                    return LoadProfileFromDb(db, existingSnapshot.PropertySnapshotId, property);

                // Step 5 — No fresh snapshot — pull from API and persist
                return PullAndPersistProfile(db, clip, property, organizationId);
            }
        }

        // ---------------------------------------------------------------
        // Get profile by Clip — skips address search, uses existing Property row.
        // Used by PropertyDetail.aspx where we already have the clip from the URL.
        // Does NOT record a PropertySearch entry (no re-search, no double-count).
        // ---------------------------------------------------------------

        public PropertyProfileResult GetProfileByClip(string clip, Guid organizationId)
        {
            if (string.IsNullOrWhiteSpace(clip))
                return PropertyProfileResult.NotFound();

            using (var db = new DCReyla())
            {
                var property = db.Properties.FirstOrDefault(p => p.Clip == clip);

                if (property == null)
                    return PropertyProfileResult.NotFound();

                // If FipsCode is missing, do a lightweight address re-search just to
                // retrieve it. FIPS only comes from the search endpoint — it's not
                // available in site-location or property-detail responses.
                if (string.IsNullOrWhiteSpace(property.FipsCode)
                    && !string.IsNullOrWhiteSpace(property.StreetAddress))
                {
                    try
                    {
                        string city  = property.CityNameRaw ?? string.Empty;
                        string state = string.Empty;
                        string zip   = property.ZipCodeRaw  ?? string.Empty;

                        if (property.CityExtendedId.HasValue)
                        {
                            var ce = db.City_Extendeds.FirstOrDefault(
                                c => c.CityExtendedId == property.CityExtendedId.Value);
                            if (ce != null) { city = ce.City; state = ce.Code; zip = ce.Zip; }
                        }

                        var searchResult = _api.SearchByAddressWithGeocode(
                            property.StreetAddress, city, state, zip);

                        var match = searchResult?.Items?.FirstOrDefault(i => i.Clip == clip);

                        if (match?.PropertyApn?.FipsCode != null)
                        {
                            property.FipsCode     = match.PropertyApn.FipsCode;
                            property.Apn          = string.IsNullOrWhiteSpace(property.Apn)
                                                    ? match.PropertyApn.Apn : property.Apn;
                            property.UpdatedAtUtc = DateTime.UtcNow;
                            db.SubmitChanges();
                        }
                    }
                    catch { /* Non-fatal — FipsCode stays null, doc fetch shows guidance */ }
                }

                // Check for a fresh org-level snapshot first
                var now = DateTime.UtcNow;
                var existingSnapshot = db.PropertySnapshots
                    .FirstOrDefault(s =>
                        s.PropertyId     == property.PropertyId &&
                        s.OrganizationId == organizationId      &&
                        s.ExpiresAtUtc   >  now                 &&
                        s.IsDeleted      == false);

                if (existingSnapshot != null)
                    return LoadProfileFromDb(db, existingSnapshot.PropertySnapshotId, property);

                // No snapshot — pull from API and persist
                return PullAndPersistProfile(db, clip, property, organizationId);
            }
        }

        // ---------------------------------------------------------------
        // Pull all 13 endpoints and persist
        // ---------------------------------------------------------------

        private PropertyProfileResult PullAndPersistProfile(
            DCReyla db,
            string  clip,
            Property property,
            Guid    organizationId)
        {
            var result = new PropertyProfileResult { Property = property };

            // ---------------------------------------------------------------
            // Step A: Call /property-detail first — this single composite
            // endpoint is the most reliable source and returns buildings,
            // ownership, siteLocation, taxAssessment, and ownership transfers
            // all at once. Individual endpoints may return empty for properties
            // with limited county coverage; property-detail fills those gaps.
            // ---------------------------------------------------------------
            var propertyDetail = TryGet("propertyDetail", () => _api.GetPropertyDetail(clip));
            result.PropertyDetail = propertyDetail;

            // ---------------------------------------------------------------
            // Step B: Call all individual endpoints — TryGet returns null on
            // any failure. Where individual endpoints return data they take
            // precedence over property-detail (more granular/complete). Where
            // they return null, we fall back to property-detail data below.
            // ---------------------------------------------------------------
            result.Ownership              = TryGet("ownership",              () => _api.GetOwnership(clip));
            result.OwnershipTransfers     = TryGet("ownershipTransfers",     () => _api.GetOwnershipTransfers(clip));
            result.Mortgage               = TryGet("mortgage",               () => _api.GetMortgage(clip));
            result.EnrichedVoluntaryLiens = TryGet("enrichedVoluntaryLiens", () => _api.GetEnrichedVoluntaryLiens(clip));
            result.InvoluntaryLiens       = TryGet("involuntaryLiens",       () => _api.GetInvoluntaryLiens(clip));
            result.TaxAssessment          = TryGet("taxAssessment",          () => _api.GetTaxAssessment(clip));
            result.Building               = TryGet("building",               () => _api.GetBuilding(clip));
            result.BuildingPermits        = TryGet("buildingPermits",        () => _api.GetBuildingPermits(clip));
            result.Avm                    = TryGet("avm",                    () => _api.GetAvm(clip));
            result.Propensity             = TryGet("propensity",             () => _api.GetPropensity(clip));
            result.Hoa                    = TryGet("hoa",                    () => _api.GetHoa(clip));
            result.ClimateRisk            = TryGet("climateRisk",            () => _api.GetClimateRisk(clip));

            // ---------------------------------------------------------------
            // Step C: Backfill from property-detail where individual endpoints
            // returned no data. This is the key fix for properties where county
            // coverage is limited on the granular endpoints.
            // ---------------------------------------------------------------
            BackfillFromPropertyDetail(result, propertyDetail);

            // Enrich Property row with zoning / lot data from site-location
            var siteLocation = TryGet("siteLocation", () => _api.GetSiteLocation(clip));
            if (siteLocation?.Data != null)
            {
                var site = siteLocation.Data;

                if (!string.IsNullOrWhiteSpace(site.LandUseAndZoningCodes?.ZoningCode))
                    property.ZoningCode = site.LandUseAndZoningCodes.ZoningCode;

                if (!string.IsNullOrWhiteSpace(site.LandUseAndZoningCodes?.LandUseCodeDescription))
                    property.PropertyUseCode = site.LandUseAndZoningCodes.LandUseCodeDescription;

                if (site.Lot?.AreaSquareFeet.HasValue == true && site.Lot.AreaSquareFeet > 0)
                    property.LotSquareFeet = (int)site.Lot.AreaSquareFeet;

                if (!string.IsNullOrWhiteSpace(site.JurisdictionCounty?.Name) &&
                    string.IsNullOrWhiteSpace(property.County))
                    property.County = site.JurisdictionCounty.Name;

                // Also capture FIPS if it was missing (e.g. for comp-sourced properties)
                if (!string.IsNullOrWhiteSpace(site.JurisdictionCounty?.FipsCode) &&
                    string.IsNullOrWhiteSpace(property.FipsCode))
                    property.FipsCode = site.JurisdictionCounty.FipsCode;

                property.UpdatedAtUtc = DateTime.UtcNow;
                db.SubmitChanges();
            }
            else if (propertyDetail?.SiteLocation?.Data != null)
            {
                // Fallback: use siteLocation from the property-detail composite call
                var site = propertyDetail.SiteLocation.Data;

                if (!string.IsNullOrWhiteSpace(site.LandUseAndZoningCodes?.ZoningCode))
                    property.ZoningCode = site.LandUseAndZoningCodes.ZoningCode;

                if (!string.IsNullOrWhiteSpace(site.LandUseAndZoningCodes?.LandUseCodeDescription))
                    property.PropertyUseCode = site.LandUseAndZoningCodes.LandUseCodeDescription;

                if (site.Lot?.AreaSquareFeet.HasValue == true && site.Lot.AreaSquareFeet > 0)
                    property.LotSquareFeet = (int)site.Lot.AreaSquareFeet;

                if (!string.IsNullOrWhiteSpace(site.JurisdictionCounty?.Name) &&
                    string.IsNullOrWhiteSpace(property.County))
                    property.County = site.JurisdictionCounty.Name;

                if (!string.IsNullOrWhiteSpace(site.JurisdictionCounty?.FipsCode) &&
                    string.IsNullOrWhiteSpace(property.FipsCode))
                    property.FipsCode = site.JurisdictionCounty.FipsCode;

                property.UpdatedAtUtc = DateTime.UtcNow;
                db.SubmitChanges();
            }

            // Persist snapshot and all detail records.
            // Wrapped in try/catch — if the DB write fails the caller still gets
            // the live API data; it just won't be cached for next time.
            try
            {
                var snapshotId = PersistSnapshot(db, property.PropertyId, organizationId, result);
                result.PropertySnapshotId = snapshotId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "[PropertySnapshotService] PersistSnapshot failed for clip {0}: {1}",
                    clip, ex.Message);
            }

            result.IsFromCache = false;

            return result;
        }

        // ---------------------------------------------------------------
        // Backfill from property-detail composite response
        //
        // When individual endpoints return no data (empty Items, null Data),
        // use the equivalent section from the /property-detail call.
        // This is especially important for less-covered counties where
        // the granular endpoints return 404/empty but property-detail succeeds.
        // ---------------------------------------------------------------

        private void BackfillFromPropertyDetail(PropertyProfileResult result, PropertyDetailResponse pd)
        {
            if (pd == null) return;

            // --- Ownership backfill ---
            if (result.Ownership?.Data == null && pd.Ownership?.Data != null)
            {
                var src    = pd.Ownership.Data;
                var pdNames = src.CurrentOwners?.OwnerNames ?? new List<PropertyDetailOwnerName>();
                var mail   = src.CurrentOwnerMailingInfo?.MailingAddress;

                var ownerNames = new List<OwnerName>();
                foreach (var n in pdNames)
                    ownerNames.Add(new OwnerName { Name = n.Name, FirstName = n.FirstName, LastName = n.LastName });

                result.Ownership = new OwnershipResult
                {
                    Data = new OwnershipData
                    {
                        CurrentOwners = new CurrentOwners
                        {
                            OwnerNames          = ownerNames,
                            OccupancyCode       = src.CurrentOwners?.OccupancyCode,
                            OwnershipRightsCode = src.CurrentOwners?.OwnershipRightsCode
                        },
                        CurrentOwnerMailingInfo = mail == null ? null : new CurrentOwnerMailingInfo
                        {
                            MailingAddress = new OwnerMailingAddress
                            {
                                StreetAddress = mail.StreetAddress,
                                City          = mail.City,
                                State         = mail.State,
                                ZipCode       = mail.ZipCode
                            }
                        }
                    }
                };
            }

            // --- Ownership transfers backfill ---
            // Use mostRecentOwnerTransfer from property-detail when individual endpoint is empty
            bool transfersEmpty = result.OwnershipTransfers?.Items == null
                               || result.OwnershipTransfers.Items.Count == 0;

            if (transfersEmpty)
            {
                // Merge mostRecentOwnerTransfer + lastMarketSale items, deduplicate by doc number
                var pdItems = new List<OwnershipTransferItem>();

                void AddTransferItems(PropertyDetailTransfers src)
                {
                    if (src?.Items == null) return;
                    foreach (var i in src.Items)
                    {
                        if (i?.TransactionDetails == null) continue;
                        pdItems.Add(new OwnershipTransferItem
                        {
                            TransactionDetails = new OwnershipTransferDetails
                            {
                                SaleDateDerived    = i.TransactionDetails.SaleDateDerived,
                                SaleAmount         = i.TransactionDetails.SaleAmount,
                                DeedCategoryCode   = i.TransactionDetails.DeedCategoryCode,
                                SaleDocumentNumber = i.TransactionDetails.SaleDocumentNumber
                            },
                            BuyerDetails = i.BuyerDetails?.BuyerNames?.Count > 0
                                ? new OwnershipTransferPartyDetails
                                    {
                                        BuyerNames = i.BuyerDetails.BuyerNames.Select(
                                            n => new OwnerName { Name = n.Name, FirstName = n.FirstName, LastName = n.LastName })
                                            .ToList()
                                    }
                                : null,
                            SellerDetails = i.SellerDetails?.SellerNames?.Count > 0
                                ? new OwnershipTransferPartyDetails
                                    {
                                        SellerNames = i.SellerDetails.SellerNames.Select(
                                            n => new OwnerName { Name = n.Name, FirstName = n.FirstName, LastName = n.LastName })
                                            .ToList()
                                    }
                                : null
                        });
                    }
                }

                AddTransferItems(pd.MostRecentOwnerTransfer);
                AddTransferItems(pd.LastMarketSale);

                if (pdItems.Count > 0)
                {
                    result.OwnershipTransfers = new OwnershipTransfersResult
                    {
                        Items = pdItems
                    };
                }
            }

            // --- Tax assessment backfill ---
            bool taxEmpty = result.TaxAssessment?.Items == null
                         || result.TaxAssessment.Items.Count == 0;

            if (taxEmpty && pd.TaxAssessment?.Items?.Count > 0)
            {
                result.TaxAssessment = new TaxAssessmentResult
                {
                    Items = pd.TaxAssessment.Items.ConvertAll(i => new TaxAssessmentItem
                    {
                        TaxAmount = i.TaxAmount == null ? null : new TaxAmountData
                        {
                            TotalTaxAmount  = i.TaxAmount.TotalTaxAmount,
                            NetTaxAmount    = i.TaxAmount.NetTaxAmount,
                            BilledYear      = i.TaxAmount.BilledYear,
                            DelinquentYear  = i.TaxAmount.DelinquentYear
                        },
                        AssessedValue = i.AssessedValue == null ? null : new AssessedValueData
                        {
                            CalculatedTotalValue       = i.AssessedValue.CalculatedTotalValue,
                            CalculatedLandValue        = i.AssessedValue.CalculatedLandValue,
                            CalculatedImprovementValue = i.AssessedValue.CalculatedImprovementValue,
                            TaxableValue               = i.AssessedValue.TaxableValue,
                            TaxAssessedYear            = i.AssessedValue.TaxAssessedYear
                        }
                    })
                };
            }

            // --- Buildings backfill ---
            bool buildingEmpty = result.Building?.Data == null;

            if (buildingEmpty && pd.Buildings?.Data != null)
            {
                var src     = pd.Buildings.Data;
                var summary = src.AllBuildingsSummary;
                var bldg0   = src.Buildings?.Count > 0 ? src.Buildings[0] : null;
                var cd      = bldg0?.ConstructionDetails;
                var ia      = bldg0?.InteriorArea;
                var ir      = bldg0?.InteriorRooms;
                var vp      = bldg0?.StructureVerticalProfile;

                result.Building = new BuildingResult
                {
                    Data = new BuildingData
                    {
                        AllBuildingsSummary = summary == null ? null : new AllBuildingsSummary
                        {
                            LivingAreaSquareFeet = summary.LivingAreaSquareFeet,
                            BedroomsCount        = summary.BedroomsCount,
                            BathroomsCount       = summary.BathroomsCount
                        },
                        Buildings = bldg0 == null ? new List<Building>() : new List<Building>
                        {
                            new Building
                            {
                                ConstructionDetails = cd == null ? null : new ConstructionDetails
                                {
                                    YearBuilt                       = cd.YearBuilt,
                                    EffectiveYearBuilt              = cd.EffectiveYearBuilt,
                                    ConstructionTypeCode            = cd.ConstructionTypeCode,
                                    BuildingImprovementConditionCode= cd.BuildingImprovementConditionCode
                                },
                                InteriorArea = ia == null ? null : new InteriorArea
                                {
                                    UniversalBuildingAreaSquareFeet = ia.UniversalBuildingAreaSquareFeet,
                                    LivingAreaSquareFeet            = ia.LivingAreaSquareFeet
                                },
                                InteriorRooms = ir == null ? null : new InteriorRooms
                                {
                                    TotalCount      = ir.TotalCount,
                                    BedroomsCount   = ir.BedroomsCount,
                                    BathroomsCount  = ir.BathroomsCount
                                },
                                StructureVerticalProfile = vp == null ? null : new StructureVerticalProfile
                                {
                                    StoriesCount = vp.StoriesCount
                                },
                                StructureExterior = bldg0.StructureExterior == null ? null : new StructureExterior
                                {
                                    Roof = bldg0.StructureExterior.Roof == null ? null : new RoofInfo
                                    {
                                        RoofMaterialTypeCode = bldg0.StructureExterior.Roof.RoofMaterialTypeCode
                                    }
                                }
                            }
                        }
                    }
                };
            }
        }

        // ---------------------------------------------------------------
        // Persist snapshot parent + all 12 detail tables
        // ---------------------------------------------------------------

        private Guid PersistSnapshot(
            DCReyla              db,
            Guid                 propertyId,
            Guid                 organizationId,
            PropertyProfileResult result)
        {
            var now = DateTime.UtcNow;

            // Parent snapshot record — owned by the org, not an individual user
            var snapshot = new PropertySnapshot
            {
                PropertySnapshotId = Guid.NewGuid(),
                PropertyId         = propertyId,
                OrganizationId     = organizationId,
                PulledAtUtc        = now,
                ExpiresAtUtc       = now.AddDays(SnapshotExpiryDays),
                IsActive           = true,
                IsDeleted          = false,
                CreatedAtUtc       = now
            };
            db.PropertySnapshots.InsertOnSubmit(snapshot);
            db.SubmitChanges(); // Commit parent first so child FKs resolve

            Guid sid = snapshot.PropertySnapshotId;

            // Ownership — API returns nested: data.currentOwners.ownerNames[] + currentOwnerMailingInfo.mailingAddress
            if (result.Ownership?.Data != null)
            {
                var d      = result.Ownership.Data;
                var owners = d.CurrentOwners?.OwnerNames ?? new System.Collections.Generic.List<OwnerName>();
                var mail   = d.CurrentOwnerMailingInfo?.MailingAddress;

                db.PropertySnapshotOwnerships.InsertOnSubmit(new PropertySnapshotOwnership
                {
                    PropertySnapshotOwnershipId = Guid.NewGuid(),
                    PropertySnapshotId          = sid,
                    OwnerName                   = owners.Count > 0 ? owners[0].Name      : null,
                    Owner1FirstName             = owners.Count > 0 ? owners[0].FirstName : null,
                    Owner1LastName              = owners.Count > 0 ? owners[0].LastName  : null,
                    Owner2FirstName             = owners.Count > 1 ? owners[1].FirstName : null,
                    Owner2LastName              = owners.Count > 1 ? owners[1].LastName  : null,
                    AbsenteeOwnerStatus         = d.CurrentOwners?.OccupancyCode,
                    OwnershipType               = d.CurrentOwners?.OwnershipRightsCode,
                    MailingAddress              = mail?.StreetAddress,
                    MailingCity                 = mail?.City,
                    MailingState                = mail?.State,
                    MailingZipCode              = mail?.ZipCode,
                    CreatedAtUtc                = now
                });
            }

            // Ownership Transfers — API returns items[] each with transactionDetails + buyerDetails + sellerDetails
            if (result.OwnershipTransfers?.Items != null)
            {
                foreach (var item in result.OwnershipTransfers.Items)
                {
                    if (item?.TransactionDetails == null) continue;
                    var td = item.TransactionDetails;

                    string buyerName  = null;
                    string sellerName = null;
                    if (item.BuyerDetails?.BuyerNames?.Count   > 0) buyerName  = item.BuyerDetails.BuyerNames[0].Name;
                    if (item.SellerDetails?.SellerNames?.Count > 0) sellerName = item.SellerDetails.SellerNames[0].Name;

                    db.PropertySnapshotOwnershipTransfers.InsertOnSubmit(
                        new PropertySnapshotOwnershipTransfer
                        {
                            PropertySnapshotOwnershipTransferId = Guid.NewGuid(),
                            PropertySnapshotId                  = sid,
                            SaleDate                            = ParseDate(td.SaleDateDerived),
                            SaleAmount                          = td.SaleAmount,
                            BuyerName                           = buyerName,
                            SellerName                          = sellerName,
                            DeedType                            = td.DeedCategoryCode,
                            DocumentNumber                      = td.SaleDocumentNumber,
                            CreatedAtUtc                        = now
                        });
                }
            }

            // Mortgage — API returns items[] each with mortgageTransactionDetail + lenderDetail
            if (result.Mortgage?.Items != null)
            {
                foreach (var item in result.Mortgage.Items)
                {
                    if (item?.TransactionDetail == null) continue;
                    var td = item.TransactionDetail;

                    string lenderName = item.LenderDetail?.LenderCompanyName
                                     ?? item.LenderDetail?.LenderFullName;

                    // date fields are YYYYMMDD integers — convert to DateTime
                    DateTime? originationDate = td.Date.HasValue
                        ? ParseIntDate(td.Date.Value) : (DateTime?)null;
                    DateTime? maturityDate = td.DueDate.HasValue
                        ? ParseIntDate(td.DueDate.Value) : (DateTime?)null;

                    db.PropertySnapshotMortgages.InsertOnSubmit(new PropertySnapshotMortgage
                    {
                        PropertySnapshotMortgageId = Guid.NewGuid(),
                        PropertySnapshotId         = sid,
                        LenderName                 = lenderName,
                        LoanAmount                 = td.Amount,
                        OriginationDate            = originationDate,
                        LoanType                   = td.LoanTypeCodeDescription ?? td.LoanTypeCode,
                        InterestRate               = td.InterestRate,
                        LoanPosition               = td.LienPosition?.ToString(),
                        MaturityDate               = maturityDate,
                        CreatedAtUtc               = now
                    });
                }
            }

            // Voluntary Liens — one row per lien
            if (result.EnrichedVoluntaryLiens?.Data != null)
            {
                foreach (var l in result.EnrichedVoluntaryLiens.Data)
                {
                    db.PropertySnapshotVoluntaryLiens.InsertOnSubmit(new PropertySnapshotVoluntaryLien
                    {
                        PropertySnapshotVoluntaryLienId = Guid.NewGuid(),
                        PropertySnapshotId              = sid,
                        LienAmount                      = l.LienAmount,
                        LienType                        = l.LienType,
                        LenderName                      = l.LenderName,
                        RecordingDate                   = ParseDate(l.RecordingDate),
                        DocumentNumber                  = l.DocumentNumber,
                        CreatedAtUtc                    = now
                    });
                }
            }

            // Involuntary Liens — one row per lien
            if (result.InvoluntaryLiens?.Data != null)
            {
                foreach (var l in result.InvoluntaryLiens.Data)
                {
                    db.PropertySnapshotInvoluntaryLiens.InsertOnSubmit(
                        new PropertySnapshotInvoluntaryLien
                        {
                            PropertySnapshotInvoluntaryLienId = Guid.NewGuid(),
                            PropertySnapshotId                = sid,
                            LienAmount                        = l.LienAmount,
                            LienType                          = l.LienType,
                            CreditorName                      = l.CreditorName,
                            RecordingDate                     = ParseDate(l.RecordingDate),
                            ReleaseDate                       = ParseDate(l.ReleaseDate),
                            DocumentNumber                    = l.DocumentNumber,
                            CreatedAtUtc                      = now
                        });
                }
            }

            // Tax Assessment — API returns items[]; use first item. Structure:
            // items[].taxAmount.{ totalTaxAmount, billedYear, delinquentYear }
            // items[].assessedValue.{ calculatedTotalValue, calculatedLandValue,
            //                         calculatedImprovementValue, taxAssessedYear }
            var taxItem = result.TaxAssessment?.Items?.Count > 0
                          ? result.TaxAssessment.Items[0] : null;
            if (taxItem != null)
            {
                var ta = taxItem.TaxAmount;
                var av = taxItem.AssessedValue;

                db.PropertySnapshotTaxAssessments.InsertOnSubmit(new PropertySnapshotTaxAssessment
                {
                    PropertySnapshotTaxAssessmentId = Guid.NewGuid(),
                    PropertySnapshotId              = sid,
                    AssessedValue                   = av?.CalculatedTotalValue,
                    AssessedLandValue               = av?.CalculatedLandValue,
                    AssessedImprovementValue        = av?.CalculatedImprovementValue,
                    MarketValue                     = av?.TaxableValue,
                    TaxAmount                       = ta?.TotalTaxAmount ?? ta?.NetTaxAmount,
                    TaxYear                         = ta?.BilledYear?.ToString(),
                    TaxDelinquentYear               = ta?.DelinquentYear?.ToString(),
                    CreatedAtUtc                    = now
                });
            }

            // Building — API returns data.allBuildingsSummary (summary totals) +
            // data.Buildings[] (per-building detail). Use summary for top-level sqft/rooms,
            // and first building entry for yearBuilt, construction details.
            if (result.Building?.Data != null)
            {
                var bd      = result.Building.Data;
                var summary = bd.AllBuildingsSummary;
                var bldg0   = bd.Buildings?.Count > 0 ? bd.Buildings[0] : null;
                var cd      = bldg0?.ConstructionDetails;
                var ia      = bldg0?.InteriorArea;
                var ir      = bldg0?.InteriorRooms;
                var vp      = bldg0?.StructureVerticalProfile;

                // Best sqft: universalBuildingAreaSquareFeet > livingAreaSquareFeet > summary
                decimal? sqft = ia?.UniversalBuildingAreaSquareFeet.HasValue == true
                    ? (decimal?)ia.UniversalBuildingAreaSquareFeet.Value
                    : (ia?.LivingAreaSquareFeet ?? summary?.LivingAreaSquareFeet);

                db.PropertySnapshotBuildings.InsertOnSubmit(new PropertySnapshotBuilding
                {
                    PropertySnapshotBuildingId = Guid.NewGuid(),
                    PropertySnapshotId         = sid,
                    YearBuilt                  = cd?.YearBuilt,
                    EffectiveYearBuilt         = cd?.EffectiveYearBuilt,
                    GrossLivingArea            = sqft,
                    TotalRooms                 = ir?.TotalCount,
                    Bedrooms                   = ir?.BedroomsCount ?? summary?.BedroomsCount,
                    Bathrooms                  = ir?.BathroomsCount ?? summary?.BathroomsCount,
                    Stories                    = vp?.StoriesCount,
                    ConstructionType           = cd?.ConstructionTypeCode,
                    RoofType                   = bldg0?.StructureExterior?.Roof?.RoofMaterialTypeCode,
                    BuildingCondition          = cd?.BuildingImprovementConditionCode,
                    CreatedAtUtc               = now
                });
            }

            // Building Permits — one row per permit
            if (result.BuildingPermits?.Data != null)
            {
                foreach (var p in result.BuildingPermits.Data)
                {
                    db.PropertySnapshotBuildingPermits.InsertOnSubmit(
                        new PropertySnapshotBuildingPermit
                        {
                            PropertySnapshotBuildingPermitId = Guid.NewGuid(),
                            PropertySnapshotId               = sid,
                            PermitNumber                     = p.PermitNumber,
                            PermitType                       = p.PermitType,
                            IssueDate                        = ParseDate(p.IssueDate),
                            CompletionDate                   = ParseDate(p.CompletionDate),
                            Status                           = p.Status,
                            JobValue                         = p.JobValue,
                            Description                      = p.Description,
                            CreatedAtUtc                     = now
                        });
                }
            }

            // AVM — API returns summary.{estimatedValue, lowValue, highValue, confidenceScore}
            // (not data.estimatedValue — that shape was wrong)
            if (result.Avm?.Summary != null)
            {
                var s = result.Avm.Summary;
                db.PropertySnapshotAvms.InsertOnSubmit(new PropertySnapshotAvm
                {
                    PropertySnapshotAvmId       = Guid.NewGuid(),
                    PropertySnapshotId          = sid,
                    EstimatedValue              = s.EstimatedValue,
                    EstimatedValueHigh          = s.HighValue,
                    EstimatedValueLow           = s.LowValue,
                    ForecastStandardDeviation   = s.ForecastStandardDeviation,
                    ConfidenceScore             = s.ConfidenceScore,
                    ValuationDate               = ParseDate(s.ProcessedDate),
                    RentEstimatedValue          = null,   // not in RVM summary response
                    RentEstimatedValueHigh      = null,
                    RentEstimatedValueLow       = null,
                    CapRate                     = null,
                    CreatedAtUtc                = now
                });

                // Back-fill the flat Data property so PropertyDetail page can read it
                result.Avm.Data = new AvmData
                {
                    EstimatedValue            = s.EstimatedValue,
                    EstimatedValueHigh        = s.HighValue,
                    EstimatedValueLow         = s.LowValue,
                    ForecastStandardDeviation = s.ForecastStandardDeviation,
                    ConfidenceScore           = s.ConfidenceScore,
                    ValuationDate             = s.ProcessedDate
                };
            }

            // Propensity
            if (result.Propensity?.Data != null)
            {
                var d = result.Propensity.Data;
                db.PropertySnapshotPropensities.InsertOnSubmit(new PropertySnapshotPropensity
                {
                    PropertySnapshotPropensityId = Guid.NewGuid(),
                    PropertySnapshotId           = sid,
                    PropensityScore              = d.PropensityScore,
                    PropensityTier               = d.PropensityTier,
                    PropensityScoreDate          = ParseDate(d.PropensityScoreDate),
                    CreatedAtUtc                 = now
                });
            }

            // HOA
            if (result.Hoa?.Data != null)
            {
                var d = result.Hoa.Data;
                db.PropertySnapshotHoas.InsertOnSubmit(new PropertySnapshotHoa
                {
                    PropertySnapshotHoaId = Guid.NewGuid(),
                    PropertySnapshotId    = sid,
                    HoaName               = d.HoaName,
                    HoaFeeAmount          = d.HoaFeeAmount,
                    HoaFeeFrequency       = d.HoaFeeFrequency,
                    HoaPhone              = d.HoaPhone,
                    CreatedAtUtc          = now
                });
            }

            // Climate Risk
            if (result.ClimateRisk?.Data != null)
            {
                var d = result.ClimateRisk.Data;
                db.PropertySnapshotClimateRisks.InsertOnSubmit(new PropertySnapshotClimateRisk
                {
                    PropertySnapshotClimateRiskId = Guid.NewGuid(),
                    PropertySnapshotId            = sid,
                    FloodRiskScore                = d.FloodRiskScore,
                    FloodRiskLabel                = d.FloodRiskLabel,
                    FireRiskScore                 = d.FireRiskScore,
                    FireRiskLabel                 = d.FireRiskLabel,
                    WindRiskScore                 = d.WindRiskScore,
                    WindRiskLabel                 = d.WindRiskLabel,
                    HeatRiskScore                 = d.HeatRiskScore,
                    HeatRiskLabel                 = d.HeatRiskLabel,
                    CreatedAtUtc                  = now
                });
            }

            // Commit all detail records in one shot
            db.SubmitChanges();

            return sid;
        }

        // ---------------------------------------------------------------
        // Load cached snapshot from DB
        // ---------------------------------------------------------------

        private PropertyProfileResult LoadProfileFromDb(
            DCReyla db,
            Guid    snapshotId,
            Property property)
        {
            var result = new PropertyProfileResult
            {
                PropertySnapshotId = snapshotId,
                Property           = property,
                IsFromCache        = true
            };

            // Ownership — reconstruct nested shape that PropertyDetail reads
            var own = db.PropertySnapshotOwnerships
                .FirstOrDefault(x => x.PropertySnapshotId == snapshotId);
            if (own != null)
            {
                var ownerNames = new System.Collections.Generic.List<OwnerName>();
                if (!string.IsNullOrEmpty(own.OwnerName))
                    ownerNames.Add(new OwnerName
                    {
                        Name      = own.OwnerName,
                        FirstName = own.Owner1FirstName,
                        LastName  = own.Owner1LastName
                    });
                if (!string.IsNullOrEmpty(own.Owner2FirstName) || !string.IsNullOrEmpty(own.Owner2LastName))
                    ownerNames.Add(new OwnerName
                    {
                        FirstName = own.Owner2FirstName,
                        LastName  = own.Owner2LastName
                    });

                result.Ownership = new OwnershipResult
                {
                    Data = new OwnershipData
                    {
                        CurrentOwners = new CurrentOwners
                        {
                            OwnerNames         = ownerNames,
                            OccupancyCode      = own.AbsenteeOwnerStatus,
                            OwnershipRightsCode = own.OwnershipType
                        },
                        CurrentOwnerMailingInfo = new CurrentOwnerMailingInfo
                        {
                            MailingAddress = new OwnerMailingAddress
                            {
                                StreetAddress = own.MailingAddress,
                                City          = own.MailingCity,
                                State         = own.MailingState,
                                ZipCode       = own.MailingZipCode
                            }
                        }
                    }
                };
            }

            // Ownership Transfers — reconstruct Items[] shape
            var transfers = db.PropertySnapshotOwnershipTransfers
                .Where(x => x.PropertySnapshotId == snapshotId)
                .OrderByDescending(x => x.SaleDate)
                .ToList();
            if (transfers.Any())
            {
                result.OwnershipTransfers = new OwnershipTransfersResult
                {
                    Items = transfers.Select(t => new OwnershipTransferItem
                    {
                        TransactionDetails = new OwnershipTransferDetails
                        {
                            SaleDateDerived    = t.SaleDate?.ToString("yyyy-MM-dd"),
                            SaleAmount         = t.SaleAmount,
                            DeedCategoryCode   = t.DeedType,
                            SaleDocumentNumber = t.DocumentNumber
                        },
                        BuyerDetails  = string.IsNullOrEmpty(t.BuyerName)  ? null
                            : new OwnershipTransferPartyDetails
                              { BuyerNames = new System.Collections.Generic.List<OwnerName>
                                { new OwnerName { Name = t.BuyerName } } },
                        SellerDetails = string.IsNullOrEmpty(t.SellerName) ? null
                            : new OwnershipTransferPartyDetails
                              { SellerNames = new System.Collections.Generic.List<OwnerName>
                                { new OwnerName { Name = t.SellerName } } }
                    }).ToList()
                };
            }

            // Mortgage — reconstruct Items[] shape
            var mortgages = db.PropertySnapshotMortgages
                .Where(x => x.PropertySnapshotId == snapshotId).ToList();
            if (mortgages.Any())
            {
                result.Mortgage = new MortgageResult
                {
                    Items = mortgages.Select(m => new MortgageTransactionItem
                    {
                        TransactionDetail = new MortgageTransactionDetail
                        {
                            Amount           = m.LoanAmount,
                            InterestRate     = m.InterestRate,
                            LienPosition     = int.TryParse(m.LoanPosition, out int pos) ? pos : (int?)null,
                            LoanTypeCodeDescription = m.LoanType
                        },
                        LenderDetail = new MortgageLenderDetail { LenderCompanyName = m.LenderName }
                    }).ToList()
                };
            }

            // Voluntary Liens
            var vLiens = db.PropertySnapshotVoluntaryLiens
                .Where(x => x.PropertySnapshotId == snapshotId).ToList();
            if (vLiens.Any())
            {
                result.EnrichedVoluntaryLiens = new EnrichedVoluntaryLiensResult
                {
                    Data = vLiens.Select(l => new VoluntaryLien
                    {
                        LienAmount     = l.LienAmount,
                        LienType       = l.LienType,
                        LenderName     = l.LenderName,
                        RecordingDate  = l.RecordingDate?.ToString("yyyy-MM-dd"),
                        DocumentNumber = l.DocumentNumber
                    }).ToList()
                };
            }

            // Involuntary Liens
            var iLiens = db.PropertySnapshotInvoluntaryLiens
                .Where(x => x.PropertySnapshotId == snapshotId).ToList();
            if (iLiens.Any())
            {
                result.InvoluntaryLiens = new InvoluntaryLiensResult
                {
                    Data = iLiens.Select(l => new InvoluntaryLien
                    {
                        LienAmount     = l.LienAmount,
                        LienType       = l.LienType,
                        CreditorName   = l.CreditorName,
                        RecordingDate  = l.RecordingDate?.ToString("yyyy-MM-dd"),
                        ReleaseDate    = l.ReleaseDate?.ToString("yyyy-MM-dd"),
                        DocumentNumber = l.DocumentNumber
                    }).ToList()
                };
            }

            // Tax Assessment — reconstruct Items[] shape
            var tax = db.PropertySnapshotTaxAssessments
                .FirstOrDefault(x => x.PropertySnapshotId == snapshotId);
            if (tax != null)
            {
                result.TaxAssessment = new TaxAssessmentResult
                {
                    Items = new System.Collections.Generic.List<TaxAssessmentItem>
                    {
                        new TaxAssessmentItem
                        {
                            TaxAmount = new TaxAmountData
                            {
                                TotalTaxAmount  = tax.TaxAmount,
                                BilledYear      = int.TryParse(tax.TaxYear, out int yr) ? yr : (int?)null,
                                DelinquentYear  = int.TryParse(tax.TaxDelinquentYear, out int dy) ? dy : (int?)null
                            },
                            AssessedValue = new AssessedValueData
                            {
                                CalculatedTotalValue       = tax.AssessedValue,
                                CalculatedLandValue        = tax.AssessedLandValue,
                                CalculatedImprovementValue = tax.AssessedImprovementValue,
                                TaxableValue               = tax.MarketValue
                            }
                        }
                    }
                };
            }

            // Building — reconstruct AllBuildingsSummary + Buildings[] shape
            var bldg = db.PropertySnapshotBuildings
                .FirstOrDefault(x => x.PropertySnapshotId == snapshotId);
            if (bldg != null)
            {
                result.Building = new BuildingResult
                {
                    Data = new BuildingData
                    {
                        AllBuildingsSummary = new AllBuildingsSummary
                        {
                            LivingAreaSquareFeet = bldg.GrossLivingArea,
                            BedroomsCount        = bldg.Bedrooms,
                            BathroomsCount       = bldg.Bathrooms,
                            RoomsCount           = bldg.TotalRooms
                        },
                        Buildings = new System.Collections.Generic.List<Building>
                        {
                            new Building
                            {
                                ConstructionDetails = new ConstructionDetails
                                {
                                    YearBuilt                        = bldg.YearBuilt,
                                    EffectiveYearBuilt               = bldg.EffectiveYearBuilt,
                                    ConstructionTypeCode             = bldg.ConstructionType,
                                    BuildingImprovementConditionCode = bldg.BuildingCondition
                                },
                                InteriorArea = new InteriorArea
                                {
                                    LivingAreaSquareFeet = bldg.GrossLivingArea
                                },
                                InteriorRooms = new InteriorRooms
                                {
                                    TotalCount    = bldg.TotalRooms,
                                    BedroomsCount = bldg.Bedrooms,
                                    BathroomsCount = bldg.Bathrooms
                                },
                                StructureVerticalProfile = new StructureVerticalProfile
                                {
                                    StoriesCount = bldg.Stories
                                },
                                StructureExterior = bldg.RoofType == null ? null : new StructureExterior
                                {
                                    Roof = new RoofInfo { RoofMaterialTypeCode = bldg.RoofType }
                                }
                            }
                        }
                    }
                };
            }

            // Building Permits
            var permits = db.PropertySnapshotBuildingPermits
                .Where(x => x.PropertySnapshotId == snapshotId).ToList();
            if (permits.Any())
            {
                result.BuildingPermits = new BuildingPermitsResult
                {
                    Data = permits.Select(p => new BuildingPermit
                    {
                        PermitNumber   = p.PermitNumber,
                        PermitType     = p.PermitType,
                        IssueDate      = p.IssueDate?.ToString("yyyy-MM-dd"),
                        CompletionDate = p.CompletionDate?.ToString("yyyy-MM-dd"),
                        Status         = p.Status,
                        JobValue       = p.JobValue,
                        Description    = p.Description
                    }).ToList()
                };
            }

            // AVM — reconstruct Summary shape + back-fill flat Data for detail page
            var avm = db.PropertySnapshotAvms
                .FirstOrDefault(x => x.PropertySnapshotId == snapshotId);
            if (avm != null)
            {
                result.Avm = new AvmResult
                {
                    Summary = new AvmSummary
                    {
                        EstimatedValue            = avm.EstimatedValue,
                        HighValue                 = avm.EstimatedValueHigh,
                        LowValue                  = avm.EstimatedValueLow,
                        ForecastStandardDeviation = avm.ForecastStandardDeviation,
                        ConfidenceScore           = avm.ConfidenceScore,
                        ProcessedDate             = avm.ValuationDate?.ToString("yyyy-MM-dd")
                    },
                    Data = new AvmData
                    {
                        EstimatedValue            = avm.EstimatedValue,
                        EstimatedValueHigh        = avm.EstimatedValueHigh,
                        EstimatedValueLow         = avm.EstimatedValueLow,
                        ForecastStandardDeviation = avm.ForecastStandardDeviation,
                        ConfidenceScore           = avm.ConfidenceScore,
                        ValuationDate             = avm.ValuationDate?.ToString("yyyy-MM-dd"),
                        RentEstimatedValue        = avm.RentEstimatedValue,
                        RentEstimatedValueHigh    = avm.RentEstimatedValueHigh,
                        RentEstimatedValueLow     = avm.RentEstimatedValueLow,
                        CapRate                   = avm.CapRate
                    }
                };
            }

            // Propensity
            var prop = db.PropertySnapshotPropensities
                .FirstOrDefault(x => x.PropertySnapshotId == snapshotId);
            if (prop != null)
            {
                result.Propensity = new PropensityResult
                {
                    Data = new PropensityData
                    {
                        PropensityScore     = prop.PropensityScore,
                        PropensityTier      = prop.PropensityTier,
                        PropensityScoreDate = prop.PropensityScoreDate?.ToString("yyyy-MM-dd")
                    }
                };
            }

            // HOA
            var hoa = db.PropertySnapshotHoas
                .FirstOrDefault(x => x.PropertySnapshotId == snapshotId);
            if (hoa != null)
            {
                result.Hoa = new HoaResult
                {
                    Data = new HoaData
                    {
                        HoaName         = hoa.HoaName,
                        HoaFeeAmount    = hoa.HoaFeeAmount,
                        HoaFeeFrequency = hoa.HoaFeeFrequency,
                        HoaPhone        = hoa.HoaPhone
                    }
                };
            }

            // Climate Risk
            var climate = db.PropertySnapshotClimateRisks
                .FirstOrDefault(x => x.PropertySnapshotId == snapshotId);
            if (climate != null)
            {
                result.ClimateRisk = new ClimateRiskResult
                {
                    Data = new ClimateRiskData
                    {
                        FloodRiskScore = climate.FloodRiskScore,
                        FloodRiskLabel = climate.FloodRiskLabel,
                        FireRiskScore  = climate.FireRiskScore,
                        FireRiskLabel  = climate.FireRiskLabel,
                        WindRiskScore  = climate.WindRiskScore,
                        WindRiskLabel  = climate.WindRiskLabel,
                        HeatRiskScore  = climate.HeatRiskScore,
                        HeatRiskLabel  = climate.HeatRiskLabel
                    }
                };
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Record individual user search history
        // ---------------------------------------------------------------

        private void RecordPropertySearch(
            DCReyla db,
            Guid    propertyId,
            Guid    organizationId,
            Guid    userId,
            string  rawInput)
        {
            db.PropertySearches.InsertOnSubmit(new PropertySearch
            {
                PropertySearchId = Guid.NewGuid(),
                PropertyId       = propertyId,
                OrganizationId   = organizationId,
                UserId           = userId,
                RawSearchInput   = rawInput,
                CreatedAtUtc     = DateTime.UtcNow
            });
            db.SubmitChanges();
        }

        // ---------------------------------------------------------------
        // Build a new Property record from a CoreLogic search match
        // ---------------------------------------------------------------

        private Property CreatePropertyRecord(DCReyla db, GeocodeDetail match)
        {
            Guid? cityExtendedId = null;
            string zip   = match.PropertyAddress?.ZipCode;
            string state = match.PropertyAddress?.State;

            if (!string.IsNullOrEmpty(zip) && !string.IsNullOrEmpty(state))
            {
                var cityRow = db.City_Extendeds
                    .FirstOrDefault(c => c.Zip == zip && c.Code == state);

                if (cityRow != null)
                    cityExtendedId = cityRow.CityExtendedId;
            }

            return new Property
            {
                PropertyId      = Guid.NewGuid(),
                Clip            = match.Clip,
                Apn             = match.PropertyApn?.Apn,
                FipsCode        = match.PropertyApn?.FipsCode,
                StreetAddress   = match.PropertyAddress?.StreetAddress,
                CityExtendedId  = cityExtendedId,
                CityNameRaw     = match.PropertyAddress?.City,
                ZipCodeRaw      = zip,
                County          = match.PropertyAddress?.County,
                Latitude        = (decimal)(match.Geocode?.Latitude ?? 0),
                Longitude       = (decimal)(match.Geocode?.Longitude ?? 0),
                CreatedAtUtc    = DateTime.UtcNow
            };
        }

        // ---------------------------------------------------------------
        // Build a Property record from a ComparableProperty (radius search)
        // Comparables already include lat/lng directly — no geocode call needed.
        // ---------------------------------------------------------------

        private Property CreatePropertyRecordFromComparable(DCReyla db, ComparableProperty comp)
        {
            Guid? cityExtendedId = null;
            string zip   = comp.ZipCode;
            string state = comp.State;

            if (!string.IsNullOrEmpty(zip) && !string.IsNullOrEmpty(state))
            {
                var cityRow = db.City_Extendeds
                    .FirstOrDefault(c => c.Zip == zip && c.Code == state);

                if (cityRow != null)
                    cityExtendedId = cityRow.CityExtendedId;
            }

            return new Property
            {
                PropertyId     = Guid.NewGuid(),
                Clip           = comp.Clip,
                Apn            = null,   // Not returned by comparables endpoint
                FipsCode       = null,   // Not returned by comparables endpoint
                StreetAddress  = comp.StreetAddress,
                CityExtendedId = cityExtendedId,
                CityNameRaw    = comp.City,
                ZipCodeRaw     = zip,
                Latitude       = (decimal)(comp.Latitude ?? 0),
                Longitude      = (decimal)(comp.Longitude ?? 0),
                CreatedAtUtc   = DateTime.UtcNow
            };
        }

        // ---------------------------------------------------------------
        // Upsert a comparable property into the Property table.
        // Called by ProspectService after GetComparables() returns results.
        // If the Clip already exists, updates lat/lng if they were null.
        // Returns the PropertyId (new or existing).
        // ---------------------------------------------------------------

        public Guid UpsertComparableProperty(DCReyla db, ComparableProperty comp)
        {
            var existing = db.Properties.FirstOrDefault(p => p.Clip == comp.Clip);

            if (existing != null)
            {
                // Refresh coordinates if they were missing
                bool changed = false;

                if (existing.Latitude == 0 && comp.Latitude.HasValue)
                {
                    existing.Latitude = (decimal)comp.Latitude;
                    changed = true;
                }

                if (existing.Longitude == 0 && comp.Longitude.HasValue)
                {
                    existing.Longitude = (decimal)comp.Longitude;
                    changed = true;
                }

                if (changed)
                {
                    existing.UpdatedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();
                }

                return existing.PropertyId;
            }

            // New property — insert it
            var property = CreatePropertyRecordFromComparable(db, comp);
            db.Properties.InsertOnSubmit(property);
            db.SubmitChanges();

            return property.PropertyId;
        }

        // ---------------------------------------------------------------
        // Force-refresh a snapshot regardless of expiry.
        // Called when user clicks "Get Fresh Data" on the prospect page.
        // Marks the existing snapshot deleted, pulls all 13 endpoints again.
        // ---------------------------------------------------------------

        public PropertyProfileResult ForceRefreshByClip(string clip, Guid organizationId)
        {
            using (var db = new DCReyla())
            {
                var property = db.Properties.FirstOrDefault(p => p.Clip == clip);
                if (property == null) return PropertyProfileResult.NotFound();
                return ForceRefresh(property.PropertyId, organizationId);
            }
        }

        public PropertyProfileResult ForceRefresh(
            Guid   propertyId,
            Guid   organizationId)
        {
            using (var db = new DCReyla())
            {
                // Mark all existing org snapshots for this property as deleted
                var stale = db.PropertySnapshots
                    .Where(s =>
                        s.PropertyId     == propertyId     &&
                        s.OrganizationId == organizationId &&
                        s.IsDeleted      == false)
                    .ToList();

                var now = DateTime.UtcNow;

                foreach (var s in stale)
                {
                    s.IsDeleted      = true;
                    s.IsActive       = false;
                    s.UpdatedAtUtc   = now;

                    // Record when the user requested fresh data
                    s.LastRefreshRequestedUtc = now;
                }

                db.SubmitChanges();

                // Load the property record
                var property = db.Properties.FirstOrDefault(p => p.PropertyId == propertyId);
                if (property == null)
                    return PropertyProfileResult.NotFound();

                return PullAndPersistProfile(db, property.Clip, property, organizationId);
            }
        }

        // ---------------------------------------------------------------
        // Safe API call wrapper — returns null on any exception
        // ---------------------------------------------------------------

        // ── Endpoint call diagnostics ─────────────────────────────────────
        // Populated during PullAndPersistProfile — keyed by endpoint name
        public Dictionary<string, string> EndpointDiagnostics { get; }
            = new Dictionary<string, string>();

        private T TryGet<T>(string endpointName, Func<T> apiCall) where T : class
        {
            try
            {
                var result = apiCall();
                EndpointDiagnostics[endpointName] = result != null ? "OK" : "OK (null result)";
                return result;
            }
            catch (CoreLogicApiException ex)
            {
                var msg = $"HTTP {ex.HttpStatusCode} — {ex.ResponseBody}";
                EndpointDiagnostics[endpointName] = msg;
                System.Diagnostics.Trace.TraceError(
                    "[PropertySnapshotService] {0}: {1}", endpointName, msg);
                return null;
            }
            catch (Exception ex)
            {
                EndpointDiagnostics[endpointName] = ex.Message;
                System.Diagnostics.Trace.TraceError(
                    "[PropertySnapshotService] {0}: {1}", endpointName, ex.Message);
                return null;
            }
        }

        // ---------------------------------------------------------------
        // Parse date strings from CoreLogic (format: yyyy-MM-dd)
        // ---------------------------------------------------------------

        private DateTime? ParseDate(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            DateTime parsed;
            return DateTime.TryParse(value, out parsed) ? parsed : (DateTime?)null;
        }

        /// <summary>
        /// Parses CoreLogic integer date fields (YYYYMMDD) to DateTime.
        /// Mortgage date/recordingDate/dueDate fields are returned as integers.
        /// </summary>
        private DateTime? ParseIntDate(int value)
        {
            if (value <= 0) return null;
            string s = value.ToString();
            if (s.Length != 8) return null;
            DateTime parsed;
            return DateTime.TryParseExact(s, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out parsed) ? parsed : (DateTime?)null;
        }
    }
}
