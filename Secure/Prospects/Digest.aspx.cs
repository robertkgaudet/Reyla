using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Reyla.Services;

namespace Reyla.Secure.Prospects
{
    public partial class Digest : Page
    {
        protected DigestService.DigestResult _digest;
        protected string _userFullName  = "";
        protected string _userEmail     = "";
        protected string _orgName       = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadDigest();
        }

        private void LoadDigest()
        {
            var orgId = GetOrgId();
            if (orgId == Guid.Empty) return;

            int window = 1;
            if (int.TryParse(Request.QueryString["days"], out int qd) && qd > 0 && qd <= 30)
                window = qd;

            var svc = new DigestService();
            _digest = svc.Build(orgId, windowDays: window, staleThresholdDays: 14, gapThresholdDays: 30);
        }

        private Guid GetOrgId()
        {
            using (var db = new DCReyla())
            {
                var user = Membership.GetUser();
                if (user == null) return Guid.Empty;

                _userEmail = user.Email ?? "";
                var uid    = (Guid)user.ProviderUserKey;
                var prof   = db.Profiles.FirstOrDefault(p => p.UserId == uid);
                if (prof == null) return Guid.Empty;

                _userFullName = ((prof.FirstName ?? "") + " " + (prof.LastName ?? "")).Trim();
                if (string.IsNullOrEmpty(_userFullName)) _userFullName = _userEmail;

                var orgId = prof.OrganizationId ?? Guid.Empty;
                if (orgId != Guid.Empty)
                {
                    var org = db.Organizations.FirstOrDefault(o => o.OrganizationId == orgId);
                    _orgName = org?.Name ?? "";
                }

                return orgId;
            }
        }

        protected string Ago(int days) =>
            days == -1 ? "Never contacted" :
            days == 0  ? "Today" :
            days == 1  ? "Yesterday" :
                         days + " days ago";

        protected string StageLabel(string stage)
        {
            switch (stage)
            {
                case "Lead":          return "Lead";
                case "Qualified":     return "Qualified";
                case "LOI":           return "LOI";
                case "UnderContract": return "Under Contract";
                case "Closed":        return "Closed";
                case "Dead":          return "Dead";
                default:              return stage ?? "--";
            }
        }

        protected string StageBadgeColor(string stage)
        {
            switch (stage)
            {
                case "Lead":          return "secondary";
                case "Qualified":     return "primary";
                case "LOI":           return "info";
                case "UnderContract": return "warning";
                case "Closed":        return "success";
                case "Dead":          return "danger";
                default:              return "secondary";
            }
        }
    }
}
