<%@ Page Title="" Language="C#" MasterPageFile="~/ReylaBasic.Master" AutoEventWireup="true" CodeBehind="Signin.aspx.cs" Inherits="Reyla.Signin" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- SweetAlert2 assets -->
    <link href="/Theme/assets/plugins/sweetalert2/sweetalert2.min.css" rel="stylesheet" />
    <script src="/Theme/assets/plugins/sweetalert2/sweetalert2.min.js"></script>

    <div class="auth-brand text-center mb-4">
        <a href="/Index.html" class="logo-dark">
			<img src="/Theme/assets/images/logo-sm-color.png" alt="TextOS Logo" height="80">
        </a>
        <a href="/Index.html" class="logo-light">
			<img src="/Theme/assets/images/logo-sm-color.png" alt="TextOS Logo" height="80">
        </a>
        <h4 class="fw-bold mt-3">Welcome, Sign In to Reyla</h4>
        <p class="text-muted w-lg-75 mx-auto">Let’s get you signed in. Enter your username and password to continue.</p>
		<asp:ValidationSummary ID="ValidationSummary1" runat="server" ForeColor="Red" />
    </div>
	
    <div class="">
        <div class="mb-3">
			<asp:Label ID="lblUsername" CssClass="form-label" runat="server" Text="Username:" AssociatedControlID="txtUsername">Username <span class="text-danger">*</span></asp:Label>
			<div class="input-group">
				<asp:TextBox ID="txtUsername" CssClass="form-control" runat="server" placeholder="Username" required></asp:TextBox>
			</div>
        </div>
            
        <div class="mb-3">
			<asp:Label ID="lblPassword" CssClass="form-label" runat="server" Text="Password:" AssociatedControlID="txtPassword">Password <span class="text-danger">*</span></asp:Label>
			<div class="input-group">
				<asp:TextBox ID="txtPassword" CssClass="form-control" runat="server" TextMode="Password" placeholder="••••••••" required></asp:TextBox>
			</div>
        </div>
            
        <div class="d-flex justify-content-between align-items-center mb-3">
            <div class="form-check">
                <input class="form-check-input form-check-input-light fs-14" type="checkbox" id="rememberMe">
                <label class="form-check-label" for="rememberMe">Keep me signed in</label>
            </div>
            <a href="ResetPassword.aspx" class="text-decoration-underline link-offset-3 text-muted">Forgot Password?</a>
        </div>
            
        <div class="d-grid">
			<asp:Button ID="btnLogin" CssClass="btn btn-primary fw-semibold py-2" runat="server" Text="Sign In" OnClick="btnLogin_Click" />
        </div>
            
        <p class="text-muted text-center mt-4 mb-0">
            New here? <a href="Register.aspx" class="text-decoration-underline link-offset-3 fw-semibold">Create an account</a>
        </p>
    </div>
</asp:Content>