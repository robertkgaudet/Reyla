<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MasterPageSidenavMenu.ascx.cs" Inherits="Reyla.UserControls.MasterPageSidenavMenu" %>

<%-- Navigation bar — only rendered for authenticated users --%>
<asp:Panel ID="pnlNav" runat="server">

<nav class="topnav-bar navbar navbar-expand-lg">
    <div class="container-fluid px-3">

        <!-- Mobile toggle -->
        <button class="navbar-toggler d-lg-none" type="button" data-bs-toggle="collapse"
                data-bs-target="#topnavMenu" aria-controls="topnavMenu"
                aria-expanded="false" aria-label="Toggle navigation">
            <i data-lucide="menu" class="fs-18"></i>
        </button>

        <!-- Mobile user name -->
        <span class="fw-semibold ms-auto me-2 d-lg-none">
            <i class="ti ti-user-circle fs-15 me-1 align-middle"></i>
            <asp:Label ID="lblFullNameMobile" runat="server"></asp:Label>
        </span>

        <div class="collapse navbar-collapse" id="topnavMenu">
            <ul class="navbar-nav align-items-center gap-1">
                <li class="nav-item">
                    <a class="nav-link" href="/Secure/Prospects/Digest">
                        <i class="ti ti-report-analytics fs-15 me-1 align-middle"></i>
                        <span>Digest</span>
                    </a>
                </li>
                <li class="nav-item">
                    <a class="nav-link" href="/Secure/Prospects/Pipeline">
                        <i class="ti ti-layout-kanban fs-15 me-1 align-middle"></i>
                        <span>Pipeline</span>
                    </a>
                </li>
                <li class="nav-item dropdown">
                    <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
                        <i class="ti ti-building-estate fs-15 me-1 align-middle"></i>
                        <span>Current Prospects</span>
                    </a>
                    <ul class="dropdown-menu">
                        <li><a class="dropdown-item" href="/Secure/Index"><i class="ti ti-map fs-15 me-1 align-middle"></i><span>Map</span></a></li>
                        <li><a class="dropdown-item" href="/Secure/Prospects/Prospects"><i class="ti ti-building-estate fs-15 me-1 align-middle"></i><span>List</span></a></li>
                    </ul>
                </li>
                <li class="nav-item">
                    <a class="nav-link" href="/Secure/Prospects/ProspectMap">
                        <i class="ti ti-home fs-15 me-1 align-middle"></i>
                        <span>Prospecting</span>
                    </a>
                </li>
				<li class="nav-item">
					<a class="nav-link" href="/Secure/Contacts/Index.aspx">
						<i class="ti ti-users fs-15 me-1 align-middle"></i>
						<span>Contacts</span>
					</a>
				</li>
                <li class="nav-item">
                    <a class="nav-link" href="/Secure/Prospects/Search">
                        <i class="ti ti-circle-plus fs-15 me-1 align-middle"></i>
                        <span>Search</span>
                    </a>
                </li>
            </ul>
            <!-- User name pushed to the right -->
            <ul class="navbar-nav ms-auto align-items-center">
                <li class="nav-item d-none d-lg-flex">
                    <span class="nav-link fw-semibold">
                        <i class="ti ti-user-circle fs-15 me-1 align-middle"></i>
                        <asp:Label ID="lblFullName" runat="server"></asp:Label>
                    </span>
                </li>
            </ul>
        </div>
    </div>
</nav>

</asp:Panel><%-- /pnlNav --%>

<style>
    .topnav-bar {
        background-color: #ffffff !important;
        border-bottom: 1px solid var(--bs-border-color);
        min-height: 44px;
        z-index: 999;
        position: sticky;
        top: 65px;
    }

    [data-bs-theme="dark"] .topnav-bar {
        background-color: var(--bs-body-bg) !important;
    }

    .topnav-bar .navbar-toggler {
        display: none;
    }
    @media (max-width: 991.98px) {
        .topnav-bar .navbar-toggler {
            display: block;
        }
    }

    .topnav-bar .nav-link {
        font-size: 0.875rem;
        font-weight: 500;
        padding: 0.4rem 0.75rem;
        color: var(--bs-body-color);
        border-radius: 4px;
        transition: background 0.15s;
    }

    .topnav-bar .nav-link:hover,
    .topnav-bar .nav-link.active {
        background-color: var(--bs-primary-bg-subtle);
        color: var(--bs-primary);
    }

    .dropdown-submenu { position: relative; }
    .dropdown-submenu > .dropdown-menu { top: 0; left: 100%; margin-top: -4px; display: none; }
    .dropdown-submenu:hover > .dropdown-menu { display: block; }
    .dropdown-submenu > a::after {
        display: inline-block; float: right; margin-top: 4px;
        border-top: 0.3em solid transparent; border-bottom: 0.3em solid transparent;
        border-left: 0.3em solid currentColor; content: "";
    }
</style>
