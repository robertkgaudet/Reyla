<%@ Page Title="Contacts" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="Reyla.Secure.Contacts.Index" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

<!-- Page Header -->
<div class="row mb-3">
    <div class="col-12">
        <div class="card">
            <div class="card-body py-3">
                <div class="d-flex align-items-center justify-content-between flex-wrap gap-2">
                    <div>
                        <h4 class="mb-1 fw-bold">Contacts</h4>
                        <p class="text-muted mb-0 fs-sm">Manage property owners, buyers, and key contacts across your pipeline.</p>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <span class="badge text-bg-secondary fs-xs" id="badgeCount" runat="server">0 contacts</span>
                        <button type="button" class="btn btn-primary btn-sm" onclick="openAddContactModal()">
                            <i class="ti ti-user-plus me-1"></i>Add Contact
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Filters -->
<div class="row mb-3">
    <div class="col-12">
        <div class="card">
            <div class="card-body py-2">
                <div class="row g-2 align-items-center">
                    <div class="col-12 col-md-5">
                        <div class="input-group input-group-sm">
                            <span class="input-group-text bg-transparent border-end-0">
                                <i class="ti ti-search text-muted"></i>
                            </span>
                            <asp:TextBox ID="txtSearch" runat="server"
                                CssClass="form-control border-start-0 ps-0"
                                placeholder="Search name, email, phone, company..."
                                AutoPostBack="false" />
                        </div>
                    </div>
                    <div class="col-12 col-md-3">
                        <asp:DropDownList ID="ddlContactType" runat="server"
                            CssClass="form-select form-select-sm"
                            AutoPostBack="false">
                            <asp:ListItem Value="" Text="All Contact Types" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-12 col-md-2">
                        <asp:DropDownList ID="ddlStatus" runat="server"
                            CssClass="form-select form-select-sm"
                            AutoPostBack="false">
                            <asp:ListItem Value="active" Text="Active Only" Selected="True" />
                            <asp:ListItem Value="all"    Text="Show All" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-12 col-md-2 d-flex gap-2">
                        <button type="button" class="btn btn-primary btn-sm flex-fill" onclick="applyFilters()">
                            <i class="ti ti-filter me-1"></i>Filter
                        </button>
                        <button type="button" class="btn btn-outline-secondary btn-sm" onclick="clearFilters()" title="Clear">
                            <i class="ti ti-x"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Contact List -->
<div class="row">
    <div class="col-12">
        <div class="card">
            <div class="card-body p-0">

                <!-- Empty state -->
                <div id="divEmpty" class="text-center py-5" style="display:none;">
                    <i class="ti ti-users text-muted" style="font-size:3rem;"></i>
                    <h6 class="mt-3 text-muted">No contacts found</h6>
                    <p class="text-muted fs-sm mb-3">Try adjusting your filters or add a new contact.</p>
                    <button type="button" class="btn btn-primary btn-sm" onclick="openAddContactModal()">
                        <i class="ti ti-user-plus me-1"></i>Add First Contact
                    </button>
                </div>

                <!-- Contact rows -->
                <div id="divContactList">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th class="ps-3" style="width:40%;">Contact</th>
                                    <th style="width:20%;">Contact Types</th>
                                    <th style="width:15%;">Buyer Score</th>
                                    <th style="width:15%;">Properties</th>
                                    <th class="text-end pe-3" style="width:10%;">Actions</th>
                                </tr>
                            </thead>
                            <tbody id="tbodyContacts"></tbody>
                        </table>
                    </div>
                </div>

            </div>
        </div>
    </div>
</div>

