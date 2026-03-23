<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MasterPageTopbar.ascx.cs" Inherits="Reyla.UserControls.MasterPageTopbar" %>

        <header class="app-topbar">
			<style>
				.button-collapse-toggle:hover {
				  cursor:pointer;
				}
			</style>
            <div class="color-line"></div>
            <div class="container-fluid topbar-menu">
                <div class="d-flex align-items-center justify-content-center gap-2">
                    <!-- Topbar Brand Logo -->
					<div class="logo-topbar text-center">
						<!-- Logo -->
						<a href="/Secure/Index" class="text-dark">
							<div class="d-flex justify-content-center align-items-center gap-2">
								<img src="/Theme/assets/images/logo-black.png" alt="Reyla Logo" height="40">
							</div>
						</a>
					</div>

                    <div class="d-lg-none d-flex mx-1">
                        <a href="/Secure/Index">
                            <img src="/Theme/assets/images/logo-sm.png" height="40" alt="Logo">
                        </a>
                    </div>

					<!-- Search — hidden for anonymous users -->
					<div id="divSearch" runat="server" class="app-search d-flex">
						<input type="search" class="form-control topbar-search" name="search" placeholder="Search for something...">
						<i data-lucide="search" class="app-search-icon text-muted"></i>
					</div>
                </div> <!-- .d-flex-->

                <div class="d-flex align-items-center gap-2">

                    <!-- Notification Dropdown — authenticated only -->
                    <div id="divNotif" runat="server" class="topbar-item">
                        <div class="dropdown">
                            <button class="topbar-link dropdown-toggle drop-arrow-none" data-bs-toggle="dropdown" data-bs-offset="0,19" type="button" data-bs-auto-close="outside" aria-haspopup="false" aria-expanded="false">
                                <i data-lucide="bell" class="fs-xxl"></i>
                                <span class="badge badge-square text-bg-success topbar-badge">
                                    <asp:Literal ID="litNotifBadge" runat="server" Text="0" />
                                </span>
                            </button>

                            <div class="dropdown-menu p-0 dropdown-menu-end dropdown-menu-lg">
                                <div class="px-3 py-2 border-bottom">
                                    <div class="row align-items-center">
                                        <div class="col">
                                            <h6 class="m-0 fs-md fw-semibold">Notifications</h6>
                                        </div>
                                        <div class="col text-end">
                                            <a href="#!" class="badge text-bg-light badge-label py-1">
                                                <asp:Literal ID="litNotifCount" runat="server" Text="0 Alerts" />
                                            </a>
                                        </div>
                                    </div>
                                </div>

                                <div style="max-height: 300px;" data-simplebar>
                                    <asp:Literal ID="litNotifItems" runat="server" />
                                </div>

                                <!-- All-->
                                <a href="/Secure/Activity" class="dropdown-item text-center text-reset text-decoration-underline link-offset-2 fw-bold notify-item border-top border-light py-2">
                                    View All Activity
                                </a>

                            </div>
                        </div>
                    </div>

                    <!-- Button Trigger Customizer Offcanvas — authenticated only -->
                    <div id="divSettings" runat="server" class="topbar-item d-none d-sm-flex">
                        <button class="topbar-link" data-bs-toggle="offcanvas" data-bs-target="#theme-settings-offcanvas" type="button">
                            <i data-lucide="settings" class="fs-xxl"></i>
                        </button>
                    </div>

                    <!-- Light/Dark Mode Button — authenticated only -->
                    <div id="divDarkMode" runat="server" class="topbar-item d-none d-sm-flex">
                        <button class="topbar-link" id="light-dark-mode" type="button">
                            <i data-lucide="moon" class="fs-xxl mode-light-moon"></i>
                            <i data-lucide="sun" class="fs-xxl mode-light-sun"></i>
                        </button>
                    </div>

                    <!-- User Dropdown — authenticated only -->
                    <div id="divUserMenu" runat="server" class="topbar-item nav-user">
                        <div class="dropdown">
                            <a class="topbar-link dropdown-toggle drop-arrow-none px-2" data-bs-toggle="dropdown" data-bs-offset="0,13" href="#!" aria-haspopup="false" aria-expanded="false">
                                <img src="/Theme/assets/images/logo-sm.png" width="32" class="rounded-circle me-lg-2 d-flex" alt="user-image">
                            </a>
                            <div class="dropdown-menu dropdown-menu-end">
                                <!-- Header -->
                                <div class="dropdown-header noti-title">
                                    <h6 class="text-overflow m-0">Welcome back!</h6>
                                </div>

                                <!-- My Profile -->
                                <a href="/Secure/Profile" class="dropdown-item">
                                    <i class="ti ti-user-circle me-2 fs-17 align-middle"></i>
                                    <span class="align-middle">Profile</span>
                                </a>

                                <!-- Notifications -->
                                <a href="/Secure/Notifications" class="dropdown-item">
                                    <i class="ti ti-bell-ringing me-2 fs-17 align-middle"></i>
                                    <span class="align-middle">Notifications</span>
                                </a>

                                <!-- Wallet -->
                                <a href="/Secure/Income" class="dropdown-item">
                                    <i class="ti ti-credit-card me-2 fs-17 align-middle"></i>
                                    <span class="align-middle">Income: <span class="fw-semibold">$985.25</span></span>
                                </a>

                                <!-- Settings -->
                                <a href="/Secure/Settings" class="dropdown-item">
                                    <i class="ti ti-settings-2 me-2 fs-17 align-middle"></i>
                                    <span class="align-middle">Account Settings</span>
                                </a>

                                <!-- Support -->
                                <a href="/Secure/Support" class="dropdown-item">
                                    <i class="ti ti-headset me-2 fs-17 align-middle"></i>
                                    <span class="align-middle">Support Center</span>
                                </a>

                                <!-- Homer Template -->
                                <a href="/Theme/Index.html" class="dropdown-item">
                                    <i class="ti ti-brush me-2 fs-17 align-middle"></i>
                                    <span class="align-middle">Homer Template</span>
                                </a>

                                <!-- Divider -->
                                <div class="dropdown-divider"></div>

                                <!-- Logout -->
								<a href="javascript:void(0);" class="dropdown-item text-danger fw-semibold" id="lnkLogout" runat="server" onserverclick="lnkLogout_ServerClick">
									<i class="ti ti-logout-2 me-2 fs-17 align-middle"></i>
									<span class="align-middle">Log Out</span>
								</a>
                            </div>

                        </div>
                    </div>

                    <!-- Sign In / Register — anonymous only -->
                    <div id="divAnonLinks" runat="server" class="d-flex align-items-center gap-2">
                        <a href="/Signin.aspx" class="btn btn-outline-primary btn-sm fw-semibold">
                            <i class="ti ti-login-2 me-1 align-middle"></i>Sign In
                        </a>
                        <a href="/Register.aspx" class="btn btn-primary btn-sm fw-semibold">
                            <i class="ti ti-user-plus me-1 align-middle"></i>Register
                        </a>
                    </div>

                </div>
            </div>
        </header>
