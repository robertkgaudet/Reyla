using System;
using System.Collections.Generic;
using System.Linq;

namespace Reyla.Services
{
    public class DigestService
    {
        public class DigestResult
        {
            public DateTime SinceUtc             { get; set; }
            public DateTime GeneratedAt          { get; set; }
            public int      WindowDays           { get; set; }
            public int      ActiveDeals          { get; set; }
            public int      DealsInLead          { get; set; }
            public int      DealsInLOI           { get; set; }
            public int      DealsUnderContract   { get; set; }
            public int      DealsClosedPeriod    { get; set; }
            public int      DealsMoved           { get; set; }
            public int      NewProspects         { get; set; }
            public int      NotesAdded           { get; set; }
            public int      ContactsMade         { get; set; }
            public int      TotalActiveProspects { get; set; }
            public List<StaleDealRow>     StaleDeals     { get; set; } = new List<StaleDealRow>();
            public List<OutreachGapRow>   OutreachGaps   { get; set; } = new List<OutreachGapRow>();
            public List<SignalAlertRow>   SignalAlerts   { get; set; } = new List<SignalAlertRow>();
            public List<UpcomingCloseRow> UpcomingCloses { get; set; } = new List<UpcomingCloseRow>();
            public List<ActivityLogEntry> RecentActivity { get; set; } = new List<ActivityLogEntry>();
        }

        public class StaleDealRow
        {
            public Guid   DealId          { get; set; }
            public string Title           { get; set; }
            public string Stage           { get; set; }
            public int    DaysInStage     { get; set; }
            public string PropertyAddress { get; set; }
            public string Clip            { get; set; }
        }

        public class OutreachGapRow
        {
            public Guid   ProspectId       { get; set; }
            public string Address          { get; set; }
            public string CityLine         { get; set; }
            public int    DaysSinceContact { get; set; }
            public string Clip             { get; set; }
        }

        public class SignalAlertRow
        {
            public string Address    { get; set; }
            public string Clip       { get; set; }
            public string SignalType { get; set; }
            public string Detail     { get; set; }
            public string Color      { get; set; }
        }

        public class UpcomingCloseRow
        {
            public Guid     DealId         { get; set; }
            public string   Title          { get; set; }
            public string   Stage          { get; set; }
            public DateTime CloseDate      { get; set; }
            public int      DaysUntilClose { get; set; }
            public decimal? AskingPrice    { get; set; }
            public string   Clip           { get; set; }
        }

