using System.Net.Mail;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PIMP_MY_RIDE.Services;

namespace PIMP_MY_RIDE.Components.Pages;

[Route("/signup")]
public sealed class Signup : ComponentBase
{
    // ── Injected services ─────────────────────────────────────────────
    [Inject] private SupabaseGymService Backend { get; set; } = null!;
    [Inject] private NavigationManager  Nav     { get; set; } = null!;

    // ── State ─────────────────────────────────────────────────────────
    private string  name       = "";
    private string  username   = "";
    private string  email      = "";
    private string  phone      = "";
    private string  password   = "";
    private bool    showPw;
    private bool    submitting;
    private string? validationErr;
    private string? serverErr;
    private string? DisplayError => validationErr ?? serverErr;

    // ── Render ────────────────────────────────────────────────────────
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        // Root wrapper
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "auth-solo");

        // ── Dark green header ────────────────────────────────────────
        b.OpenElement(2, "div");
        b.AddAttribute(3, "class", "auth-solo-header");

        b.AddMarkupContent(4,
            "<i class=\"auth-orb orb-a\"></i>" +
            "<i class=\"auth-orb orb-b\"></i>");

        // Brand
        b.AddMarkupContent(5,
            "<div class=\"auth-solo-brand\">" +
              "<span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span>" +
              "<span>PMR</span>" +
            "</div>");

        // Tagline
        b.AddMarkupContent(6,
            "<div class=\"auth-solo-tagline\">" +
              "<span class=\"eyebrow\">MEMBER PORTAL</span>" +
              "<h1>Create account</h1>" +
              "<p>Join your gym community and manage your membership.</p>" +
            "</div>");

        b.CloseElement(); // auth-solo-header

        // ── White body ───────────────────────────────────────────────
        b.OpenElement(7, "div");
        b.AddAttribute(8, "class", "auth-solo-body");

        // Error banner
        if (!string.IsNullOrWhiteSpace(DisplayError))
        {
            b.OpenElement(9, "div");
            b.AddAttribute(10, "class", "auth-feedback");
            b.AddAttribute(11, "role", "alert");
            b.AddContent(12, DisplayError);
            b.CloseElement();
        }

        // ── Form ─────────────────────────────────────────────────────
        b.OpenElement(13, "form");
        b.AddAttribute(14, "class", "signup-form");
        b.AddAttribute(15, "autocomplete", "off");
        b.AddAttribute(16, "novalidate", true);
        b.AddAttribute<EventArgs>(17, "onsubmit",
            EventCallback.Factory.Create<EventArgs>(this, SubmitAsync));
        b.AddEventPreventDefaultAttribute(18, "onsubmit", true);

        // Full name
        RenderField(b, 19, "su-name",   "Full name",     "text",     "e.g. Chika Okafor",
            "words", name,  v => name  = v, false);

        // Username
        RenderField(b, 30, "su-user",   "Username",      "text",     "Choose a username",
            "off",   username, v => username = v, false);

        // Email
        RenderField(b, 41, "su-email",  "Email address", "email",    "you@example.com",
            "off",   email, v => email  = v, false);

        // Phone (optional)
        b.OpenElement(52, "div");
        b.AddAttribute(53, "class", "signup-field");
        b.OpenElement(54, "label");
        b.AddAttribute(55, "for", "su-phone");
        b.AddContent(56, "Phone number ");
        b.OpenElement(57, "small");
        b.AddContent(58, "(optional)");
        b.CloseElement();
        b.CloseElement(); // label
        b.OpenElement(59, "input");
        b.AddAttribute(60, "id",          "su-phone");
        b.AddAttribute(61, "type",        "tel");
        b.AddAttribute(62, "placeholder", "080 1234 5678");
        b.AddAttribute(63, "autocomplete","off");
        b.AddAttribute(64, "value",       phone);
        b.AddAttribute<ChangeEventArgs>(65, "onchange",
            EventCallback.Factory.CreateBinder(this, v => phone = v ?? "", phone));
        b.SetUpdatesAttributeName("value");
        b.CloseElement(); // input
        b.CloseElement(); // div.signup-field

        // Password + toggle
        b.OpenElement(66, "div");
        b.AddAttribute(67, "class", "signup-field password-field");
        b.OpenElement(68, "label");
        b.AddAttribute(69, "for", "su-pw");
        b.AddContent(70, "Password");
        b.CloseElement();
        b.OpenElement(71, "input");
        b.AddAttribute(72, "id",           "su-pw");
        b.AddAttribute(73, "type",         showPw ? "text" : "password");
        b.AddAttribute(74, "placeholder",  "Create a password");
        b.AddAttribute(75, "autocomplete", "off");
        b.AddAttribute(76, "minlength",    "8");
        b.AddAttribute(77, "required",     true);
        b.AddAttribute(78, "value",        password);
        b.AddAttribute<ChangeEventArgs>(79, "onchange",
            EventCallback.Factory.CreateBinder(this, v => password = v ?? "", password));
        b.SetUpdatesAttributeName("value");
        b.CloseElement(); // input
        // Eye toggle
        b.OpenElement(80, "button");
        b.AddAttribute(81, "type",       "button");
        b.AddAttribute(82, "class",      "password-toggle");
        b.AddAttribute(83, "aria-label", showPw ? "Hide password" : "Show password");
        b.AddAttribute<MouseEventArgs>(84, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => { showPw = !showPw; }));
        b.AddMarkupContent(85, showPw
            ? "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"m4 4 16 16\"/></svg>"
            : "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/></svg>");
        b.CloseElement(); // button
        b.CloseElement(); // div.signup-field.password-field

        // Submit button
        b.OpenElement(86, "button");
        b.AddAttribute(87, "class",    "primary-button wide");
        b.AddAttribute(88, "type",     "submit");
        b.AddAttribute(89, "disabled", submitting);
        b.AddContent(90, submitting ? "Creating account…" : "Create account");
        if (!submitting)
            b.AddMarkupContent(91, " <span>→</span>");
        b.CloseElement();

        // Hint
        b.OpenElement(92, "small");
        b.AddAttribute(93, "class", "form-hint");
        b.AddContent(94, "No email verification required. Your account is ready immediately.");
        b.CloseElement();

        b.CloseElement(); // form

        // Switch to sign-in
        b.OpenElement(95, "div");
        b.AddAttribute(96, "class", "auth-switch");
        b.AddContent(97, "Already a member? ");
        b.OpenElement(98, "button");
        b.AddAttribute(99, "type", "button");
        b.AddAttribute<MouseEventArgs>(100, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Nav.NavigateTo("/")));
        b.AddContent(101, "Sign in");
        b.CloseElement(); // button
        b.CloseElement(); // div.auth-switch

        // Footer
        b.OpenElement(102, "footer");
        b.AddAttribute(103, "class", "auth-solo-footer");
        b.AddContent(104, "© 2026 PMR · Here for your next milestone");
        b.CloseElement();

        b.CloseElement(); // auth-solo-body
        b.CloseElement(); // auth-solo
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private void RenderField(
        RenderTreeBuilder b, int seq,
        string id, string label, string type, string placeholder,
        string autocapitalize, string value, Action<string> setter,
        bool required)
    {
        b.OpenElement(seq,     "div");
        b.AddAttribute(seq+1,  "class", "signup-field");
        b.OpenElement(seq+2,   "label");
        b.AddAttribute(seq+3,  "for",   id);
        b.AddContent(seq+4,    label);
        b.CloseElement(); // label
        b.OpenElement(seq+5,   "input");
        b.AddAttribute(seq+6,  "id",            id);
        b.AddAttribute(seq+7,  "type",          type);
        b.AddAttribute(seq+8,  "placeholder",   placeholder);
        b.AddAttribute(seq+9,  "autocomplete",  "off");
        b.AddAttribute(seq+10, "autocapitalize", autocapitalize);
        if (required) b.AddAttribute(seq+11, "required", true);
        b.AddAttribute(seq+12, "value",          value);
        b.AddAttribute<ChangeEventArgs>(seq+13, "onchange",
            EventCallback.Factory.CreateBinder(this, v => setter(v ?? ""), value));
        b.SetUpdatesAttributeName("value");
        b.CloseElement(); // input
        b.CloseElement(); // div
    }

    // ── Logic ─────────────────────────────────────────────────────────
    private async Task SubmitAsync()
    {
        if (submitting) return;
        validationErr = Validate();
        if (validationErr is not null) return;

        submitting = true;
        serverErr  = null;
        await InvokeAsync(StateHasChanged);
        try
        {
            var needsConfirm = await Backend.SignUpMemberAsync(
                name.Trim(), username.Trim(), email.Trim(), phone.Trim(), password);

            if (needsConfirm)
            {
                serverErr =
                    $"A confirmation email was sent to {email.Trim()}. " +
                    "Confirm it, then sign in. " +
                    "(To skip this: Supabase → Authentication → Providers → Email → " +
                    "disable \"Confirm email\".)";
                return;
            }

            Nav.NavigateTo("/");
        }
        catch (Exception ex) { serverErr = ex.Message; }
        finally { submitting = false; }
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Enter your full name.";

        var u = username.Trim();
        if (u.Length is < 3 or > 30
            || !char.IsAsciiLetterOrDigit(u[0])
            || u.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not '-'))
            return "Username: 3–30 chars, letters/numbers/dots/underscores/hyphens, start with letter or number.";

        if (!MailAddress.TryCreate(email.Trim(), out var addr)
            || !string.Equals(addr.Address, email.Trim(), StringComparison.OrdinalIgnoreCase))
            return "Enter a valid email address.";

        if (password.Length < 8)
            return "Password must be at least 8 characters.";

        return null;
    }
}
