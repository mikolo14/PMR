using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PIMP_MY_RIDE.Services;

namespace PIMP_MY_RIDE.Components.Pages;

[Route("/")]
public sealed class Home : ComponentBase
{
    // ── Injected services ─────────────────────────────────────────────
    [Inject] private SupabaseGymService Backend   { get; set; } = null!;
    [Inject] private NavigationManager  Nav       { get; set; } = null!;
    [Inject] private MemberQrScanner    QrScanner { get; set; } = null!;

    // ── Query string params (pre-filled from signup) ───────────────────
    [SupplyParameterFromQuery(Name = "u")] private string? PrefillUsername { get; set; }
    [SupplyParameterFromQuery(Name = "p")] private string? PrefillPassword { get; set; }

    // ── State ─────────────────────────────────────────────────────────
    private bool   isSignedIn;
    private bool   signingIn;
    private string loginUsername = "";
    private string loginPassword = "";
    private bool   showLoginPw;
    private string? authError;
    private string  section = "Overview";
    private string? toast;
    private bool    checkingIn;
    private string? checkinMsg;
    private MemberProfile?               currentMember;
    private IReadOnlyList<PaymentRecord> memberPayments = Array.Empty<PaymentRecord>();

    // ── Computed ──────────────────────────────────────────────────────
    private bool IsMembershipActive =>
        currentMember?.MembershipEndsOn is DateOnly d &&
        d >= DateOnly.FromDateTime(DateTime.Now);

    private string DisplayName =>
        string.IsNullOrWhiteSpace(currentMember?.FullName) ? "Member" : currentMember.FullName;

    private string Initials => string.Concat(
        DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Take(2).Select(p => char.ToUpperInvariant(p[0])));

    private string PlanName =>
        IsMembershipActive
            ? (currentMember?.Plan?.Name ?? "Active membership")
            : "No active plan";

    private string RenewalLabel =>
        currentMember?.MembershipEndsOn is DateOnly d
            ? d.ToString("d MMMM yyyy") : "Not active";

    // ── Lifecycle ─────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        // Pre-fill credentials passed from signup page
        if (!string.IsNullOrWhiteSpace(PrefillUsername))
            loginUsername = PrefillUsername;
        if (!string.IsNullOrWhiteSpace(PrefillPassword))
            loginPassword = PrefillPassword;

