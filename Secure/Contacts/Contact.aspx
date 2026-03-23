<%@ Page Title="Contact" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="Reyla.Secure.Contacts.Contact" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

<asp:HiddenField ID="hdnContactId"    runat="server" />
<asp:HiddenField ID="hdnHandlerUrl"   runat="server" />
<asp:HiddenField ID="hdnCanEdit"      runat="server" />

<!-- Back link -->
<div class="mb-3">
    <a href="/Secure/Contacts/Index.aspx" class="btn btn-outline-secondary btn-sm">
        <i class="ti ti-arrow-left me-1"></i>Back to Contacts
    </a>
</div>

<!-- Not found state -->
<asp:Panel ID="pnlNotFound" runat="server" Visible="false">
    <div class="card">
        <div class="card-body text-center py-5">
            <i class="ti ti-user-off text-muted" style="font-size:3rem;"></i>
            <h5 class="mt-3">Contact Not Found</h5>
            <p class="text-muted">This contact may have been deleted or you don't have access.</p>
            <a href="/Secure/Contacts/Index.aspx" class="btn btn-primary btn-sm">Back to Contacts</a>
        </div>
    </div>
</asp:Panel>

<!-- Main content -->
<asp:Panel ID="pnlMain" runat="server">

    <!-- Header card -->
    <div class="card mb-3">
        <div class="card-body">
            <div class="d-flex align-items-start gap-3 flex-wrap">

                <!-- Avatar -->
                <div id="divAvatar" runat="server"
                     class="rounded-circle d-flex align-items-center justify-content-center fw-bold text-white flex-shrink-0"
                     style="width:64px;height:64px;font-size:22px;">
                    <asp:Literal ID="litInitials" runat="server" />
                </div>

                <!-- Identity -->
                <div class="flex-grow-1">
                    <div class="d-flex align-items-center gap-2 flex-wrap mb-1">
                        <h4 class="mb-0 fw-bold" id="divDisplayName" runat="server"></h4>
                        <asp:PlaceHolder ID="phTypeBadges" runat="server" />
                        <asp:PlaceHolder ID="phBuyerScore" runat="server" />
                    </div>
                    <div class="text-muted fs-sm" id="divCompanyLine" runat="server"></div>
                    <div class="text-muted fs-xs mt-1">
                        Added <asp:Literal ID="litCreatedDate" runat="server" />
                        <asp:Panel ID="pnlUpdatedDate" runat="server" CssClass="d-inline">
                            &nbsp;&bull;&nbsp;Updated <asp:Literal ID="litUpdatedDate" runat="server" />
                        </asp:Panel>
                    </div>
                </div>

                <!-- Actions -->
                <div class="d-flex gap-2 flex-shrink-0 flex-wrap">

                    <!-- Add to Contacts dropdown -->
                    <div class="dropdown">
                        <button type="button" class="btn btn-success btn-sm dropdown-toggle"
                                data-bs-toggle="dropdown" aria-expanded="false">
                            <i class="ti ti-user-plus me-1"></i>Add to Contacts
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow-sm" style="min-width:260px;">
                            <li class="px-3 pt-2 pb-1">
                                <div class="fs-xs text-muted fw-semibold text-uppercase" style="letter-spacing:.04em;">Download &amp; Import</div>
                            </li>
                            <li>
                                <a class="dropdown-item py-2" href='<%= VCardUrl %>'>
                                    <div class="d-flex align-items-center gap-2">
                                        <i class="ti ti-download fs-5 text-success"></i>
                                        <div>
                                            <div class="fw-semibold fs-sm">Download vCard (.vcf)</div>
                                            <div class="text-muted fs-xs">Universal &mdash; works with all contact apps</div>
                                        </div>
                                    </div>
                                </a>
                            </li>
                            <li><hr class="dropdown-divider" /></li>
                            <li class="px-3 pt-1 pb-1">
                                <div class="fs-xs text-muted fw-semibold text-uppercase" style="letter-spacing:.04em;">Open Contact App</div>
                            </li>
                            <li>
                                <a class="dropdown-item py-2" href="https://contacts.google.com" target="_blank">
                                    <div class="d-flex align-items-center gap-2">
                                        <i class="ti ti-brand-google fs-5 text-muted"></i>
                                        <div>
                                            <div class="fw-semibold fs-sm">Google Contacts</div>
                                            <div class="text-muted fs-xs">Download vCard first, then import</div>
                                        </div>
                                    </div>
                                </a>
                            </li>
                            <li>
                                <a class="dropdown-item py-2" href="https://outlook.live.com/people" target="_blank">
                                    <div class="d-flex align-items-center gap-2">
                                        <i class="ti ti-brand-office fs-5 text-muted"></i>
                                        <div>
                                            <div class="fw-semibold fs-sm">Outlook Contacts</div>
                                            <div class="text-muted fs-xs">Download vCard first, then import</div>
                                        </div>
                                    </div>
                                </a>
                            </li>
                        </ul>
                    </div>

                    <button type="button" class="btn btn-primary btn-sm" id="btnEditHeader" onclick="openEditModal()">
                        <i class="ti ti-pencil me-1"></i>Edit Contact
                    </button>
                    <button type="button" class="btn btn-outline-danger btn-sm" onclick="confirmDelete()">
                        <i class="ti ti-trash me-1"></i>Delete
                    </button>
                </div>

            </div>
        </div>
    </div>

    <div class="row g-3">

        <!-- Left column -->
        <div class="col-12 col-xl-5">

            <!-- Contact info card -->
            <div class="card mb-3">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <h5 class="card-title mb-0"><i class="ti ti-address-book me-1 text-primary"></i>Contact Info</h5>
                </div>
                <div class="card-body">
                    <div class="row g-3">
                        <div class="col-12 col-sm-6">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Phone</div>
                            <div><asp:Literal ID="litPhone" runat="server" /></div>
                        </div>
                        <div class="col-12 col-sm-6">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Mobile</div>
                            <div><asp:Literal ID="litMobile" runat="server" /></div>
                        </div>
                        <div class="col-12">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Email</div>
                            <div><asp:Literal ID="litEmail" runat="server" /></div>
                        </div>
                        <div class="col-12">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">LinkedIn</div>
                            <div><asp:Literal ID="litLinkedIn" runat="server" /></div>
                        </div>
                        <div class="col-12">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Preferred Contact</div>
                            <div class="d-flex align-items-center gap-2">
                                <select id="selPreferred" class="form-select form-select-sm" style="max-width:160px;"
                                        onchange="savePreferred(this)">
                                    <option value="">-- Select --</option>
                                    <option value="Phone">Phone</option>
                                    <option value="Email">Email</option>
                                    <option value="Mail">Mail</option>
                                    <option value="InPerson">In Person</option>
                                </select>
                                <span id="spnPreferredSaved" class="text-success fs-xs" style="display:none;">
                                    <i class="ti ti-check"></i> Saved
                                </span>
                            </div>
                        </div>
                        <asp:Panel ID="pnlNotes" runat="server" CssClass="col-12">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Notes</div>
                            <div class="fs-sm" style="white-space:pre-wrap;"><asp:Literal ID="litNotes" runat="server" /></div>
                        </asp:Panel>
                    </div>
                </div>
            </div>

            <!-- Contact types card -->
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0"><i class="ti ti-tag me-1 text-secondary"></i>Contact Types</h5>
                </div>
                <div class="card-body">
                    <asp:Panel ID="pnlNoTypes" runat="server">
                        <p class="text-muted fst-italic fs-sm mb-0">No contact types assigned.</p>
                    </asp:Panel>
                    <asp:Panel ID="pnlTypesList" runat="server">
                        <asp:Literal ID="litTypesList" runat="server" />
                    </asp:Panel>
                </div>
            </div>

        </div>

        <!-- Right column -->
        <div class="col-12 col-xl-7">

            <!-- Properties card -->
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0"><i class="ti ti-building me-1 text-warning"></i>Linked Properties</h5>
                </div>
                <div class="card-body p-0">
                    <asp:Panel ID="pnlNoProperties" runat="server">
                        <div class="text-center py-4">
                            <i class="ti ti-building-off text-muted" style="font-size:2rem;"></i>
                            <p class="text-muted fs-sm mt-2 mb-0">Not linked to any properties yet.</p>
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlPropertiesList" runat="server">
                        <div class="table-responsive">
                            <table class="table table-hover align-middle mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th class="ps-3">Property</th>
                                        <th>Role</th>
                                        <th class="text-end pe-3">Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptProperties" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td class="ps-3">
                                                    <div class="fw-semibold fs-sm">
                                                        <a href='<%# "/Secure/Prospects/PropertyDetail.aspx?clip=" + Eval("Clip") %>'
                                                           class="text-body text-decoration-none">
                                                            <%# Eval("StreetAddress") %>
                                                        </a>
                                                    </div>
                                                    <div class="text-muted fs-xs"><%# Eval("CityNameRaw") %> <%# Eval("ZipCodeRaw") %></div>
                                                </td>
                                                <td>
                                                    <%# (bool)Eval("IsPrimary") ? "<span class='badge text-bg-primary fs-xs'>Primary</span>" : "<span class='text-muted fs-xs'>Linked</span>" %>
                                                </td>
                                                <td class="text-end pe-3">
                                                    <a href='<%# "/Secure/Prospects/PropertyDetail.aspx?clip=" + Eval("Clip") %>'
                                                       class="btn btn-outline-primary btn-sm py-0 px-2" title="View property">
                                                        <i class="ti ti-eye fs-sm"></i>
                                                    </a>
                                                </td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </asp:Panel>
                </div>
            </div>

            <!-- Activity card -->
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0"><i class="ti ti-activity me-1 text-success"></i>Recent Activity</h5>
                </div>
                <div class="card-body p-0">
                    <asp:Panel ID="pnlNoActivity" runat="server">
                        <div class="text-center py-4">
                            <p class="text-muted fs-sm mb-0">No activity recorded yet.</p>
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlActivityList" runat="server">
                        <asp:Repeater ID="rptActivity" runat="server">
                            <ItemTemplate>
                                <div class="d-flex align-items-start gap-3 px-3 py-2 border-bottom">
                                    <div class="rounded-circle bg-light d-flex align-items-center justify-content-center flex-shrink-0 mt-1"
                                         style="width:32px;height:32px;">
                                        <i class='<%# Eval("Icon") %> text-muted fs-sm'></i>
                                    </div>
                                    <div class="flex-grow-1">
                                        <div class="fs-sm"><%# Eval("Summary") %></div>
                                        <div class="text-muted fs-xs"><%# Eval("When") %></div>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </asp:Panel>
                </div>
            </div>

        </div>
    </div>

