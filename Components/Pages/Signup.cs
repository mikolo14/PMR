using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PIMP_MY_RIDE.Services;

namespace PIMP_MY_RIDE.Components.Pages;

[Route("/signup")]
public sealed class Signup : ComponentBase
{
    [Inject] private SupabaseGymService Backend { get; set; } = null!;
    [Inject] private NavigationManager  Nav     { get; set; } = null!;

    // ── Form state ────────────────────────────────────────────────────
    private string  name      = "";
    private string  username  = "";
    private string  email     = "";
    private string  phone     = "";
    private string  password  = "";
    private bool    showPw;
    private bool    submitting;
    private string? validationErr;
    private string? serverErr;
    private string? DisplayError => validationErr ?? serverErr;

    // ── Success state ─────────────────────────────────────────────────
    private bool    accountCreated;
    private string  createdUsername = "";
    private string  createdPassword = "";

    // ── Render ────────────────────────────────────────────────────────
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        if (accountCreated)
            RenderSuccess(b);
        else
            RenderForm(b);
    }

    // ════════════════════════════════════════════════════════════════
    //  SUCCESS SCREEN
    // ════════════════════════════════════════════════════════════════
    private void RenderSuccess(RenderTreeBuilder b)
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "auth-solo");

        // Header
        b.OpenElement(2, "div");
        b.AddAttribute(3, "class", "auth-solo-header");
        b.AddMarkupContent(4,
            "<i class=\"auth-orb orb-a\"></i><i class=\"auth-orb orb-b\"></i>" +
            "<div class=\"auth-solo-brand\">" +
              "<span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span>" +
              "<span>PMR</span>" +
            "</div>" +
            "<div class=\"auth-solo-tagline\">" +
              "<span class=\"eyebrow\">YOU'RE IN</span>" +
              "<h1>Account created!</h1>" +
              "<p>Your PMR membership account is ready.</p>" +
            "</div>");
        b.CloseElement();

        // Body
        b.OpenElement(5, "div");
        b.AddAttribute(6, "class", "auth-solo-body");

        // Success card
        b.AddMarkupContent(7,
            "<div class=\"success-card\">" +
              "<div class=\"success-icon\">✓</div>" +
              "<p>Your account has been created. Sign in below to access your membership.</p>" +
            "</div>");

        // Credentials summary
        b.OpenElement(8, "div");
        b.AddAttribute(9, "class", "created-creds");
        b.AddMarkupContent(10, "<small>YOUR LOGIN DETAILS</small>");
        b.OpenElement(11, "div");
        b.AddAttribute(12, "class", "cred-row");
        b.AddMarkupContent(13, "<span>Username</span>");
        b.OpenElement(14, "b");
        b.AddContent(15, createdUsername);
        b.CloseElement();
        b.CloseElement();
        b.OpenElement(16, "div");
        b.AddAttribute(17, "class", "cred-row");
        b.AddMarkupContent(18, "<span>Password</span>");
        b.AddMarkupContent(19, "<b>••••••••</b>");
        b.CloseElement();
        b.CloseElement(); // created-creds

        // Go to sign in button
        b.OpenElement(20, "button");
        b.AddAttribute(21, "class", "primary-button wide");
        b.AddAttribute(22, "type",  "button");
        b.AddAttribute<MouseEventArgs>(23, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, GoToSignIn));
        b.AddContent(24, "Go to sign in ");
        b.AddMarkupContent(25, "<span>→</span>");
        b.CloseElement();

        b.OpenElement(26, "footer");
        b.AddAttribute(27, "class", "auth-solo-footer");
        b.AddContent(28, "© 2026 PMR · Here for your next milestone");
        b.CloseElement();

        b.CloseElement(); // body
        b.CloseElement(); // auth-solo
    }

    // ════════════════════════════════════════════════════════════════
    //  SIGN-UP FORM
    // ════════════════════════════════════════════════════════════════
    private void RenderForm(RenderTreeBuilder b)
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "auth-solo");

        // Header
        b.OpenElement(2, "div");
        b.AddAttribute(3, "class", "auth-solo-header");
        b.AddMarkupContent(4,
            "<i class=\"auth-orb orb-a\"></i><i class=\"auth-orb orb-b\"></i>" +
            "<div class=\"auth-solo-brand\">" +
              "<span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span>" +
              "<span>PMR</span>" +
            "</div>" +
            "<div class=\"auth-solo-tagline\">" +
              "<span class=\"eyebrow\">MEMBER PORTAL</span>" +
              "<h1>Create account</h1>" +
              "<p>Join your gym community and manage your membership.</p>" +
            "</div>");
        b.CloseElement();

        // Body
        b.OpenElement(5, "div");
        b.AddAttribute(6, "class", "auth-solo-body");

        // Error
        if (!string.IsNullOrWhiteSpace(DisplayError))
        {
            b.OpenElement(7, "div");
            b.AddAttribute(8, "class", "auth-feedback");
            b.AddAttribute(9, "role", "alert");
            b.AddContent(10, DisplayError);
            b.CloseElement();
        }

        // Form
        b.OpenElement(11, "form");
        b.AddAttribute(12, "class", "signup-form");
        b.AddAttribute(13, "autocomplete", "off");
        b.AddAttribute(14, "novalidate", true);
        b.AddAttribute<EventArgs>(15, "onsubmit",
            EventCallback.Factory.Create<EventArgs>(this, SubmitAsync));
        b.AddEventPreventDefaultAttribute(16, "onsubmit", true);

        RenderField(b, 17, "su-name",  "Full name",     "text",  "e.g. Chika Okafor",  "words", name,     v => name     = v);
        RenderField(b, 28, "su-user",  "Username",      "text",  "Choose a username",  "off",   username, v => username = v);
        RenderField(b, 39, "su-email", "Email address", "email", "you@example.com",    "off",   email,    v => email    = v);

        // Phone (optional)
        b.OpenElement(50, "div");
        b.AddAttribute(51, "class", "signup-field");
        b.OpenElement(52, "label");
        b.AddAttribute(53, "for", "su-phone");
        b.AddContent(54, "Phone ");
        b.OpenElement(55, "small"); b.AddContent(56, "(optional)"); b.CloseElement();
        b.CloseElement();
        b.OpenElement(57, "input");
        b.AddAttribute(58, "id", "su-phone"); b.AddAttribute(59, "type", "tel");
        b.AddAttribute(60, "placeholder", "080 1234 5678"); b.AddAttribute(61, "autocomplete", "off");
        b.AddAttribute(62, "value", phone);
        b.AddAttribute<ChangeEventArgs>(63, "onchange",
            EventCallback.Factory.CreateBinder(this, v => phone = v ?? "", phone));
        b.SetUpdatesAttributeName("value");
        b.CloseElement(); b.CloseElement();

        // Password
        b.OpenElement(64, "div"); b.AddAttribute(65, "class", "signup-field password-field");
        b.OpenElement(66, "label"); b.AddAttribute(67, "for", "su-pw"); b.AddContent(68, "Password"); b.CloseElement();
        b.OpenElement(69, "input");
        b.AddAttribute(70, "id", "su-pw"); b.AddAttribute(71, "type", showPw ? "text" : "password");
        b.AddAttribute(72, "placeholder", "Create a password"); b.AddAttribute(73, "autocomplete", "off");
        b.AddAttribute(74, "minlength", "8"); b.AddAttribute(75, "required", true);
        b.AddAttribute(76, "value", password);
        b.AddAttribute<ChangeEventArgs>(77, "onchange",
            EventCallback.Factory.CreateBinder(this, v => password = v ?? "", password));
        b.SetUpdatesAttributeName("value"); b.CloseElement();
        b.OpenElement(78, "button"); b.AddAttribute(79, "type", "button"); b.AddAttribute(80, "class", "password-toggle");
        b.AddAttribute(81, "aria-label", showPw ? "Hide password" : "Show password");
        b.AddAttribute<MouseEventArgs>(82, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => { showPw = !showPw; }));
        b.AddMarkupContent(83, showPw
            ? "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"m4 4 16 16\"/></svg>"
            : "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/></svg>");
        b.CloseElement(); b.CloseElement();

        // Submit
        b.OpenElement(84, "button"); b.AddAttribute(85, "class", "primary-button wide");
        b.AddAttribute(86, "type", "submit"); b.AddAttribute(87, "disabled", submitting);
        b.AddContent(88, submitting ? "Creating account…" : "Create account");
        if (!submitting) b.AddMarkupContent(89, " <span>→</span>");
        b.CloseElement();

        b.OpenElement(90, "small"); b.AddAttribute(91, "class", "form-hint");
        b.AddContent(92, "No email verification required. Your account is ready immediately.");
        b.CloseElement();

        b.CloseElement(); // form

        // Switch link
        b.OpenElement(93, "div"); b.AddAttribute(94, "class", "auth-switch");
        b.AddContent(95, "Already a member? ");
        b.OpenElement(96, "button"); b.AddAttribute(97, "type", "button");
        b.AddAttribute<MouseEventArgs>(98, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Nav.NavigateTo("/")));
        b.AddContent(99, "Sign in"); b.CloseElement();
        b.CloseElement();

        b.OpenElement(100, "footer"); b.AddAttribute(101, "class", "auth-solo-footer");
        b.AddContent(102, "© 2026 PMR · Here for your next milestone"); b.CloseElement();

        b.CloseElement(); // body
        b.CloseElement(); // auth-solo
    }

    // ── Field helper ──────────────────────────────────────────────────
    private void RenderField(RenderTreeBuilder b, int seq,
        string id, string label, string type, string placeholder,
        string autocapitalize, string value, Action<string> setter)
    {
        b.OpenElement(seq,    "div"); b.AddAttribute(seq+1, "class", "signup-field");
        b.OpenElement(seq+2,  "label"); b.AddAttribute(seq+3, "for", id); b.AddContent(seq+4, label); b.CloseElement();
        b.OpenElement(seq+5,  "input");
        b.AddAttribute(seq+6,  "id",            id);
        b.AddAttribute(seq+7,  "type",          type);
        b.AddAttribute(seq+8,  "placeholder",   placeholder);
        b.AddAttribute(seq+9,  "autocomplete",  "off");
        b.AddAttribute(seq+10, "autocapitalize", autocapitalize);
        b.AddAttribute(seq+11, "value",          value);
        b.AddAttribute<ChangeEventArgs>(seq+12, "onchange",
            EventCallback.Factory.CreateBinder(this, v => setter(v ?? ""), value));
        b.SetUpdatesAttributeName("value");
        b.CloseElement(); b.CloseElement();
    }

    // ── Logic ─────────────────────────────────────────────────────────
    private async Task SubmitAsync()
    {
        if (submitting) return;
        validationErr = Validate();
        serverErr = null;
        if (validationErr is not null) { await InvokeAsync(StateHasChanged); return; }

        submitting = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            var needsConfirm = await Backend.SignUpMemberAsync(
                name.Trim(), username.Trim(), email.Trim(), phone.Trim(), password);

            if (needsConfirm)
            {
                serverErr = "Email confirmation required. Disable \"Confirm email\" in Supabase → Authentication → Providers → Email, then try again.";
                return;
            }

            // Sign out the auto-created session so user logs in manually
            try { await Backend.SignOutAsync(); } catch { }

            // Store credentials to pre-fill the login page
            createdUsername = username.Trim();
            createdPassword = password;
            accountCreated  = true;
        }
        catch (Exception ex)
        {
            serverErr = $"[{ex.GetType().Name}] {ex.Message}";
        }
        finally
        {
            submitting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void GoToSignIn()
    {
        // Pass credentials via query string so Home.cs can pre-fill them
        Nav.NavigateTo($"/?u={Uri.EscapeDataString(createdUsername)}&p={Uri.EscapeDataString(createdPassword)}");
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(name)) return "Enter your full name.";

        var u = username.Trim();
        if (u.Length is < 3 or > 30 || !char.IsAsciiLetterOrDigit(u[0])
            || u.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not '-'))
            return "Username: 3–30 chars, letters/numbers/dots/underscores/hyphens, start with letter or number.";

        if (!Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
            return "Enter a valid email address.";

        if (password.Length < 8) return "Password must be at least 8 characters.";

        return null;
    }
}