        // Restore session — if token is valid, go straight to dashboard
        try
        {
            currentMember = await Backend.GetCurrentMemberAsync();
            if (currentMember is not null)
            {
                memberPayments = await Backend.GetPaymentsAsync(50);
                isSignedIn = true;
            }
        }
        catch { /* no valid session — show sign-in */ }
    }

    // ── Render ────────────────────────────────────────────────────────
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        if (isSignedIn)
            RenderDashboard(b);
        else
            RenderSignIn(b);
    }

    // ════════════════════════════════════════════════════════════════
    //  SIGN-IN SCREEN
    // ════════════════════════════════════════════════════════════════
    private void RenderSignIn(RenderTreeBuilder b)
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "auth-solo");

        // Dark green header
        b.OpenElement(2, "div");
        b.AddAttribute(3, "class", "auth-solo-header");
        b.AddMarkupContent(4,
            "<i class=\"auth-orb orb-a\"></i>" +
            "<i class=\"auth-orb orb-b\"></i>" +
            "<div class=\"auth-solo-brand\">" +
              "<span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span>" +
              "<span>PMR</span>" +
            "</div>" +
            "<div class=\"auth-solo-tagline\">" +
              "<span class=\"eyebrow\">MEMBER PORTAL</span>" +
              "<h1>Welcome back</h1>" +
              "<p>Sign in to check your membership and payments.</p>" +
            "</div>");
        b.CloseElement();

        // White body
        b.OpenElement(5, "div");
        b.AddAttribute(6, "class", "auth-solo-body");

        // Error banner
        if (!string.IsNullOrWhiteSpace(authError))
        {
            b.OpenElement(7, "div");
            b.AddAttribute(8, "class", "auth-feedback");
            b.AddAttribute(9, "role", "alert");
            b.AddContent(10, authError);
            b.CloseElement();
        }

        // Form
        b.OpenElement(11, "form");
        b.AddAttribute(12, "class", "auth-form");
        b.AddAttribute(13, "autocomplete", "off");
        b.AddAttribute<EventArgs>(14, "onsubmit",
            EventCallback.Factory.Create<EventArgs>(this, SignInAsync));
        b.AddEventPreventDefaultAttribute(15, "onsubmit", true);

        // Username field
        b.OpenElement(16, "label");
        b.AddAttribute(17, "for", "li-user");
        b.AddContent(18, "Username");
        b.OpenElement(19, "input");
        b.AddAttribute(20, "id",           "li-user");
        b.AddAttribute(21, "type",         "text");
        b.AddAttribute(22, "placeholder",  "Your username");
        b.AddAttribute(23, "autocomplete", "username");
        b.AddAttribute(24, "required",     true);
        b.AddAttribute(25, "value",        loginUsername);
        b.AddAttribute<ChangeEventArgs>(26, "oninput",
            EventCallback.Factory.CreateBinder(this, v => loginUsername = v ?? "", loginUsername));
        b.SetUpdatesAttributeName("value");
        b.CloseElement(); // input
        b.CloseElement(); // label

        // Password field + eye toggle
        b.OpenElement(27, "label");
        b.AddAttribute(28, "for",   "li-pw");
        b.AddAttribute(29, "class", "password-label");
        b.AddContent(30, "Password");
        b.OpenElement(31, "input");
        b.AddAttribute(32, "id",           "li-pw");
        b.AddAttribute(33, "type",         showLoginPw ? "text" : "password");
        b.AddAttribute(34, "placeholder",  "Enter your password");
        b.AddAttribute(35, "autocomplete", "current-password");
        b.AddAttribute(36, "required",     true);
        b.AddAttribute(37, "value",        loginPassword);
        b.AddAttribute<ChangeEventArgs>(38, "oninput",
            EventCallback.Factory.CreateBinder(this, v => loginPassword = v ?? "", loginPassword));
        b.SetUpdatesAttributeName("value");
        b.CloseElement(); // input
        b.OpenElement(39, "button");
        b.AddAttribute(40, "type",       "button");
        b.AddAttribute(41, "class",      "password-toggle");
        b.AddAttribute(42, "aria-label", showLoginPw ? "Hide password" : "Show password");
        b.AddAttribute<MouseEventArgs>(43, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => { showLoginPw = !showLoginPw; }));
        b.AddMarkupContent(44, showLoginPw
            ? "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"m4 4 16 16\"/></svg>"
            : "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/></svg>");
        b.CloseElement(); // button toggle
        b.CloseElement(); // label

        // Forgot row
        b.OpenElement(45, "div");
        b.AddAttribute(46, "class", "form-between");
        b.OpenElement(47, "small");
        b.AddContent(48, "Use your username and password");
        b.CloseElement();
        b.OpenElement(49, "button");
        b.AddAttribute(50, "type", "button");
        b.AddAttribute<MouseEventArgs>(51, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                toast = "Contact your gym administrator to reset your password."));
        b.AddContent(52, "Forgot password?");
        b.CloseElement();
        b.CloseElement(); // div.form-between

        // Submit
        b.OpenElement(53, "button");
        b.AddAttribute(54, "class",    "primary-button wide");
        b.AddAttribute(55, "type",     "submit");
        b.AddAttribute(56, "disabled", signingIn);
        b.AddContent(57, signingIn ? "Signing in…" : "Sign in");
        if (!signingIn) b.AddMarkupContent(58, " <span>→</span>");
        b.CloseElement();

        b.CloseElement(); // form

        // Switch to sign-up
        b.OpenElement(59, "div");
        b.AddAttribute(60, "class", "auth-switch");
        b.AddContent(61, "New to the gym? ");
        b.OpenElement(62, "button");
        b.AddAttribute(63, "type", "button");
        b.AddAttribute<MouseEventArgs>(64, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Nav.NavigateTo("/signup")));
        b.AddContent(65, "Create an account");
        b.CloseElement();
        b.CloseElement();

        // Footer
        b.OpenElement(66, "footer");
        b.AddAttribute(67, "class", "auth-solo-footer");
        b.AddContent(68, "© 2026 PMR · Here for your next milestone");
        b.CloseElement();

        b.CloseElement(); // auth-solo-body
        b.CloseElement(); // auth-solo

        // Toast (outside auth-solo so it floats above everything)
        RenderToast(b, 69);
    }

    // ════════════════════════════════════════════════════════════════
    //  MEMBER DASHBOARD
    // ════════════════════════════════════════════════════════════════
    private void RenderDashboard(RenderTreeBuilder b)
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "member-app");

        b.OpenElement(2, "div");
        b.AddAttribute(3, "class", "member-shell");

        RenderSidebar(b, 4);
        RenderMainArea(b, 100);

        b.CloseElement(); // member-shell

        RenderMobileNav(b, 600);
        RenderToast(b, 650);

        b.CloseElement(); // member-app
    }

    // ── Sidebar ───────────────────────────────────────────────────────
    private void RenderSidebar(RenderTreeBuilder b, int seq)
    {
        b.OpenElement(seq,   "aside");
        b.AddAttribute(seq+1,"class", "sidebar");

        // Brand
        b.OpenElement(seq+2, "a");
        b.AddAttribute(seq+3,"class",  "brand");
        b.AddAttribute(seq+4,"href",   "#");
        b.AddAttribute<MouseEventArgs>(seq+5, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Go("Overview")));
        b.AddEventPreventDefaultAttribute(seq+6, "onclick", true);
        b.AddMarkupContent(seq+7,
            "<span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span>" +
            "<span>PMR</span>");
        b.CloseElement();

        b.AddMarkupContent(seq+8, "<div class=\"side-caption\">MEMBER SPACE</div>");

        // Nav buttons
        b.OpenElement(seq+9,  "nav");
        b.AddAttribute(seq+10,"class","side-nav");
        NavBtn(b, seq+11, "Overview",   "⌂", "Overview");
        NavBtn(b, seq+20, "Membership", "◈", "My membership");
        NavBtn(b, seq+29, "Payments",   "▤", "Payments");
        NavBtn(b, seq+38, "Check-in",   "▦", "Check in");
        NavBtn(b, seq+47, "Profile",    "◎", "My profile");
        b.CloseElement(); // nav

        b.AddMarkupContent(seq+56, "<div class=\"sidebar-spacer\"></div>");

        // Help widget
        b.OpenElement(seq+57, "div");
        b.AddAttribute(seq+58,"class","sidebar-help");
        b.AddMarkupContent(seq+59, "<b>✦</b><strong>Need a hand?</strong><p>Our team is happy to help with your membership.</p>");
        b.OpenElement(seq+60, "button");
        b.AddAttribute<MouseEventArgs>(seq+61,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this,
                () => toast = "Contact your gym for membership support."));
        b.AddContent(seq+62,"Contact the gym ↗");
        b.CloseElement();
        b.CloseElement();

        // User footer
        b.OpenElement(seq+63,"div");
        b.AddAttribute(seq+64,"class","sidebar-user");
        b.OpenElement(seq+65,"span");
        b.AddAttribute(seq+66,"class","avatar");
        b.AddContent(seq+67, Initials);
        b.CloseElement();
        b.OpenElement(seq+68,"span");
        b.OpenElement(seq+69,"b");
        b.AddContent(seq+70, DisplayName);
        b.CloseElement();
        b.AddMarkupContent(seq+71,"<small>PMR member</small>");
        b.CloseElement();
        b.OpenElement(seq+72,"button");
        b.AddAttribute(seq+73,"class",    "logout");
        b.AddAttribute(seq+74,"aria-label","Sign out");
        b.AddAttribute<MouseEventArgs>(seq+75,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, SignOutAsync));
        b.AddContent(seq+76,"↗");
        b.CloseElement();
        b.CloseElement(); // sidebar-user

        b.CloseElement(); // aside
    }

    // ── Main area ─────────────────────────────────────────────────────
    private void RenderMainArea(RenderTreeBuilder b, int seq)
    {
        b.OpenElement(seq,   "div");
        b.AddAttribute(seq+1,"class","main-area");

        // Topbar
        b.OpenElement(seq+2, "header");
        b.AddAttribute(seq+3,"class","topbar");
        b.AddMarkupContent(seq+4,
            "<div class=\"mobile-brand\">" +
              "<span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span> PMR" +
            "</div>");
        b.OpenElement(seq+5,"div");
        b.AddAttribute(seq+6,"class","breadcrumbs");
        b.AddContent(seq+7,"Member space ");
        b.AddMarkupContent(seq+8,"<span>/</span> ");
        b.OpenElement(seq+9,"b");
        b.AddContent(seq+10, section);
        b.CloseElement();
        b.CloseElement();
        b.OpenElement(seq+11,"div");
        b.AddAttribute(seq+12,"class","top-actions");
        b.OpenElement(seq+13,"time");
        b.AddContent(seq+14, DateTime.Now.ToString("ddd, d MMM"));
        b.CloseElement();
        b.AddMarkupContent(seq+15,"<button class=\"bell\" aria-label=\"Notifications\">♧<i></i></button>");
        b.OpenElement(seq+16,"button");
        b.AddAttribute(seq+17,"class","avatar top-avatar");
        b.AddAttribute(seq+18,"aria-label","My profile");
        b.AddAttribute<MouseEventArgs>(seq+19,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Go("Profile")));
        b.AddContent(seq+20, Initials);
        b.CloseElement();
        b.CloseElement(); // top-actions
        b.CloseElement(); // header

        // Content
        b.OpenElement(seq+21,"main");
        b.AddAttribute(seq+22,"class","content");

        if (section == "Overview")   RenderOverview(b,   seq+23);
        if (section == "Membership") RenderMembership(b, seq+23);
        if (section == "Payments")   RenderPayments(b,   seq+23);
        if (section == "Check-in")   RenderCheckin(b,    seq+23);
        if (section == "Profile")    RenderProfile(b,    seq+23);

        b.CloseElement(); // main
        b.CloseElement(); // main-area
    }

    // ── Overview section ──────────────────────────────────────────────
    private void RenderOverview(RenderTreeBuilder b, int seq)
    {
        // Welcome
        b.OpenElement(seq,   "section");
        b.AddAttribute(seq+1,"class","welcome");
        b.OpenElement(seq+2, "div");
        b.AddMarkupContent(seq+3,"<span class=\"eyebrow\">YOUR MEMBERSHIP</span>");
        b.OpenElement(seq+4, "h1");
        b.AddContent(seq+5, $"Welcome back, {DisplayName} ");
        b.AddMarkupContent(seq+6,"<span>✳</span>");
        b.CloseElement();
        b.OpenElement(seq+7,"p");
        b.AddContent(seq+8, IsMembershipActive
            ? "Your membership is active. Keep up the great work."
            : "Make your first payment to activate your gym membership.");
        b.CloseElement();
        b.CloseElement(); // div
        b.OpenElement(seq+9,"div");
        b.AddAttribute(seq+10,"class","member-id");
        b.AddMarkupContent(seq+11,"<i></i> MEMBER ID ");
        b.OpenElement(seq+12,"b");
        b.AddContent(seq+13, currentMember?.MemberCode ?? "—");
        b.CloseElement();
        b.CloseElement();
        b.CloseElement(); // section.welcome

        // Feature grid
        b.OpenElement(seq+14,"section");
        b.AddAttribute(seq+15,"class","feature-grid");

        // Member card
        b.OpenElement(seq+16,"article");
        b.AddAttribute(seq+17,"class","member-card");
        b.OpenElement(seq+18,"div");
        b.AddAttribute(seq+19,"class","card-top");
        b.AddMarkupContent(seq+20,"<b><i>✳</i> LIFTDESK</b>");
        if (IsMembershipActive)
            b.AddMarkupContent(seq+21,"<span class=\"active-pill\"><i></i> ACTIVE MEMBER</span>");
        else
            b.AddMarkupContent(seq+22,"<span class=\"inactive-pill\">PAYMENT REQUIRED</span>");
        b.CloseElement();
        b.AddMarkupContent(seq+23,"<div class=\"card-label\">YOUR CURRENT PLAN</div>");
        b.OpenElement(seq+24,"h2");
        b.AddContent(seq+25, PlanName);
        b.CloseElement();
        b.AddMarkupContent(seq+26,"<p>Full gym access <span>·</span> 1 month</p><div class=\"card-rule\"></div>");
        b.OpenElement(seq+27,"div");
        b.AddAttribute(seq+28,"class","card-foot");
        b.OpenElement(seq+29,"div");
        b.AddMarkupContent(seq+30,"<small>NEXT RENEWAL</small>");
        b.OpenElement(seq+31,"b");
        b.AddContent(seq+32, RenewalLabel);
        b.CloseElement();
        b.CloseElement();
        b.AddMarkupContent(seq+33,"<div><small>PLAN PRICE</small><b class=\"muted-white\">Set by your gym</b></div>");
        b.CloseElement();
        b.AddMarkupContent(seq+34,"<i class=\"card-watermark\">✳</i>");
        b.CloseElement(); // member-card

        // Renewal card
        b.OpenElement(seq+35,"article");
        b.AddAttribute(seq+36,"class","renewal-card");
        b.AddMarkupContent(seq+37,
            "<div class=\"renewal-top\">" +
              "<span class=\"renewal-icon\">↗</span>" +
              "<span class=\"upcoming\">UPCOMING</span>" +
            "</div>" +
            "<span class=\"eyebrow\">YOUR NEXT PAYMENT</span>" +
            "<h2>Keep your momentum.</h2>");
        b.OpenElement(seq+38,"p");
        b.AddContent(seq+39,"Your plan renews on ");
        b.OpenElement(seq+40,"b");
        b.AddContent(seq+41, RenewalLabel);
        b.CloseElement();
        b.AddContent(seq+42,". Pay securely when you're ready.");
        b.CloseElement();
        b.OpenElement(seq+43,"button");
        b.AddAttribute(seq+44,"class","primary-button");
        b.AddAttribute<MouseEventArgs>(seq+45,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this,
                () => toast = "Paystack payment integration coming soon."));
        b.AddContent(seq+46,"Pay with Paystack ");
        b.AddMarkupContent(seq+47,"<span>→</span>");
        b.CloseElement();
        b.AddMarkupContent(seq+48,"<small class=\"secure\">⌑ Secure payment powered by Paystack</small>");
        b.CloseElement(); // renewal-card

        b.CloseElement(); // feature-grid

        // Quick grid
        b.OpenElement(seq+49,"section");
        b.AddAttribute(seq+50,"class","quick-grid");
        QuickCard(b, seq+51, "Membership", "green", "◈", "MEMBERSHIP STATUS",
            IsMembershipActive ? "Active" : "Inactive", "View details");
        QuickCard(b, seq+60, "Payments",   "amber", "▤", "PAYMENT HISTORY",
            $"{memberPayments.Count} records", "See all payments");
        QuickCard(b, seq+69, "Check-in",   "blue",  "▦", "GYM CHECK-IN",
            "Scan QR code", "Record your visit");
        b.CloseElement();

        b.OpenElement(seq+78,"div");
        b.AddAttribute(seq+79,"class","member-footer");
        b.AddMarkupContent(seq+80,"© 2026 PMR <b>·</b> Here for your next milestone");
        b.CloseElement();
    }

    // ── Membership section ────────────────────────────────────────────
    private void RenderMembership(RenderTreeBuilder b, int seq)
    {
        b.AddMarkupContent(seq,
            "<div class=\"page-heading\">" +
              "<span class=\"eyebrow\">YOUR PLAN</span>" +
              "<h1>My membership</h1>" +
              "<p>Your current plan and renewal details.</p>" +
            "</div>");

        b.OpenElement(seq+1,"article");
        b.AddAttribute(seq+2,"class","detail-card");
        b.OpenElement(seq+3,"div");
        b.AddMarkupContent(seq+4,"<span class=\"detail-label\">CURRENT PLAN</span>");
        b.OpenElement(seq+5,"h2");
        b.AddContent(seq+6, PlanName);
        b.CloseElement();
        b.AddMarkupContent(seq+7,"<p>Full gym access · Monthly renewal</p>");
        b.CloseElement();
        b.OpenElement(seq+8,"div");
        b.AddAttribute(seq+9,"class","detail-side");
        b.AddMarkupContent(seq+10,"<small>STATUS</small>");
        b.OpenElement(seq+11,"b");
        b.AddContent(seq+12, IsMembershipActive ? "Active" : "Inactive");
        b.CloseElement();
        b.AddMarkupContent(seq+13,"<em>RENEWS ON</em>");
        b.OpenElement(seq+14,"b");
        b.AddContent(seq+15, RenewalLabel);
        b.CloseElement();
        b.CloseElement();
        b.CloseElement(); // detail-card

        b.AddMarkupContent(seq+16,
            "<div class=\"plans-head\">" +
              "<span class=\"eyebrow\">AVAILABLE PLANS</span>" +
              "<h2>Choose a plan</h2>" +
              "<p>All plans include full gym access. Pay at your next renewal.</p>" +
            "</div>");

        b.OpenElement(seq+17,"section");
        b.AddAttribute(seq+18,"class","plan-grid");
        PlanCard(b, seq+19, "Monthly",   "1 MONTH",   "green", "◷", "Flexible access, renewed monthly.");
        PlanCard(b, seq+28, "Quarterly", "3 MONTHS",  "amber", "◈", "Three months of uninterrupted training.");
        PlanCard(b, seq+37, "Annual",    "12 MONTHS", "blue",  "✳", "A full year to build a routine.");
        b.CloseElement();
    }

    // ── Payments section ──────────────────────────────────────────────
    private void RenderPayments(RenderTreeBuilder b, int seq)
    {
        b.AddMarkupContent(seq,
            "<div class=\"page-heading\">" +
              "<span class=\"eyebrow\">BILLING</span>" +
              "<h1>Payments</h1>" +
              "<p>Your membership payment history.</p>" +
            "</div>");

        // Next payment
        b.OpenElement(seq+1,"div");
        b.AddAttribute(seq+2,"class","next-payment");
        b.OpenElement(seq+3,"div");
        b.AddMarkupContent(seq+4,"<span class=\"eyebrow\">NEXT PAYMENT DUE</span>");
        b.OpenElement(seq+5,"h2");
        b.AddContent(seq+6, RenewalLabel);
        b.CloseElement();
        b.OpenElement(seq+7,"p");
        b.AddContent(seq+8, $"Renews your {currentMember?.Plan?.Name ?? "current"} plan");
        b.CloseElement();
        b.CloseElement();
        b.OpenElement(seq+9,"button");
        b.AddAttribute(seq+10,"class","primary-button");
        b.AddAttribute<MouseEventArgs>(seq+11,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this,
                () => toast = "Paystack payment integration coming soon."));
        b.AddContent(seq+12,"Pay with Paystack ");
        b.AddMarkupContent(seq+13,"<span>→</span>");
        b.CloseElement();
        b.CloseElement(); // next-payment

        // History panel
        b.OpenElement(seq+14,"div");
        b.AddAttribute(seq+15,"class","panel history-panel");
        b.OpenElement(seq+16,"div");
        b.AddAttribute(seq+17,"class","panel-head");
        b.AddMarkupContent(seq+18,
            "<div><span class=\"eyebrow\">HISTORY</span><h2>Past payments</h2></div>");
        b.OpenElement(seq+19,"span");
        b.AddAttribute(seq+20,"class","history-count");
        b.AddContent(seq+21, $"{memberPayments.Count} total");
        b.CloseElement();
        b.CloseElement();

        if (memberPayments.Count == 0)
        {
            b.AddMarkupContent(seq+22,
                "<div class=\"empty-large\">" +
                  "<span class=\"empty-icon\">▤</span>" +
                  "<div><b>No payments yet</b>" +
                  "<p>Your payment history will appear here after your first payment.</p></div>" +
                "</div>");
        }
        else
        {
            b.OpenElement(seq+22,"div");
            b.AddAttribute(seq+23,"class","admin-table-wrap");
            b.OpenElement(seq+24,"table");
            b.AddAttribute(seq+25,"class","admin-table");
            b.AddMarkupContent(seq+26,
                "<thead><tr>" +
                  "<th>PLAN</th><th>AMOUNT</th><th>METHOD</th><th>DATE</th><th>STATUS</th>" +
                "</tr></thead>");
            b.OpenElement(seq+27,"tbody");
            int row = seq+28;
            foreach (var p in memberPayments)
            {
                b.OpenElement(row,   "tr");
                b.OpenElement(row+1, "td"); b.AddContent(row+2, p.Plan?.Name ?? "—"); b.CloseElement();
                b.OpenElement(row+3, "td");
                b.OpenElement(row+4, "b"); b.AddContent(row+5, $"₦{p.AmountNgn:N0}"); b.CloseElement();
                b.CloseElement();
                b.OpenElement(row+6, "td"); b.AddContent(row+7, p.Method); b.CloseElement();
                b.OpenElement(row+8, "td"); b.AddContent(row+9, p.CreatedAt.ToLocalTime().ToString("d MMM yyyy")); b.CloseElement();
                b.OpenElement(row+10,"td");
                b.OpenElement(row+11,"span");
                b.AddAttribute(row+12,"class",$"admin-status{(p.Status == "successful" ? " success" : "")}");
                b.AddContent(row+13, p.Status);
                b.CloseElement();
                b.CloseElement();
                b.CloseElement(); // tr
                row += 14;
            }
            b.CloseElement(); // tbody
            b.CloseElement(); // table
            b.CloseElement(); // div
        }
        b.CloseElement(); // panel

        b.AddMarkupContent(seq+22+14,
            "<div class=\"security-note\">" +
              "<span>⌑</span>" +
              "<div><b>Secure payments via Paystack</b>" +
              "<p>Your card details are never stored on our servers.</p></div>" +
            "</div>");
    }

    // ── Check-in section ──────────────────────────────────────────────
    private void RenderCheckin(RenderTreeBuilder b, int seq)
    {
        b.AddMarkupContent(seq,
            "<div class=\"page-heading\">" +
              "<span class=\"eyebrow\">ATTENDANCE</span>" +
              "<h1>Check in</h1>" +
              "<p>Scan the gym QR code to record your visit.</p>" +
            "</div>");

        b.OpenElement(seq+1,"div");
        b.AddAttribute(seq+2,"class","checkin-layout");

        // Scan card
        b.OpenElement(seq+3,"article");
        b.AddAttribute(seq+4,"class","checkin-card");
        b.AddMarkupContent(seq+5,
            "<div class=\"checkin-icon\">▦</div>" +
            "<span class=\"eyebrow\">GYM CHECK-IN</span>" +
            "<h2>Scan &amp; check in</h2>" +
            "<p>Point your camera at the QR code at the gym entrance to record today's visit.</p>");
        b.OpenElement(seq+6,"button");
        b.AddAttribute(seq+7,"class","primary-button");
        b.AddAttribute(seq+8,"disabled", checkingIn);
        b.AddAttribute<MouseEventArgs>(seq+9,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, CheckInAsync));
        b.AddContent(seq+10, checkingIn ? "Opening camera…" : "Scan gym QR code");
        b.AddMarkupContent(seq+11," <span>→</span>");
        b.CloseElement();
        if (!string.IsNullOrWhiteSpace(checkinMsg))
        {
            b.OpenElement(seq+12,"small");
            b.AddAttribute(seq+13,"class","secure");
            b.AddContent(seq+14, checkinMsg);
            b.CloseElement();
        }
        b.CloseElement(); // article

        // Help panel
        b.AddMarkupContent(seq+15,
            "<div class=\"panel checkin-help\">" +
              "<span class=\"eyebrow\">HOW IT WORKS</span>" +
              "<h2>Three steps to check in</h2>" +
              "<ol>" +
                "<li>Tap <b>Scan gym QR code</b> above.</li>" +
                "<li>Point your camera at the QR code at the gym entrance.</li>" +
                "<li>Your attendance is recorded automatically.</li>" +
              "</ol>" +
              "<div class=\"info-banner\"><span>ⓘ</span>" +
                "One check-in per day. The QR code refreshes every 2 minutes for security." +
              "</div>" +
            "</div>");

        b.CloseElement(); // checkin-layout
    }

    // ── Profile section ───────────────────────────────────────────────
    private void RenderProfile(RenderTreeBuilder b, int seq)
    {
        // ── Hero card ─────────────────────────────────────────────────
        b.OpenElement(seq, "div");
        b.AddAttribute(seq+1, "class", "profile-page");

        // Avatar + name hero
        b.OpenElement(seq+2, "div");
        b.AddAttribute(seq+3, "class", "profile-hero");

        b.OpenElement(seq+4, "div");
        b.AddAttribute(seq+5, "class", "profile-hero-avatar");
        b.AddContent(seq+6, Initials);
        b.CloseElement();

        b.OpenElement(seq+7, "div");
        b.AddAttribute(seq+8, "class", "profile-hero-info");
        b.OpenElement(seq+9, "h1");
        b.AddContent(seq+10, DisplayName);
        b.CloseElement();
        b.OpenElement(seq+11, "span");
        b.AddAttribute(seq+12, "class", "profile-hero-username");
        b.AddContent(seq+13, "@" + (currentMember?.Username ?? ""));
        b.CloseElement();
        b.OpenElement(seq+14, "div");
        b.AddAttribute(seq+15, "class", "profile-hero-badges");
        if (IsMembershipActive)
            b.AddMarkupContent(seq+16, "<span class=\"profile-badge green\"><i></i> Active member</span>");
        else
            b.AddMarkupContent(seq+17, "<span class=\"profile-badge amber\">No active plan</span>");
        b.OpenElement(seq+18, "span");
        b.AddAttribute(seq+19, "class", "profile-badge grey");
        b.AddContent(seq+20, currentMember?.MemberCode ?? "—");
        b.CloseElement();
        b.CloseElement(); // badges
        b.CloseElement(); // hero-info
        b.CloseElement(); // profile-hero

        // ── Info section ──────────────────────────────────────────────
        b.OpenElement(seq+21, "div");
        b.AddAttribute(seq+22, "class", "profile-section");
        b.AddMarkupContent(seq+23, "<div class=\"profile-section-title\">Personal details</div>");

        ProfileInfoRow(b, seq+24, "person",   "Full name",    currentMember?.FullName ?? "—");
        ProfileInfoRow(b, seq+32, "at",        "Username",     currentMember?.Username ?? "—");
        ProfileInfoRow(b, seq+40, "mail",      "Email",        currentMember?.Email    ?? "—");
        ProfileInfoRow(b, seq+48, "phone",     "Phone",        string.IsNullOrWhiteSpace(currentMember?.Phone) ? "Not added" : currentMember!.Phone);

        b.CloseElement(); // profile-section

        // ── Membership section ────────────────────────────────────────
        b.OpenElement(seq+56, "div");
        b.AddAttribute(seq+57, "class", "profile-section");
        b.AddMarkupContent(seq+58, "<div class=\"profile-section-title\">Membership</div>");

        ProfileInfoRow(b, seq+59, "plan",    "Current plan",  PlanName);
        ProfileInfoRow(b, seq+67, "renewal", "Renews on",     RenewalLabel);
        ProfileInfoRow(b, seq+75, "id",      "Member ID",     currentMember?.MemberCode ?? "—");

        b.CloseElement(); // profile-section

        // ── Sign out section ──────────────────────────────────────────
        b.OpenElement(seq+83, "div");
        b.AddAttribute(seq+84, "class", "profile-signout-section");

        b.OpenElement(seq+85, "div");
        b.AddAttribute(seq+86, "class", "profile-signout-card");

        b.OpenElement(seq+87, "div");
        b.AddAttribute(seq+88, "class", "profile-signout-info");
        b.OpenElement(seq+89, "b"); b.AddContent(seq+90, "Sign out"); b.CloseElement();
        b.OpenElement(seq+91, "p"); b.AddContent(seq+92, "You'll be returned to the sign-in screen."); b.CloseElement();
        b.CloseElement();

        b.OpenElement(seq+93, "button");
        b.AddAttribute(seq+94, "class", "profile-signout-btn");
        b.AddAttribute<MouseEventArgs>(seq+95, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, SignOutAsync));
        b.AddMarkupContent(seq+96,
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
              "<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4\"/>" +
              "<polyline points=\"16 17 21 12 16 7\"/>" +
              "<line x1=\"21\" y1=\"12\" x2=\"9\" y2=\"12\"/>" +
            "</svg>" +
            "Sign out");
        b.CloseElement(); // button

        b.CloseElement(); // signout-card
        b.CloseElement(); // signout-section

        b.OpenElement(seq+97, "footer");
        b.AddAttribute(seq+98, "class", "profile-page-footer");
        b.AddContent(seq+99, "© 2026 PMR · Here for your next milestone");
        b.CloseElement();

        b.CloseElement(); // profile-page
    }

    private void ProfileInfoRow(RenderTreeBuilder b, int seq,
        string icon, string label, string value)
    {
        string svg = icon switch
        {
            "person"  => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 20c0-4 3.6-7 8-7s8 3 8 7\"/></svg>",
            "at"      => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M16 8v5a3 3 0 0 0 6 0v-1a10 10 0 1 0-3.92 7.94\"/></svg>",
            "mail"    => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"2\" y=\"4\" width=\"20\" height=\"16\" rx=\"2\"/><path d=\"m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7\"/></svg>",
            "phone"   => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 13a19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 3.62 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z\"/></svg>",
            "plan"    => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z\"/></svg>",
            "renewal" => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/></svg>",
            _         => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"2\" y=\"2\" width=\"20\" height=\"20\" rx=\"2\"/><path d=\"M12 8v8m-4-4h8\"/></svg>",
        };

        b.OpenElement(seq,   "div"); b.AddAttribute(seq+1, "class", "profile-info-row");
        b.OpenElement(seq+2, "div"); b.AddAttribute(seq+3, "class", "profile-info-icon");
        b.AddMarkupContent(seq+4, svg);
        b.CloseElement();
        b.OpenElement(seq+5, "div"); b.AddAttribute(seq+6, "class", "profile-info-text");
        b.OpenElement(seq+7, "small"); b.AddContent(seq+8, label); b.CloseElement();
        b.OpenElement(seq+9, "b");    b.AddContent(seq+10, value); b.CloseElement();
        b.CloseElement();
        b.CloseElement();
    }

    // ── Mobile bottom nav ─────────────────────────────────────────────
    private void RenderMobileNav(RenderTreeBuilder b, int seq)
    {
        b.OpenElement(seq,   "nav");
        b.AddAttribute(seq+1,"class","mobile-nav");
        MobileNavBtn(b, seq+2,  "Overview",   "⌂", "Home");
        MobileNavBtn(b, seq+10, "Membership", "◈", "Plan");
        MobileNavBtn(b, seq+18, "Payments",   "▤", "Payments");
        MobileNavBtn(b, seq+26, "Check-in",   "▦", "Check in");
        MobileNavBtn(b, seq+34, "Profile",    "◎", "Profile");
        b.CloseElement();
    }

    // ── Toast ─────────────────────────────────────────────────────────
    private void RenderToast(RenderTreeBuilder b, int seq)
    {
        if (string.IsNullOrWhiteSpace(toast)) return;
        b.OpenElement(seq,   "div");
        b.AddAttribute(seq+1,"class","toast");
        b.AddAttribute(seq+2,"role","status");
        b.AddContent(seq+3, $"ⓘ {toast} ");
        b.OpenElement(seq+4,"button");
        b.AddAttribute(seq+5,"class","toast-close");
        b.AddAttribute(seq+6,"aria-label","Dismiss");
        b.AddAttribute<MouseEventArgs>(seq+7,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => toast = null));
        b.AddContent(seq+8,"×");
        b.CloseElement();
        b.CloseElement();
    }

    // ── Small helpers ─────────────────────────────────────────────────
    private void NavBtn(RenderTreeBuilder b, int seq, string id, string icon, string label)
    {
        b.OpenElement(seq,   "button");
        b.AddAttribute(seq+1,"class", section == id ? "selected" : "");
        b.AddAttribute<MouseEventArgs>(seq+2,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Go(id)));
        b.OpenElement(seq+3, "span"); b.AddContent(seq+4, icon); b.CloseElement();
        b.AddContent(seq+5, " " + label);
        b.CloseElement();
    }

    private void MobileNavBtn(RenderTreeBuilder b, int seq, string id, string icon, string label)
    {
        b.OpenElement(seq,   "button");
        b.AddAttribute(seq+1,"class", section == id ? "selected" : "");
        b.AddAttribute<MouseEventArgs>(seq+2,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Go(id)));
        b.OpenElement(seq+3,"span"); b.AddContent(seq+4, icon); b.CloseElement();
        b.OpenElement(seq+5,"small"); b.AddContent(seq+6, label); b.CloseElement();
        b.CloseElement();
    }

    private void QuickCard(RenderTreeBuilder b, int seq,
        string target, string colour, string icon, string eyebrow, string value, string sub)
    {
        b.OpenElement(seq,   "button");
        b.AddAttribute(seq+1,"class","quick-card");
        b.AddAttribute<MouseEventArgs>(seq+2,"onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Go(target)));
        b.OpenElement(seq+3,"i");
        b.AddAttribute(seq+4,"class",$"quick-icon {colour}");
        b.AddContent(seq+5, icon);
        b.CloseElement();
        b.OpenElement(seq+6,"span");
        b.OpenElement(seq+7,"small"); b.AddContent(seq+8, eyebrow); b.CloseElement();
        b.OpenElement(seq+9,"b");    b.AddContent(seq+10, value);   b.CloseElement();
        b.OpenElement(seq+11,"em");  b.AddContent(seq+12, sub);     b.CloseElement();
        b.CloseElement();
        b.AddMarkupContent(seq+13,"<strong>›</strong>");
        b.CloseElement();
    }

    private void PlanCard(RenderTreeBuilder b, int seq,
        string planName, string duration, string colour, string icon, string desc)
    {
        b.OpenElement(seq,   "article");
        b.AddAttribute(seq+1,"class","plan-card");
        b.OpenElement(seq+2,"div");
        b.AddAttribute(seq+3,"class",$"plan-icon {colour}");
        b.AddContent(seq+4, icon);
        b.CloseElement();
        b.OpenElement(seq+5,"small"); b.AddContent(seq+6, duration); b.CloseElement();
        b.OpenElement(seq+7,"h3");    b.AddContent(seq+8, planName); b.CloseElement();
        b.OpenElement(seq+9,"p");     b.AddContent(seq+10, desc);    b.CloseElement();
        b.OpenElement(seq+11,"b");    b.AddContent(seq+12,"Set by your gym"); b.CloseElement();
        if (currentMember?.Plan?.Name == planName)
        {
            b.OpenElement(seq+13,"span");
            b.AddAttribute(seq+14,"class","current-plan");
            b.AddContent(seq+15,"CURRENT PLAN");
            b.CloseElement();
        }
        b.CloseElement();
    }

    // ── Auth logic ────────────────────────────────────────────────────
    private async Task SignInAsync()
    {
        if (signingIn) return;
        authError = null;
        signingIn = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            await Backend.SignInWithUsernameAsync(loginUsername.Trim(), loginPassword);
            currentMember  = await Backend.GetCurrentMemberAsync();
            memberPayments = await Backend.GetPaymentsAsync(50);
            isSignedIn     = true;
        }
        catch (Exception ex)
        {
            authError = $"[{ex.GetType().Name}] {ex.Message}";
        }
        finally
        {
            signingIn = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SignOutAsync()
    {
        try { await Backend.SignOutAsync(); } catch { }
        isSignedIn     = false;
        currentMember  = null;
        memberPayments = Array.Empty<PaymentRecord>();
        loginUsername  = "";
        loginPassword  = "";
        authError      = null;
        section        = "Overview";
        // Navigate to clear query string params too
        Nav.NavigateTo("/", forceLoad: false);
    }

    private async Task CheckInAsync()
    {
        if (checkingIn) return;
        checkingIn  = true;
        checkinMsg  = null;
        try
        {
            var token = await QrScanner.ScanAsync();
            if (string.IsNullOrWhiteSpace(token)) { checkinMsg = "No QR code was scanned."; return; }
            await Backend.RecordMemberCheckinAsync(token);
            checkinMsg = "✓ Checked in successfully. Have a great workout!";
            toast      = "You're checked in for today!";
        }
        catch (Exception ex) { checkinMsg = ex.Message; }
        finally { checkingIn = false; }
    }

    private void Go(string s) { section = s; }
}