<!-- Add / Edit Contact Modal -->
<div class="modal fade" id="modalContact" tabindex="-1" aria-labelledby="modalContactLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title fw-bold" id="modalContactLabel">Add Contact</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <input type="hidden" id="hdnContactId" value="" />

                <!-- Validation errors -->
                <div id="divValidationErrors" class="alert alert-danger d-none mb-3">
                    <ul id="ulValidationErrors" class="mb-0 ps-3"></ul>
                </div>

                <!-- Duplicate warning -->
                <div id="divDupWarning" class="alert alert-warning d-none">
                    <i class="ti ti-alert-triangle me-1"></i>
                    <span id="spnDupWarning"></span>
                    <a href="#" id="lnkViewDup" class="alert-link ms-1">View existing contact</a>
                </div>

                <div class="row g-3">
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">First Name <span class="text-muted fw-normal">(or Last Name required)</span></label>
                        <input type="text" id="txtFirstName" class="form-control form-control-sm" placeholder="First name" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Last Name</label>
                        <input type="text" id="txtLastName" class="form-control form-control-sm" placeholder="Last name" />
                    </div>
                    <div class="col-12">
                        <label class="form-label fw-semibold fs-sm">Company</label>
                        <input type="text" id="txtCompanyName" class="form-control form-control-sm" placeholder="Company name" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Email</label>
                        <input type="email" id="txtEmail" class="form-control form-control-sm" placeholder="email@example.com" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Phone</label>
                        <input type="text" id="txtPhone" class="form-control form-control-sm" placeholder="(555) 000-0000" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">Mobile Phone</label>
                        <input type="text" id="txtMobilePhone" class="form-control form-control-sm" placeholder="(555) 000-0000" />
                    </div>
                    <div class="col-12 col-md-6">
                        <label class="form-label fw-semibold fs-sm">LinkedIn URL</label>
                        <input type="url" id="txtLinkedInUrl" class="form-control form-control-sm" placeholder="https://linkedin.com/in/..." />
                    </div>
                    <div class="col-12">
                        <label class="form-label fw-semibold fs-sm">Contact Types <span class="text-muted fw-normal">(select all that apply)</span></label>
                        <div id="divContactTypes" class="d-flex flex-wrap gap-2 mt-1"></div>
                    </div>
                    <div class="col-12">
                        <label class="form-label fw-semibold fs-sm">Notes</label>
                        <textarea id="txtNotes" class="form-control form-control-sm" rows="3" placeholder="Any notes about this contact..."></textarea>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary btn-sm" id="btnSaveContact" onclick="saveContact()">
                    <i class="ti ti-device-floppy me-1"></i>Save Contact
                </button>
            </div>
        </div>
    </div>
</div>

<!-- Delete Confirm Modal -->
<div class="modal fade" id="modalDelete" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-sm">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title fw-bold">Delete Contact?</h6>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <p class="mb-0 fs-sm">This will permanently remove <strong id="spnDeleteName"></strong> from your contacts. This cannot be undone.</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-danger btn-sm" id="btnConfirmDelete" onclick="confirmDelete()">
                    <i class="ti ti-trash me-1"></i>Delete
                </button>
            </div>
        </div>
    </div>
</div>

<!-- Hidden server data -->
<asp:HiddenField ID="hdnContactsJson"     runat="server" />
<asp:HiddenField ID="hdnContactTypesJson" runat="server" />
<asp:HiddenField ID="hdnCanSeeAll"        runat="server" />

<script type="text/javascript">
var _allContacts  = [];
var _contactTypes = [];
var _deleteId     = null;
var _handlerUrl   = '<%= ResolveUrl("~/Secure/Contacts/ContactHandler.ashx") %>';

var _avatarColors = ['#3b82f6','#10b981','#f59e0b','#ef4444','#8b5cf6','#06b6d4','#ec4899','#84cc16'];

document.addEventListener('DOMContentLoaded', function () {
    var raw   = document.getElementById('<%= hdnContactsJson.ClientID %>').value;
    var types = document.getElementById('<%= hdnContactTypesJson.ClientID %>').value;

    _allContacts  = raw   ? JSON.parse(raw)   : [];
    _contactTypes = types ? JSON.parse(types) : [];

    buildTypeCheckboxes();
    populateTypeFilter();
    renderContacts(_allContacts);
});

