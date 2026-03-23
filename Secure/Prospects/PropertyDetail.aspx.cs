using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Reyla.Services;
using Reyla.Services.Models;

namespace Reyla.Secure.Prospects
{
    /// <summary>
    /// Full property detail page.  Accepts either:
    ///   ?clip=...                     — loads property by CLIP directly
    ///   ?propertyId=...               — loads via internal Property GUID
    ///   ?prospectId=...&amp;comp=1    — loads a saved comp's property
    ///
    /// All snapshot data comes from PropertySnapshotService (cached 30 days).
    /// Transaction history is fetched live from CoreLogic and stored in
    /// hdnDocResult so the Documents tab can issue individual document fetches
    /// without re-calling the transaction history endpoint.
    /// </summary>
    public partial class PropertyDetail : System.Web.UI.Page
    {
        // ── Injected helpers ──────────────────────────────────────────────
        private readonly PropertySnapshotService _snapshot = new PropertySnapshotService();
        private readonly CoreLogicPropertyService _api      = new CoreLogicPropertyService();

        // ── State ─────────────────────────────────────────────────────────
        private PropertyProfileResult _profile;
        private List<DocRow>          _docRows;
        private ProspectDetail        _prospect;  // null = not a prospect

        // ── Internal DTO for prospect card ───────────────────────────────
        private class ProspectDetail
        {
            public Guid     ProspectId    { get; set; }
            public string   Status        { get; set; }
            public bool     IsActive      { get; set; }
            public DateTime CreatedAtUtc  { get; set; }
            public DateTime? UpdatedAtUtc { get; set; }
            public string   AgentName     { get; set; }
            public string   AgentEmail    { get; set; }
            public string   OrgName       { get; set; }
            public int      CompCount     { get; set; }
            // Deal pipeline fields
            public Guid?     DealId        { get; set; }
            public string    DealStage     { get; set; }
            public string    DealTitle     { get; set; }
            public decimal?  AskingPrice   { get; set; }
            public decimal?  OfferPrice    { get; set; }
            public DateTime? CloseDate     { get; set; }
        }

