<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MasterPageThemeSettings.ascx.cs" Inherits="Reyla.UserControls.MasterPageThemeSettings" %>

<%-- Theme settings offcanvas — authenticated users only --%>
<asp:Panel ID="pnlThemeSettings" runat="server">
    <div class="offcanvas offcanvas-end overflow-hidden" tabindex="-1" id="theme-settings-offcanvas">
        <div class="d-flex justify-content-between text-bg-primary gap-2 p-3" style="background-image: url(assets/images/user-bg-pattern.png);">
            <div>
                <h5 class="mb-1 fw-bold text-white text-uppercase">Reyla Agent</h5>
                <p class="text-white text-opacity-75 fst-italic fw-medium mb-0">Your daily digest and action items.</p>
            </div>
            <div class="flex-grow-0">
                <button type="button" class="d-block btn btn-sm bg-white bg-opacity-25 text-white rounded-circle btn-icon" data-bs-dismiss="offcanvas"><i class="ti ti-x fs-lg"></i></button>
            </div>
        </div>

        <div class="offcanvas-body p-0 h-100" data-simplebar>
            <div class="p-3 border-bottom border-dashed">
                <h5 class="mb-3 fw-bold">Act Now</h5>
                <div class="row g-3">
                    <div class="col-6"></div>
                    <div class="col-6"></div>
                    <div class="col-6"></div>
                    <div class="col-6"></div>
                    <div class="col-6"></div>
                    <div class="col-6"></div>
                </div>
            </div>

            <div class="p-3 border-bottom border-dashed">
                <h5 class="mb-3 fw-bold">Prospects</h5>
                <div class="row">
                    <div class="col-4"></div>
                    <div class="col-4"></div>
                    <div class="col-4"></div>
                </div>
            </div>

            <div class="p-3 border-bottom border-dashed">
                <h5 class="mb-3 fw-bold">Reyla Says Do This</h5>
                <div class="row g-3">
                    <div class="col-4"></div>
                    <div class="col-4"></div>
                    <div class="col-4"></div>
                </div>
            </div>
        </div>

        <div class="offcanvas-footer border-top p-3 text-center">
            <div class="row">
                <div class="col-6"></div>
                <div class="col-6"></div>
            </div>
        </div>
    </div>
</asp:Panel><%-- /pnlThemeSettings --%>

<!-- Vendor js -->
<script src="/Theme/assets/js/vendors.min.js"></script>

<!-- App js -->
<script src="/Theme/assets/js/app.js"></script>