// ----------------------------------------------------------------
// Render
// ----------------------------------------------------------------
function renderContacts(contacts) {
    var tbody    = document.getElementById('tbodyContacts');
    var divList  = document.getElementById('divContactList');
    var divEmpty = document.getElementById('divEmpty');
    var badge    = document.getElementById('<%= badgeCount.ClientID %>');

    badge.textContent = contacts.length + ' contact' + (contacts.length !== 1 ? 's' : '');

    if (!contacts.length) {
        divList.style.display  = 'none';
        divEmpty.style.display = '';
        return;
    }

    divList.style.display  = '';
    divEmpty.style.display = 'none';

    var html = '';
    contacts.forEach(function (c) {
        var initials   = getInitials(c.FirstName, c.LastName, c.CompanyName);
        var color      = getAvatarColor(c.ContactId);
        var typeBadges = buildTypeBadges(c.ContactTypes);
        var scoreHtml  = buildScorePill(c.BuyerScore);
        var propHtml   = buildPropertyCount(c.Properties);
        var detailUrl  = '/Secure/Contacts/Contact.aspx?contactId=' + c.ContactId;

        html += '<tr>';
        html += '<td class="ps-3"><div class="d-flex align-items-center gap-3">';
        html += '<div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0 fw-bold text-white fs-sm" style="width:40px;height:40px;background:' + color + ';">' + initials + '</div>';
        html += '<div><div class="fw-semibold">';
        html += '<a href="' + detailUrl + '" class="text-body text-decoration-none">';
        html += escHtml(((c.FirstName || '') + ' ' + (c.LastName || '')).trim() || c.CompanyName || '(No Name)');
        html += '</a>';
        if (c.LinkedInUrl) html += ' <a href="' + escHtml(c.LinkedInUrl) + '" target="_blank" class="text-primary ms-1"><i class="ti ti-brand-linkedin fs-sm"></i></a>';
        html += '</div>';
        html += '<div class="text-muted fs-xs">';
        if (c.CompanyName && (c.FirstName || c.LastName)) html += escHtml(c.CompanyName) + ' &bull; ';
        if (c.Email) html += '<a href="mailto:' + escHtml(c.Email) + '" class="text-muted">' + escHtml(c.Email) + '</a>';
        if (c.Email && c.Phone) html += ' &bull; ';
        if (c.Phone) html += escHtml(c.Phone);
        html += '</div></div></div></td>';
        html += '<td>' + typeBadges + '</td>';
        html += '<td>' + scoreHtml + '</td>';
        html += '<td>' + propHtml + '</td>';
        html += '<td class="text-end pe-3"><div class="d-flex justify-content-end gap-1">';
        html += '<a href="' + detailUrl + '" class="btn btn-outline-primary btn-sm py-0 px-2" title="View"><i class="ti ti-eye fs-sm"></i></a>';
        html += '<button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" title="Edit" onclick="openEditContactModal(\'' + c.ContactId + '\')"><i class="ti ti-pencil fs-sm"></i></button>';
        html += '<button type="button" class="btn btn-outline-danger btn-sm py-0 px-2" title="Delete" onclick="openDeleteModal(\'' + c.ContactId + '\', \'' + escHtml(getDisplayName(c)) + '\')"><i class="ti ti-trash fs-sm"></i></button>';
        html += '</div></td></tr>';
    });

    tbody.innerHTML = html;
}

// ----------------------------------------------------------------
// Filters
// ----------------------------------------------------------------
function applyFilters() {
    var search  = document.getElementById('<%= txtSearch.ClientID %>').value.toLowerCase().trim();
    var typeVal = document.getElementById('<%= ddlContactType.ClientID %>').value;
    var status  = document.getElementById('<%= ddlStatus.ClientID %>').value;

    var filtered = _allContacts.filter(function (c) {
        if (status === 'active' && !c.IsActive) return false;
        if (typeVal && (!c.ContactTypes || c.ContactTypes.indexOf(typeVal) === -1)) return false;
        if (search) {
            var hay = [c.FirstName, c.LastName, c.CompanyName, c.Email, c.Phone, c.MobilePhone].join(' ').toLowerCase();
            if (hay.indexOf(search) === -1) return false;
        }
        return true;
    });
    renderContacts(filtered);
}