</asp:Panel>

<!-- Edit Contact Modal -->
<div class="modal fade" id="modalEditContact" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title fw-bold">Edit Contact</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">

                <div id="divEditValidation" class="alert alert-danger d-none mb-3">
                    <ul id="ulEditValidation" class="mb-0 ps-3"></ul>
                </div>

                <div class="row g-3">
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">First Name <span class="text-muted fw-normal">(or Last Name required)</span></label>
                        <input type="text" id="editFirstName" class="form-control form-control-sm" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Last Name</label>
                        <input type="text" id="editLastName" class="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label fw-semibold fs-sm">Company</label>
                        <input type="text" id="editCompany" class="form-control form-control-sm" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Email</label>
                        <input type="email" id="editEmail" class="form-control form-control-sm" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Phone</label>
                        <input type="text" id="editPhone" class="form-control form-control-sm" placeholder="(555) 000-0000" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Mobile Phone</label>
                        <input type="text" id="editMobile" class="form-control form-control-sm" placeholder="(555) 000-0000" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">LinkedIn URL</label>
                        <input type="url" id="editLinkedIn" class="form-control form-control-sm" placeholder="https://linkedin.com/in/..." />
                    </div>
                    <div class="col-12">
                        <label class="form-label fw-semibold fs-sm">Contact Types <span class="text-muted fw-normal">(select all that apply)</span></label>
                        <div id="divEditTypes" class="d-flex flex-wrap gap-2 mt-1"></div>
                    </div>
                    <div class="col-12">
                        <label class="form-label fw-semibold fs-sm">Notes</label>
                        <textarea id="editNotes" class="form-control form-control-sm" rows="3"></textarea>
                    </div>
                </div>

            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary btn-sm" id="btnSaveEdit" onclick="saveEdit()">
                    <i class="ti ti-device-floppy me-1"></i>Save Changes
                </button>
            </div>
        </div>
    </div>
