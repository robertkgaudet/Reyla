using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reyla.Services.Models
{
    // ===================================================================
    // IMPORTANT — ALL JSON SHAPES VERIFIED AGAINST:
    //   CoreLogic SDP Property API v2.0 OpenAPI 3 swagger spec
    //
    // Prior models had WRONG property paths throughout, causing silent
    // deserialization failures (everything mapped to null).
    // ===================================================================


    // ===================================================================
    // 1. PROPERTY SEARCH  —  /v2/properties/search
    //    + GEOCODE via /v2/properties/{clip}/site-location
    // ===================================================================

    public class GeocodeProduct
    {
        [JsonProperty("items")]
        public List<GeocodeDetail> Items { get; set; } = new List<GeocodeDetail>();
    }

    public class GeocodeDetail
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("v1PropertyId")]
        public string V1PropertyId { get; set; }

        [JsonProperty("propertyAddress")]
        public PropertySearchAddress PropertyAddress { get; set; }

        [JsonProperty("propertyAPN")]
        public PropertyApn PropertyApn { get; set; }

        /// <summary>Populated from site-location coordinatesParcel. Null if that call fails.</summary>
        public Geocode Geocode { get; set; }
    }

    public class Geocode
    {
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    // RAW /v2/properties/search response (internal)
    internal class PropertySearchProductV2
    {
        [JsonProperty("items")]
        public List<PropertySearchDetailV2> Items { get; set; } = new List<PropertySearchDetailV2>();
    }

    internal class PropertySearchDetailV2
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("v1PropertyId")]
        public string V1PropertyId { get; set; }

        [JsonProperty("propertyAddress")]
        public PropertySearchAddressV2 PropertyAddress { get; set; }

        [JsonProperty("propertyAPN")]
        public PropertyApn PropertyApn { get; set; }
    }

    internal class PropertySearchAddressV2
    {
        [JsonProperty("streetAddress")]
        public string StreetAddress { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }

        [JsonProperty("county")]
        public string County { get; set; }
    }

    // RAW site-location response (internal — used by geocode fallback)
    internal class SingleApiResponseSiteLocationData
    {
        [JsonProperty("data")]
        public SiteLocationData Data { get; set; }
    }

    internal class SiteLocationData
    {
        [JsonProperty("coordinatesParcel")]
        public CoordinatesParcel CoordinatesParcel { get; set; }
    }

    internal class CoordinatesParcel
    {
        [JsonProperty("lat")]
        public decimal? Lat { get; set; }

        [JsonProperty("lng")]
        public decimal? Lng { get; set; }
    }

    public class PropertySearchAddress
    {
        [JsonProperty("streetAddress")]
        public string StreetAddress { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }

        [JsonProperty("county")]
        public string County { get; set; }
    }

    public class PropertyApn
    {
        [JsonProperty("apn")]
        public string Apn { get; set; }

        [JsonProperty("fipsCode")]
        public string FipsCode { get; set; }
    }


    // ===================================================================
    // 2. OWNERSHIP  —  GET /v2/properties/{clip}/ownership
    //
    // Actual response shape:
    // { data: { clip, currentOwners: { ownerNames: [{ownerName}],
    //           ownershipRightsCode, occupancyCode },
    //   currentOwnerMailingInfo: { mailingAddress: { streetAddress,
    //           city, state, zipCode } } } }
    // ===================================================================

    public class OwnershipResult
    {
        [JsonProperty("data")]
        public OwnershipData Data { get; set; }
    }

    public class OwnershipData
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("currentOwners")]
        public CurrentOwners CurrentOwners { get; set; }

        [JsonProperty("currentOwnerMailingInfo")]
        public CurrentOwnerMailingInfo CurrentOwnerMailingInfo { get; set; }
    }

    public class CurrentOwners
    {
        [JsonProperty("ownerNames")]
        public List<OwnerName> OwnerNames { get; set; } = new List<OwnerName>();

        /// <summary>e.g. "I" = individual, "C" = corporate, "T" = trust</summary>
        [JsonProperty("ownershipRightsCode")]
        public string OwnershipRightsCode { get; set; }

        /// <summary>"Y" = absentee owner (motivated seller signal)</summary>
        [JsonProperty("occupancyCode")]
        public string OccupancyCode { get; set; }

        [JsonProperty("ownerEtalCode")]
        public string OwnerEtalCode { get; set; }
    }

    public class OwnerName
    {
        /// <summary>Full name as returned by API ("fullName" JSON field).</summary>
        [JsonProperty("fullName")]
        public string Name { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("middleName")]
        public string MiddleName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("firstNameAndMiddleInitial")]
        public string FirstNameAndMiddleInitial { get; set; }

        /// <summary>API returns "Y"/"N" string, not a true boolean.</summary>
        [JsonProperty("isCorporate")]
        public string IsCorporate { get; set; }

        [JsonProperty("sequenceNumber")]
        public int? SequenceNumber { get; set; }
    }

    public class CurrentOwnerMailingInfo
    {
        [JsonProperty("mailingAddress")]
        public OwnerMailingAddress MailingAddress { get; set; }

        /// <summary>"Y" = owner opted out of mailing</summary>
        [JsonProperty("ownerMailingOptOutIndicator")]
        public string OwnerMailingOptOutIndicator { get; set; }
    }

    public class OwnerMailingAddress
    {
        [JsonProperty("streetAddress")]
        public string StreetAddress { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }
    }

    // ===================================================================
    // 3. OWNERSHIP TRANSFERS  —  GET /v2/properties/{clip}/ownership-transfers/all/latest
    //
    // Actual response shape:
    // { metadata: {...}, items: [{ clip, transactionDetails: {
    //   saleDateDerived, saleAmount, saleDocumentTypeCode,
    //   saleDocumentNumber, isCashPurchase }, buyerDetails: [...],
    //   sellerDetails: [...] }] }
    // ===================================================================

    public class OwnershipTransfersResult
    {
        [JsonProperty("metadata")]
        public OwnershipTransferMetadata Metadata { get; set; }

        [JsonProperty("items")]
        public List<OwnershipTransferItem> Items { get; set; } = new List<OwnershipTransferItem>();
    }

    public class OwnershipTransferMetadata
    {
        [JsonProperty("totalRecords")]
        public int? TotalRecords { get; set; }
    }

    public class OwnershipTransferItem
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("transactionDetails")]
        public OwnershipTransferDetails TransactionDetails { get; set; }

        [JsonProperty("buyerDetails")]
        public OwnershipTransferPartyDetails BuyerDetails { get; set; }

        [JsonProperty("sellerDetails")]
        public OwnershipTransferPartyDetails SellerDetails { get; set; }

        [JsonProperty("titleCompany")]
        public OwnershipTransferTitleCompany TitleCompany { get; set; }
    }

    public class OwnershipTransferDetails
    {
        /// <summary>Cotality derived sale date — most reliable date field.</summary>
        [JsonProperty("saleDateDerived")]
        public string SaleDateDerived { get; set; }

        [JsonProperty("saleRecordingDateDerived")]
        public string SaleRecordingDateDerived { get; set; }

        [JsonProperty("saleAmount")]
        public decimal? SaleAmount { get; set; }

        [JsonProperty("deedCategoryCode")]
        public string DeedCategoryCode { get; set; }

        [JsonProperty("saleDocumentTypeCode")]
        public string SaleDocumentTypeCode { get; set; }

        [JsonProperty("saleDocumentNumber")]
        public string SaleDocumentNumber { get; set; }

        [JsonProperty("saleBookNumber")]
        public string SaleBookNumber { get; set; }

        [JsonProperty("salePageNumber")]
        public string SalePageNumber { get; set; }

        [JsonProperty("isCashPurchase")]
        public bool? IsCashPurchase { get; set; }

        [JsonProperty("isForeclosureReo")]
        public bool? IsForeclosureReo { get; set; }

        [JsonProperty("isShortSale")]
        public bool? IsShortSale { get; set; }
    }

    public class OwnershipTransferPartyDetails
    {
        /// <summary>Used by buyerDetails — JSON field is "buyerNames".</summary>
        [JsonProperty("buyerNames")]
        public List<OwnerName> BuyerNames { get; set; } = new List<OwnerName>();

        /// <summary>Used by sellerDetails — JSON field is "sellerNames".</summary>
        [JsonProperty("sellerNames")]
        public List<OwnerName> SellerNames { get; set; } = new List<OwnerName>();

        /// <summary>Convenience: returns whichever name list is populated.</summary>
        [JsonIgnore]
        public List<OwnerName> OwnerNames =>
            BuyerNames?.Count > 0 ? BuyerNames :
            SellerNames?.Count > 0 ? SellerNames :
            new List<OwnerName>();

        [JsonProperty("relationshipTypeCode")]
        public string RelationshipTypeCode { get; set; }

        [JsonProperty("ownershipRightsCode")]
        public string OwnershipRightsCode { get; set; }

        [JsonProperty("occupancyCode")]
        public string OccupancyCode { get; set; }

        [JsonProperty("etalCode")]
        public string EtalCode { get; set; }
    }

    public class OwnershipTransferTitleCompany
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    // PropertySnapshotService still calls into OwnershipTransfer objects.
    // Provide a flat OwnershipTransfer class that PropertySnapshotService.cs
    // can populate from the new OwnershipTransferItem shape.
    // (PropertySnapshotService maps OwnershipTransfersResult → DB rows)
    public class OwnershipTransfer
    {
        public string  SaleDate     { get; set; }
        public decimal? SaleAmount  { get; set; }
        public string  BuyerName    { get; set; }
        public string  SellerName   { get; set; }
        public string  DeedType     { get; set; }
        public string  DocumentNumber { get; set; }
    }


    // ===================================================================
    // 4. MORTGAGE  —  GET /v2/properties/{clip}/mortgage
    //
    // Actual response shape:
    // { clip, countyMortgageCoverageSummary: {...},
    //   items: [{ mortgageTransactionDetail: { amount, date,
    //   interestRate, loanTypeCode, statusIndicator },
    //   lenderDetail: { lenderCompanyName }, borrowerDetail: {} }] }
    // ===================================================================

    public class MortgageResult
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("items")]
        public List<MortgageTransactionItem> Items { get; set; } = new List<MortgageTransactionItem>();
    }

    public class MortgageTransactionItem
    {
        [JsonProperty("mortgageTransactionDetail")]
        public MortgageTransactionDetail TransactionDetail { get; set; }

        [JsonProperty("lenderDetail")]
        public MortgageLenderDetail LenderDetail { get; set; }

        [JsonProperty("borrowerDetail")]
        public MortgageBorrowerDetail BorrowerDetail { get; set; }
    }

    public class MortgageTransactionDetail
    {
        /// <summary>Loan amount at origination.</summary>
        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        /// <summary>Borrower signature date — format YYYYMMDD integer.</summary>
        [JsonProperty("date")]
        public int? Date { get; set; }

        [JsonProperty("recordingDate")]
        public int? RecordingDate { get; set; }

        [JsonProperty("interestRate")]
        public decimal? InterestRate { get; set; }

        [JsonProperty("loanTypeCode")]
        public string LoanTypeCode { get; set; }

        [JsonProperty("loanTypeCodeDescription")]
        public string LoanTypeCodeDescription { get; set; }

        /// <summary>"O" = open, "C" = closed/paid off</summary>
        [JsonProperty("statusIndicator")]
        public string StatusIndicator { get; set; }

        [JsonProperty("term")]
        public int? Term { get; set; }

        [JsonProperty("termCode")]
        public string TermCode { get; set; }

        [JsonProperty("purposeCode")]
        public string PurposeCode { get; set; }

        [JsonProperty("lienPosition")]
        public int? LienPosition { get; set; }

        [JsonProperty("dueDate")]
        public int? DueDate { get; set; }

        [JsonProperty("documentTypeCode")]
        public string DocumentTypeCode { get; set; }

        [JsonProperty("documentNumber")]
        public string DocumentNumber { get; set; }
    }

    public class MortgageLenderDetail
    {
        [JsonProperty("lenderCompanyName")]
        public string LenderCompanyName { get; set; }

        [JsonProperty("lenderFullName")]
        public string LenderFullName { get; set; }
    }

    public class MortgageBorrowerDetail
    {
        [JsonProperty("borrowers")]
        public List<MortgageBorrower> Borrowers { get; set; } = new List<MortgageBorrower>();
    }

    public class MortgageBorrower
    {
        [JsonProperty("fullName")]
        public string FullName { get; set; }
    }

    // Flat MortgageLoan for backward compat with PropertySnapshotService
    public class MortgageLoan
    {
        public string  LenderName      { get; set; }
        public decimal? LoanAmount     { get; set; }
        public string  OriginationDate { get; set; }
        public string  LoanType        { get; set; }
        public decimal? InterestRate   { get; set; }
        public string  LoanPosition    { get; set; }
        public string  MaturityDate    { get; set; }
    }


    // ===================================================================
    // 5. ENRICHED VOLUNTARY LIENS  —  GET /v2/properties/liens/enriched/{clip}
    // ===================================================================

    public class EnrichedVoluntaryLiensResult
    {
        [JsonProperty("data")]
        public List<VoluntaryLien> Data { get; set; } = new List<VoluntaryLien>();
    }

    public class VoluntaryLien
    {
        [JsonProperty("lienAmount")]
        public decimal? LienAmount { get; set; }

        [JsonProperty("lienType")]
        public string LienType { get; set; }

        [JsonProperty("lenderName")]
        public string LenderName { get; set; }

        [JsonProperty("recordingDate")]
        public string RecordingDate { get; set; }

        [JsonProperty("documentNumber")]
        public string DocumentNumber { get; set; }
    }

    // ===================================================================
    // 6. INVOLUNTARY LIENS  —  GET /v2/properties/liens/involuntary-liens/{clipId}
    // ===================================================================

    public class InvoluntaryLiensResult
    {
        [JsonProperty("data")]
        public List<InvoluntaryLien> Data { get; set; } = new List<InvoluntaryLien>();
    }

    public class InvoluntaryLien
    {
        [JsonProperty("lienAmount")]
        public decimal? LienAmount { get; set; }

        [JsonProperty("lienType")]
        public string LienType { get; set; }

        [JsonProperty("creditorName")]
        public string CreditorName { get; set; }

        [JsonProperty("recordingDate")]
        public string RecordingDate { get; set; }

        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }

        [JsonProperty("documentNumber")]
        public string DocumentNumber { get; set; }
    }


    // ===================================================================
    // 7. TAX ASSESSMENT  —  GET /v2/properties/{clip}/tax-assessments/latest
    //
    // Actual response shape:
    // { metadata: {...}, items: [{ clip,
    //   taxAmount: { totalTaxAmount, billedYear, delinquentYear },
    //   assessedValue: { calculatedTotalValue, calculatedLandValue,
    //                    calculatedImprovementValue, taxableValue,
    //                    taxAssessedYear } }] }
    // ===================================================================

    public class TaxAssessmentResult
    {
        [JsonProperty("metadata")]
        public TaxAssessmentMetadata Metadata { get; set; }

        [JsonProperty("items")]
        public List<TaxAssessmentItem> Items { get; set; } = new List<TaxAssessmentItem>();
    }

    public class TaxAssessmentMetadata
    {
        [JsonProperty("totalRecords")]
        public int? TotalRecords { get; set; }
    }

    public class TaxAssessmentItem
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("taxAmount")]
        public TaxAmountData TaxAmount { get; set; }

        [JsonProperty("assessedValue")]
        public AssessedValueData AssessedValue { get; set; }
    }

    public class TaxAmountData
    {
        [JsonProperty("billedYear")]
        public int? BilledYear { get; set; }

        [JsonProperty("delinquentYear")]
        public int? DelinquentYear { get; set; }

        [JsonProperty("totalTaxAmount")]
        public decimal? TotalTaxAmount { get; set; }

        [JsonProperty("netTaxAmount")]
        public decimal? NetTaxAmount { get; set; }

        [JsonProperty("propertyTaxRate")]
        public decimal? PropertyTaxRate { get; set; }
    }

    public class AssessedValueData
    {
        [JsonProperty("taxAssessedYear")]
        public int? TaxAssessedYear { get; set; }

        /// <summary>Land + Improvement total — closest to market value.</summary>
        [JsonProperty("calculatedTotalValue")]
        public decimal? CalculatedTotalValue { get; set; }

        [JsonProperty("calculatedLandValue")]
        public decimal? CalculatedLandValue { get; set; }

        [JsonProperty("calculatedImprovementValue")]
        public decimal? CalculatedImprovementValue { get; set; }

        [JsonProperty("taxableValue")]
        public decimal? TaxableValue { get; set; }
    }

    // Flat TaxAssessmentData kept for PropertySnapshotService compatibility
    public class TaxAssessmentData
    {
        public decimal? AssessedValue            { get; set; }
        public decimal? AssessedLandValue        { get; set; }
        public decimal? AssessedImprovementValue { get; set; }
        public decimal? MarketValue              { get; set; }
        public decimal? TaxAmount               { get; set; }
        public string   TaxYear                 { get; set; }
        public string   TaxDelinquentYear       { get; set; }
    }


    // ===================================================================
    // 8. BUILDINGS  —  GET /v2/properties/{clip}/buildings
    //
    // Actual response shape:
    // { data: { clip,
    //   allBuildingsSummary: { livingAreaSquareFeet, totalAreaSquareFeet,
    //     bedroomsCount, bathroomsCount, roomsCount, storiesCount... },
    //   Buildings: [{ constructionDetails: { yearBuilt, effectiveYearBuilt,
    //     constructionTypeCode, buildingImprovementConditionCode },
    //     interiorArea: { universalBuildingAreaSquareFeet, livingAreaSquareFeet },
    //     interiorRooms: { totalCount, bedroomsCount, bathroomsCount },
    //     structureVerticalProfile: { storiesCount },
    //     structureExterior: { roof: { roofMaterialTypeCode } } }] } }
    // ===================================================================

    public class BuildingResult
    {
        [JsonProperty("data")]
        public BuildingData Data { get; set; }
    }

    public class BuildingData
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("allBuildingsSummary")]
        public AllBuildingsSummary AllBuildingsSummary { get; set; }

        [JsonProperty("Buildings")]
        public List<Building> Buildings { get; set; } = new List<Building>();
    }

    public class AllBuildingsSummary
    {
        /// <summary>Best overall sqft for the property (all buildings combined).</summary>
        [JsonProperty("livingAreaSquareFeet")]
        public decimal? LivingAreaSquareFeet { get; set; }

        [JsonProperty("totalAreaSquareFeet")]
        public decimal? TotalAreaSquareFeet { get; set; }

        [JsonProperty("officeSpaceSquareFeet")]
        public decimal? OfficeSpaceSquareFeet { get; set; }

        [JsonProperty("bedroomsCount")]
        public int? BedroomsCount { get; set; }

        [JsonProperty("bathroomsCount")]
        public decimal? BathroomsCount { get; set; }

        [JsonProperty("roomsCount")]
        public decimal? RoomsCount { get; set; }

        [JsonProperty("buildingsCount")]
        public int? BuildingsCount { get; set; }

        [JsonProperty("unitsCount")]
        public int? UnitsCount { get; set; }

        [JsonProperty("elevatorsCount")]
        public int? ElevatorsCount { get; set; }

        [JsonProperty("loadingDocksCount")]
        public int? LoadingDocksCount { get; set; }
    }

    public class Building
    {
        [JsonProperty("constructionDetails")]
        public ConstructionDetails ConstructionDetails { get; set; }

        [JsonProperty("interiorArea")]
        public InteriorArea InteriorArea { get; set; }

        [JsonProperty("interiorRooms")]
        public InteriorRooms InteriorRooms { get; set; }

        [JsonProperty("structureVerticalProfile")]
        public StructureVerticalProfile StructureVerticalProfile { get; set; }

        [JsonProperty("structureClassification")]
        public StructureClassification StructureClassification { get; set; }

        [JsonProperty("structureExterior")]
        public StructureExterior StructureExterior { get; set; }
    }

    public class ConstructionDetails
    {
        [JsonProperty("yearBuilt")]
        public int? YearBuilt { get; set; }

        [JsonProperty("effectiveYearBuilt")]
        public int? EffectiveYearBuilt { get; set; }

        [JsonProperty("constructionTypeCode")]
        public string ConstructionTypeCode { get; set; }

        [JsonProperty("buildingQualityTypeCode")]
        public string BuildingQualityTypeCode { get; set; }

        [JsonProperty("buildingStyleTypeCode")]
        public string BuildingStyleTypeCode { get; set; }

        /// <summary>Condition code — motivated seller signal if poor.</summary>
        [JsonProperty("buildingImprovementConditionCode")]
        public string BuildingImprovementConditionCode { get; set; }

        [JsonProperty("frameTypeCode")]
        public string FrameTypeCode { get; set; }

        [JsonProperty("foundationTypeCode")]
        public string FoundationTypeCode { get; set; }
    }

    public class InteriorArea
    {
        /// <summary>Best single sqft value — most accurate per Cotality docs.</summary>
        [JsonProperty("universalBuildingAreaSquareFeet")]
        public int? UniversalBuildingAreaSquareFeet { get; set; }

        [JsonProperty("buildingAreaSquareFeet")]
        public int? BuildingAreaSquareFeet { get; set; }

        [JsonProperty("livingAreaSquareFeet")]
        public decimal? LivingAreaSquareFeet { get; set; }

        [JsonProperty("buildingGrossAreaSquareFeet")]
        public decimal? BuildingGrossAreaSquareFeet { get; set; }
    }

    public class InteriorRooms
    {
        [JsonProperty("totalCount")]
        public int? TotalCount { get; set; }

        [JsonProperty("bedroomsCount")]
        public int? BedroomsCount { get; set; }

        [JsonProperty("bathroomsCount")]
        public decimal? BathroomsCount { get; set; }

        [JsonProperty("fullBathroomsCount")]
        public int? FullBathroomsCount { get; set; }

        [JsonProperty("halfBathroomsCount")]
        public int? HalfBathroomsCount { get; set; }
    }

    public class StructureVerticalProfile
    {
        [JsonProperty("storiesCount")]
        public decimal? StoriesCount { get; set; }

        [JsonProperty("storiesTypeCode")]
        public string StoriesTypeCode { get; set; }
    }

    public class StructureClassification
    {
        [JsonProperty("buildingTypeCode")]
        public string BuildingTypeCode { get; set; }

        [JsonProperty("buildingClassCode")]
        public string BuildingClassCode { get; set; }

        [JsonProperty("gradeTypeCode")]
        public string GradeTypeCode { get; set; }
    }

    public class StructureExterior
    {
        [JsonProperty("roof")]
        public RoofInfo Roof { get; set; }
    }

    public class RoofInfo
    {
        [JsonProperty("roofMaterialTypeCode")]
        public string RoofMaterialTypeCode { get; set; }

        [JsonProperty("roofConstructionTypeCode")]
        public string RoofConstructionTypeCode { get; set; }
    }

    // Flat BuildingData for PropertySnapshotService compatibility
    public class BuildingDataFlat
    {
        public int?     YearBuilt         { get; set; }
        public int?     EffectiveYearBuilt { get; set; }
        public decimal? GrossLivingArea   { get; set; }
        public int?     TotalRooms        { get; set; }
        public int?     Bedrooms          { get; set; }
        public decimal? Bathrooms         { get; set; }
        public decimal? Stories           { get; set; }
        public string   ConstructionType  { get; set; }
        public string   RoofType          { get; set; }
        public string   BuildingCondition { get; set; }
    }


    // ===================================================================
    // 9. BUILDING PERMITS  —  GET /v2/properties/{clip}/building-permits
    // ===================================================================

    public class BuildingPermitsResult
    {
        [JsonProperty("data")]
        public List<BuildingPermit> Data { get; set; } = new List<BuildingPermit>();
    }

    public class BuildingPermit
    {
        [JsonProperty("permitNumber")]
        public string PermitNumber { get; set; }

        [JsonProperty("permitType")]
        public string PermitType { get; set; }

        [JsonProperty("issueDate")]
        public string IssueDate { get; set; }

        [JsonProperty("completionDate")]
        public string CompletionDate { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("jobValue")]
        public decimal? JobValue { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }


    // ===================================================================
    // 10. AVM  —  GET /v2/properties/{clip}/avm/thv/RVM/summary
    //
    // Actual response shape:
    // { summary: { estimatedValue, lowValue, highValue,
    //   forecastStandardDeviation, confidenceScore, processedDate },
    //   subjectPropertyInformation: { property: { yearBuilt,
    //     livingArea, ... }, location: { latitude, longitude } },
    //   comparables: [...] }
    // ===================================================================

    public class AvmResult
    {
        [JsonProperty("summary")]
        public AvmSummary Summary { get; set; }

        [JsonProperty("subjectPropertyInformation")]
        public AvmSubjectPropertyInfo SubjectPropertyInformation { get; set; }

        // Keep Data property for PropertySnapshotService compatibility
        // — populated via mapping in PropertySnapshotService.PullAndPersistProfile
        public AvmData Data { get; set; }
    }

    public class AvmSummary
    {
        [JsonProperty("estimatedValue")]
        public decimal? EstimatedValue { get; set; }

        [JsonProperty("lowValue")]
        public decimal? LowValue { get; set; }

        [JsonProperty("highValue")]
        public decimal? HighValue { get; set; }

        [JsonProperty("forecastStandardDeviation")]
        public decimal? ForecastStandardDeviation { get; set; }

        [JsonProperty("confidenceScore")]
        public decimal? ConfidenceScore { get; set; }

        [JsonProperty("processedDate")]
        public string ProcessedDate { get; set; }
    }

    public class AvmSubjectPropertyInfo
    {
        [JsonProperty("property")]
        public AvmPropertyInfo Property { get; set; }

        [JsonProperty("location")]
        public AvmLocationInfo Location { get; set; }

        [JsonProperty("tax")]
        public AvmTaxInfo Tax { get; set; }
    }

    public class AvmPropertyInfo
    {
        [JsonProperty("yearBuilt")]
        public string YearBuilt { get; set; }

        [JsonProperty("livingArea")]
        public string LivingArea { get; set; }

        [JsonProperty("lotArea")]
        public string LotArea { get; set; }

        [JsonProperty("totalBaths")]
        public string TotalBaths { get; set; }

        [JsonProperty("bedrooms")]
        public string Bedrooms { get; set; }

        [JsonProperty("numberOfStories")]
        public string NumberOfStories { get; set; }
    }

    public class AvmLocationInfo
    {
        [JsonProperty("latitude")]
        public decimal? Latitude { get; set; }

        [JsonProperty("longitude")]
        public decimal? Longitude { get; set; }

        [JsonProperty("landUse")]
        public string LandUse { get; set; }

        [JsonProperty("absenteeOwner")]
        public string AbsenteeOwner { get; set; }
    }

    public class AvmTaxInfo
    {
        [JsonProperty("assessedValue")]
        public string AssessedValue { get; set; }

        [JsonProperty("improvementValue")]
        public string ImprovementValue { get; set; }

        [JsonProperty("landValue")]
        public string LandValue { get; set; }
    }

    // Flat AvmData for PropertySnapshotService compatibility
    public class AvmData
    {
        public decimal? EstimatedValue        { get; set; }
        public decimal? EstimatedValueHigh    { get; set; }
        public decimal? EstimatedValueLow     { get; set; }
        public decimal? ForecastStandardDeviation { get; set; }
        public decimal? ConfidenceScore       { get; set; }
        public string   ValuationDate         { get; set; }
        public decimal? RentEstimatedValue    { get; set; }
        public decimal? RentEstimatedValueHigh { get; set; }
        public decimal? RentEstimatedValueLow  { get; set; }
        public decimal? CapRate               { get; set; }
    }


    // ===================================================================
    // 11. PROPENSITY (SALE SCORE)  —  GET /v2/properties/propensity-scores/{clip}/sale-score
    // ===================================================================

    public class PropensityResult
    {
        [JsonProperty("data")]
        public PropensityData Data { get; set; }
    }

    public class PropensityData
    {
        [JsonProperty("propensityScore")]
        public decimal? PropensityScore { get; set; }

        [JsonProperty("propensityTier")]
        public string PropensityTier { get; set; }

        [JsonProperty("propensityScoreDate")]
        public string PropensityScoreDate { get; set; }
    }


    // ===================================================================
    // 12. HOA  —  GET /v2/properties/{clip}/home-owners-association
    // ===================================================================

    public class HoaResult
    {
        [JsonProperty("data")]
        public HoaData Data { get; set; }
    }

    public class HoaData
    {
        [JsonProperty("hoaName")]
        public string HoaName { get; set; }

        [JsonProperty("hoaFeeAmount")]
        public decimal? HoaFeeAmount { get; set; }

        [JsonProperty("hoaFeeFrequency")]
        public string HoaFeeFrequency { get; set; }

        [JsonProperty("hoaPhone")]
        public string HoaPhone { get; set; }
    }


    // ===================================================================
    // 13. CLIMATE RISK  —  GET /v2/properties/{clip}/climate-risk-analytics/ar6/comprehensive
    // ===================================================================

    public class ClimateRiskResult
    {
        [JsonProperty("data")]
        public ClimateRiskData Data { get; set; }
    }

    public class ClimateRiskData
    {
        [JsonProperty("floodRiskScore")]
        public decimal? FloodRiskScore { get; set; }

        [JsonProperty("floodRiskLabel")]
        public string FloodRiskLabel { get; set; }

        [JsonProperty("fireRiskScore")]
        public decimal? FireRiskScore { get; set; }

        [JsonProperty("fireRiskLabel")]
        public string FireRiskLabel { get; set; }

        [JsonProperty("windRiskScore")]
        public decimal? WindRiskScore { get; set; }

        [JsonProperty("windRiskLabel")]
        public string WindRiskLabel { get; set; }

        [JsonProperty("heatRiskScore")]
        public decimal? HeatRiskScore { get; set; }

        [JsonProperty("heatRiskLabel")]
        public string HeatRiskLabel { get; set; }
    }


    // ===================================================================
    // 14. PROPERTY COMPARABLES  —  GET /v2/properties/{clipId}/comparables
    // ===================================================================

    public class PropertyComparablesResult
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("comparables")]
        public List<ComparableProperty> Comparables { get; set; } = new List<ComparableProperty>();
    }

    public class ComparableProperty
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("streetAddress")]
        public string StreetAddress { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("zip")]
        public string ZipCode { get; set; }

        [JsonProperty("latitude")]
        public decimal? Latitude { get; set; }

        [JsonProperty("longitude")]
        public decimal? Longitude { get; set; }

        [JsonProperty("distance")]
        public decimal? Distance { get; set; }

        [JsonProperty("bedrooms")]
        public int? Bedrooms { get; set; }

        [JsonProperty("baths")]
        public int? Baths { get; set; }

        [JsonProperty("buildingSquareFeet")]
        public int? BuildingSquareFeet { get; set; }

        [JsonProperty("lotSquareFeet")]
        public int? LotSquareFeet { get; set; }

        [JsonProperty("yearBuilt")]
        public string YearBuilt { get; set; }

        [JsonProperty("saleDate")]
        public string SaleDate { get; set; }

        [JsonProperty("salePrice")]
        public decimal? SalePrice { get; set; }

        [JsonProperty("pricePerSquareFoot")]
        public decimal? PricePerSquareFoot { get; set; }

        [JsonProperty("recordingDate")]
        public string RecordingDate { get; set; }
    }


    // ===================================================================
    // 15. SITE LOCATION  —  GET /v2/properties/{clip}/site-location
    //     (extended — also used internally by geocode)
    // ===================================================================

    public class SiteLocationResponse
    {
        [JsonProperty("data")]
        public SiteLocationExtended Data { get; set; }
    }

    public class SiteLocationExtended
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("coordinatesParcel")]
        public SiteLocationCoordinates CoordinatesParcel { get; set; }

        [JsonProperty("landUseAndZoningCodes")]
        public SiteLocationLandUse LandUseAndZoningCodes { get; set; }

        [JsonProperty("lot")]
        public SiteLocationLot Lot { get; set; }

        [JsonProperty("jurisdictionCounty")]
        public SiteLocationCounty JurisdictionCounty { get; set; }
    }

    public class SiteLocationCoordinates
    {
        [JsonProperty("lat")]
        public decimal? Lat { get; set; }

        [JsonProperty("lng")]
        public decimal? Lng { get; set; }
    }

    public class SiteLocationLandUse
    {
        [JsonProperty("zoningCode")]
        public string ZoningCode { get; set; }

        [JsonProperty("zoningCodeDescription")]
        public string ZoningCodeDescription { get; set; }

        [JsonProperty("landUseCode")]
        public string LandUseCode { get; set; }

        [JsonProperty("landUseCodeDescription")]
        public string LandUseCodeDescription { get; set; }

        [JsonProperty("propertyTypeCode")]
        public string PropertyTypeCode { get; set; }

        [JsonProperty("propertyTypeCodeDescription")]
        public string PropertyTypeCodeDescription { get; set; }

        [JsonProperty("countyLandUseDescription")]
        public string CountyLandUseDescription { get; set; }
    }

    public class SiteLocationLot
    {
        [JsonProperty("areaSquareFeet")]
        public decimal? AreaSquareFeet { get; set; }

        [JsonProperty("areaAcres")]
        public decimal? AreaAcres { get; set; }

        [JsonProperty("frontFeet")]
        public decimal? FrontFeet { get; set; }

        [JsonProperty("depthFeet")]
        public decimal? DepthFeet { get; set; }
    }

    public class SiteLocationCounty
    {
        /// <summary>FIPS county code — the API returns this field as "code".</summary>
        [JsonProperty("code")]
        public string FipsCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }


    // ===================================================================
    // 16. TRANSACTION HISTORY  —  GET /v2/properties/{clip}/transaction-history
    //
    // Actual response shape:
    // { clip, items: [{ ownershipTransfers: [{ transactionDetails:
    //   { saleDateDerived, saleAmount, ... }, buyerDetails: { ownerNames },
    //   sellerDetails: { ownerNames } }],
    //   mortgageHistory: [{ mortgageTransactionDetail: { amount, date,
    //   statusIndicator }, lenderDetail: { lenderCompanyName } }] }] }
    // ===================================================================

    public class TransactionHistoryResponse
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("items")]
        public List<TransactionHistoryItem> Items { get; set; } = new List<TransactionHistoryItem>();
    }

    public class TransactionHistoryItem
    {
        [JsonProperty("ownershipTransfers")]
        public List<TransactionHistorySaleItem> OwnershipTransfers { get; set; }
            = new List<TransactionHistorySaleItem>();

        [JsonProperty("mortgageHistory")]
        public List<TransactionHistoryMortgageItem> MortgageHistory { get; set; }
            = new List<TransactionHistoryMortgageItem>();
    }

    public class TransactionHistorySaleItem
    {
        [JsonProperty("transactionDetails")]
        public TransactionHistorySaleDetails TransactionDetails { get; set; }

        [JsonProperty("buyerDetails")]
        public TransactionHistoryPartyDetails BuyerDetails { get; set; }

        [JsonProperty("sellerDetails")]
        public TransactionHistoryPartyDetails SellerDetails { get; set; }

        [JsonProperty("titleCompany")]
        public TransactionHistoryTitleCompany TitleCompany { get; set; }
    }

    public class TransactionHistorySaleDetails
    {
        [JsonProperty("saleDateDerived")]
        public string SaleDateDerived { get; set; }

        [JsonProperty("saleRecordingDateDerived")]
        public string SaleRecordingDateDerived { get; set; }

        [JsonProperty("saleAmount")]
        public decimal? SaleAmount { get; set; }

        [JsonProperty("deedCategoryCode")]
        public string DeedCategoryCode { get; set; }

        [JsonProperty("deedCategoryCodeDescription")]
        public string DeedCategoryCodeDescription { get; set; }

        [JsonProperty("saleDocumentTypeCode")]
        public string SaleDocumentTypeCode { get; set; }

        [JsonProperty("saleDocumentNumber")]
        public string SaleDocumentNumber { get; set; }

        [JsonProperty("saleBookNumber")]
        public string SaleBookNumber { get; set; }

        [JsonProperty("salePageNumber")]
        public string SalePageNumber { get; set; }

        [JsonProperty("isCashPurchase")]
        public int? IsCashPurchase { get; set; }

        [JsonProperty("isForeclosureReo")]
        public int? IsForeclosureReo { get; set; }

        [JsonProperty("isShortSale")]
        public int? IsShortSale { get; set; }
    }

    public class TransactionHistoryPartyDetails
    {
        /// <summary>Used by buyerDetails — JSON field is "buyerNames".</summary>
        [JsonProperty("buyerNames")]
        public List<OwnerName> BuyerNames { get; set; } = new List<OwnerName>();

        /// <summary>Used by sellerDetails — JSON field is "sellerNames".</summary>
        [JsonProperty("sellerNames")]
        public List<OwnerName> SellerNames { get; set; } = new List<OwnerName>();

        /// <summary>Convenience: returns whichever name list is populated.</summary>
        [JsonIgnore]
        public List<OwnerName> OwnerNames =>
            BuyerNames?.Count > 0 ? BuyerNames :
            SellerNames?.Count > 0 ? SellerNames :
            new List<OwnerName>();
    }

    public class TransactionHistoryTitleCompany
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class TransactionHistoryMortgageItem
    {
        [JsonProperty("mortgageTransactionDetail")]
        public MortgageTransactionDetail TransactionDetail { get; set; }

        [JsonProperty("lenderDetail")]
        public MortgageLenderDetail LenderDetail { get; set; }

        [JsonProperty("borrowerDetail")]
        public MortgageBorrowerDetail BorrowerDetail { get; set; }
    }


    // ===================================================================
    // 17. DOCUMENT IMAGES  —  GET /v2/properties/document-images/{product}
    // ===================================================================

    public class DocumentImageResponse
    {
        [JsonProperty("totalPages")]
        public int? TotalPages { get; set; }

        [JsonProperty("outputType")]
        public string OutputType { get; set; }

        [JsonProperty("statusCode")]
        public int? StatusCode { get; set; }

        [JsonProperty("statusMsg")]
        public string StatusMsg { get; set; }

        [JsonProperty("images")]
        public List<string> Images { get; set; } = new List<string>();

        [JsonProperty("recorderImageCoverageSummary")]
        public DocumentCoverageSummary CoverageSummary { get; set; }
    }

    public class DocumentCoverageSummary
    {
        [JsonProperty("standardizedState")]
        public string State { get; set; }

        [JsonProperty("standardizedCounty")]
        public string County { get; set; }

        [JsonProperty("standardizedFIPsCode")]
        public string FipsCode { get; set; }

        [JsonProperty("startRecorderImageYYYY")]
        public string StartYear { get; set; }

        [JsonProperty("lastRecorderImageYYYY")]
        public string LastYear { get; set; }
    }

    // ---------------------------------------------------------------
    // /v2/properties/{clip}/property-detail
    // Single composite endpoint — returns buildings, ownership,
    // siteLocation, taxAssessment, mostRecentOwnerTransfer, lastMarketSale
    // all in one call. Use as primary/fallback when individual endpoints
    // return no data (common for less-covered counties).
    // ---------------------------------------------------------------

    public class PropertyDetailResponse
    {
        [JsonProperty("buildings")]
        public PropertyDetailBuildings Buildings { get; set; }

        [JsonProperty("ownership")]
        public PropertyDetailOwnership Ownership { get; set; }

        [JsonProperty("siteLocation")]
        public PropertyDetailSiteLocation SiteLocation { get; set; }

        [JsonProperty("taxAssessment")]
        public PropertyDetailTaxAssessment TaxAssessment { get; set; }

        [JsonProperty("mostRecentOwnerTransfer")]
        public PropertyDetailTransfers MostRecentOwnerTransfer { get; set; }

        [JsonProperty("lastMarketSale")]
        public PropertyDetailTransfers LastMarketSale { get; set; }
    }

    // --- Buildings section ---

    public class PropertyDetailBuildings
    {
        [JsonProperty("data")]
        public PropertyDetailBuildingData Data { get; set; }
    }

    public class PropertyDetailBuildingData
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("allBuildingsSummary")]
        public PropertyDetailBuildingSummary AllBuildingsSummary { get; set; }

        [JsonProperty("Buildings")]
        public List<PropertyDetailBuilding> Buildings { get; set; }
    }

    public class PropertyDetailBuildingSummary
    {
        [JsonProperty("livingAreaSquareFeet")]
        public decimal? LivingAreaSquareFeet { get; set; }

        [JsonProperty("totalAreaSquareFeet")]
        public decimal? TotalAreaSquareFeet { get; set; }

        [JsonProperty("bedroomsCount")]
        public int? BedroomsCount { get; set; }

        [JsonProperty("bathroomsCount")]
        public decimal? BathroomsCount { get; set; }

        [JsonProperty("roomsCount")]
        public decimal? RoomsCount { get; set; }

        [JsonProperty("storiesCount")]
        public decimal? StoriesCount { get; set; }
    }

    public class PropertyDetailBuilding
    {
        [JsonProperty("constructionDetails")]
        public PropertyDetailConstructionDetails ConstructionDetails { get; set; }

        [JsonProperty("interiorArea")]
        public PropertyDetailInteriorArea InteriorArea { get; set; }

        [JsonProperty("interiorRooms")]
        public PropertyDetailInteriorRooms InteriorRooms { get; set; }

        [JsonProperty("structureVerticalProfile")]
        public PropertyDetailVerticalProfile StructureVerticalProfile { get; set; }

        [JsonProperty("structureClassification")]
        public PropertyDetailStructureClassification StructureClassification { get; set; }

        [JsonProperty("structureExterior")]
        public PropertyDetailStructureExterior StructureExterior { get; set; }
    }

    public class PropertyDetailConstructionDetails
    {
        [JsonProperty("yearBuilt")]
        public int? YearBuilt { get; set; }

        [JsonProperty("effectiveYearBuilt")]
        public int? EffectiveYearBuilt { get; set; }

        [JsonProperty("constructionTypeCode")]
        public string ConstructionTypeCode { get; set; }

        [JsonProperty("buildingImprovementConditionCode")]
        public string BuildingImprovementConditionCode { get; set; }
    }

    public class PropertyDetailInteriorArea
    {
        [JsonProperty("universalBuildingAreaSquareFeet")]
        public int? UniversalBuildingAreaSquareFeet { get; set; }

        [JsonProperty("livingAreaSquareFeet")]
        public decimal? LivingAreaSquareFeet { get; set; }

        [JsonProperty("buildingAreaSquareFeet")]
        public int? BuildingAreaSquareFeet { get; set; }
    }

    public class PropertyDetailInteriorRooms
    {
        [JsonProperty("totalCount")]
        public int? TotalCount { get; set; }

        [JsonProperty("bedroomsCount")]
        public int? BedroomsCount { get; set; }

        [JsonProperty("bathroomsCount")]
        public decimal? BathroomsCount { get; set; }

        [JsonProperty("fullBathroomsCount")]
        public int? FullBathroomsCount { get; set; }

        [JsonProperty("halfBathroomsCount")]
        public int? HalfBathroomsCount { get; set; }
    }

    public class PropertyDetailVerticalProfile
    {
        [JsonProperty("storiesCount")]
        public decimal? StoriesCount { get; set; }
    }

    public class PropertyDetailStructureClassification
    {
        [JsonProperty("buildingTypeCode")]
        public string BuildingTypeCode { get; set; }
    }

    public class PropertyDetailStructureExterior
    {
        [JsonProperty("roof")]
        public PropertyDetailRoof Roof { get; set; }
    }

    public class PropertyDetailRoof
    {
        [JsonProperty("roofMaterialTypeCode")]
        public string RoofMaterialTypeCode { get; set; }
    }

    // --- Ownership section ---

    public class PropertyDetailOwnership
    {
        [JsonProperty("data")]
        public PropertyDetailOwnershipData Data { get; set; }
    }

    public class PropertyDetailOwnershipData
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("currentOwners")]
        public PropertyDetailCurrentOwners CurrentOwners { get; set; }

        [JsonProperty("currentOwnerMailingInfo")]
        public PropertyDetailMailingInfo CurrentOwnerMailingInfo { get; set; }
    }

    public class PropertyDetailCurrentOwners
    {
        [JsonProperty("ownerNames")]
        public List<PropertyDetailOwnerName> OwnerNames { get; set; }

        [JsonProperty("occupancyCode")]
        public string OccupancyCode { get; set; }

        [JsonProperty("ownershipRightsCode")]
        public string OwnershipRightsCode { get; set; }
    }

    public class PropertyDetailOwnerName
    {
        [JsonProperty("fullName")]
        public string Name { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("middleName")]
        public string MiddleName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("firstNameAndMiddleInitial")]
        public string FirstNameAndMiddleInitial { get; set; }

        /// <summary>API returns "Y"/"N" string, not a true boolean.</summary>
        [JsonProperty("isCorporate")]
        public string IsCorporate { get; set; }

        [JsonProperty("sequenceNumber")]
        public int? SequenceNumber { get; set; }
    }

    public class PropertyDetailMailingInfo
    {
        [JsonProperty("mailingAddress")]
        public PropertyDetailMailingAddress MailingAddress { get; set; }
    }

    public class PropertyDetailMailingAddress
    {
        [JsonProperty("streetAddress")]
        public string StreetAddress { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }
    }

    // --- SiteLocation section ---

    public class PropertyDetailSiteLocation
    {
        [JsonProperty("data")]
        public PropertyDetailSiteLocationData Data { get; set; }
    }

    public class PropertyDetailSiteLocationData
    {
        [JsonProperty("lot")]
        public PropertyDetailLot Lot { get; set; }

        [JsonProperty("landUseAndZoningCodes")]
        public PropertyDetailLandUse LandUseAndZoningCodes { get; set; }

        [JsonProperty("jurisdictionCounty")]
        public PropertyDetailCounty JurisdictionCounty { get; set; }

        [JsonProperty("coordinatesParcel")]
        public PropertyDetailCoordinates CoordinatesParcel { get; set; }
    }

    public class PropertyDetailLot
    {
        [JsonProperty("areaSquareFeet")]
        public decimal? AreaSquareFeet { get; set; }

        [JsonProperty("areaAcres")]
        public decimal? AreaAcres { get; set; }
    }

    public class PropertyDetailLandUse
    {
        [JsonProperty("zoningCode")]
        public string ZoningCode { get; set; }

        [JsonProperty("landUseCodeDescription")]
        public string LandUseCodeDescription { get; set; }

        [JsonProperty("propertyTypeCode")]
        public string PropertyTypeCode { get; set; }

        [JsonProperty("propertyTypeCodeDescription")]
        public string PropertyTypeCodeDescription { get; set; }
    }

    public class PropertyDetailCounty
    {
        /// <summary>FIPS county code — the API returns this field as "code".</summary>
        [JsonProperty("code")]
        public string FipsCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class PropertyDetailCoordinates
    {
        [JsonProperty("lat")]
        public double? Lat { get; set; }

        [JsonProperty("lng")]
        public double? Lng { get; set; }
    }

    // --- Tax Assessment section ---

    public class PropertyDetailTaxAssessment
    {
        [JsonProperty("metadata")]
        public PropertyDetailMetadata Metadata { get; set; }

        [JsonProperty("items")]
        public List<PropertyDetailTaxItem> Items { get; set; }
    }

    public class PropertyDetailMetadata
    {
        [JsonProperty("totalRecords")]
        public int TotalRecords { get; set; }
    }

    public class PropertyDetailTaxItem
    {
        [JsonProperty("taxAmount")]
        public PropertyDetailTaxAmount TaxAmount { get; set; }

        [JsonProperty("assessedValue")]
        public PropertyDetailAssessedValue AssessedValue { get; set; }
    }

    public class PropertyDetailTaxAmount
    {
        [JsonProperty("totalTaxAmount")]
        public decimal? TotalTaxAmount { get; set; }

        [JsonProperty("netTaxAmount")]
        public decimal? NetTaxAmount { get; set; }

        [JsonProperty("billedYear")]
        public int? BilledYear { get; set; }

        [JsonProperty("delinquentYear")]
        public int? DelinquentYear { get; set; }
    }

    public class PropertyDetailAssessedValue
    {
        [JsonProperty("calculatedTotalValue")]
        public decimal? CalculatedTotalValue { get; set; }

        [JsonProperty("calculatedLandValue")]
        public decimal? CalculatedLandValue { get; set; }

        [JsonProperty("calculatedImprovementValue")]
        public decimal? CalculatedImprovementValue { get; set; }

        [JsonProperty("taxableValue")]
        public decimal? TaxableValue { get; set; }

        [JsonProperty("taxAssessedYear")]
        public int? TaxAssessedYear { get; set; }
    }

    // --- Ownership Transfers section (shared shape for mostRecentOwnerTransfer & lastMarketSale) ---

    public class PropertyDetailTransfers
    {
        [JsonProperty("metadata")]
        public PropertyDetailMetadata Metadata { get; set; }

        [JsonProperty("items")]
        public List<PropertyDetailTransferItem> Items { get; set; }
    }

    public class PropertyDetailTransferItem
    {
        [JsonProperty("clip")]
        public string Clip { get; set; }

        [JsonProperty("transactionDetails")]
        public PropertyDetailTransactionDetails TransactionDetails { get; set; }

        [JsonProperty("buyerDetails")]
        public PropertyDetailTransferParty BuyerDetails { get; set; }

        [JsonProperty("sellerDetails")]
        public PropertyDetailTransferParty SellerDetails { get; set; }

        [JsonProperty("propertyDetails")]
        public PropertyDetailPropertyInfo PropertyDetails { get; set; }

        [JsonProperty("landUseAndZoningCodes")]
        public PropertyDetailLandUse LandUseAndZoningCodes { get; set; }
    }

    public class PropertyDetailTransactionDetails
    {
        [JsonProperty("saleDateDerived")]
        public string SaleDateDerived { get; set; }

        [JsonProperty("saleAmount")]
        public decimal? SaleAmount { get; set; }

        [JsonProperty("deedCategoryCode")]
        public string DeedCategoryCode { get; set; }

        [JsonProperty("saleDocumentNumber")]
        public string SaleDocumentNumber { get; set; }

        [JsonProperty("primaryCategoryCode")]
        public string PrimaryCategoryCode { get; set; }

        [JsonProperty("saleTypeCode")]
        public string SaleTypeCode { get; set; }
    }

    public class PropertyDetailTransferParty
    {
        [JsonProperty("buyerNames")]
        public List<PropertyDetailOwnerName> BuyerNames { get; set; }

        [JsonProperty("sellerNames")]
        public List<PropertyDetailOwnerName> SellerNames { get; set; }
    }

    public class PropertyDetailPropertyInfo
    {
        [JsonProperty("actualYearBuilt")]
        public int? ActualYearBuilt { get; set; }

        [JsonProperty("effectiveYearBuilt")]
        public int? EffectiveYearBuilt { get; set; }
    }
}