function clearFilters() {
    document.getElementById('<%= txtSearch.ClientID %>').value      = '';
    document.getElementById('<%= ddlContactType.ClientID %>').value = '';
    document.getElementById('<%= ddlStatus.ClientID %>').value      = 'active';
    renderContacts(_allContacts);
}

// ----------------------------------------------------------------
// Validation
// ----------------------------------------------------------------
function validateForm() {
    var errors = [];
    clearValidationState();

    var firstName = document.getElementById('txtFirstName').value.trim();
    var lastName  = document.getElementById('txtLastName').value.trim();
    var email     = document.getElementById('txtEmail').value.trim();
    var phone     = document.getElementById('txtPhone').value.trim();
    var mobile    = document.getElementById('txtMobilePhone').value.trim();

    if (!firstName && !lastName) {
        errors.push('Please enter at least a First Name or Last Name.');
        document.getElementById('txtFirstName').classList.add('is-invalid');
        document.getElementById('txtLastName').classList.add('is-invalid');
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        errors.push('Please enter a valid email address.');
        document.getElementById('txtEmail').classList.add('is-invalid');
    }
    if (phone && phone.replace(/\D/g, '').length !== 10) {
        errors.push('Phone must be 10 digits (e.g. (555) 123-4567).');
        document.getElementById('txtPhone').classList.add('is-invalid');
    }
    if (mobile && mobile.replace(/\D/g, '').length !== 10) {
        errors.push('Mobile phone must be 10 digits.');
        document.getElementById('txtMobilePhone').classList.add('is-invalid');
    }

    if (errors.length) {
        var ul = document.getElementById('ulValidationErrors');
        ul.innerHTML = errors.map(function (e) { return '<li>' + escHtml(e) + '</li>'; }).join('');
        document.getElementById('divValidationErrors').classList.remove('d-none');
        return false;
    }
    return true;
}

function clearValidationState() {
    document.getElementById('divValidationErrors').classList.add('d-none');
    document.getElementById('ulValidationErrors').innerHTML = '';
    ['txtFirstName','txtLastName','txtEmail','txtPhone','txtMobilePhone'].forEach(function (id) {
        document.getElementById(id).classList.remove('is-invalid');
    });
}

// ----------------------------------------------------------------
// Modal open/close
// ----------------------------------------------------------------
function openAddContactModal() {
    document.getElementById('hdnContactId').value            = '';
    document.getElementById('modalContactLabel').textContent = 'Add Contact';
    clearModalForm();
    new bootstrap.Modal(document.getElementById('modalContact')).show();
}

function openEditContactModal(contactId) {
    var c = _allContacts.find(function (x) { return x.ContactId === contactId; });
    if (!c) return;

    document.getElementById('hdnContactId').value            = c.ContactId;
    document.getElementById('modalContactLabel').textContent = 'Edit Contact';
    document.getElementById('txtFirstName').value            = c.FirstName   || '';
    document.getElementById('txtLastName').value             = c.LastName    || '';
    document.getElementById('txtCompanyName').value          = c.CompanyName || '';
    document.getElementById('txtEmail').value                = c.Email       || '';
    document.getElementById('txtPhone').value                = c.Phone       || '';
    document.getElementById('txtMobilePhone').value          = c.MobilePhone || '';
    document.getElementById('txtLinkedInUrl').value          = c.LinkedInUrl || '';
    document.getElementById('txtNotes').value                = c.Notes       || '';

    clearValidationState();
    document.getElementById('divDupWarning').classList.add('d-none');

    document.querySelectorAll('#divContactTypes input[type=checkbox]').forEach(function (cb) {
        cb.checked = c.ContactTypes && c.ContactTypes.indexOf(cb.dataset.name) !== -1;
    });

    new bootstrap.Modal(document.getElementById('modalContact')).show();
}

