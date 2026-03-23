<%@ Page Title="" Language="C#" MasterPageFile="~/ReylaBasic.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="Reyla.Register" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
	<div class="auth-brand text-center mb-4">
		<a href="/Landing.html" class="logo-dark">
			<img src="/Theme/assets/images/logo-sm-color.png" alt="TextOS Logo" height="80">
		</a>
		<a href="/Landing.html" class="logo-light">
			<img src="/Theme/assets/images/logo-sm-color.png" alt="TextOS Logo" height="80">
		</a>
		<h4 class="fw-bold mt-3">Register on Reyla</h4>
		<p class="text-muted w-lg-750 mx-auto">Let's get you started.<br />

		<p class="text-muted w-lg-75 mx-auto">
			Reyla is an AI-first operating system for modern real estate.
			It helps agents and brokers see what matters, anticipate what's next,
			and move with confidence.
		</p>

		<p class="text-muted w-lg-75 mx-auto">
			Less noise. More signal.
			Decisions, relationships, and momentum—working together.
		</p>
		<asp:ValidationSummary ID="ValidationSummary1" runat="server" ForeColor="Red" />
	</div>
	<div runat="server" id="divRegistrationForm">
		<div class="mb-3">
			<div class="input-group">
				<asp:TextBox ID="txtFirstname" CssClass="form-control" runat="server" placeholder="Firstname" required></asp:TextBox>
			</div>
		</div>
	
		<div class="mb-3">
			<div class="input-group">
				<asp:TextBox ID="txtUsername" CssClass="form-control" runat="server" placeholder="Username" required></asp:TextBox>
			</div>
		</div>

		<div class="mb-3">
			<div class="input-group">
				<asp:TextBox ID="txtEmail" CssClass="form-control" runat="server" placeholder="Email" required></asp:TextBox>
			</div>
		</div>

		<div class="mb-3">
			<div class="input-group">
				<asp:TextBox ID="txtPhone" CssClass="form-control" runat="server" placeholder="Phone" required></asp:TextBox>
			</div>
		</div>

		<div class="mb-3" data-password="bar">
			<div class="input-group">
				<asp:TextBox ID="txtPassword" CssClass="form-control" runat="server" TextMode="Password" placeholder="••••••••" required></asp:TextBox>
				<button type="button" class="btn btn-outline-secondary" id="btnTogglePassword" aria-pressed="false" aria-label="Show password">Show</button>
			</div>
			<div class="password-bar my-2"></div>
			<p class="text-muted fs-xs mb-0">Use 8+ characters with letters, numbers & symbols.</p>
		</div>

		<div class="mb-3">
			<div class="input-group">
				<asp:TextBox ID="txtConfirmPassword" CssClass="form-control" runat="server" TextMode="Password" placeholder="Confirm" required></asp:TextBox>
			</div>
			<asp:CompareValidator ID="cvPasswords" runat="server" ControlToValidate="txtConfirmPassword" ControlToCompare="txtPassword" ErrorMessage="Passwords do not match." ForeColor="Red" />
		</div>

		<%-- Terms of Service & Privacy Policy acceptance
		     Required by CoreLogic MSA Licensing Addendum Section 3:
		     Permitted Users must agree in writing; agreement must name
		     CoreLogic Solutions, LLC as express third-party beneficiary. --%>
		<div class="mb-3">
			<div class="form-check align-items-start d-flex gap-2">
				<asp:CheckBox ID="chkAcceptTerms" runat="server"
					CssClass="form-check-input mt-1 flex-shrink-0"
					ClientIDMode="Static" />
				<label class="form-check-label fs-xs text-muted" for="chkAcceptTerms">
					I have read and agree to the
					<a href="/Terms.aspx" target="_blank" class="text-primary fw-semibold">Terms of Service</a>
					and
					<a href="/Privacy.aspx" target="_blank" class="text-primary fw-semibold">Privacy Policy</a>.
					I understand that property data available through Reyla is subject to
					restrictions set forth in those terms, and I acknowledge that
					<strong class="text-dark">CoreLogic Solutions, LLC</strong> is an express
					third-party beneficiary of the Terms of Service with respect to such data.
				</label>
			</div>
			<asp:CustomValidator
				ID="cvTerms"
				runat="server"
				ErrorMessage="You must accept the Terms of Service to create an account."
				OnServerValidate="cvTerms_ServerValidate"
				CssClass="text-danger fs-xs d-block mt-1"
				Display="Dynamic"
				ValidateEmptyText="true" />
		</div>

		<div class="d-grid">
			<asp:Button ID="btnRegister" CssClass="btn btn-primary fw-semibold py-2" runat="server" Text="Create Account" OnClick="btnRegister_Click" />
			<asp:RegularExpressionValidator ID="revEmail" runat="server" ControlToValidate="txtEmail" ErrorMessage="Invalid email format." ForeColor="Red" ValidationExpression="^\w+@[a-zA-Z_]+?\.[a-zA-Z]{2,3}$" />
		</div>
	</div>

	<p class="text-muted text-center mt-4 mb-0">
		Already have an account? <a href="/Signin.aspx" class="text-decoration-underline link-offset-3 fw-semibold">Sign In</a>
	</p>

	<script src="https://cdnjs.cloudflare.com/ajax/libs/jquery.mask/1.14.16/jquery.mask.min.js"></script>

	<script type="text/javascript">
		(function ($) {
			$(function () {
				var $phone = $('#<%= txtPhone.ClientID %>');
				if (!$phone.length) return;
				if (typeof $.fn.mask === 'function') {
					try { if (typeof $phone.unmask === 'function') { $phone.unmask(); } } catch (e) {}
					if (!$phone.data('mask-applied')) {
						$phone.mask('(000) 000-0000', { placeholder: '(___) ___-____' });
						$phone.attr('maxlength', 14);
						$phone.data('mask-applied', true);
					}
				} else {
					$phone.attr('maxlength', 14);
				}
			});
		})(jQuery);

		(function ($) {
			$(function () {
				var $pwd = $('#<%= txtPassword.ClientID %>');
				var $toggle = $('#btnTogglePassword');
				function setType(showPlain) {
					try {
						$pwd.attr('type', showPlain ? 'text' : 'password');
					} catch (e) {
						var attrs = { type: showPlain ? 'text' : 'password', id: $pwd.attr('id'), name: $pwd.attr('name'), class: $pwd.attr('class'), placeholder: $pwd.attr('placeholder') || '' };
						if ($pwd.attr('maxlength')) attrs.maxlength = $pwd.attr('maxlength');
						var $new = $('<input/>', attrs).val($pwd.val());
						$pwd.replaceWith($new);
						$pwd = $new;
					}
				}
				$toggle.on('click', function () {
					var isMasked = ($pwd.attr('type') === 'password');
					setType(isMasked);
					$toggle.text(isMasked ? 'Hide' : 'Show');
					$toggle.attr('aria-pressed', isMasked ? 'true' : 'false');
					$toggle.attr('aria-label', isMasked ? 'Hide password' : 'Show password');
				});
			});
		})(jQuery);
	</script>
</asp:Content>