        public DigestResult Build(Guid organizationId, int windowDays = 1, int staleThresholdDays = 14, int gapThresholdDays = 30)
        {
            var sinceUtc = DateTime.UtcNow.AddDays(-windowDays);
            var now      = DateTime.UtcNow;
            var result   = new DigestResult { SinceUtc = sinceUtc, GeneratedAt = now, WindowDays = windowDays };

            using (var db = new DCReyla())
            {
                // Activity counters
                try
                {
                    var actSvc   = new ActivityService();
                    var activity = actSvc.GetDigestActivity(organizationId, sinceUtc);
                    result.RecentActivity    = activity.Take(25).ToList();
                    result.NewProspects      = activity.Count(a => a.ActivityType == ActivityType.ProspectCreated);
                    result.NotesAdded        = activity.Count(a => a.ActivityType == ActivityType.NoteAdded || a.ActivityType == ActivityType.DealNoteAdded);
                    result.ContactsMade      = activity.Count(a => a.ActivityType == ActivityType.ProspectContacted);
                    result.DealsMoved        = activity.Count(a => a.ActivityType == ActivityType.DealStageChanged || a.ActivityType == ActivityType.DealCreated);
                    result.DealsClosedPeriod = activity.Count(a => a.ActivityType == ActivityType.DealClosed);
                }
                catch { }

                // Pipeline snapshot
                var activeDeals = new List<Deal>();
                try
                {
                    activeDeals = db.Deals.Where(d => d.OrganizationId == organizationId && d.IsDeleted == false).ToList();
                    result.ActiveDeals        = activeDeals.Count(d => d.Stage != DealService.Stages.Dead);
                    result.DealsInLead        = activeDeals.Count(d => d.Stage == DealService.Stages.Lead);
                    result.DealsInLOI         = activeDeals.Count(d => d.Stage == DealService.Stages.LOI);
                    result.DealsUnderContract = activeDeals.Count(d => d.Stage == DealService.Stages.UnderContract);
                }
                catch { }

                // Active prospects
                var activeProspects = new List<Prospect>();
                try
                {
                    activeProspects = db.Prospects.Where(p => p.OrganizationId == organizationId && p.IsDeleted == false && p.IsActive == true).ToList();
                    result.TotalActiveProspects = activeProspects.Count;
                }
                catch { }

                // Stale deals
                try
                {
                    foreach (var deal in activeDeals.Where(d => d.Stage != DealService.Stages.Dead && d.Stage != DealService.Stages.Closed))
                    {
                        var lastMove  = db.ActivityLogs
                            .Where(a => a.OrganizationId == organizationId &&
                                       (a.ActivityType == ActivityType.DealStageChanged || a.ActivityType == ActivityType.DealCreated) &&
                                        a.EntityId == deal.DealId)
                            .OrderByDescending(a => a.CreatedAtUtc).FirstOrDefault();
                        var since     = lastMove?.CreatedAtUtc ?? deal.CreatedAtUtc;
                        int daysStale = (int)(now - since).TotalDays;
                        if (daysStale < staleThresholdDays) continue;
                        string clip = null;
                        try { if (deal.ProspectId.HasValue) { var pr = db.Prospects.FirstOrDefault(p => p.ProspectId == deal.ProspectId.Value); if (pr != null) { var prop = db.Properties.FirstOrDefault(p => p.PropertyId == pr.PropertyId); clip = prop?.Clip; } } } catch { }
                        result.StaleDeals.Add(new StaleDealRow { DealId = deal.DealId, Title = deal.Title, Stage = deal.Stage, DaysInStage = daysStale, PropertyAddress = deal.PropertyAddress, Clip = clip });
                    }
                    result.StaleDeals = result.StaleDeals.OrderByDescending(s => s.DaysInStage).Take(5).ToList();
                }
                catch { }

                // Outreach gaps
                try
                {
                    foreach (var prospect in activeProspects)
                    {
                        int daysSince = prospect.ContactedDate.HasValue ? (int)(now.Date - prospect.ContactedDate.Value).TotalDays : -1;
                        if (daysSince != -1 && daysSince < gapThresholdDays) continue;
                        string addr = "Unknown", cityLine = "", clip = null;
                        try { var prop = db.Properties.FirstOrDefault(p => p.PropertyId == prospect.PropertyId); if (prop != null) { addr = prop.StreetAddress ?? "Unknown"; cityLine = (prop.CityNameRaw ?? "") + (!string.IsNullOrEmpty(prop.ZipCodeRaw) ? " " + prop.ZipCodeRaw : ""); clip = prop.Clip; } } catch { }
                        result.OutreachGaps.Add(new OutreachGapRow { ProspectId = prospect.ProspectId, Address = addr, CityLine = cityLine, DaysSinceContact = daysSince, Clip = clip });
                    }
                    result.OutreachGaps = result.OutreachGaps.OrderByDescending(g => g.DaysSinceContact).Take(8).ToList();
                }
                catch { }

                // Motivated seller signals
                try
                {
                    var propIds = activeProspects.Select(p => p.PropertyId).ToList();
                    var snaps   = db.PropertySnapshots.Where(s => propIds.Contains(s.PropertyId) && s.IsDeleted == false).ToList()
                                    .GroupBy(s => s.PropertyId).Select(g => g.OrderByDescending(s => s.CreatedAtUtc).First()).ToList();
                    foreach (var snap in snaps)
                    {
                        string addr = null, clip = null;
                        try { var prop = db.Properties.FirstOrDefault(p => p.PropertyId == snap.PropertyId); addr = prop?.StreetAddress; clip = prop?.Clip; } catch { }
                        try { var liens = db.PropertySnapshotInvoluntaryLiens.Where(l => l.PropertySnapshotId == snap.PropertySnapshotId).ToList(); if (liens.Any()) result.SignalAlerts.Add(new SignalAlertRow { Address = addr ?? "Unknown", Clip = clip, SignalType = "Lien", Detail = liens.Count + " involuntary lien" + (liens.Count > 1 ? "s" : ""), Color = "danger" }); } catch { }
                        try { var tax = db.PropertySnapshotTaxAssessments.Where(t => t.PropertySnapshotId == snap.PropertySnapshotId && !string.IsNullOrEmpty(t.TaxDelinquentYear)).FirstOrDefault(); if (tax != null) result.SignalAlerts.Add(new SignalAlertRow { Address = addr ?? "Unknown", Clip = clip, SignalType = "TaxDelinquent", Detail = "Tax delinquent since " + tax.TaxDelinquentYear, Color = "danger" }); } catch { }
                    }
                    result.SignalAlerts = result.SignalAlerts.GroupBy(s => s.Clip ?? s.Address).Select(g => g.First()).OrderByDescending(s => s.Color == "danger").Take(6).ToList();
                }
                catch { }

                // Upcoming closes
                try
                {
                    foreach (var deal in activeDeals.Where(d => d.CloseDate.HasValue && d.CloseDate.Value >= now.Date && d.CloseDate.Value <= now.Date.AddDays(30) && d.Stage != DealService.Stages.Dead).OrderBy(d => d.CloseDate).Take(5))
                    {
                        string clip = null;
                        try { if (deal.ProspectId.HasValue) { var pr = db.Prospects.FirstOrDefault(p => p.ProspectId == deal.ProspectId.Value); var prop = pr != null ? db.Properties.FirstOrDefault(p => p.PropertyId == pr.PropertyId) : null; clip = prop?.Clip; } } catch { }
                        result.UpcomingCloses.Add(new UpcomingCloseRow { DealId = deal.DealId, Title = deal.Title, Stage = deal.Stage, CloseDate = deal.CloseDate.Value, DaysUntilClose = (int)(deal.CloseDate.Value - now.Date).TotalDays, AskingPrice = deal.AskingPrice, Clip = clip });
                    }
                }
                catch { }
            }

            return result;
        }
    }
}