function clearModalForm() {
    ['txtFirstName','txtLastName','txtCompanyName','txtEmail','txtPhone','txtMobilePhone','txtLinkedInUrl','txtNotes'].forEach(function (id) {
        document.getElementById(id).value = '';
    });
    document.querySelectorAll('#divContactTypes input[type=checkbox]').forEach(function (cb) { cb.checked = false; });
    clearValidationState();
    document.getElementById('divDupWarning').classList.add('d-none');
}

// ----------------------------------------------------------------
// Save — fetch to ContactHandler.ashx
// ----------------------------------------------------------------
function saveContact() {
    if (!validateForm()) return;

    var btn = document.getElementById('btnSaveContact');
    btn.disabled  = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';

    var selectedTypeIds = [];
    document.querySelectorAll('#divContactTypes input[type=checkbox]:checked').forEach(function (cb) {
        selectedTypeIds.push(cb.value);
    });

    var payload = {
        ContactId:      document.getElementById('hdnContactId').value || null,
        FirstName:      document.getElementById('txtFirstName').value.trim(),
        LastName:       document.getElementById('txtLastName').value.trim(),
        CompanyName:    document.getElementById('txtCompanyName').value.trim(),
        Email:          document.getElementById('txtEmail').value.trim(),
        Phone:          document.getElementById('txtPhone').value.trim(),
        MobilePhone:    document.getElementById('txtMobilePhone').value.trim(),
        LinkedInUrl:    document.getElementById('txtLinkedInUrl').value.trim(),
        Notes:          document.getElementById('txtNotes').value.trim(),
        ContactTypeIds: selectedTypeIds
    };

    fetch(_handlerUrl + '?action=save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    })
    .then(function (r) { return r.text(); })
    .then(function (raw) {
        btn.disabled  = false;
        btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save Contact';
        try {
            var res = JSON.parse(raw);
            if (res.success) {
                if (res.wasDuplicate) {
                    document.getElementById('divDupWarning').classList.remove('d-none');
                    document.getElementById('spnDupWarning').textContent = 'A contact with this email or phone already exists.';
                    document.getElementById('lnkViewDup').href = '/Secure/Contacts/Contact.aspx?contactId=' + res.contactId;
                } else {
                    bootstrap.Modal.getInstance(document.getElementById('modalContact')).hide();
                    window.location.reload();
                }
            } else {
                alert('Error: ' + (res.error || 'Unknown error'));
            }
        } catch (e) {
            alert('Could not parse response: ' + raw);
        }
    })
    .catch(function (err) {
        btn.disabled  = false;
        btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save Contact';
        alert('Network error: ' + err.message);
    });
}

// ----------------------------------------------------------------
// Delete — fetch to ContactHandler.ashx
// ----------------------------------------------------------------
function openDeleteModal(contactId, displayName) {
    _deleteId = contactId;
    document.getElementById('spnDeleteName').textContent = displayName;
    new bootstrap.Modal(document.getElementById('modalDelete')).show();
}

function confirmDelete() {
    if (!_deleteId) return;
    var btn = document.getElementById('btnConfirmDelete');
    btn.disabled  = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Deleting...';

    fetch(_handlerUrl + '?action=delete&contactId=' + _deleteId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: '{}'
    })
    .then(function () {
        bootstrap.Modal.getInstance(document.getElementById('modalDelete')).hide();
        window.location.reload();
    })
    .catch(function (err) {
        btn.disabled  = false;
        btn.innerHTML = '<i class="ti ti-trash me-1"></i>Delete';
        alert('Network error: ' + err.message);
    });
}