</div>

<!-- Delete Confirm Modal -->
<div class="modal fade" id="modalDeleteContact" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-sm">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title fw-bold">Delete Contact?</h6>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <p class="mb-0 fs-sm">This will permanently delete this contact and remove them from all linked properties. This cannot be undone.</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-danger btn-sm" id="btnConfirmDelete" onclick="doDelete()">
                    <i class="ti ti-trash me-1"></i>Delete
                </button>
            </div>
        </div>
    </div>
</div>

<script type="text/javascript">
var _contactId  = document.getElementById('<%= hdnContactId.ClientID %>').value;
var _handlerUrl = document.getElementById('<%= hdnHandlerUrl.ClientID %>').value;
var _contactTypes = [];

// ── Preferred Contact ─────────────────────────────────────────
(function () {
    var serverVal = '<%= PreferredContactValue %>';
    var sel = document.getElementById('selPreferred');
    if (sel && serverVal) sel.value = serverVal;
})();

function savePreferred(sel) {
    fetch(_handlerUrl + '?action=setpreferred&contactId=' + _contactId + '&preferred=' + encodeURIComponent(sel.value), {
        method: 'POST', body: '{}'
    })
    .then(function (r) { return r.json(); })
    .then(function (res) {
        var icon = document.getElementById('spnPreferredSaved');
        if (res.success) {
            icon.style.display = '';
            setTimeout(function () { icon.style.display = 'none'; }, 2000);
        } else {
            alert('Could not save: ' + (res.error || 'Unknown error'));
        }
    })
    .catch(function (err) { alert('Network error: ' + err.message); });
}

