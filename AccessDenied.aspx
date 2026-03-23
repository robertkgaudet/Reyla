<%@ Page Title="Access Denied" Language="C#" MasterPageFile="~/ReylaBasic.Master" AutoEventWireup="true" CodeBehind="AccessDenied.aspx.cs" Inherits="Reyla.AccessDenied" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
	<div class="container py-5 text-center">
		<h1 class="display-5">Access Denied</h1>
		<p class="lead">Your account does not have permission to view that page.</p>
		<p>If you believe this is an error, sign in with a different account or contact support.</p>

		<div class="d-flex justify-content-center mt-4">
			<asp:Button ID="btnSignOut" runat="server" CssClass="btn btn-secondary me-2" Text="Sign out" OnClick="btnSignOut_Click" />
			<a class="btn btn-primary" href="/SignIn.aspx">Sign in with a different account</a>
		</div>
	</div>
</asp:Content>