// ----------------------------------------------------------------
// Helpers
// ----------------------------------------------------------------
function buildTypeCheckboxes() {
    var html = '';
    _contactTypes.forEach(function (t) {
        html += '<div class="form-check form-check-inline mb-1">';
        html += '<input class="form-check-input" type="checkbox" id="ct_' + t.ContactTypeId + '" value="' + t.ContactTypeId + '" data-name="' + escHtml(t.Name) + '">';
        html += '<label class="form-check-label fs-sm" for="ct_' + t.ContactTypeId + '">' + escHtml(t.Name) + '</label>';
        html += '</div>';
    });
    document.getElementById('divContactTypes').innerHTML = html;
}

function populateTypeFilter() {
    var dd = document.getElementById('<%= ddlContactType.ClientID %>');
    _contactTypes.forEach(function (t) {
        var opt = document.createElement('option');
        opt.value = t.Name; opt.textContent = t.Name;
        dd.appendChild(opt);
    });
}

function buildTypeBadges(types) {
    if (!types || !types.length) return '<span class="text-muted fs-xs">&mdash;</span>';
    var colors = { 'Property Owner':'text-bg-primary','Buyer':'text-bg-success','Seller':'text-bg-warning','Decision Maker':'text-bg-info','Attorney':'text-bg-secondary','Property Manager':'text-bg-secondary','Broker / Agent':'text-bg-secondary','LLC / Trust Representative':'text-bg-dark','Third Party':'text-bg-secondary','Other':'text-bg-secondary' };
    var priority = ['Buyer', 'Seller'];

    // Sort: Buyer and Seller first, then everything else
    var sorted = types.slice().sort(function (a, b) {
        var ai = priority.indexOf(a);
        var bi = priority.indexOf(b);
        if (ai === -1 && bi === -1) return 0;
        if (ai === -1) return 1;
        if (bi === -1) return -1;
        return ai - bi;
    });

    return sorted.slice(0, 3).map(function (t) {
        var isPriority = priority.indexOf(t) !== -1;
        var sizeClass  = isPriority ? 'fs-xs fw-bold' : 'fs-xs opacity-75';
        var style      = isPriority ? ' style="font-size:0.8rem!important;"' : ' style="font-size:0.7rem!important;"';
        return '<span class="badge ' + (colors[t] || 'text-bg-secondary') + ' ' + sizeClass + ' me-1"' + style + '>' + escHtml(t) + '</span>';
    }).join('') + (sorted.length > 3 ? '<span class="text-muted fs-xs">+' + (sorted.length - 3) + '</span>' : '');
}

function buildScorePill(score) {
    if (score === null || score === undefined) return '<span class="text-muted fs-xs">&mdash;</span>';
    var cls = score >= 70 ? 'text-bg-success' : score >= 40 ? 'text-bg-warning' : 'text-bg-danger';
    var lbl = score >= 70 ? 'High' : score >= 40 ? 'Med' : 'Low';
    return '<span class="badge ' + cls + ' fs-xs">' + score + ' <span class="opacity-75">' + lbl + '</span></span>';
}

function buildPropertyCount(props) {
    if (!props || !props.length) return '<span class="text-muted fs-xs">None</span>';
    return '<span class="badge text-bg-light text-dark border fs-xs"><i class="ti ti-building me-1"></i>' + props.length + ' propert' + (props.length === 1 ? 'y' : 'ies') + '</span>';
}

function getInitials(first, last, company) {
    if (first && last) return (first[0] + last[0]).toUpperCase();
    if (first) return first[0].toUpperCase();
    if (last)  return last[0].toUpperCase();
    if (company) return company[0].toUpperCase();
    return '?';
}

function getAvatarColor(id) {
    var sum = 0;
    for (var i = 0; i < Math.min((id||'').length, 8); i++) sum += id.charCodeAt(i);
    return _avatarColors[sum % _avatarColors.length];
}

function getDisplayName(c) {
    return ((c.FirstName || '') + ' ' + (c.LastName || '')).trim() || c.CompanyName || 'this contact';
}

function escHtml(str) {
    if (!str) return '';
    return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&#39;');
}
</script>

</asp:Content>