// ── Edit modal ────────────────────────────────────────────────
function openEditModal() {
    // Load contact types if not yet loaded
    if (!_contactTypes.length) {
        fetch(_handlerUrl + '?action=gettypes')
            .then(function (r) { return r.json(); })
            .then(function (res) {
                _contactTypes = res.types || [];
                buildEditTypeCheckboxes();
                seedEditForm();
                new bootstrap.Modal(document.getElementById('modalEditContact')).show();
                initEditMasks();
            });
    } else {
        seedEditForm();
        new bootstrap.Modal(document.getElementById('modalEditContact')).show();
        initEditMasks();
    }
}

function buildEditTypeCheckboxes() {
    var html = '';
    _contactTypes.forEach(function (t) {
        html += '<div class="form-check form-check-inline mb-1">';
        html += '<input class="form-check-input" type="checkbox" id="et_' + t.ContactTypeId + '" value="' + t.ContactTypeId + '" data-name="' + esc(t.Name) + '">';
        html += '<label class="form-check-label fs-sm" for="et_' + t.ContactTypeId + '">' + esc(t.Name) + '</label>';
        html += '</div>';
    });
    document.getElementById('divEditTypes').innerHTML = html;
}

function seedEditForm() {
    document.getElementById('editFirstName').value = '<%= EscJs(Contact_FirstName) %>';
    document.getElementById('editLastName').value  = '<%= EscJs(Contact_LastName) %>';
    document.getElementById('editCompany').value   = '<%= EscJs(Contact_Company) %>';
    document.getElementById('editEmail').value     = '<%= EscJs(Contact_Email) %>';
    document.getElementById('editPhone').value     = '<%= EscJs(Contact_Phone) %>';
    document.getElementById('editMobile').value    = '<%= EscJs(Contact_Mobile) %>';
    document.getElementById('editLinkedIn').value  = '<%= EscJs(Contact_LinkedIn) %>';
    document.getElementById('editNotes').value     = '<%= EscJs(Contact_Notes) %>';

    var currentTypes = '<%= Contact_TypeIds %>'.split(',').filter(Boolean);
    document.querySelectorAll('#divEditTypes input[type=checkbox]').forEach(function (cb) {
        cb.checked = currentTypes.indexOf(cb.value) !== -1;
    });

    document.getElementById('divEditValidation').classList.add('d-none');
    ['editFirstName','editLastName','editEmail','editPhone','editMobile'].forEach(function (id) {
        document.getElementById(id).classList.remove('is-invalid');
    });
}

function initEditMasks() {
    ['editPhone', 'editMobile'].forEach(function (id) {
        var el = document.getElementById(id);
        if (!el || el._maskDone) return;
        el._maskDone = true;
        el.addEventListener('input', function () {
            var d = el.value.replace(/\D/g,'').substring(0,10);
            if      (d.length === 0) el.value = '';
            else if (d.length <= 3)  el.value = '(' + d;
            else if (d.length <= 6)  el.value = '(' + d.substring(0,3) + ') ' + d.substring(3);
            else                     el.value = '(' + d.substring(0,3) + ') ' + d.substring(3,6) + '-' + d.substring(6);
        });
        el.addEventListener('keydown', function (e) {
            var allow = ['Backspace','Delete','ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Tab','Home','End'];
            if (allow.indexOf(e.key) >= 0) return;
            if (!/^\d$/.test(e.key)) e.preventDefault();
        });
        el.addEventListener('blur', function () {
            var d = el.value.replace(/\D/g,'');
            if (d.length > 0 && d.length < 10) el.classList.add('is-invalid');
            else el.classList.remove('is-invalid');
        });
        el.addEventListener('focus', function () { el.classList.remove('is-invalid'); });
    });
}