        // ── Page lifecycle ────────────────────────────────────────────────

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadProfile();
                if (_profile != null && !_profile.PropertyNotFound)
                {
                    LoadProspect();
                    BuildDocRows();
                }
                BindAll();
                BindProspectCard();
                BindOwnerContact();
                BindNotes();
                BindCompMgmt();
                BindDiagnostics();
            }
            else
            {
                // Re-hydrate profile from session so repeater helpers work
                _profile = Session["PropertyDetail_Profile"] as PropertyProfileResult;
                _docRows = DeserialiseDocRows(hdnDocResult.Value);
                rptDocs.DataSource = _docRows;
                rptDocs.DataBind();

                // Always reload prospect on postback so note/contact handlers have _prospect
                if (_profile != null && !_profile.PropertyNotFound)
                    LoadProspect();

                string eventTarget = Request.Form["__EVENTTARGET"] ?? string.Empty;

                if (eventTarget == "ToggleComp")
                {
                    HandleToggleComp();
                }
                else if (eventTarget == "RecalcValuation")
                {
                    BindProspectCard();
                    BindCompMgmt();
                }
                // Owner contact save, note add/edit/delete, and contacted buttons
                // are all handled by their own LinkButton event handlers directly.
            }
        }

        // ── Load & bind ───────────────────────────────────────────────────

        private void LoadProfile()
        {
            var clip       = Request.QueryString["clip"];
            var propertyId = Request.QueryString["propertyId"];
            var prospectId = Request.QueryString["prospectId"];

            var orgId  = GetCurrentOrganizationId();
            var userId = GetCurrentUserId();

            if (orgId == Guid.Empty || userId == Guid.Empty)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            // Resolve clip — prefer querystring clip, fall back to propertyId lookup
            if (string.IsNullOrWhiteSpace(clip) && !string.IsNullOrWhiteSpace(propertyId)
                && Guid.TryParse(propertyId, out var pid))
            {
                using (var db = new DCReyla())
                {
                    var prop = db.Properties.FirstOrDefault(p => p.PropertyId == pid);
                    clip = prop?.Clip;
                }
            }

            if (string.IsNullOrWhiteSpace(clip))
            {
                Response.Redirect("~/Secure/Prospects/Search.aspx");
                return;
            }

            // Load profile directly by clip — no re-search, no PropertySearch record
            _profile = _snapshot.GetProfileByClip(clip, orgId);

            if (_profile == null || _profile.PropertyNotFound)
            {
                Response.Redirect("~/Secure/Prospects/Search.aspx");
                return;
            }

            // Store in session for postback reuse
            Session["PropertyDetail_Profile"] = _profile;

            // Back-to-comps link — show if prospectId is on the querystring
            if (!string.IsNullOrWhiteSpace(prospectId))
                lnkBackToComps.NavigateUrl = $"~/Secure/Prospects/Comps.aspx?prospectId={prospectId}";
            else
                lnkBackToComps.Visible = false;
        }

        private void BuildDocRows()
        {
            _docRows = new List<DocRow>();

            var clip     = _profile?.Property?.Clip;
            var fipsCode = _profile?.Property?.FipsCode;

            if (string.IsNullOrWhiteSpace(clip)) return;

            try
            {
                var history = _api.GetTransactionHistory(clip);

                // TransactionHistoryResponse.Items is List<TransactionHistoryItem>
                // Each TransactionHistoryItem has .OwnershipTransfers[] + .MortgageHistory[]
                if (history?.Items != null)
                {
                    foreach (var item in history.Items)
                    {
                        // Sale/deed transactions
                        if (item.OwnershipTransfers != null)
                        {
                            foreach (var sale in item.OwnershipTransfers)
                            {
                                var td = sale.TransactionDetails;
                                if (td == null) continue;

                                var docNum  = td.SaleDocumentNumber;
                                var recDate = td.SaleRecordingDateDerived ?? td.SaleDateDerived;

                                if (string.IsNullOrWhiteSpace(docNum)
                                    || string.IsNullOrWhiteSpace(recDate)) continue;

                                string buyerName  = sale.BuyerDetails?.OwnerNames?.Count  > 0 ? sale.BuyerDetails.OwnerNames[0].Name  : null;
                                string sellerName = sale.SellerDetails?.OwnerNames?.Count > 0 ? sale.SellerDetails.OwnerNames[0].Name : null;

                                _docRows.Add(new DocRow
                                {
                                    RowKey                  = MakeRowKey(recDate, docNum),
                                    DocumentNumber          = docNum,
                                    RecordingDate           = recDate,
                                    DocumentType            = td.DeedCategoryCode,
                                    DocumentTypeDescription = td.DeedCategoryCodeDescription ?? td.DeedCategoryCode,
                                    TransactionType         = td.SaleDocumentTypeCode,
                                    BuyerName               = buyerName,
                                    SellerName              = sellerName,
                                    FipsCode                = fipsCode
                                });
                            }
                        }

                        // Mortgage transactions
                        if (item.MortgageHistory != null)
                        {
                            foreach (var mtg in item.MortgageHistory)
                            {
                                var td = mtg.TransactionDetail;
                                if (td == null) continue;

                                // Mortgage dates are stored as YYYYMMDD integers
                                var recDateStr = td.Date.HasValue ? td.Date.Value.ToString() : null;
                                var lenderName = mtg.LenderDetail?.LenderCompanyName ?? mtg.LenderDetail?.LenderFullName;

                                if (string.IsNullOrWhiteSpace(recDateStr)) continue;

                                _docRows.Add(new DocRow
                                {
                                    RowKey          = MakeRowKey(recDateStr, lenderName ?? recDateStr),
                                    DocumentNumber  = recDateStr,
                                    RecordingDate   = recDateStr,
                                    DocumentType    = "Mortgage",
                                    DocumentTypeDescription = td.LoanTypeCodeDescription ?? td.LoanTypeCode ?? "Mortgage",
                                    LenderName      = lenderName,
                                    FipsCode        = fipsCode
                                });
                            }
                        }
                    }
                }
            }
            catch (CoreLogicApiException)
            {
                // Transaction history unavailable — documents tab shows empty state
            }

            // Also pull document numbers from ownership transfers (already in snapshot)
            // OwnershipTransfers has .Items[], each with .TransactionDetails sub-object
            var transfers = _profile?.OwnershipTransfers?.Items;
            if (transfers != null)
            {
                foreach (var t in transfers)
                {
                    var td = t?.TransactionDetails;
                    if (td == null) continue;

                    var docNum  = td.SaleDocumentNumber;
                    var recDate = td.SaleDateDerived;

                    if (string.IsNullOrWhiteSpace(docNum) || string.IsNullOrWhiteSpace(recDate)) continue;

                    var key = MakeRowKey(recDate, docNum);
                    if (_docRows.Any(r => r.RowKey == key)) continue;

                    string buyerName  = t.BuyerDetails?.OwnerNames?.Count  > 0 ? t.BuyerDetails.OwnerNames[0].Name  : null;
                    string sellerName = t.SellerDetails?.OwnerNames?.Count > 0 ? t.SellerDetails.OwnerNames[0].Name : null;

                    _docRows.Add(new DocRow
                    {
                        RowKey                  = key,
                        DocumentNumber          = docNum,
                        RecordingDate           = recDate,
                        DocumentType            = td.DeedCategoryCode,
                        DocumentTypeDescription = td.DeedCategoryCode,
                        BuyerName               = buyerName,
                        SellerName              = sellerName,
                        FipsCode                = fipsCode
                    });
                }
            }

            // Sort newest first
            _docRows = _docRows
                .OrderByDescending(r => r.RecordingDate)
                .ToList();

            hdnDocResult.Value = JsonConvert.SerializeObject(_docRows);
        }

        private void BindAll()
        {
            if (_profile == null || _profile.PropertyNotFound) return;

            // Diagnostic — visible in VS Output / Trace when debugging
            System.Diagnostics.Trace.TraceInformation(
                "[PropertyDetail.BindAll] Clip={0} IsFromCache={1} " +
                "Ownership={2} Building={3} Tax={4} Avm={5} Propensity={6}",
                _profile.Property?.Clip,
                _profile.IsFromCache,
                _profile.Ownership?.Data?.CurrentOwners != null,
                _profile.Building?.Data?.AllBuildingsSummary != null || _profile.Building?.Data?.Buildings?.Count > 0,
                _profile.TaxAssessment?.Items?.Count > 0,
                _profile.Avm?.Data != null,
                _profile.Propensity?.Data != null);

            var prop = _profile.Property;

            // ── Hidden fields ─────────────────────────────────────────────
            hdnClip.Value     = prop?.Clip ?? string.Empty;
            hdnFipsCode.Value = prop?.FipsCode ?? string.Empty;
            hdnLat.Value      = prop != null ? prop.Latitude.ToString()  : string.Empty;
            hdnLng.Value      = prop != null ? prop.Longitude.ToString() : string.Empty;
            hdnStreetViewKey.Value  = ConfigurationManager.AppSettings["Google.StreetViewApiKey"] ?? string.Empty;
            hdnPropertyId.Value     = prop?.PropertyId.ToString() ?? string.Empty;
            hdnPeopleHandlerUrl.Value = ResolveUrl("~/Secure/Prospects/PropertyContactHandler.ashx");

            // ── Header ────────────────────────────────────────────────────
            var address  = prop?.StreetAddress ?? "(Address unavailable)";

            // Resolve display city/state/zip from City_Extended
            string propCity  = prop?.CityNameRaw ?? string.Empty;
            string propState = string.Empty;
            string propZip   = prop?.ZipCodeRaw  ?? string.Empty;
            if (prop?.CityExtendedId.HasValue == true)
            {
                using (var dbLoc = new DCReyla())
                {
                    var ce = dbLoc.City_Extendeds.FirstOrDefault(
                                 c => c.CityExtendedId == prop.CityExtendedId.Value);
                    if (ce != null)
                    {
                        propCity  = ce.City;
                        propState = ce.Code;
                        propZip   = ce.Zip;
                    }
                }
            }
            var cityLine = string.Join(", ", new[] { propCity, propState, propZip }
                               .Where(s => !string.IsNullOrWhiteSpace(s)));

            litAddress.Text      = HttpUtility.HtmlEncode(address);
            litCityStateZip.Text = HttpUtility.HtmlEncode(cityLine);

            // Street view photo
            if (prop != null && (prop.Latitude != 0 || prop.Longitude != 0))
            {
                var svKey = hdnStreetViewKey.Value;
                if (!string.IsNullOrWhiteSpace(svKey))
                {
                    imgStreetView.ImageUrl =
                        $"https://maps.googleapis.com/maps/api/streetview" +
                        $"?size=440x280&location={prop.Latitude},{prop.Longitude}" +
                        $"&fov=90&pitch=0&source=outdoor&key={svKey}";
                    imgStreetView.Style["display"] = "";
                    divPhotoPlaceholder.Visible    = false;
                }

                lnkGoogleMaps.NavigateUrl =
                    $"https://www.google.com/maps/@?api=1&map_action=pano" +
                    $"&viewpoint={prop.Latitude},{prop.Longitude}";
            }
            else
            {
                lnkGoogleMaps.Visible = false;
            }

            // ── Motivated-seller signal badges ────────────────────────────
            BindSignalBadges();

            // ── Overview tab ──────────────────────────────────────────────
            litApn.Text         = F(prop?.Apn);
            litClip.Text        = F(prop?.Clip);
            litPropertyUse.Text = F(prop?.PropertyUseCode);
            litZoning.Text      = F(prop?.ZoningCode);
            litLotSize.Text     = prop?.LotSquareFeet.HasValue == true
                                    ? $"{prop.LotSquareFeet.Value:N0} sqft" : Na();
            litCounty.Text      = F(prop?.County);
            litFips.Text        = F(prop?.FipsCode);

            // Building: data.allBuildingsSummary for totals, Buildings[0] for per-building detail
            var bldgData    = _profile.Building?.Data;
            var bldgSummary = bldgData?.AllBuildingsSummary;
            var bldg0       = bldgData?.Buildings?.Count > 0 ? bldgData.Buildings[0] : null;
            var bldgCd      = bldg0?.ConstructionDetails;
            var bldgIa      = bldg0?.InteriorArea;
            var bldgIr      = bldg0?.InteriorRooms;
            var bldgVp      = bldg0?.StructureVerticalProfile;

            // Best sqft: universalBuildingAreaSquareFeet > livingAreaSquareFeet > summary
            decimal? sqftVal = bldgIa?.UniversalBuildingAreaSquareFeet.HasValue == true
                ? (decimal?)bldgIa.UniversalBuildingAreaSquareFeet.Value
                : (bldgIa?.LivingAreaSquareFeet ?? bldgSummary?.LivingAreaSquareFeet);

            litBldgSqFt.Text     = sqftVal.HasValue ? $"{sqftVal.Value:N0} sqft" : Na();
            litYearBuilt.Text    = F(bldgCd?.YearBuilt?.ToString());
            litEffYearBuilt.Text = F(bldgCd?.EffectiveYearBuilt?.ToString());
            litStories.Text      = F(bldgVp?.StoriesCount?.ToString("G"));
            litConstruction.Text = F(bldgCd?.ConstructionTypeCode);
            litRoofType.Text     = F(bldg0?.StructureExterior?.Roof?.RoofMaterialTypeCode);
            litCondition.Text    = F(bldgCd?.BuildingImprovementConditionCode);
            litBedrooms.Text     = F((bldgIr?.BedroomsCount ?? bldgSummary?.BedroomsCount)?.ToString());
            litBathrooms.Text    = F((bldgIr?.BathroomsCount ?? bldgSummary?.BathroomsCount)?.ToString("G"));
            litTotalRooms.Text   = F(bldgIr?.TotalCount?.ToString());

            var avm = _profile.Avm?.Data;
            litAvmValue.Text      = Money(avm?.EstimatedValue);
            litAvmRange.Text      = (avm?.EstimatedValueLow.HasValue == true && avm?.EstimatedValueHigh.HasValue == true)
                                      ? $"{Money(avm.EstimatedValueLow)} – {Money(avm.EstimatedValueHigh)}" : Na();
            litAvmConfidence.Text = avm?.ConfidenceScore.HasValue == true
                                      ? $"{avm.ConfidenceScore:P0}" : Na();
            litRentEstimate.Text  = avm?.RentEstimatedValue.HasValue == true
                                      ? $"{Money(avm.RentEstimatedValue)}/mo" : Na();
            litCapRate.Text       = avm?.CapRate.HasValue == true
                                      ? $"{avm.CapRate:P2}" : Na();
            litEquity.Text        = Money(_profile.EstimatedEquity);

            // Tax assessment — items[0] with taxAmount + assessedValue sub-objects
            var taxItem = _profile.TaxAssessment?.Items?.Count > 0
                          ? _profile.TaxAssessment.Items[0] : null;
            var taxAmt  = taxItem?.TaxAmount;
            var taxAv   = taxItem?.AssessedValue;

            litTaxYear.Text             = F(taxAv?.TaxAssessedYear?.ToString() ?? taxAmt?.BilledYear?.ToString());
            litAssessedValue.Text       = Money(taxAv?.CalculatedTotalValue);
            litLandValue.Text           = Money(taxAv?.CalculatedLandValue);
            litImprovementValue.Text    = Money(taxAv?.CalculatedImprovementValue);
            litTaxAmount.Text           = Money(taxAmt?.TotalTaxAmount ?? taxAmt?.NetTaxAmount);
            litTaxDelinquent.Text       = taxAmt?.DelinquentYear.HasValue == true && taxAmt.DelinquentYear.Value > 0
                                            ? $"<span style='color:#dc2626;font-weight:600'>{taxAmt.DelinquentYear}</span>"
                                            : "<span class='na'>None</span>";

            var prop2 = _profile.Propensity?.Data;
            litPropensityScore.Text = F(prop2?.PropensityScore?.ToString("N1"));
            litPropensityTier.Text  = F(prop2?.PropensityTier);
            litAbsentee.Text        = _profile.IsAbsenteeOwner
                                        ? "<span style='color:#d97706;font-weight:600'>Yes</span>"
                                        : "<span class='na'>No</span>";
            litInvoluntaryLiens.Text  = _profile.HasInvoluntaryLiens
                                        ? $"<span style='color:#dc2626;font-weight:600'>{_profile.InvoluntaryLiens?.Data?.Count ?? 0} lien(s)</span>"
                                        : "<span class='na'>None</span>";
            litTaxDelinquencySignal.Text = _profile.HasTaxDelinquency
                                        ? "<span style='color:#dc2626;font-weight:600'>Yes</span>"
                                        : "<span class='na'>No</span>";
            litPermitSignal.Text      = _profile.HasOpenOrExpiredPermits
                                        ? "<span style='color:#d97706;font-weight:600'>Yes</span>"
                                        : "<span class='na'>No</span>";

            // HOA
            var hoa = _profile.Hoa?.Data;
            if (hoa != null && !string.IsNullOrWhiteSpace(hoa.HoaName))
            {
                pnlHoa.Visible    = true;
                litHoaName.Text   = F(hoa.HoaName);
                litHoaFee.Text    = Money(hoa.HoaFeeAmount);
                litHoaFrequency.Text = F(hoa.HoaFeeFrequency);
                litHoaPhone.Text  = F(hoa.HoaPhone);
            }

            // Climate risk
            var climate = _profile.ClimateRisk?.Data;
            litFloodRisk.Text = RiskLabel(climate?.FloodRiskScore, climate?.FloodRiskLabel);
            litFireRisk.Text  = RiskLabel(climate?.FireRiskScore,  climate?.FireRiskLabel);
            litWindRisk.Text  = RiskLabel(climate?.WindRiskScore,  climate?.WindRiskLabel);
            litHeatRisk.Text  = RiskLabel(climate?.HeatRiskScore,  climate?.HeatRiskLabel);

            // Building permits
            var permits = _profile.BuildingPermits?.Data;
            if (permits != null && permits.Count > 0)
            {
                rptPermits.DataSource = permits;
                rptPermits.DataBind();
                pnlNoPermits.Visible = false;
            }
            else
            {
                phPermits.Visible    = false;
                pnlNoPermits.Visible = true;
            }

            // ── Ownership tab — new nested shape: currentOwners.ownerNames[] + currentOwnerMailingInfo
            var ownData   = _profile.Ownership?.Data;
            var owners    = ownData?.CurrentOwners?.OwnerNames ?? new System.Collections.Generic.List<OwnerName>();
            var mailAddr  = ownData?.CurrentOwnerMailingInfo?.MailingAddress;

            string ownerName1 = owners.Count > 0 ? owners[0].Name : null;
            string o1Full     = owners.Count > 0
                ? string.Join(" ", new[] { owners[0].FirstName, owners[0].LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))) : null;
            string o2Full     = owners.Count > 1
                ? string.Join(" ", new[] { owners[1].FirstName, owners[1].LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))) : null;

            litOwnerName.Text       = F(ownerName1);
            litOwner1.Text          = F(o1Full);
            litOwner2.Text          = F(o2Full);
            litOwnershipType.Text   = F(ownData?.CurrentOwners?.OwnershipRightsCode);
            litAbsenteeOwner.Text   = ownData?.CurrentOwners?.OccupancyCode == "Y"
                                        ? "<span style='color:#d97706;font-weight:600'>Yes — absentee</span>"
                                        : "<span class='na'>No</span>";
            litMailStreet.Text      = F(mailAddr?.StreetAddress);
            litMailCityStateZip.Text = F(string.Join(", ", new[]
                                         { mailAddr?.City, mailAddr?.State, mailAddr?.ZipCode }
                                         .Where(s => !string.IsNullOrWhiteSpace(s))));

            // Transfers — Items[] with nested TransactionDetails/BuyerDetails/SellerDetails.
            // Project to flat TransferRow DTO so ASPX Eval names work without change.
            var transfers2 = _profile.OwnershipTransfers?.Items;
            if (transfers2 != null && transfers2.Count > 0)
            {
                rptTransfers.DataSource = transfers2.Select(t =>
                {
                    var td = t?.TransactionDetails;
                    string buyer  = t?.BuyerDetails?.OwnerNames?.Count  > 0 ? t.BuyerDetails.OwnerNames[0].Name  : null;
                    string seller = t?.SellerDetails?.OwnerNames?.Count > 0 ? t.SellerDetails.OwnerNames[0].Name : null;
                    return new TransferRow
                    {
                        SaleDate       = td?.SaleDateDerived,
                        BuyerName      = buyer,
                        SellerName     = seller,
                        SaleAmount     = td?.SaleAmount,
                        DeedType       = td?.DeedCategoryCode,
                        DocumentNumber = td?.SaleDocumentNumber
                    };
                }).ToList();
                rptTransfers.DataBind();
                pnlNoTransfers.Visible = false;
            }
            else
            {
                phTransfers.Visible    = false;
                pnlNoTransfers.Visible = true;
            }

            // Mortgage — Items[] with nested TransactionDetail/LenderDetail sub-objects.
            // Project to flat MortgageRow DTO so ASPX Eval names work without change.
            var mortgages = _profile.Mortgage?.Items;
            if (mortgages != null && mortgages.Count > 0)
            {
                rptMortgages.DataSource = mortgages.Select(m =>
                {
                    var td = m?.TransactionDetail;
                    string lender = m?.LenderDetail?.LenderCompanyName ?? m?.LenderDetail?.LenderFullName;
                    // Origination date: integer YYYYMMDD — format via FormatDate helper
                    string origDate    = td?.Date.HasValue    == true ? td.Date.Value.ToString()    : null;
                    string maturityDate = td?.DueDate.HasValue == true ? td.DueDate.Value.ToString() : null;
                    return new MortgageRow
                    {
                        OriginationDate = origDate,
                        LenderName      = lender,
                        LoanAmount      = td?.Amount,
                        LoanType        = td?.LoanTypeCodeDescription ?? td?.LoanTypeCode,
                        InterestRate    = td?.InterestRate,
                        LoanPosition    = td?.LienPosition?.ToString(),
                        MaturityDate    = maturityDate
                    };
                }).ToList();
                rptMortgages.DataBind();
                pnlNoMortgages.Visible = false;
            }
            else
            {
                phMortgages.Visible    = false;
                pnlNoMortgages.Visible = true;
            }

            // Voluntary liens
            var volLiens = _profile.EnrichedVoluntaryLiens?.Data;
            if (volLiens != null && volLiens.Count > 0)
            {
                rptVolLiens.DataSource = volLiens;
                rptVolLiens.DataBind();
                pnlNoVolLiens.Visible = false;
            }
            else
            {
                phVolLiens.Visible    = false;
                pnlNoVolLiens.Visible = true;
            }

            // Involuntary liens
            var invLiens = _profile.InvoluntaryLiens?.Data;
            if (invLiens != null && invLiens.Count > 0)
            {
                rptInvLiens.DataSource = invLiens;
                rptInvLiens.DataBind();
                pnlNoInvLiens.Visible = false;
            }
            else
            {
                phInvLiens.Visible    = false;
                pnlNoInvLiens.Visible = true;
            }

            // ── Documents tab ─────────────────────────────────────────────
            if (_docRows != null && _docRows.Count > 0)
            {
                rptDocs.DataSource = _docRows;
                rptDocs.DataBind();
                pnlNoDocs.Visible = false;
            }
            else
            {
                pnlDocs.Visible   = false;
                pnlNoDocs.Visible = true;
            }
        }

        private void BindSignalBadges()
        {
            var signals = new System.Text.StringBuilder();

            if (_profile.IsAbsenteeOwner)
                signals.Append(Badge("Absentee Owner", "amber"));

            if (_profile.HasInvoluntaryLiens)
                signals.Append(Badge($"{_profile.InvoluntaryLiens.Data.Count} Involuntary Lien(s)", "red"));

            if (_profile.HasTaxDelinquency)
            {
                var taxDelinqYear = _profile.TaxAssessment?.Items?.Count > 0
                    ? _profile.TaxAssessment.Items[0]?.TaxAmount?.DelinquentYear?.ToString() : null;
                signals.Append(Badge($"Tax Delinquent {taxDelinqYear}", "red"));
            }

            if (_profile.HasOpenOrExpiredPermits)
                signals.Append(Badge("Open/Expired Permits", "amber"));

            var score = _profile.PropensityScore;
            if (score.HasValue)
            {
                var tier  = score >= 80 ? "red" : score >= 50 ? "amber" : "green";
                signals.Append(Badge($"Propensity {score:N0}", tier));
            }

            if (!_profile.IsAbsenteeOwner && !_profile.HasInvoluntaryLiens && !_profile.HasTaxDelinquency)
                signals.Append(Badge("No distress signals", "grey"));

            phSignals.Controls.Add(new LiteralControl(signals.ToString()));
        }

        // ── Prospect card ─────────────────────────────────────────────────

        private void LoadProspect()
        {
            var prop  = _profile?.Property;
            if (prop == null) return;

            var orgId = GetCurrentOrganizationId();

            using (var db = new DCReyla())
            {
                var p = db.Prospects.FirstOrDefault(x =>
                            x.PropertyId     == prop.PropertyId &&
                            x.OrganizationId == orgId           &&
                            x.IsDeleted      == false);

                if (p == null) return;

                // Agent name from Profile, email from Membership
                string agentName  = string.Empty;
                string agentEmail = string.Empty;

                var mem = System.Web.Security.Membership.GetUser(p.UserId);
                if (mem != null) agentEmail = mem.Email ?? string.Empty;

                var profile = db.Profiles.FirstOrDefault(x => x.UserId == p.UserId);
                if (profile != null)
                    agentName = (profile.FirstName + " " + profile.LastName).Trim();

                if (string.IsNullOrWhiteSpace(agentName))
                    agentName = agentEmail; // fall back to email as display name

                // Org name
                string orgName = string.Empty;
                var org = db.Organizations.FirstOrDefault(x => x.OrganizationId == orgId);
                if (org != null) orgName = org.Name ?? string.Empty;

                // Saved comp count
                int compCount = 0;
                var searchIds = db.ProspectSearches
                    .Where(s => s.OrganizationId == orgId)
                    .Select(s => s.ProspectSearchId)
                    .ToList();
                if (searchIds.Any())
                    compCount = db.ProspectSearchProperties
                        .Where(sp => searchIds.Contains(sp.ProspectSearchId))
                        .Select(sp => sp.PropertyId)
                        .Distinct()
                        .Count();

                // Deal — find the active deal for this prospect
                Guid?     dealId      = null;
                string    dealStage   = null;
                string    dealTitle   = null;
                decimal?  askingPrice = null;
                decimal?  offerPrice  = null;
                DateTime? closeDate   = null;
                try
                {
                    var deal = db.Deals.FirstOrDefault(d =>
                        d.ProspectId     == p.ProspectId &&
                        d.OrganizationId == orgId        &&
                        d.IsDeleted      == false);
                    if (deal != null)
                    {
                        dealId      = deal.DealId;
                        dealStage   = deal.Stage;
                        dealTitle   = deal.Title;
                        askingPrice = deal.AskingPrice;
                        offerPrice  = deal.OfferPrice;
                        closeDate   = deal.CloseDate;
                    }
                }
                catch { /* non-fatal — deal table may not exist yet */ }

                _prospect = new ProspectDetail
                {
                    ProspectId   = p.ProspectId,
                    Status       = p.Status       ?? "New",
                    IsActive     = p.IsActive,
                    CreatedAtUtc = p.CreatedAtUtc,
                    UpdatedAtUtc = p.UpdatedAtUtc,
                    AgentName    = agentName,
                    AgentEmail   = agentEmail,
                    OrgName      = orgName,
                    CompCount    = compCount,
                    DealId       = dealId,
                    DealStage    = dealStage,
                    DealTitle    = dealTitle,
                    AskingPrice  = askingPrice,
                    OfferPrice   = offerPrice,
                    CloseDate    = closeDate
                };
            }
        }

        private void BindProspectCard()
        {
            bool isProspect = _prospect != null;

            btnToggleProspect.Text     = "★ Add as Prospect";
            btnToggleProspect.CssClass = "btn btn-sm btn-success";
            btnToggleProspect.Visible  = !isProspect;
            btnToggleProspect.OnClientClick = string.Empty;

            // Build prospect card HTML
            var sb = new System.Text.StringBuilder();

            if (isProspect)
            {
                var p       = _prospect;
                var updated = p.UpdatedAtUtc.HasValue
                    ? p.UpdatedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy")
                    : string.Empty;

                // Status badge colour
                string statusCss = p.Status == "Active"   ? "signal-green"
                                 : p.Status == "Closed"   ? "signal-grey"
                                 : p.Status == "New"      ? "signal-blue"
                                 : "signal-amber";

                string compsUrl     = ResolveUrl($"~/Secure/Prospects/Comps.aspx?prospectId={p.ProspectId}");
                string removeScript = "if(confirm('Remove this property as a prospect? This cannot be undone.')){__doPostBack('" + btnRemoveProspect.UniqueID + "','');}return false;";
                string statusHtml   = HttpUtility.HtmlEncode(p.Status);
                string orgHtml      = HttpUtility.HtmlEncode(p.OrgName.Length   > 0 ? p.OrgName   : "—");
                string agentHtml    = HttpUtility.HtmlEncode(p.AgentName.Length > 0 ? p.AgentName : "—");
                string emailHtml    = HttpUtility.HtmlEncode(p.AgentEmail.Length > 0 ? p.AgentEmail : "—");
                string addedHtml    = p.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy");
                string updatedHtml  = p.UpdatedAtUtc.HasValue
                                        ? p.UpdatedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy")
                                        : "—";

                sb.Append("<div class=\"prospect-card active\">");
                sb.Append("<div class=\"prospect-card-header\">");
                sb.Append("<div class=\"prospect-card-title\">");
                sb.Append("<svg width=\"18\" height=\"18\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#16a34a\" stroke-width=\"2\">");
                sb.Append("<path d=\"M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z\"/>");
                sb.Append("<polyline points=\"9 22 9 12 15 12 15 22\"/></svg>");
                sb.Append("<h3>Active Prospect</h3>");
                sb.Append("<span class=\"prospect-pill active\">" + statusHtml + "</span>");
                sb.Append("</div>"); // prospect-card-title
                sb.Append("<div class=\"prospect-card-actions\">");
                sb.Append("<a href=\"" + compsUrl + "\" class=\"btn btn-sm btn-outline-primary\" style=\"font-size:12px;\">");
                sb.Append("<svg width=\"12\" height=\"12\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\">");
                sb.Append("<circle cx=\"11\" cy=\"11\" r=\"8\"/><line x1=\"21\" y1=\"21\" x2=\"16.65\" y2=\"16.65\"/></svg> View Comps</a> ");
                sb.Append("<a href=\"#\" onclick=\"" + HttpUtility.HtmlAttributeEncode(removeScript) + "\" class=\"btn btn-sm btn-outline-danger\" style=\"font-size:12px;\">&#10005; Remove Prospect</a>");
                sb.Append("</div>"); // prospect-card-actions
                sb.Append("</div>"); // prospect-card-header
                sb.Append("<div class=\"prospect-facts\">");
                sb.Append("<div class=\"prospect-fact\"><label>Organization</label><span>" + orgHtml + "</span></div>");
                sb.Append("<div class=\"prospect-fact\"><label>Agent</label><span>" + agentHtml + "</span></div>");
                sb.Append("<div class=\"prospect-fact\"><label>Agent Email</label><span>" + emailHtml + "</span></div>");
                sb.Append("<div class=\"prospect-fact\"><label>Added</label><span>" + addedHtml + "</span></div>");
                sb.Append("<div class=\"prospect-fact\"><label>Last Updated</label><span>" + updatedHtml + "</span></div>");
                sb.Append("<div class=\"prospect-fact\"><label>Saved Comps</label><span>" + p.CompCount.ToString("N0") + "</span></div>");
                sb.Append("</div>"); // prospect-facts

                // ── Deal Pipeline Stage Tracker ───────────────────────
                sb.Append("<div class=\"mt-3 pt-3\" style=\"border-top:1px dashed #d1d5db;\">");
                sb.Append("<div class=\"d-flex align-items-center gap-2 mb-2\">");
                sb.Append("<i class=\"ti ti-layout-kanban text-primary fs-16\"></i>");
                sb.Append("<span class=\"fw-bold fs-sm\">Deal Pipeline</span>");

                if (p.DealId.HasValue)
                {
                    string pipelineUrl = ResolveUrl("~/Secure/Prospects/Pipeline.aspx");
                    sb.Append("<a href=\"" + pipelineUrl + "\" class=\"btn btn-outline-primary btn-sm ms-auto py-0 px-2 fs-xs\">" +
                              "<i class=\"ti ti-external-link me-1\"></i>View Pipeline</a>");
                }
                else
                {
                    sb.Append("<span class=\"text-muted fs-xs ms-auto\">No deal yet</span>");
                }
                sb.Append("</div>"); // d-flex

                // Deal price/date facts + valuation comparison
                // Calculate valuation first so we can use it in the comparison
                var orgId  = GetCurrentOrganizationId();
                var valSvc = new ReylaValuationService();
                var val    = valSvc.Calculate(p.ProspectId, orgId);

                if (p.DealId.HasValue && (p.AskingPrice.HasValue || p.OfferPrice.HasValue || p.CloseDate.HasValue || val.Success))
                {
                    sb.Append("<div class=\"prospect-facts mt-2 mb-3\">");

                    if (p.AskingPrice.HasValue)
                    {
                        // Color-code asking vs estimate
                        string askColor = "#111827";
                        string askSuffix = "";
                        if (val.Success && val.PointEstimate > 0)
                        {
                            double pct = (double)((p.AskingPrice.Value - val.PointEstimate) / val.PointEstimate * 100);
                            if (pct > 5)       { askColor = "#dc2626"; askSuffix = " <small style='font-weight:400;font-size:11px;'>(" + pct.ToString("F0") + "% over)</small>"; }
                            else if (pct < -5) { askColor = "#16a34a"; askSuffix = " <small style='font-weight:400;font-size:11px;'>(" + Math.Abs(pct).ToString("F0") + "% under)</small>"; }
                            else               { askSuffix = " <small style='font-weight:400;font-size:11px;color:#6b7280;'>(at est.)</small>"; }
                        }
                        sb.Append("<div class=\"prospect-fact\"><label>Asking Price</label>" +
                                  "<span style=\"color:" + askColor + ";font-weight:600;\">$" + p.AskingPrice.Value.ToString("N0") + askSuffix + "</span></div>");
                    }

                    if (p.OfferPrice.HasValue)
                    {
                        string offColor = "#166534";
                        string offSuffix = "";
                        if (val.Success && val.PointEstimate > 0)
                        {
                            double pct = (double)((p.OfferPrice.Value - val.PointEstimate) / val.PointEstimate * 100);
                            if (pct > 5)       { offColor = "#dc2626"; offSuffix = " <small style='font-weight:400;font-size:11px;'>(" + pct.ToString("F0") + "% over)</small>"; }
                            else if (pct < -5) { offColor = "#16a34a"; offSuffix = " <small style='font-weight:400;font-size:11px;'>(" + Math.Abs(pct).ToString("F0") + "% under)</small>"; }
                            else               { offSuffix = " <small style='font-weight:400;font-size:11px;color:#6b7280;'>(at est.)</small>"; }
                        }
                        sb.Append("<div class=\"prospect-fact\"><label>Offer Price</label>" +
                                  "<span style=\"color:" + offColor + ";font-weight:700;\">$" + p.OfferPrice.Value.ToString("N0") + offSuffix + "</span></div>");
                    }

                    if (val.Success)
                        sb.Append("<div class=\"prospect-fact\"><label>Reyla Estimate</label>" +
                                  "<span style=\"color:#1e40af;font-weight:600;\">$" + val.PointEstimate.ToString("N0") +
                                  " <small style='font-weight:400;font-size:11px;color:#6b7280;'>" + val.ConfidenceLabel + " conf</small></span></div>");

                    if (p.CloseDate.HasValue)
                        sb.Append("<div class=\"prospect-fact\"><label>Target Close</label>" +
                                  "<span>" + p.CloseDate.Value.ToString("MMM d, yyyy") + "</span></div>");

                    sb.Append("</div>");
                }

                // Horizontal stage stepper
                var stages = new[] {
                    new { Key = "Lead",          Label = "Lead" },
                    new { Key = "Qualified",     Label = "Qualified" },
                    new { Key = "LOI",           Label = "LOI" },
                    new { Key = "UnderContract", Label = "Under Contract" },
                    new { Key = "Closed",        Label = "Closed" },
                    new { Key = "Dead",          Label = "Dead" }
                };

                string currentStage = p.DealStage ?? (p.DealId.HasValue ? "Lead" : null);

                // Stage colour map
                sb.Append("<div style=\"display:flex;gap:0;border-radius:8px;overflow:hidden;border:1px solid #e2e8f0;\">");
                foreach (var stage in stages)
                {
                    bool isActive  = currentStage == stage.Key;
                    bool isPast    = currentStage != null && stage.Key != "Dead" &&
                                     Array.IndexOf(new[]{"Lead","Qualified","LOI","UnderContract","Closed","Dead"}, stage.Key) <
                                     Array.IndexOf(new[]{"Lead","Qualified","LOI","UnderContract","Closed","Dead"}, currentStage);
                    bool isDead    = stage.Key == "Dead";

                    string bg, color, fw;
                    if (isActive && isDead)      { bg = "#fef2f2"; color = "#dc2626"; fw = "700"; }
                    else if (isActive)           { bg = "#1e40af"; color = "#fff";    fw = "700"; }
                    else if (isPast)             { bg = "#dbeafe"; color = "#1e40af"; fw = "500"; }
                    else if (!p.DealId.HasValue) { bg = "#f9fafb"; color = "#9ca3af"; fw = "400"; }
                    else                        { bg = "#f9fafb"; color = "#9ca3af"; fw = "400"; }

                    string title = isActive ? "Current stage" : (isPast ? "Completed" : "");

                    sb.Append("<div style=\"flex:1;text-align:center;padding:6px 4px;font-size:11px;" +
                              "font-weight:" + fw + ";background:" + bg + ";color:" + color + ";" +
                              "white-space:nowrap;overflow:hidden;text-overflow:ellipsis;border-right:1px solid #e2e8f0;" +
                              "\" title=\"" + HttpUtility.HtmlAttributeEncode(title) + "\">");
                    if (isActive)
                        sb.Append("<i class=\"ti ti-circle-dot\" style=\"font-size:10px;margin-right:3px;\"></i>");
                    else if (isPast)
                        sb.Append("<i class=\"ti ti-check\" style=\"font-size:10px;margin-right:3px;\"></i>");
                    sb.Append(HttpUtility.HtmlEncode(stage.Label));
                    sb.Append("</div>");
                }
                sb.Append("</div>"); // stage stepper

                if (!p.DealId.HasValue)
                {
                    sb.Append("<p class=\"text-muted fs-xs mt-2 mb-0\">" +
                              "<i class=\"ti ti-info-circle me-1\"></i>" +
                              "This prospect will appear on the Pipeline board when a deal is created via Search or Pipeline.</p>");
                }

                sb.Append("</div>"); // deal pipeline section

                // ── Reyla Valuation ──────────────────────────────────────
                string reylaValId = "reylaVal_" + p.ProspectId.ToString("N");

                sb.Append("<div id=\"" + reylaValId + "\">");

                if (val.Success)
                {
                    sb.Append("<div class=\"mt-3 pt-3\" style=\"border-top:1px dashed #d1d5db;\">");
                    sb.Append("<div class=\"d-flex align-items-center gap-2 mb-2\">");
                    sb.Append("<i class=\"ti ti-calculator text-primary fs-18\"></i>");
                    sb.Append("<span class=\"fw-bold fs-sm\">Reyla Valuation</span>");
                    sb.Append("<span class=\"badge text-bg-" + val.ConfidenceColor + " ms-1\">" + val.ConfidenceLabel + " Confidence</span>");
                    sb.Append("<span class=\"text-muted fs-xs\">" + val.CompsUsed + " comps used</span>");
                    sb.Append("</div>"); // d-flex

                    // Big estimate display
                    sb.Append("<div class=\"d-flex align-items-baseline gap-3 mb-1\">");
                    sb.Append("<h3 class=\"mb-0 text-primary fw-bold\">" + val.PointEstimate.ToString("C0") + "</h3>");
                    sb.Append("<span class=\"text-muted fs-xs\">range: " + val.RangeLow.ToString("C0") + " – " + val.RangeHigh.ToString("C0") + "</span>");
                    sb.Append("</div>");

                    // Methodology stats
                    sb.Append("<div class=\"d-flex gap-3 flex-wrap\">");
                    sb.Append("<span class=\"text-muted fs-xs\"><strong>" + val.WeightedMedianPpsf.ToString("C0") + "/sqft</strong> weighted median</span>");
                    sb.Append("<span class=\"text-muted fs-xs\"><strong>" + val.SubjectSqFt.ToString("N0") + " sqft</strong> subject size</span>");
                    if (val.SubjectYearBuilt.HasValue)
                        sb.Append("<span class=\"text-muted fs-xs\"><strong>" + val.SubjectYearBuilt.Value + "</strong> yr built</span>");
                    sb.Append("</div>"); // methodology

                    sb.Append("<p class=\"text-muted fs-xxs mt-2 mb-0\">Based on your saved comps. Weighted by recency and distance. Not a licensed appraisal.</p>");
                    sb.Append("</div>"); // valuation section
                }
                else if (p.CompCount > 0)
                {
                    sb.Append("<div class=\"mt-3 pt-3\" style=\"border-top:1px dashed #d1d5db;\">");
                    sb.Append("<div class=\"d-flex align-items-center gap-2 mb-1\">");
                    sb.Append("<i class=\"ti ti-calculator text-muted fs-18\"></i>");
                    sb.Append("<span class=\"fw-semibold fs-sm text-muted\">Reyla Valuation</span>");
                    sb.Append("</div>");
                    sb.Append("<div class=\"alert alert-warning py-2 mb-0\"><i class=\"ti ti-alert-triangle me-1\"></i>" + HttpUtility.HtmlEncode(val.ErrorMessage) + "</div>");
                    sb.Append("</div>");
                }

                sb.Append("</div>"); // reylaValId

                sb.Append("</div>"); // prospect-card
            }
            else
            {
                sb.Append(@"
<div class=""prospect-card inactive"">
    <div class=""prospect-card-header"">
        <div class=""prospect-card-title"">
            <svg width=""18"" height=""18"" viewBox=""0 0 24 24"" fill=""none"" stroke=""#9ca3af"" stroke-width=""2"">
                <path d=""M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z""/>
                <polyline points=""9 22 9 12 15 12 15 22""/>
            </svg>
            <h3>Not a Prospect</h3>
            <span class=""prospect-pill inactive"">Untracked</span>
        </div>
    </div>
    <p style=""margin:0; font-size:13px; color:#6b7280;"">
        This property is not currently being tracked as a prospect by your organization.
        Click <strong>★ Add as Prospect</strong> above to start tracking it.
    </p>
</div>");
            }

            phProspectCard.Controls.Add(new LiteralControl(sb.ToString()));
        }

        protected void btnToggleProspect_Click(object sender, EventArgs e)
        {
            var prop  = (_profile ?? Session["PropertyDetail_Profile"] as PropertyProfileResult)?.Property;
            if (prop == null) return;

            var orgId  = GetCurrentOrganizationId();
            var userId = GetCurrentUserId();
            var svc    = new ProspectService();

            // Re-check current state to decide add vs remove
            using (var db = new DCReyla())
            {
                var existing = db.Prospects.FirstOrDefault(x =>
                    x.PropertyId     == prop.PropertyId &&
                    x.OrganizationId == orgId           &&
                    x.IsDeleted      == false);

                if (existing == null)
                    svc.CreateProspect(prop.PropertyId, orgId, userId);
                else
                    svc.DeleteProspect(existing.ProspectId, orgId, userId);
            }

            // Redirect to self — reload with fresh prospect state
            Response.Redirect(Request.RawUrl);
        }

        // ---------------------------------------------------------------
        // BindCompMgmt — renders the saved comp list with + / × toggle buttons
        // so the user can add/remove individual comps without leaving the page.
        // ---------------------------------------------------------------

        private void BindCompMgmt()
        {
            if (_prospect == null || phCompMgmt == null) return;

            var orgId      = GetCurrentOrganizationId();
            var svc        = new ProspectService();
            var selected   = svc.GetSelectedComps(_prospect.ProspectId, orgId);

            // Also load the full cache so we can offer "available but not selected" comps
            List<ComparableProperty> allCached = new List<ComparableProperty>();
            using (var db = new DCReyla())
            {
                var cache = db.ProspectCompsCaches
                    .Where(c => c.ProspectId == _prospect.ProspectId)
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .FirstOrDefault();

                if (cache != null && !string.IsNullOrEmpty(cache.CompsJson))
                {
                    try { allCached = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ComparableProperty>>(cache.CompsJson)
                                      ?? new List<ComparableProperty>(); }
                    catch { }
                }
            }

            var selectedClips  = new HashSet<string>(selected.Select(c => c.Clip));
            var notSelected    = allCached
                .Where(c => !selectedClips.Contains(c.Clip))
                .ToList();  // No server-side sort or Take limit — DataTable handles all of it

            if (!selected.Any() && !notSelected.Any())
            {
                // No comps at all — show a prompt card with a button to open the Valuation tab
                string prospectIdForPrompt = _prospect.ProspectId.ToString();
                var sbPrompt = new System.Text.StringBuilder();
                sbPrompt.Append("<div class=\"card mb-3\">");
                sbPrompt.Append("<div class=\"card-header\"><h5 class=\"card-title mb-0\"><i class=\"ti ti-chart-bar me-2\"></i>Comparables</h5></div>");
                sbPrompt.Append("<div class=\"card-body text-center py-5\">");
                sbPrompt.Append("<i class=\"ti ti-building-estate text-muted\" style=\"font-size:2.5rem;\"></i>");
                sbPrompt.Append("<h5 class=\"mt-3 mb-1\">No Comparables Yet</h5>");
                sbPrompt.Append("<p class=\"text-muted fs-sm mb-4\">Run a comp search to find nearby sales.<br>Your Reyla Valuation will calculate automatically once comps are saved.</p>");
                sbPrompt.Append("<a href=\"/Secure/Prospects/Comps.aspx?prospectId=" + prospectIdForPrompt + "\" class=\"btn btn-primary\">");
                sbPrompt.Append("<i class=\"ti ti-search me-2\"></i>Find Comparables</a>");
                sbPrompt.Append("</div></div>");
                phCompMgmt.Controls.Add(new LiteralControl(sbPrompt.ToString()));
                return;
            }

            var sb = new System.Text.StringBuilder();
            string prospectIdStr = _prospect.ProspectId.ToString();
            string tableId       = "tblAvailComps_" + prospectIdStr.Replace("-", "");
            string selectedListId = "selComps_" + prospectIdStr.Replace("-", "");
            string valuationId    = "reylaVal_" + prospectIdStr.Replace("-", "");

            // Scoped styles: popover image containment + responsive
            sb.Append("<style>");
            sb.Append(".sv-popover-img { width:100%; max-width:300px; height:auto; display:block; border-radius:4px; }");
            sb.Append(".popover { --bs-popover-max-width: 320px; }");
            sb.Append(".popover-body { padding: 0.4rem; }");
            sb.Append("@media(max-width:576px){ .popover { --bs-popover-max-width: 220px; } }");
            sb.Append("</style>");

            sb.Append("<div class=\"card mb-3\">");
            sb.Append("<div class=\"card-header d-flex align-items-center justify-content-between\">");
            sb.Append("<h5 class=\"card-title mb-0\"><i class=\"ti ti-chart-bar me-2\"></i>Comparables");
            // selectedCount badge — JS updates this span in place
            sb.Append("<span id=\"selCountBadge_" + prospectIdStr.Replace("-","") + "\" class=\"badge text-bg-primary ms-2\">" + selected.Count + "</span></h5>");
            sb.Append("<div class=\"d-flex gap-2\">");

            // Clear All — data-action driven, no postback
            sb.Append("<a href=\"javascript:void(0)\" id=\"btnClearAll_" + prospectIdStr.Replace("-","") + "\" " +
                      "class=\"btn btn-outline-danger btn-sm comp-toggle-btn" + (selected.Any() ? "" : " d-none") + "\" " +
                      "data-prospect=\"" + prospectIdStr + "\" data-action=\"clear\">");
            sb.Append("<i class=\"ti ti-trash me-1\"></i>Clear All</a>");

            sb.Append("<a href=\"/Secure/Prospects/Comps.aspx?prospectId=" + prospectIdStr + "\" class=\"btn btn-outline-primary btn-sm\">");
            sb.Append("<i class=\"ti ti-refresh me-1\"></i>Full Comp Review</a>");
            sb.Append("</div></div>"); // d-flex + card-header
            sb.Append("<div class=\"card-body p-0\">");

            // ── Subject property header row for comparison context ───
            string svKey = System.Configuration.ConfigurationManager.AppSettings["Google.StreetViewApiKey"] ?? "";
            var subjectProperty = _profile?.Property;
            if (subjectProperty != null)
            {
                string subjectAddr  = HttpUtility.HtmlEncode(subjectProperty.StreetAddress ?? "—");
                string subjectCity  = HttpUtility.HtmlEncode(
                    (subjectProperty.CityNameRaw ?? "") + (subjectProperty.ZipCodeRaw != null ? " " + subjectProperty.ZipCodeRaw : ""));

                // Subject snapshot stats for comparison
                string subSqft = "—", subYr = "—", subPpsf = "—", subPrice = "—", subDist = "0.00 mi";
                using (var db2 = new DCReyla())
                {
                    var snap = db2.PropertySnapshots
                        .Where(s => s.PropertyId == subjectProperty.PropertyId && s.IsDeleted == false)
                        .OrderByDescending(s => s.CreatedAtUtc).FirstOrDefault();
                    if (snap != null)
                    {
                        var bldg = db2.PropertySnapshotBuildings
                            .FirstOrDefault(b => b.PropertySnapshotId == snap.PropertySnapshotId);
                        var xfer = db2.PropertySnapshotOwnershipTransfers
                            .Where(t => t.PropertySnapshotId == snap.PropertySnapshotId)
                            .OrderByDescending(t => t.SaleDate).FirstOrDefault();
                        if (bldg?.GrossLivingArea.HasValue == true && bldg.GrossLivingArea > 0)
                        {
                            subSqft = ((int)bldg.GrossLivingArea.Value).ToString("N0") + " sqft";
                            if (bldg.YearBuilt.HasValue && bldg.YearBuilt > 0)
                                subYr = bldg.YearBuilt.Value.ToString();
                        }
                        if (xfer?.SaleAmount.HasValue == true && xfer.SaleAmount > 0)
                        {
                            subPrice = "$" + xfer.SaleAmount.Value.ToString("N0");
                            if (bldg?.GrossLivingArea.HasValue == true && bldg.GrossLivingArea > 0)
                                subPpsf = "$" + Math.Round(xfer.SaleAmount.Value / (decimal)bldg.GrossLivingArea.Value).ToString("N0") + "/sqft";
                        }
                    }
                }

                // Street View popover for subject — click only, no hover flicker
                string subSvUrl = subjectProperty.Latitude != 0 && subjectProperty.Longitude != 0 && !string.IsNullOrEmpty(svKey)
                    ? "https://maps.googleapis.com/maps/api/streetview?size=320x180&location=" +
                      subjectProperty.Latitude + "," + subjectProperty.Longitude +
                      "&fov=90&pitch=0&source=outdoor&key=" + svKey
                    : "";
                string subSvBtn = !string.IsNullOrEmpty(subSvUrl)
                    ? "<a href=\"javascript:void(0)\" class=\"btn btn-sm btn-outline-secondary py-0 px-1 flex-shrink-0 sv-popover-btn\" " +
                      "data-bs-toggle=\"popover\" data-bs-trigger=\"click\" data-bs-placement=\"left\" data-bs-html=\"true\" " +
                      "data-bs-content=\"&lt;img src=&apos;" + subSvUrl + "&apos; class=&apos;sv-popover-img&apos; /&gt;\" " +
                      "title=\"" + HttpUtility.HtmlAttributeEncode(subjectProperty.StreetAddress ?? "") + "\">" +
                      "<i class=\"ti ti-camera\"></i></a>"
                    : "";

                sb.Append("<div class=\"px-3 pt-3 pb-2\">");
                sb.Append("<p class=\"text-muted fs-xs fw-semibold text-uppercase mb-2\">Target / Subject Property</p>");
                // Dashed border, padding inside, neutral background so badges pop
                sb.Append("<div class=\"d-flex align-items-center gap-2 p-3 rounded\" style=\"border:2px dashed #6c757d;background:#f8f9fa;\">");
                sb.Append("<div class=\"flex-grow-1 min-width-0\">");
                sb.Append("<div class=\"fw-bold fs-xs\">" + subjectAddr + "</div>");
                sb.Append("<div class=\"text-muted fs-xs mb-1\">" + subjectCity + "</div>");
                sb.Append("<div class=\"d-flex flex-wrap gap-2\">");
                sb.Append("<span class=\"badge text-bg-primary\">" + subDist + "</span>");
                if (subPrice != "—") sb.Append("<span class=\"badge text-bg-warning\">" + subPrice + "</span>");
                if (subSqft != "—")  sb.Append("<span class=\"badge text-bg-light text-dark border\">" + subSqft + "</span>");
                if (subPpsf != "—")  sb.Append("<span class=\"badge text-bg-secondary\">" + subPpsf + "</span>");
                if (subYr   != "—")  sb.Append("<span class=\"badge text-bg-light text-dark border\">Built " + subYr + "</span>");
                sb.Append("</div></div>");
                sb.Append(subSvBtn);
                sb.Append("</div></div>"); // d-flex + px-3
            }

            // ── Selected comps — wrapped in a container JS can rebuild ──
            // Always render the container div so the JS engine can populate it after fetch
            {
                sb.Append("<div id=\"" + selectedListId + "\">");
                if (selected.Any())
                {
                    sb.Append("<div class=\"px-3 pt-3 pb-1\"><p class=\"text-muted fs-xs fw-semibold text-uppercase mb-2\">Selected Comparables</p></div>");
                    sb.Append("<ul class=\"list-group list-group-flush\">");

                    foreach (var comp in selected.OrderBy(c => c.Distance))
                    {
                        string addr     = HttpUtility.HtmlEncode(comp.StreetAddress ?? "—");
                        string ppsf     = comp.SalePrice.HasValue && comp.BuildingSquareFeet.HasValue && comp.BuildingSquareFeet > 0
                                          ? "$" + Math.Round(comp.SalePrice.Value / comp.BuildingSquareFeet.Value).ToString("N0") + "/sqft" : "";
                        string price    = comp.SalePrice.HasValue ? "$" + comp.SalePrice.Value.ToString("N0") : "";
                        string sqft     = comp.BuildingSquareFeet.HasValue ? comp.BuildingSquareFeet.Value.ToString("N0") + " sqft" : "";
                        string dist     = comp.Distance.HasValue  ? comp.Distance.Value.ToString("F2") + " mi" : "";
                        string saleDate = FormatCompDate(comp.SaleDate);

                        // Remove button — data-attributes only, no postback
                        string safeClip = HttpUtility.HtmlAttributeEncode(comp.Clip ?? "");
                        string removeBtn = "<a href=\"javascript:void(0)\" " +
                                           "class=\"btn btn-outline-danger btn-sm py-0 px-2 flex-shrink-0 comp-toggle-btn\" " +
                                           "data-prospect=\"" + prospectIdStr + "\" " +
                                           "data-clip=\"" + safeClip + "\" " +
                                           "data-action=\"remove\" title=\"Remove\">" +
                                           "<i class=\"ti ti-x\"></i></a>";

                        // Street View popover
                        string svBtn = "";
                        if (comp.Latitude.HasValue && comp.Longitude.HasValue && !string.IsNullOrEmpty(svKey))
                        {
                            string svUrl = "https://maps.googleapis.com/maps/api/streetview?size=320x180&location=" +
                                           comp.Latitude.Value + "," + comp.Longitude.Value +
                                           "&fov=90&pitch=0&source=outdoor&key=" + svKey;
                            svBtn = "<a href=\"javascript:void(0)\" class=\"btn btn-sm btn-outline-secondary py-0 px-1 flex-shrink-0 sv-popover-btn\" " +
                                    "data-bs-toggle=\"popover\" data-bs-trigger=\"click\" data-bs-placement=\"left\" data-bs-html=\"true\" " +
                                    "data-bs-content=\"&lt;img src=&apos;" + svUrl + "&apos; class=&apos;sv-popover-img&apos; /&gt;\" " +
                                    "title=\"" + HttpUtility.HtmlAttributeEncode(comp.StreetAddress ?? "") + "\">" +
                                    "<i class=\"ti ti-camera\"></i></a>";
                        }

                        sb.Append("<li class=\"list-group-item d-flex align-items-center gap-2 py-2\">");
                        sb.Append("<div class=\"flex-grow-1 min-width-0\">");
                        sb.Append("<div class=\"fw-semibold fs-xs\"><a href=\"/Secure/Prospects/PropertyDetail.aspx?clip=" + safeClip + "\" target=\"_blank\" class=\"text-dark\">" + addr + " <i class=\"ti ti-external-link\" style=\"font-size:0.7em;opacity:0.6;\"></i></a></div>");
                        sb.Append("<div class=\"d-flex flex-wrap gap-2 mt-1\">");
                        if (!string.IsNullOrEmpty(dist))     sb.Append("<span class=\"badge text-bg-primary\">"         + dist     + "</span>");
                        if (!string.IsNullOrEmpty(saleDate)) sb.Append("<span class=\"badge text-bg-success\">"         + saleDate + "</span>");
                        if (!string.IsNullOrEmpty(price))    sb.Append("<span class=\"badge text-bg-warning\">"         + price    + "</span>");
                        if (!string.IsNullOrEmpty(sqft))     sb.Append("<span class=\"badge text-bg-light text-dark\">" + sqft     + "</span>");
                        if (!string.IsNullOrEmpty(ppsf))     sb.Append("<span class=\"badge text-bg-secondary\">"       + ppsf     + "</span>");
                        sb.Append("</div></div>");
                        sb.Append(svBtn);
                        sb.Append(removeBtn);
                        sb.Append("</li>");
                    }
                    sb.Append("</ul>");
                }
                sb.Append("</div>"); // selectedListId
            }

            // ── Available to add — collapsible DataTable ────────────
            if (notSelected.Any())
            {
                string collapseId = "availComps_" + prospectIdStr.Replace("-", "");

                // Prominent toggle button
                sb.Append("<div class=\"p-3 border-top\">");
                sb.Append("<button class=\"btn btn-outline-secondary w-100 d-flex align-items-center justify-content-between\" " +
                          "type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#" + collapseId + "\">");
                sb.Append("<span><i class=\"ti ti-circle-plus me-2 text-success\"></i>" +
                          "<strong>" + notSelected.Count + " more comps available</strong> — sort, search, then click <i class=\"ti ti-plus\"></i> to add</span>");
                sb.Append("<i class=\"ti ti-chevron-down\"></i>");
                sb.Append("</button>");
                sb.Append("</div>"); // p-3

                sb.Append("<div class=\"collapse\" id=\"" + collapseId + "\">");
                sb.Append("<div class=\"table-responsive\">");

                // Custom CSS scoped to this table for Address padding and info line
                sb.Append("<style>");
                sb.Append("#" + tableId + " td:first-child, #" + tableId + " th:first-child { padding-left: 1.25rem; }");
                sb.Append("#" + tableId + "_wrapper .dataTables_info { padding-left: 1.25rem; }");
                sb.Append("#" + tableId + "_wrapper .dataTables_length { padding-left: 1.25rem; }");
                sb.Append("</style>");

                sb.Append("<table id=\"" + tableId + "\" class=\"table table-sm table-hover align-middle mb-0\" style=\"width:100%\">");
                sb.Append("<thead><tr>");
                sb.Append("<th>Address</th>");
                sb.Append("<th>Distance</th>");
                sb.Append("<th>Date Sold</th>");
                sb.Append("<th>Price</th>");
                sb.Append("<th>$/sqft</th>");
                sb.Append("<th>Sq Ft</th>");
                sb.Append("<th>Yr Built</th>");
                sb.Append("<th></th>"); // Street View — not sortable
                sb.Append("<th></th>"); // Add button — not sortable
                sb.Append("</tr></thead><tbody>");

                foreach (var comp in notSelected)
                {
                    string addr          = HttpUtility.HtmlEncode(comp.StreetAddress ?? "—");
                    decimal distVal      = comp.Distance ?? 999m;
                    string distDisp      = comp.Distance.HasValue ? comp.Distance.Value.ToString("F2") + " mi" : "—";
                    string saleDateDisp  = FormatCompDate(comp.SaleDate);
                    string saleDateOrder = comp.SaleDate ?? "00000000";
                    decimal priceVal     = comp.SalePrice ?? 0;
                    string priceDisp     = priceVal > 0 ? "$" + priceVal.ToString("N0") : "—";
                    decimal ppsfVal      = comp.SalePrice.HasValue && comp.BuildingSquareFeet.HasValue && comp.BuildingSquareFeet > 0
                                           ? Math.Round(comp.SalePrice.Value / comp.BuildingSquareFeet.Value, 2) : 0;
                    string ppsfDisp      = ppsfVal > 0 ? "$" + ppsfVal.ToString("N0") : "—";
                    int sqftVal          = comp.BuildingSquareFeet ?? 0;
                    string sqftDisp      = sqftVal > 0 ? sqftVal.ToString("N0") : "—";
                    string yrBuilt       = comp.YearBuilt ?? "—";

                    // Street View popover for DataTable row — click only
                    string rowSvBtn = "";
                    if (comp.Latitude.HasValue && comp.Longitude.HasValue && !string.IsNullOrEmpty(svKey))
                    {
                        string svUrl = "https://maps.googleapis.com/maps/api/streetview?size=320x180&location=" +
                                       comp.Latitude.Value + "," + comp.Longitude.Value +
                                       "&fov=90&pitch=0&source=outdoor&key=" + svKey;
                        rowSvBtn = "<a href=\"javascript:void(0)\" class=\"btn btn-sm btn-outline-secondary py-0 px-1 sv-popover-btn\" " +
                                   "data-bs-toggle=\"popover\" data-bs-trigger=\"click\" data-bs-placement=\"left\" data-bs-html=\"true\" " +
                                   "data-bs-content=\"&lt;img src=&apos;" + svUrl + "&apos; class=&apos;sv-popover-img&apos; /&gt;\" " +
                                   "title=\"" + HttpUtility.HtmlAttributeEncode(comp.StreetAddress ?? "") + "\">" +
                                   "<i class=\"ti ti-camera\"></i></a>";
                    }

                    string dtClip = HttpUtility.HtmlAttributeEncode(comp.Clip ?? "");
                    sb.Append("<tr>");
                    sb.Append("<td class=\"fw-semibold\"><a href=\"/Secure/Prospects/PropertyDetail.aspx?clip=" + dtClip + "\" target=\"_blank\" class=\"text-dark\">" + addr + " <i class=\"ti ti-external-link\" style=\"font-size:0.7em;opacity:0.6;\"></i></a></td>");
                    sb.Append("<td data-order=\"" + distVal.ToString("F4") + "\">" + distDisp + "</td>");
                    sb.Append("<td data-order=\"" + saleDateOrder + "\">" + saleDateDisp + "</td>");
                    sb.Append("<td data-order=\"" + priceVal.ToString("F2") + "\">" + priceDisp + "</td>");
                    sb.Append("<td data-order=\"" + ppsfVal.ToString("F2") + "\">" + ppsfDisp + "</td>");
                    sb.Append("<td data-order=\"" + sqftVal + "\">" + sqftDisp + "</td>");
                    sb.Append("<td>" + yrBuilt + "</td>");
                    sb.Append("<td>" + rowSvBtn + "</td>");
                    sb.Append("<td><a href=\"javascript:void(0)\" " +
                              "class=\"btn btn-outline-success btn-sm py-0 px-2 comp-toggle-btn\" " +
                              "data-prospect=\"" + prospectIdStr + "\" " +
                              "data-clip=\"" + HttpUtility.HtmlAttributeEncode(comp.Clip ?? "") + "\" " +
                              "data-action=\"add\" title=\"Add to valuation\">" +
                              "<i class=\"ti ti-plus\"></i></a></td>");
                    sb.Append("</tr>");
                }

                sb.Append("</tbody></table></div>"); // table-responsive
                sb.Append("</div>"); // collapse
            }

            sb.Append("</div></div>"); // card-body + card

            // ── Single JS engine: fetch-based toggle + valuation update + DataTable init ──
            sb.Append("<script>");
            sb.Append("(function(){");

            // IDs scoped to this prospect
            sb.Append("var PROSPECT_ID='" + prospectIdStr + "';");
            sb.Append("var SEL_LIST=document.getElementById('" + selectedListId + "');");
            sb.Append("var VAL_EL=document.getElementById('" + valuationId + "');");
            sb.Append("var COUNT_BADGE=document.getElementById('selCountBadge_" + prospectIdStr.Replace("-","") + "');");
            sb.Append("var CLEAR_BTN=document.getElementById('btnClearAll_" + prospectIdStr.Replace("-","") + "');");
            sb.Append("var TABLE_ID='#" + tableId + "';");
            string svKeyJs = HttpUtility.JavaScriptStringEncode(svKey);

            // Store removed DataTable rows by clip so we can re-add them on remove/clear
            sb.Append("var _removedRows={};"); // clip -> DataTable row data array

            // ── Street View popover init ─────────────────────────────
            sb.Append("function initSvPopovers(){");
            sb.Append("document.querySelectorAll('.sv-popover-btn').forEach(function(el){");
            sb.Append("if(bootstrap.Popover.getInstance(el))return;");
            sb.Append("new bootstrap.Popover(el,{html:true,sanitize:false,trigger:'click',placement:'left'});");
            sb.Append("el.addEventListener('show.bs.popover',function(){");
            sb.Append("document.querySelectorAll('.sv-popover-btn').forEach(function(o){");
            sb.Append("if(o!==el){var i=bootstrap.Popover.getInstance(o);if(i)i.hide();}});});});");
            sb.Append("if(!document._svOutside){document._svOutside=true;");
            sb.Append("document.addEventListener('click',function(e){");
            sb.Append("if(!e.target.closest('.sv-popover-btn')&&!e.target.closest('.popover')){");
            sb.Append("document.querySelectorAll('.sv-popover-btn').forEach(function(o){var i=bootstrap.Popover.getInstance(o);if(i)i.hide();});}});}}");

            // ── Get DataTable instance safely ────────────────────────
            sb.Append("function getDT(){");
            sb.Append("return $.fn.DataTable&&$.fn.DataTable.isDataTable(TABLE_ID)?$(TABLE_ID).DataTable():null;");
            sb.Append("}");

            // ── Build selected comps HTML from JSON response ─────────
            sb.Append("function buildSelectedList(comps){");
            sb.Append("if(!SEL_LIST)return;");
            sb.Append("if(!comps||comps.length===0){SEL_LIST.innerHTML='';return;}");
            sb.Append("var h='<div class=\"px-3 pt-3 pb-1\"><p class=\"text-muted fs-xs fw-semibold text-uppercase mb-2\">Selected Comparables</p></div>';");
            sb.Append("h+='<ul class=\"list-group list-group-flush\">';");
            sb.Append("comps.forEach(function(c){");
            sb.Append("var svBtn='';");
            sb.Append("if(c.lat&&c.lng&&'" + svKeyJs + "'){");
            sb.Append("var svUrl='https://maps.googleapis.com/maps/api/streetview?size=320x180&location='+c.lat+','+c.lng+'&fov=90&pitch=0&source=outdoor&key=" + svKeyJs + "';");
            sb.Append("svBtn='<a href=\"javascript:void(0)\" class=\"btn btn-sm btn-outline-secondary py-0 px-1 flex-shrink-0 sv-popover-btn\"'");
            sb.Append("+'data-bs-toggle=\"popover\" data-bs-trigger=\"click\" data-bs-placement=\"left\" data-bs-html=\"true\"'");
            sb.Append("+'data-bs-content=\"<img src=&apos;'+svUrl+'&apos; class=&apos;sv-popover-img&apos; />\" title=\"'+c.address+'\"><i class=\"ti ti-camera\"></i></a>';}");
            sb.Append("h+='<li class=\"list-group-item d-flex align-items-center gap-2 py-2\">';");
            sb.Append("h+='<div class=\"flex-grow-1 min-width-0\">';");
            sb.Append("h+='<div class=\"fw-semibold fs-xs\"><a href=\"/Secure/Prospects/PropertyDetail.aspx?clip='+c.clip+'\" target=\"_blank\" class=\"text-dark\">'+c.address+' <i class=\"ti ti-external-link\" style=\"font-size:0.7em;opacity:0.6;\"></i></a></div>';");
            sb.Append("h+='<div class=\"d-flex flex-wrap gap-2 mt-1\">';");
            sb.Append("if(c.dist)    h+='<span class=\"badge text-bg-primary\">'+c.dist+'</span>';");
            sb.Append("if(c.saleDate)h+='<span class=\"badge text-bg-success\">'+c.saleDate+'</span>';");
            sb.Append("if(c.price)   h+='<span class=\"badge text-bg-warning\">'+c.price+'</span>';");
            sb.Append("if(c.sqft)    h+='<span class=\"badge text-bg-light text-dark\">'+c.sqft+'</span>';");
            sb.Append("if(c.ppsf)    h+='<span class=\"badge text-bg-secondary\">'+c.ppsf+'</span>';");
            sb.Append("h+='</div></div>';");
            sb.Append("h+=svBtn;");
            sb.Append("h+='<a href=\"javascript:void(0)\" class=\"btn btn-outline-danger btn-sm py-0 px-2 flex-shrink-0 comp-toggle-btn\"'");
            sb.Append("+'data-prospect=\"'+PROSPECT_ID+'\" data-clip=\"'+c.clip+'\" data-action=\"remove\" title=\"Remove\">'");
            sb.Append("+'<i class=\"ti ti-x\"></i></a>';");
            sb.Append("h+='</li>';});");
            sb.Append("h+='</ul>';");
            sb.Append("SEL_LIST.innerHTML=h;");
            sb.Append("initSvPopovers();");
            sb.Append("wireToggleButtons(SEL_LIST);");
            sb.Append("}");

            // ── Sync DataTable after add/remove/clear ────────────────
            // On ADD:  remove the row from DataTable, store its data in _removedRows
            // On REMOVE: restore stored row data back into DataTable
            // On CLEAR: restore ALL stored rows back into DataTable
            sb.Append("function syncDataTable(action,clip,btn){");
            sb.Append("var dt=getDT(); if(!dt)return;");

            sb.Append("if(action==='add'){");
            sb.Append("if(btn){var tr=btn.closest('tr');if(tr){var r=dt.row(tr);if(r&&r.length){_removedRows[clip]=r.data();r.remove();dt.draw(false);}}}");

            sb.Append("}else if(action==='remove'){");
            sb.Append("if(_removedRows[clip]){dt.row.add(_removedRows[clip]).draw(false);delete _removedRows[clip];wireToggleButtons();}");

            sb.Append("}else if(action==='clear'){");
            sb.Append("Object.keys(_removedRows).forEach(function(c){dt.row.add(_removedRows[c]);delete _removedRows[c];});");
            sb.Append("dt.draw(false);wireToggleButtons();");
            sb.Append("}");
            sb.Append("}");

            // ── Update valuation display from JSON ───────────────────
            sb.Append("function updateValuation(val){");
            sb.Append("if(!VAL_EL)return;");
            sb.Append("if(!val||!val.success){");
            sb.Append("VAL_EL.innerHTML='<div class=\"alert alert-warning mt-3 mb-0\"><i class=\"ti ti-alert-triangle me-1\"></i>'+(val?val.errorMessage:'Unable to calculate')+'</div>';");
            sb.Append("return;}");
            sb.Append("var cc=val.confidenceColor,cl=val.confidenceLabel;");
            sb.Append("var h='<div class=\"mt-3 pt-3\" style=\"border-top:1px dashed #d1d5db;\">';");
            sb.Append("h+='<div class=\"d-flex align-items-center gap-2 mb-2\">';");
            sb.Append("h+='<i class=\"ti ti-calculator text-primary fs-18\"></i>';");
            sb.Append("h+='<span class=\"fw-bold fs-sm\">Reyla Valuation</span>';");
            sb.Append("h+='<span class=\"badge text-bg-'+cc+' ms-1\">'+cl+' Confidence</span>';");
            sb.Append("h+='<span class=\"text-muted fs-xs\">'+val.compsUsed+' comps used</span>';");
            sb.Append("h+='<a href=\"javascript:void(0)\" class=\"btn btn-outline-secondary btn-sm ms-auto py-0 px-2 fs-xs\" onclick=\"reylaRecalc()\"><i class=\"ti ti-refresh me-1\"></i>Recalculate</a>';");
            sb.Append("h+='</div>';");
            sb.Append("h+='<div class=\"d-flex align-items-baseline gap-3 mb-1\">';");
            sb.Append("h+='<h3 class=\"mb-0 text-primary fw-bold\">$'+Number(val.pointEstimate).toLocaleString()+'</h3>';");
            sb.Append("h+='<span class=\"text-muted fs-xs\">range: $'+Number(val.rangeLow).toLocaleString()+' – $'+Number(val.rangeHigh).toLocaleString()+'</span>';");
            sb.Append("h+='</div>';");
            sb.Append("h+='<div class=\"d-flex gap-3 flex-wrap\">';");
            sb.Append("h+='<span class=\"text-muted fs-xs\"><strong>$'+Number(val.weightedMedianPpsf).toLocaleString()+'/sqft</strong> weighted median</span>';");
            sb.Append("h+='<span class=\"text-muted fs-xs\"><strong>'+Number(val.subjectSqFt).toLocaleString()+' sqft</strong> subject size</span>';");
            sb.Append("if(val.subjectYearBuilt) h+='<span class=\"text-muted fs-xs\"><strong>'+val.subjectYearBuilt+'</strong> yr built</span>';");
            sb.Append("h+='</div>';");
            sb.Append("h+='<p class=\"text-muted fs-xxs mt-2 mb-0\">Based on your saved comps. Not a licensed appraisal.</p>';");
            sb.Append("h+='</div>';");
            sb.Append("VAL_EL.innerHTML=h;");
            sb.Append("}");

            // ── reylaRecalc ──────────────────────────────────────────
            sb.Append("function reylaRecalc(){");
            sb.Append("if(!VAL_EL)return;");
            sb.Append("VAL_EL.innerHTML='<div class=\"d-flex align-items-center gap-2 mt-3 pt-3\" style=\"border-top:1px dashed #d1d5db;\"><span class=\"spinner-border spinner-border-sm text-primary\"></span><span class=\"text-muted fs-xs\">Recalculating...</span></div>';");
            sb.Append("var fd=new FormData();fd.append('prospectId',PROSPECT_ID);fd.append('clip','__RECALC_ONLY__');fd.append('action','recalc');");
            sb.Append("fetch('/Secure/Prospects/CompToggle.ashx',{method:'POST',body:fd,credentials:'same-origin'})");
            sb.Append(".then(function(r){return r.json();})");
            sb.Append(".then(function(data){if(data.valuation)updateValuation(data.valuation);})");
            sb.Append(".catch(function(e){console.error(e);});");
            sb.Append("}");

            // ── Core fetch toggle function ───────────────────────────
            sb.Append("function compToggle(prospectId,clip,action,btn){");
            sb.Append("if(btn){btn.disabled=true;btn.innerHTML='<span class=\"spinner-border spinner-border-sm\"></span>';}");
            sb.Append("var fd=new FormData();");
            sb.Append("fd.append('prospectId',prospectId);");
            sb.Append("fd.append('clip',clip||'');");
            sb.Append("fd.append('action',action);");
            sb.Append("fetch('/Secure/Prospects/CompToggle.ashx',{method:'POST',body:fd,credentials:'same-origin'})");
            sb.Append(".then(function(r){");
            sb.Append("if(!r.ok){return r.text().then(function(t){throw new Error('HTTP '+r.status+': '+t.substring(0,300));});}");
            sb.Append("return r.json();})");
            sb.Append(".then(function(data){");
            sb.Append("if(!data.success){alert(data.errorMessage||'Error');if(btn){btn.disabled=false;btn.innerHTML=action==='add'?'<i class=\"ti ti-plus\"></i>':'<i class=\"ti ti-x\"></i>';}return;}");
            sb.Append("if(COUNT_BADGE)COUNT_BADGE.textContent=data.selectedCount;");
            sb.Append("if(CLEAR_BTN){if(data.selectedCount>0)CLEAR_BTN.classList.remove('d-none');else CLEAR_BTN.classList.add('d-none');}");
            // Sync DataTable BEFORE rebuilding selected list (row must still exist for removal)
            sb.Append("syncDataTable(action,clip,btn);");
            sb.Append("buildSelectedList(data.selectedComps);");
            sb.Append("if(VAL_EL)updateValuation(data.valuation);");
            sb.Append("if(btn&&action==='add'){btn.disabled=false;btn.innerHTML='<i class=\"ti ti-plus\"></i>';}");
            sb.Append("if(btn&&action==='remove'){btn.disabled=false;btn.innerHTML='<i class=\"ti ti-x\"></i>';}");
            sb.Append("if(btn&&action==='clear'){btn.disabled=false;btn.innerHTML='<i class=\"ti ti-trash me-1\"></i>Clear All';}");
            sb.Append("}).catch(function(e){");
            sb.Append("console.error('[CompToggle] fetch error:',e.message||e);");
            sb.Append("alert('Request failed. See browser console (F12) for details.');");
            sb.Append("if(btn){btn.disabled=false;btn.innerHTML=action==='add'?'<i class=\"ti ti-plus\"></i>':'<i class=\"ti ti-x\"></i>';}");
            sb.Append("});}"); // closes compToggle

            // ── Wire toggle buttons ──────────────────────────────────
            sb.Append("function wireToggleButtons(scope){");
            sb.Append("(scope||document).querySelectorAll('.comp-toggle-btn').forEach(function(btn){");
            sb.Append("if(btn._wired)return; btn._wired=true;");
            sb.Append("btn.addEventListener('click',function(e){e.preventDefault();");
            sb.Append("var action=btn.dataset.action,clip=btn.dataset.clip,pid=btn.dataset.prospect;");
            sb.Append("if(action==='clear'){if(!confirm('Remove all comps from this valuation?'))return;}");
            sb.Append("compToggle(pid,clip,action,btn);});});");
            sb.Append("}");

            // ── DataTable init via window.load ───────────────────────
            sb.Append("window.addEventListener('load',function(){");
            sb.Append("var base='/Theme/assets/plugins/datatables/';");
            sb.Append("var scripts=[base+'dataTables.min.js',base+'dataTables.bootstrap5.min.js',");
            sb.Append("base+'dataTables.responsive.min.js',base+'responsive.bootstrap5.min.js'];");
            sb.Append("function loadNext(i){");
            sb.Append("if(i>=scripts.length){");
            sb.Append("if(!$.fn.DataTable.isDataTable(TABLE_ID)){");
            sb.Append("$(TABLE_ID).DataTable({");
            sb.Append("order:[[1,'asc']],pageLength:10,");
            sb.Append("lengthMenu:[[10,25,50,-1],[10,25,50,'All']],responsive:true,");
            sb.Append("columnDefs:[{targets:[-1,-2],orderable:false,searchable:false}],");
            sb.Append("drawCallback:function(){initSvPopovers();wireToggleButtons();}");
            sb.Append("});}else{initSvPopovers();wireToggleButtons();}return;}");
            sb.Append("if(document.querySelector('script[src=\"'+scripts[i]+'\"]')){loadNext(i+1);return;}");
            sb.Append("var s=document.createElement('script');s.src=scripts[i];");
            sb.Append("s.onload=function(){loadNext(i+1);};document.body.appendChild(s);}");
            sb.Append("loadNext(0);");
            sb.Append("initSvPopovers();wireToggleButtons();");
            sb.Append("});");

            sb.Append("})();"); // end IIFE
            sb.Append("</script>");

            phCompMgmt.Controls.Add(new LiteralControl(sb.ToString()));
        }

        private string FormatCompDate(string saleDateStr)
        {
            if (string.IsNullOrEmpty(saleDateStr)) return "";
            string clean = saleDateStr.Replace("-", "");
            if (clean.Length < 6) return saleDateStr;
            if (!int.TryParse(clean.Substring(0, 4), out int year) ||
                !int.TryParse(clean.Substring(4, 2), out int month)) return saleDateStr;
            var months = new[]{"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"};
            if (month < 1 || month > 12) return year.ToString();
            return months[month - 1] + " " + year;
        }

        // ---------------------------------------------------------------
        // HandleToggleComp — processes + / × clicks on comp rows
        // ---------------------------------------------------------------

        private void HandleToggleComp()
        {
            string clip        = hdnToggleClip.Value?.Trim();
            string action      = hdnToggleAction.Value?.Trim();
            string prospectStr = hdnToggleProspectId.Value?.Trim();

            if (string.IsNullOrEmpty(clip) || string.IsNullOrEmpty(action)) return;
            if (!Guid.TryParse(prospectStr, out Guid prospectId)) return;

            var orgId = GetCurrentOrganizationId();
            var svc   = new ProspectService();

            if (clip == "__CLEAR_ALL__")
                svc.ClearAllComps(prospectId, orgId);
            else
                svc.ToggleComp(prospectId, clip, action == "add", orgId);

            if (_profile != null && !_profile.PropertyNotFound)
                LoadProspect();

            BindProspectCard();
            BindCompMgmt();
        }

        protected void btnRefreshData_Click(object sender, EventArgs e)
        {
            var clip  = hdnClip.Value;
            var orgId = GetCurrentOrganizationId();

            if (!string.IsNullOrWhiteSpace(clip) && orgId != Guid.Empty)
                _snapshot.ForceRefreshByClip(clip, orgId);

            // Redirect to self — clean GET so postback state is gone
            var qs = Request.QueryString.ToString();
            Response.Redirect(Request.AppRelativeCurrentExecutionFilePath +
                              (string.IsNullOrEmpty(qs) ? "" : "?" + qs));
        }

        // ── Document fetch postback ───────────────────────────────────────

        protected void rptDocs_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "FetchDoc") return;

            var rowKey = e.CommandArgument?.ToString();
            if (string.IsNullOrWhiteSpace(rowKey)) return;

            var rows = DeserialiseDocRows(hdnDocResult.Value);
            var row  = rows?.FirstOrDefault(r => r.RowKey == rowKey);
            if (row == null) return;

            pnlDocViewer.Visible = true;
            litDocTitle.Text     = HttpUtility.HtmlEncode(
                $"{row.DocumentTypeDescription ?? row.DocumentType} — {FormatDate(row.RecordingDate)} — Doc #{row.DocumentNumber}");

            // FipsCode may be missing on properties added before the FipsCode column was
            // populated. Fall back to the hidden field which is always set from the profile.
            var fipsCode = !string.IsNullOrWhiteSpace(row.FipsCode)
                ? row.FipsCode
                : hdnFipsCode.Value;

            if (string.IsNullOrWhiteSpace(fipsCode))
            {
                ShowDocError(
                    "Cannot retrieve document: the FIPS county code for this property is not on record. " +
                    "Click 'Refresh Data' at the top of the page to re-pull property data from CoreLogic, " +
                    "then try again.");
                ScriptManager.RegisterStartupScript(this, GetType(), "showDocs",
                    "document.querySelectorAll('.tab-btn')[2].click();", true);
                return;
            }

            try
            {
                var result = _api.GetDocumentImage(
                    fipsCode,
                    row.RecordingDate,
                    row.DocumentNumber);

                if (result?.Images != null && result.Images.Count > 0)
                {
                    pnlDocPages.Visible  = true;
                    pnlDocError.Visible  = false;
                    rptDocPages.DataSource = result.Images;
                    rptDocPages.DataBind();
                }
                else
                {
                    var msg = result?.StatusMsg ?? "No document pages returned.";
                    ShowDocError($"Document not available: {msg}");
                }
            }
            catch (ArgumentException ax)
            {
                ShowDocError($"Missing required document parameter: {ax.Message}");
            }
            catch (CoreLogicApiException ex)
            {
                ShowDocError(ex.HttpStatusCode == 404
                    ? "Document image not found in CoreLogic's archive. Coverage may not extend to this county or date range."
                    : $"Error retrieving document (HTTP {ex.HttpStatusCode}): {ex.Message}");
            }

            // Switch to documents tab via JS
            ScriptManager.RegisterStartupScript(this, GetType(), "showDocs",
                "document.querySelectorAll('.tab-btn')[2].click();", true);
        }

        private void ShowDocError(string msg)
        {
            pnlDocError.Visible  = true;
            pnlDocPages.Visible  = false;
            litDocError.Text     = HttpUtility.HtmlEncode(msg);
        }

        // ── Formatting helpers (called from ASPX inline <%# %>) ──────────

        protected string FormatDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "<span class='na'>—</span>";

            // YYYYMMDD
            if (raw.Length == 8 && DateTime.TryParseExact(raw, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d1))
                return d1.ToString("MMM d, yyyy");

            // YYYY-MM-DD or other parseable
            if (DateTime.TryParse(raw, out var d2))
                return d2.ToString("MMM d, yyyy");

            return HttpUtility.HtmlEncode(raw);
        }

        protected string FormatMoney(object val)
        {
            if (val == null) return Na();
            if (val is decimal d && d != 0) return $"${d:N0}";
            if (decimal.TryParse(val.ToString(), out var parsed) && parsed != 0) return $"${parsed:N0}";
            return Na();
        }

        protected string FormatRate(object val)
        {
            if (val == null) return Na();
            if (val is decimal d && d != 0) return $"{d:N3}%";
            if (decimal.TryParse(val.ToString(), out var parsed) && parsed != 0) return $"{parsed:N3}%";
            return Na();
        }

        protected bool IsOpenPermit(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            return status.IndexOf("Open",    StringComparison.OrdinalIgnoreCase) >= 0 ||
                   status.IndexOf("Expired", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static string Money(decimal? val) =>
            val.HasValue && val.Value != 0 ? $"${val.Value:N0}" : Na();

        private static string F(string val) =>
            string.IsNullOrWhiteSpace(val)
                ? Na()
                : HttpUtility.HtmlEncode(val);

        private static string Na() => "<span class='na'>—</span>";

        private static string Badge(string text, string colour) =>
            $"<span class='signal-badge signal-{colour}'>{HttpUtility.HtmlEncode(text)}</span>";

        private static string RiskLabel(decimal? score, string label)
        {
            if (!score.HasValue && string.IsNullOrWhiteSpace(label)) return Na();
            var display = string.IsNullOrWhiteSpace(label)
                ? score?.ToString("N0") ?? "—"
                : $"{label} ({score:N0})";
            return HttpUtility.HtmlEncode(display);
        }

        private static string MakeRowKey(string recordingDate, string documentNumber) =>
            $"{recordingDate}_{documentNumber}".Replace(" ", "_").Replace("/", "-");

        private static List<DocRow> DeserialiseDocRows(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<DocRow>();
            try { return JsonConvert.DeserializeObject<List<DocRow>>(json) ?? new List<DocRow>(); }
            catch { return new List<DocRow>(); }
        }

        // ── API diagnostics ───────────────────────────────────────────────

        private void BindDiagnostics()
        {
            // Only show to admins after a live API pull — not on cached loads
            if (!Roles.IsUserInRole("Admin")) return;
            if (_profile == null || _profile.IsFromCache) return;

            var diag = _snapshot.EndpointDiagnostics;
            if (diag == null || diag.Count == 0) return;

            var sb = new System.Text.StringBuilder();
            sb.Append("<details style=\"margin-bottom:18px;\">");
            sb.Append("<summary style=\"cursor:pointer;font-size:13px;color:#6b7280;padding:8px 0;\">▸ CoreLogic API diagnostics (fresh pull)</summary>");
            sb.Append("<div style=\"background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;padding:12px 16px;margin-top:6px;\">");
            sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:12px;font-family:monospace;\">");
            sb.Append("<tr><th style=\"text-align:left;padding:4px 12px 4px 0;color:#9ca3af;\">Endpoint</th>");
            sb.Append("<th style=\"text-align:left;padding:4px 0;color:#9ca3af;\">Result</th></tr>");

            foreach (var kv in diag)
            {
                bool ok      = kv.Value == "OK";
                string color = ok ? "#16a34a" : "#dc2626";
                string icon  = ok ? "✓" : "✗";
                string val   = HttpUtility.HtmlEncode(kv.Value.Length > 120
                                   ? kv.Value.Substring(0, 120) + "…"
                                   : kv.Value);
                sb.Append($"<tr>");
                sb.Append($"<td style=\"padding:3px 12px 3px 0;color:#374151;\">{HttpUtility.HtmlEncode(kv.Key)}</td>");
                sb.Append($"<td style=\"padding:3px 0;color:{color};\">{icon} {val}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table></div></details>");
            phDiagnostics.Controls.Add(new LiteralControl(sb.ToString()));
        }

        // ── Auth helpers ──────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var user = System.Web.Security.Membership.GetUser();
            return user != null ? (Guid)user.ProviderUserKey : Guid.Empty;
        }

        private Guid GetCurrentOrganizationId()
        {
            using (var db = new DCReyla())
            {
                var userId  = GetCurrentUserId();
                var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                return profile?.OrganizationId ?? Guid.Empty;
            }
        }

        // ── Owner Contact — bind ──────────────────────────────────────────
        // Read-only display of primary contact from People tab.
        // PreferredContact lives on the Contact record, saved via Ajax.

        private void BindOwnerContact()
        {
            if (_profile == null || _profile.PropertyNotFound || _profile.Property == null)
                return;

            var propertyId = _profile.Property.PropertyId;
            var orgId      = GetCurrentOrganizationId();

            Contact primaryContact   = null;
            string  preferredContact = null;

            using (var db = new DCReyla())
            {
                var link = db.PropertyContacts
                    .FirstOrDefault(pc =>
                        pc.PropertyId     == propertyId &&
                        pc.OrganizationId == orgId      &&
                        pc.IsPrimary      == true       &&
                        pc.IsDeleted      == false);

                if (link != null)
                {
                    primaryContact = db.Contacts
                        .FirstOrDefault(c => c.ContactId == link.ContactId && c.IsDeleted == false);
                    preferredContact = primaryContact?.PreferredContact;
                }
            }

            if (primaryContact == null)
            {
                pnlContactEmpty.Visible = true;
                pnlContactData.Visible  = false;
                hdnContactSaved.Value   = string.Empty;
            }
            else
            {
                pnlContactEmpty.Visible = false;
                pnlContactData.Visible  = true;

                // Seed contactId so JS Ajax call knows which contact to update
                hdnContactSaved.Value = primaryContact.ContactId.ToString();

                string fullName = ((primaryContact.FirstName ?? string.Empty) + " " +
                                   (primaryContact.LastName  ?? string.Empty)).Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = primaryContact.CompanyName;

                litContactName.Text      = ContactEncode(fullName);
                litContactPhone.Text     = FormatContactPhone(primaryContact.Phone);
                litContactEmail.Text     = FormatContactEmail(primaryContact.Email);
                litContactMailing.Text   = ContactEncode(primaryContact.CompanyName);
                litContactNotes.Text     = string.Empty;
                litPreferredContact.Text = string.Empty;

                ddlPreferredContact.ClearSelection();
                if (!string.IsNullOrEmpty(preferredContact))
                {
                    var item = ddlPreferredContact.Items.FindByValue(preferredContact);
                    if (item != null) item.Selected = true;
                }
            }

            // Keep hidden edit fields empty — no longer used
            txtContactName.Text    = string.Empty;
            txtContactPhone.Text   = string.Empty;
            txtContactEmail.Text   = string.Empty;
            txtContactMailing.Text = string.Empty;
            txtContactNotes.Text   = string.Empty;
        }

        private void RenderContactReadView(ProspectOwnerContact contact)
        {
            // Use Style not Visible — pnlContactEdit renders in DOM (style=display:none)
            // so JS can toggle it. Visible=false would suppress rendering entirely.
            pnlContactEdit.Style["display"] = "none";
            pnlContactRead.Style.Remove("display");
            btnEditContact.Visible    = true;
            btnCancelContact.Visible  = false;

            bool hasData = !string.IsNullOrWhiteSpace(contact.Phone)     ||
                           !string.IsNullOrWhiteSpace(contact.Email)     ||
                           !string.IsNullOrWhiteSpace(contact.ContactName);

            pnlContactEmpty.Visible = !hasData;
            pnlContactData.Visible  =  hasData;

            if (hasData)
            {
                litContactName.Text      = ContactEncode(contact.ContactName);
                litContactPhone.Text     = FormatContactPhone(contact.Phone);
                litContactEmail.Text     = FormatContactEmail(contact.Email);
                litContactMailing.Text   = ContactEncode(contact.MailingAddress);
                litPreferredContact.Text = FormatPreferredContact(contact.PreferredContact);
                pnlContactNotes.Visible  = !string.IsNullOrWhiteSpace(contact.Notes);
                litContactNotes.Text     = ContactEncode(contact.Notes);
            }
        }

        private void RenderContactEditView(ProspectOwnerContact contact)
        {
            pnlContactRead.Style["display"]      = "none";
            pnlContactEdit.Style.Remove("display");
            btnEditContact.Visible               = false;
            btnCancelContact.Visible             = true;
            pnlContactSaveAlert.Style["display"] = "none";

            txtContactName.Text    = contact.ContactName    ?? string.Empty;
            txtContactPhone.Text   = contact.Phone          ?? string.Empty;
            txtContactEmail.Text   = contact.Email          ?? string.Empty;
            txtContactMailing.Text = contact.MailingAddress ?? string.Empty;
            txtContactNotes.Text   = contact.Notes          ?? string.Empty;

            ddlPreferredContact.ClearSelection();
            if (!string.IsNullOrEmpty(contact.PreferredContact))
            {
                var item = ddlPreferredContact.Items.FindByValue(contact.PreferredContact);
                if (item != null) item.Selected = true;
            }
        }

        // ── Owner Contact event handlers ──────────────────────────────────
        // Edit and Cancel are now handled purely by JS (contactEdit/contactCancel).
        // These server methods are kept as safety fallbacks but are no longer
        // wired to visible buttons.

        protected void btnEditContact_Click(object sender, EventArgs e) { /* JS-only now */ }

        protected void btnCancelContact_Click(object sender, EventArgs e) { /* JS-only now */ }

        protected void btnSaveContact_Click(object sender, EventArgs e)
        {
            // No-op — PreferredContact is saved via Ajax (setpreferred action on PropertyContactHandler.ashx)
        }

        // ── Owner Contact format helpers ──────────────────────────────────

        private string ContactEncode(string s) =>
            string.IsNullOrWhiteSpace(s)
                ? "<span class='text-muted'>—</span>"
                : HttpUtility.HtmlEncode(s);

        private string FormatContactPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "<span class='text-muted'>—</span>";
            return $"<a href='tel:{HttpUtility.HtmlAttributeEncode(phone)}' class='text-decoration-none'>" +
                   $"<i class='ti ti-phone me-1'></i>{HttpUtility.HtmlEncode(phone)}</a>";
        }

        private string FormatContactEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "<span class='text-muted'>—</span>";
            return $"<a href='mailto:{HttpUtility.HtmlAttributeEncode(email)}' class='text-decoration-none'>" +
                   $"<i class='ti ti-mail me-1'></i>{HttpUtility.HtmlEncode(email)}</a>";
        }

        private string FormatPreferredContact(string pref)
        {
            switch (pref)
            {
                case "Phone":    return "<span class='badge text-bg-primary'><i class='ti ti-phone me-1'></i>Phone</span>";
                case "Email":    return "<span class='badge text-bg-info'><i class='ti ti-mail me-1'></i>Email</span>";
                case "Mail":     return "<span class='badge text-bg-secondary'><i class='ti ti-mail-forward me-1'></i>Mail</span>";
                case "InPerson": return "<span class='badge text-bg-success'><i class='ti ti-user me-1'></i>In Person</span>";
                default:         return "<span class='text-muted'>—</span>";
            }
        }

        // ── Outreach Notes — seed JS ──────────────────────────────────────
        // Notes are rendered entirely by JS via NoteToggle.ashx fetch calls.
        // BindNotes() just passes the prospectId to the hidden field so the
        // JS engine knows which prospect to operate on, and pre-renders the
        // initial state from the server so the first tab open is instant.

        private void BindNotes()
        {
            if (_prospect == null)
            {
                hdnNotesProspectId.Value = string.Empty;
                // Emit JS to hide content, show empty state
                ScriptManager.RegisterStartupScript(this, GetType(), "notesInit",
                    "document.getElementById('contactNotesContent').style.display='none';" +
                    "document.getElementById('contactNotesEmpty').style.display='';", true);
                return;
            }

            hdnNotesProspectId.Value = _prospect.ProspectId.ToString();

            // Build initial notes JSON for first render without a fetch round-trip
            var svc   = new ProspectNoteService();
            var notes = svc.GetNotes(_prospect.ProspectId);

            string contactedJs;
            using (var db = new DCReyla())
            {
                var p = db.Prospects.FirstOrDefault(x => x.ProspectId == _prospect.ProspectId);
                if (p?.ContactedDate != null)
                    contactedJs = string.Format("{{isContacted:true,dateDisplay:'{0}',contactedNotes:'{1}'}}",
                        p.ContactedDate.Value.ToString("MMM d, yyyy").Replace("'", "\\'"),
                        (p.ContactedNotes ?? string.Empty).Replace("'", "\\'"));
                else
                    contactedJs = "{isContacted:false,dateDisplay:null,contactedNotes:''}";
            }

            var notesJson = Newtonsoft.Json.JsonConvert.SerializeObject(
                notes.Select(n => new {
                    noteId       = n.ProspectNoteId.ToString(),
                    noteText     = n.NoteText,
                    authorName   = n.AuthorName,
                    createdLocal = n.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
                    updatedLocal = n.UpdatedAtUtc.HasValue
                                   ? n.UpdatedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt")
                                   : (string)null,
                    isEdited     = n.UpdatedAtUtc.HasValue
                }).ToList());

            // Deal notes — load if this prospect has a deal
            string dealNotesJson = "[]";
            string dealIdJs      = "null";
            if (_prospect.DealId.HasValue)
            {
                dealIdJs = "'" + _prospect.DealId.Value.ToString() + "'";
                try
                {
                    var dealSvc  = new DealService();
                    var dealNotes = dealSvc.GetDealNotes(_prospect.DealId.Value);
                    dealNotesJson = Newtonsoft.Json.JsonConvert.SerializeObject(
                        dealNotes.Select(n => new {
                            noteId       = n.DealNoteId.ToString(),
                            dealId       = n.DealId.ToString(),
                            noteText     = n.NoteText,
                            authorName   = n.AuthorName,
                            createdLocal = n.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
                            updatedLocal = n.UpdatedAtUtc.HasValue
                                           ? n.UpdatedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt")
                                           : (string)null,
                            isEdited     = n.UpdatedAtUtc.HasValue
                        }).ToList());
                }
                catch { /* non-fatal */ }
            }

            // Emit JS to show content and seed the UI
            string initScript = string.Format(
                "document.getElementById('contactNotesContent').style.display='';" +
                "document.getElementById('contactNotesEmpty').style.display='none';" +
                "NOTES_PROSPECT_ID='{0}';" +
                "DEAL_ID={1};" +
                "renderNotes({2});" +
                "renderContacted({3});" +
                "renderDealNotesFromServer({4});",
                _prospect.ProspectId.ToString(),
                dealIdJs,
                notesJson,
                contactedJs,
                dealNotesJson);

            ScriptManager.RegisterStartupScript(this, GetType(), "notesInit", initScript, true);
        }

    }   // end PropertyDetail class

    // ── Transfer row DTO (projected flat for rptTransfers Eval binding) ─────

    public class TransferRow
    {
        public string   SaleDate       { get; set; }
        public string   BuyerName      { get; set; }
        public string   SellerName     { get; set; }
        public decimal? SaleAmount     { get; set; }
        public string   DeedType       { get; set; }
        public string   DocumentNumber { get; set; }
    }

    // ── Mortgage row DTO (projected flat for rptMortgages Eval binding) ──────

    public class MortgageRow
    {
        public string   OriginationDate { get; set; }
        public string   LenderName      { get; set; }
        public decimal? LoanAmount      { get; set; }
        public string   LoanType        { get; set; }
        public decimal? InterestRate    { get; set; }
        public string   LoanPosition    { get; set; }
        public string   MaturityDate    { get; set; }
    }

    // ── Doc row DTO (serialised into hdnDocResult hidden field) ──────────

    public class DocRow
    {
        public string RowKey                  { get; set; }
        public string DocumentNumber          { get; set; }
        public string RecordingDate           { get; set; }
        public string DocumentType            { get; set; }
        public string DocumentTypeDescription { get; set; }
        public string TransactionType         { get; set; }
        public string BuyerName               { get; set; }
        public string SellerName              { get; set; }
        public string LenderName              { get; set; }
        public string FipsCode                { get; set; }
    }

}