function saveEdit() {
    var errors    = [];
    var firstName = document.getElementById('editFirstName').value.trim();
    var lastName  = document.getElementById('editLastName').value.trim();
    var email     = document.getElementById('editEmail').value.trim();
    var phone     = document.getElementById('editPhone').value.trim();
    var mobile    = document.getElementById('editMobile').value.trim();

    document.getElementById('divEditValidation').classList.add('d-none');
    ['editFirstName','editLastName','editEmail','editPhone','editMobile'].forEach(function (id) {
        document.getElementById(id).classList.remove('is-invalid');
    });

    if (!firstName && !lastName) {
        errors.push('Please enter at least a First Name or Last Name.');
        document.getElementById('editFirstName').classList.add('is-invalid');
        document.getElementById('editLastName').classList.add('is-invalid');
    }
    if (email  && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))  { errors.push('Please enter a valid email address.'); document.getElementById('editEmail').classList.add('is-invalid'); }
    if (phone  && phone.replace(/\D/g,'').length !== 10)        { errors.push('Phone must be 10 digits.'); document.getElementById('editPhone').classList.add('is-invalid'); }
    if (mobile && mobile.replace(/\D/g,'').length !== 10)       { errors.push('Mobile must be 10 digits.'); document.getElementById('editMobile').classList.add('is-invalid'); }

    if (errors.length) {
        document.getElementById('ulEditValidation').innerHTML = errors.map(function (e) { return '<li>' + esc(e) + '</li>'; }).join('');
        document.getElementById('divEditValidation').classList.remove('d-none');
        return;
    }

    var btn = document.getElementById('btnSaveEdit');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';

    var typeIds = [];
    document.querySelectorAll('#divEditTypes input[type=checkbox]:checked').forEach(function (cb) { typeIds.push(cb.value); });

    var payload = {
        ContactId:      _contactId,
        FirstName:      firstName,
        LastName:       lastName,
        CompanyName:    document.getElementById('editCompany').value.trim(),
        Email:          email,
        Phone:          phone,
        MobilePhone:    mobile,
        LinkedInUrl:    document.getElementById('editLinkedIn').value.trim(),
        Notes:          document.getElementById('editNotes').value.trim(),
        ContactTypeIds: typeIds
    };

    fetch(_handlerUrl + '?action=save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    })
    .then(function (r) { return r.text(); })
    .then(function (raw) {
        btn.disabled = false;
        btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save Changes';
        try {
            var res = JSON.parse(raw);
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('modalEditContact')).hide();
                window.location.reload();
            } else {
                alert(res.error || 'An error occurred saving the contact.');
            }
        } catch (e) {
            alert('Could not parse response: ' + raw);
        }
    })
    .catch(function (err) {
        btn.disabled = false;
        btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save Changes';
        alert('Network error: ' + err.message);
    });
}

// ── Delete ────────────────────────────────────────────────────
function confirmDelete() {
    new bootstrap.Modal(document.getElementById('modalDeleteContact')).show();
}

function doDelete() {
    var btn = document.getElementById('btnConfirmDelete');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Deleting...';

    fetch(_handlerUrl + '?action=delete&contactId=' + _contactId, {
        method: 'POST', body: '{}'
    })
    .then(function (r) { return r.json(); })
    .then(function (res) {
        if (res.success) {
            window.location.href = '/Secure/Contacts/Index.aspx';
        } else {
            btn.disabled = false;
            btn.innerHTML = '<i class="ti ti-trash me-1"></i>Delete';
            alert(res.error || 'Could not delete contact.');
        }
    })
    .catch(function (err) {
        btn.disabled = false;
        btn.innerHTML = '<i class="ti ti-trash me-1"></i>Delete';
        alert('Network error: ' + err.message);
    });
}

function esc(str) {
    if (!str) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&#39;');
}
</script>

</asp:Content>
