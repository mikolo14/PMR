using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PIMP_MY_RIDE.Services;

namespace PIMP_MY_RIDE.Components.Pages;

[Route("/")]
public class Home : ComponentBase
{	private bool isSignedIn;

	private bool isSigningIn;

	private string? authError;

	private bool signingUp;

	private string section = "Overview";

	private string? toast;

	private string signupName = "";

	private string signupEmail = "";

	private string signupPhone = "";

	private string signupUsername = "";

	private string signupPassword = "";
	private bool showSignupPassword;
	private bool showLoginPassword;

	private string loginUsername = "";

	private string loginPassword = "";

	private bool awaitingEmailConfirmation;

	private MemberProfile? currentMember;

	private global::System.Collections.Generic.IReadOnlyList<PaymentRecord> memberPayments = global::System.Array.Empty<PaymentRecord>();

	private bool IsMembershipActive
	{
		get
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			DateOnly? val = currentMember?.MembershipEndsOn;
			int result;
			if (val.HasValue)
			{
				DateOnly valueOrDefault = val.GetValueOrDefault();
				result = ((valueOrDefault >= DateOnly.FromDateTime(global::System.DateTime.Now)) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}
	}

	private string MemberDisplayName => string.IsNullOrWhiteSpace(currentMember?.FullName) ? "Member" : currentMember.FullName;

	private string MemberInitials => string.Concat<char>(Enumerable.Select<string, char>(Enumerable.Take<string>((global::System.Collections.Generic.IEnumerable<string>)MemberDisplayName.Split(' ', (StringSplitOptions)1), 2), (Func<string, char>)((string part) => char.ToUpperInvariant(part[0]))));

	private string MemberPlanName => (!IsMembershipActive) ? "No active plan" : (currentMember?.Plan?.Name ?? "Active membership");

	private string MembershipEndLabel
	{
		get
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			MemberProfile? memberProfile = currentMember;
			object obj;
			if ((object)memberProfile == null)
			{
				obj = null;
			}
			else
			{
				DateOnly? membershipEndsOn = memberProfile.MembershipEndsOn;
				if (!membershipEndsOn.HasValue)
				{
					obj = null;
				}
				else
				{
					DateOnly valueOrDefault = membershipEndsOn.GetValueOrDefault();
					obj = valueOrDefault.ToString("d MMMM yyyy");
				}
			}
			if (obj == null)
			{
				obj = "Not active";
			}
			return (string)obj;
		}
	}

	[Inject]
	private NavigationManager Navigation
	{
		get;
		set;
	} = null;

	[Inject]
	private SupabaseGymService Backend
	{
		get;
		set;
	} = null;

	[Inject]
	private MemberQrScanner QrScanner
	{
		get;
		set;
	} = null;

	protected override void BuildRenderTree(RenderTreeBuilder __builder)
	{
		//IL_0c05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0f: Expected O, but got Unknown
		//IL_0c0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cea: Expected O, but got Unknown
		//IL_0ce5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d60: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d6a: Expected O, but got Unknown
		//IL_0d65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0de0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dea: Expected O, but got Unknown
		//IL_0de5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_089a: Unknown result type (might be due to invalid IL or missing references)
		//IL_096b: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ff: Expected O, but got Unknown
		//IL_09fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_069c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a6: Expected O, but got Unknown
		//IL_06a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_071b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0725: Expected O, but got Unknown
		//IL_0720: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e60: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e6a: Expected O, but got Unknown
		//IL_0e65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0f: Expected O, but got Unknown
		//IL_0b0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ee0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eea: Expected O, but got Unknown
		//IL_0ee5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fa3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fad: Expected O, but got Unknown
		//IL_0fa8: Unknown result type (might be due to invalid IL or missing references)
		//IL_10e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_12c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_12cf: Expected O, but got Unknown
		//IL_12ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_2cc7: Unknown result type (might be due to invalid IL or missing references)
		//IL_2cd1: Expected O, but got Unknown
		//IL_2ccc: Unknown result type (might be due to invalid IL or missing references)
		//IL_2527: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ffa: Unknown result type (might be due to invalid IL or missing references)
		//IL_2004: Expected O, but got Unknown
		//IL_1fff: Unknown result type (might be due to invalid IL or missing references)
		//IL_2134: Unknown result type (might be due to invalid IL or missing references)
		//IL_213e: Expected O, but got Unknown
		//IL_2139: Unknown result type (might be due to invalid IL or missing references)
		//IL_221e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2228: Expected O, but got Unknown
		//IL_2223: Unknown result type (might be due to invalid IL or missing references)
		//IL_23cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_23d9: Expected O, but got Unknown
		//IL_23d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_29ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_29b5: Expected O, but got Unknown
		//IL_29b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_177d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1787: Expected O, but got Unknown
		//IL_1782: Unknown result type (might be due to invalid IL or missing references)
		//IL_184c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1856: Expected O, but got Unknown
		//IL_1851: Unknown result type (might be due to invalid IL or missing references)
		//IL_2a2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_2a35: Expected O, but got Unknown
		//IL_2a30: Unknown result type (might be due to invalid IL or missing references)
		//IL_2aab: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ab5: Expected O, but got Unknown
		//IL_2ab0: Unknown result type (might be due to invalid IL or missing references)
		//IL_195d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1967: Expected O, but got Unknown
		//IL_1962: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a39: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a43: Expected O, but got Unknown
		//IL_1a3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b60: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b6a: Expected O, but got Unknown
		//IL_1b65: Unknown result type (might be due to invalid IL or missing references)
		//IL_2b2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_2b35: Expected O, but got Unknown
		//IL_2b30: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e22: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e2c: Expected O, but got Unknown
		//IL_1e27: Unknown result type (might be due to invalid IL or missing references)
		//IL_2bab: Unknown result type (might be due to invalid IL or missing references)
		//IL_2bb5: Expected O, but got Unknown
		//IL_2bb0: Unknown result type (might be due to invalid IL or missing references)
		//IL_28c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_28cf: Expected O, but got Unknown
		//IL_28ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ca8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cad: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cb1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cb6: Unknown result type (might be due to invalid IL or missing references)
		__builder.OpenElement(0, "div");
		__builder.AddAttribute(1, "class", "member-app");
		if (!isSignedIn)
		{
			__builder.OpenElement(2, "section");
			__builder.AddAttribute(3, "class", "auth-page " + (signingUp ? "signup-mode" : "signin-mode"));
			__builder.AddMarkupContent(4, "<div class=\"auth-visual\"><a class=\"brand brand-light\" href=\"/\"><span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span><span>PMR</span></a><div class=\"auth-message\"><span class=\"eyebrow light\">YOUR TRAINING HOME</span><h1>Show up for<br>yourself.</h1><p>Your membership, payments and gym updates — all in one place.</p></div><div class=\"auth-quote\"><b>“</b><p>Small steps every day add up to big change.</p><small>THE PMR COMMUNITY</small></div><i class=\"auth-orb orb-a\"></i><i class=\"auth-orb orb-b\"></i></div>\r\n        ");
			__builder.OpenElement(5, "div");
			__builder.AddAttribute(6, "class", "auth-panel");
			__builder.AddMarkupContent(7, "<div class=\"auth-mobile-brand\"><span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span> PMR</div>");
			__builder.OpenElement(8, "div");
			__builder.AddAttribute(9, "class", "auth-form-wrap");
			__builder.AddMarkupContent(10, "<span class=\"eyebrow\">MEMBER PORTAL</span>");
			__builder.OpenElement(11, "h2");
			__builder.AddContent(12, signingUp ? "Create your account" : "Welcome back");
			__builder.CloseElement();
			__builder.OpenElement(13, "p");
			__builder.AddAttribute(14, "class", "auth-intro");
			__builder.AddContent(15, signingUp ? "Join your gym community and manage your membership." : "Sign in to check your membership and payments.");
			__builder.CloseElement();
			if (signingUp)
            {
                __builder.OpenComponent<PIMP_MY_RIDE.Components.Pages.SignupForm>(16);
                __builder.AddAttribute(17, "AwaitingEmailConfirmation", awaitingEmailConfirmation);
                __builder.AddAttribute(18, "ConfirmationEmail", signupEmail);
                __builder.AddAttribute(19, "ErrorMessage", authError);
                __builder.AddAttribute(20, "OnSubmit", EventCallback.Factory.Create<PIMP_MY_RIDE.Components.Pages.SignupForm.Submission>(this, Signup));
                __builder.AddAttribute(21, "OnBackToSignIn", EventCallback.Factory.Create(this, () =>
                {
                    awaitingEmailConfirmation = false;
                    signingUp = false;
                }));
                __builder.CloseComponent();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(authError))
				{
					__builder.OpenElement(92, "div");
					__builder.AddAttribute(93, "class", "auth-feedback");
					__builder.AddAttribute(94, "role", "alert");
					__builder.AddContent(95, authError);
					__builder.CloseElement();
				}
				__builder.OpenElement(96, "form");
				__builder.AddAttribute(97, "class", "auth-form");
				__builder.AddAttribute<EventArgs>(98, "onsubmit", EventCallback.Factory.Create<EventArgs>((object)this, (Func<global::System.Threading.Tasks.Task>)Signin));
				WebRenderTreeBuilderExtensions.AddEventPreventDefaultAttribute(__builder, 99, "onsubmit", true);
				__builder.OpenElement(100, "label");
				__builder.AddContent(101, "Username");
				__builder.OpenElement(102, "input");
				__builder.AddAttribute(103, "placeholder", "Your username");
				__builder.AddAttribute(104, "autocomplete", "username");
				__builder.AddAttribute(105, "required");
				__builder.AddAttribute(106, "value", BindConverter.FormatValue(loginUsername, (CultureInfo)null));
				__builder.AddAttribute<ChangeEventArgs>(107, "oninput", EventCallbackFactoryBinderExtensions.CreateBinder(EventCallback.Factory, (object)this, (Action<string>)([CompilerGenerated] (string? __value) =>
				{
					loginUsername = __value;
				}), loginUsername, (CultureInfo)null));
				__builder.SetUpdatesAttributeName("value");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(108, "label");
				__builder.AddAttribute(109, "class", "password-label");
				__builder.AddContent(110, "Password");
				__builder.OpenElement(111, "input");
				__builder.AddAttribute(112, "type", showLoginPassword ? "text" : "password");
				__builder.AddAttribute(113, "placeholder", "Enter your password");
				__builder.AddAttribute(114, "autocomplete", "current-password");
				__builder.AddAttribute(115, "required");
				__builder.AddAttribute(116, "value", BindConverter.FormatValue(loginPassword, (CultureInfo)null));
				__builder.AddAttribute<ChangeEventArgs>(117, "oninput", EventCallbackFactoryBinderExtensions.CreateBinder(EventCallback.Factory, (object)this, (Action<string>)([CompilerGenerated] (string? __value) =>
				{
					loginPassword = __value;
				}), loginPassword, (CultureInfo)null));
				__builder.SetUpdatesAttributeName("value");
				__builder.CloseElement();
				__builder.OpenElement(118, "button");
				__builder.AddAttribute(119, "type", "button");
				__builder.AddAttribute(120, "class", "password-toggle");
				__builder.AddAttribute(121, "aria-label", showLoginPassword ? "Hide password" : "Show password");
				__builder.AddAttribute<MouseEventArgs>(122, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
				{
					showLoginPassword = !showLoginPassword;
				})));
				__builder.AddMarkupContent(123, showLoginPassword ? "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"m4 4 16 16\"/></svg>" : "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/></svg>");
				__builder.CloseElement();
					__builder.CloseElement();
				__builder.OpenElement(124, "div");
				__builder.AddAttribute(125, "class", "form-between");
				__builder.AddMarkupContent(126, "<small>Use your username and password</small>");
				__builder.OpenElement(127, "button");
				__builder.AddAttribute(128, "type", "button");
				__builder.AddAttribute<MouseEventArgs>(129, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(PasswordHelp)));
				__builder.AddContent(130, "Forgot password?");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(131, "button");
				__builder.AddAttribute(132, "class", "primary-button wide");
				__builder.AddAttribute(133, "type", "submit");
				__builder.AddAttribute(134, "disabled", isSigningIn);
				__builder.AddContent(135, isSigningIn ? "Signing in…" : "Sign in");
				if (!isSigningIn)
				{
					__builder.AddMarkupContent(136, "<span>→</span>");
				}
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(137, "div");
				__builder.AddAttribute(138, "class", "auth-switch");
				__builder.AddContent(139, "New to the gym? ");
				__builder.OpenElement(140, "button");
				__builder.AddAttribute<MouseEventArgs>(141, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
				{
					signingUp = true;
				})));
				__builder.AddContent(142, "Create an account");
				__builder.CloseElement();
				__builder.CloseElement();
			}
			__builder.CloseElement();
			__builder.AddMarkupContent(143, "<footer class=\"auth-legal\">© 2026 PMR <span>·</span> Here for your next milestone</footer>");
			__builder.CloseElement();
			__builder.CloseElement();
		}
		else
		{
			__builder.OpenElement(144, "div");
			__builder.AddAttribute(145, "class", "member-shell");
			__builder.OpenElement(146, "aside");
			__builder.AddAttribute(147, "class", "sidebar");
			__builder.OpenElement(148, "a");
			__builder.AddAttribute(149, "class", "brand");
			__builder.AddAttribute(150, "href", "#");
			__builder.AddAttribute<MouseEventArgs>(151, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Overview");
			})));
			WebRenderTreeBuilderExtensions.AddEventPreventDefaultAttribute(__builder, 152, "onclick", true);
			__builder.AddMarkupContent(153, "<span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span>");
			__builder.AddMarkupContent(154, "<span>PMR</span>");
			__builder.CloseElement();
			__builder.AddMarkupContent(155, "<div class=\"side-caption\">MEMBER SPACE</div>");
			__builder.OpenElement(156, "nav");
			__builder.AddAttribute(157, "class", "side-nav");
			__builder.OpenElement(158, "button");
			__builder.AddAttribute(159, "class", (section == "Overview") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(160, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Overview");
			})));
			__builder.AddMarkupContent(161, "<span>⌂</span> Overview");
			__builder.CloseElement();
			__builder.OpenElement(162, "button");
			__builder.AddAttribute(163, "class", (section == "Membership") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(164, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Membership");
			})));
			__builder.AddMarkupContent(165, "<span>◈</span> My membership");
			__builder.CloseElement();
			__builder.OpenElement(166, "button");
			__builder.AddAttribute(167, "class", (section == "Payments") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(168, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Payments");
			})));
			__builder.AddMarkupContent(169, "<span>▤</span> Payments");
			__builder.CloseElement();
			__builder.OpenElement(170, "button");
			__builder.AddAttribute(171, "class", (section == "Check-in") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(172, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Check-in");
			})));
			__builder.AddMarkupContent(173, "<span>▦</span> Check in");
			__builder.CloseElement();
			__builder.OpenElement(174, "button");
			__builder.AddAttribute(175, "class", (section == "Profile") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(176, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Profile");
			})));
			__builder.AddMarkupContent(177, "<span>◎</span> My profile");
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.AddMarkupContent(178, "<div class=\"sidebar-spacer\"></div>");
			__builder.OpenElement(179, "div");
			__builder.AddAttribute(180, "class", "sidebar-help");
			__builder.AddMarkupContent(181, "<b>✦</b>");
			__builder.AddMarkupContent(182, "<strong>Need a hand?</strong>");
			__builder.AddMarkupContent(183, "<p>Our team is happy to help with your membership.</p>");
			__builder.OpenElement(184, "button");
			__builder.AddAttribute<MouseEventArgs>(185, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(Support)));
			__builder.AddMarkupContent(186, "Contact the gym ↗");
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.OpenElement(187, "div");
			__builder.AddAttribute(188, "class", "sidebar-user");
			__builder.OpenElement(189, "span");
			__builder.AddAttribute(190, "class", "avatar");
			__builder.AddContent(191, MemberInitials);
			__builder.CloseElement();
			__builder.OpenElement(192, "span");
			__builder.OpenElement(193, "b");
			__builder.AddContent(194, MemberDisplayName);
			__builder.CloseElement();
			__builder.AddMarkupContent(195, "<small>PMR member</small>");
			__builder.CloseElement();
			__builder.OpenElement(196, "button");
			__builder.AddAttribute(197, "class", "logout");
			__builder.AddAttribute(198, "aria-label", "Sign out");
			__builder.AddAttribute<MouseEventArgs>(199, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Func<global::System.Threading.Tasks.Task>)Signout));
			__builder.AddMarkupContent(200, "↗");
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.AddMarkupContent(201, "\r\n        ");
			__builder.OpenElement(202, "div");
			__builder.AddAttribute(203, "class", "main-area");
			__builder.OpenElement(204, "header");
			__builder.AddAttribute(205, "class", "topbar");
			__builder.AddMarkupContent(206, "<div class=\"mobile-brand\"><span class=\"brand-mark\"><img src=\"/images/pmr-mark.svg\" alt=\"\" /></span> PMR</div>");
			__builder.OpenElement(207, "div");
			__builder.AddAttribute(208, "class", "breadcrumbs");
			__builder.AddContent(209, "Member space ");
			__builder.AddMarkupContent(210, "<span>/</span> ");
			__builder.OpenElement(211, "b");
			__builder.AddContent(212, section);
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.OpenElement(213, "div");
			__builder.AddAttribute(214, "class", "top-actions");
			__builder.OpenElement(215, "time");
			__builder.AddContent(216, global::System.DateTime.Now.ToString("ddd, d MMM"));
			__builder.CloseElement();
			__builder.AddMarkupContent(217, "<button class=\"bell\" aria-label=\"Notifications\">♧<i></i></button>");
			__builder.OpenElement(218, "button");
			__builder.AddAttribute(219, "class", "avatar");
			__builder.AddAttribute(220, "aria-label", "My profile");
			__builder.AddAttribute<MouseEventArgs>(221, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Profile");
			})));
			__builder.AddContent(222, MemberInitials);
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.AddMarkupContent(223, "\r\n        ");
			__builder.OpenElement(224, "main");
			__builder.AddAttribute(225, "class", "content");
			if (section == "Overview")
			{
				__builder.OpenElement(226, "section");
				__builder.AddAttribute(227, "class", "welcome");
				__builder.OpenElement(228, "div");
				__builder.AddMarkupContent(229, "<span class=\"eyebrow\">YOUR MEMBERSHIP</span>");
				__builder.OpenElement(230, "h1");
				__builder.AddContent(231, "Welcome back, ");
				__builder.AddContent(232, MemberDisplayName);
				__builder.AddContent(233, " ");
				__builder.AddMarkupContent(234, "<span>✳</span>");
				__builder.CloseElement();
				__builder.OpenElement(235, "p");
				__builder.AddContent(236, IsMembershipActive ? "Your membership is active. Keep up the great work." : "Make your first payment to activate your gym membership.");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(237, "div");
				__builder.AddAttribute(238, "class", "member-id");
				__builder.AddMarkupContent(239, "<i></i> MEMBER ID ");
				__builder.OpenElement(240, "b");
				__builder.AddContent(241, currentMember?.MemberCode ?? "—");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(242, "\r\n            ");
				__builder.OpenElement(243, "section");
				__builder.AddAttribute(244, "class", "feature-grid");
				__builder.OpenElement(245, "article");
				__builder.AddAttribute(246, "class", "member-card");
				__builder.OpenElement(247, "div");
				__builder.AddAttribute(248, "class", "card-top");
				__builder.AddMarkupContent(249, "<b><i>✳</i> LIFTDESK</b>");
				if (IsMembershipActive)
				{
					__builder.AddMarkupContent(250, "<span class=\"active-pill\"><i></i> ACTIVE MEMBER</span>");
				}
				else
				{
					__builder.AddMarkupContent(251, "<span class=\"inactive-pill\">PAYMENT REQUIRED</span>");
				}
				__builder.CloseElement();
				__builder.AddMarkupContent(252, "<div class=\"card-label\">YOUR CURRENT PLAN</div>");
				__builder.OpenElement(253, "h2");
				__builder.AddContent(254, MemberPlanName);
				__builder.CloseElement();
				__builder.AddMarkupContent(255, "<p>Full gym access <span>·</span> 1 month</p><div class=\"card-rule\"></div>");
				__builder.OpenElement(256, "div");
				__builder.AddAttribute(257, "class", "card-foot");
				__builder.OpenElement(258, "div");
				__builder.AddMarkupContent(259, "<small>NEXT RENEWAL</small>");
				__builder.OpenElement(260, "b");
				__builder.AddContent(261, MembershipEndLabel);
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(262, "<div><small>PLAN PRICE</small><b class=\"muted-white\">Set by your gym</b></div>");
				__builder.CloseElement();
				__builder.AddMarkupContent(263, "<i class=\"card-watermark\">✳</i>");
				__builder.CloseElement();
				__builder.OpenElement(264, "article");
				__builder.AddAttribute(265, "class", "renewal-card");
				__builder.AddMarkupContent(266, "<div class=\"renewal-top\"><span class=\"renewal-icon\">↗</span><span class=\"upcoming\">UPCOMING</span></div>");
				__builder.AddMarkupContent(267, "<span class=\"eyebrow\">YOUR NEXT PAYMENT</span>");
				__builder.AddMarkupContent(268, "<h2>Keep your momentum.</h2>");
				__builder.OpenElement(269, "p");
				__builder.AddContent(270, "Your plan renews on ");
				__builder.OpenElement(271, "b");
				__builder.AddContent(272, MembershipEndLabel);
				__builder.CloseElement();
				__builder.AddMarkupContent(273, ". Pay securely when you’re ready.");
				__builder.CloseElement();
				__builder.OpenElement(274, "button");
				__builder.AddAttribute(275, "class", "primary-button");
				__builder.AddAttribute<MouseEventArgs>(276, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(Pay)));
				__builder.AddContent(277, "Pay with Paystack ");
				__builder.AddMarkupContent(278, "<span>→</span>");
				__builder.CloseElement();
				__builder.AddMarkupContent(279, "<small class=\"secure\">⌑ Secure payment powered by Paystack</small>");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(280, "\r\n            ");
				__builder.OpenElement(281, "section");
				__builder.AddAttribute(282, "class", "quick-grid");
				__builder.OpenElement(283, "button");
				__builder.AddAttribute(284, "class", "quick-card");
				__builder.AddAttribute<MouseEventArgs>(285, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
				{
					Go("Membership");
				})));
				__builder.AddMarkupContent(286, "<i class=\"quick-icon green\">◈</i>");
				__builder.OpenElement(287, "span");
				__builder.AddMarkupContent(288, "<small>MEMBERSHIP STATUS</small>");
				__builder.OpenElement(289, "b");
				__builder.AddContent(290, IsMembershipActive ? "Active" : "Inactive");
				__builder.CloseElement();
				__builder.OpenElement(291, "em");
				__builder.AddContent(292, IsMembershipActive ? "Enjoy your gym access" : "Payment needed to activate");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(293, "<strong>↗</strong>");
				__builder.CloseElement();
				__builder.OpenElement(294, "button");
				__builder.AddAttribute(295, "class", "quick-card");
				__builder.AddAttribute<MouseEventArgs>(296, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
				{
					Go("Membership");
				})));
				__builder.AddMarkupContent(297, "<i class=\"quick-icon amber\">◷</i>");
				__builder.OpenElement(298, "span");
				__builder.AddMarkupContent(299, "<small>NEXT RENEWAL</small>");
				__builder.OpenElement(300, "b");
				__builder.AddContent(301, MembershipEndLabel);
				__builder.CloseElement();
				__builder.AddMarkupContent(302, "<em>Monthly plan</em>");
				__builder.CloseElement();
				__builder.AddMarkupContent(303, "<strong>↗</strong>");
				__builder.CloseElement();
				__builder.OpenElement(304, "button");
				__builder.AddAttribute(305, "class", "quick-card");
				__builder.AddAttribute<MouseEventArgs>(306, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
				{
					Go("Payments");
				})));
				__builder.AddMarkupContent(307, "<i class=\"quick-icon blue\">₦</i>");
				__builder.AddMarkupContent(308, "<span><small>PAYMENT HISTORY</small><b>View your receipts</b><em>All payments in one place</em></span>");
				__builder.AddMarkupContent(309, "<strong>↗</strong>");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(310, "\r\n            ");
				__builder.OpenElement(311, "section");
				__builder.AddAttribute(312, "class", "lower-grid");
				__builder.OpenElement(313, "article");
				__builder.AddAttribute(314, "class", "panel");
				__builder.OpenElement(315, "div");
				__builder.AddAttribute(316, "class", "panel-head");
				__builder.AddMarkupContent(317, "<div><span class=\"eyebrow\">YOUR ACTIVITY</span><h2>Recent payments</h2></div>");
				__builder.OpenElement(318, "button");
				__builder.AddAttribute(319, "class", "text-link");
				__builder.AddAttribute<MouseEventArgs>(320, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
				{
					Go("Payments");
				})));
				__builder.AddMarkupContent(321, "See all →");
				__builder.CloseElement();
				__builder.CloseElement();
				if (((global::System.Collections.Generic.IReadOnlyCollection<PaymentRecord>)memberPayments).Count == 0)
				{
					__builder.AddMarkupContent(322, "<div class=\"empty-row\"><span class=\"empty-icon\">▤</span><div><b>Your payment history will appear here</b><p>Once the gym records a payment, your receipt and details will be saved here.</p></div></div>");
				}
				else
				{
					__builder.OpenElement(323, "div");
					__builder.AddAttribute(324, "class", "payment-list");
					global::System.Collections.Generic.IEnumerator<PaymentRecord> enumerator = Enumerable.Take<PaymentRecord>((global::System.Collections.Generic.IEnumerable<PaymentRecord>)memberPayments, 3).GetEnumerator();
					try
					{
						while (((global::System.Collections.IEnumerator)enumerator).MoveNext())
						{
							PaymentRecord current = enumerator.Current;
							__builder.OpenElement(325, "div");
							__builder.AddAttribute(326, "class", "payment-list-row");
							__builder.AddMarkupContent(327, "<span class=\"empty-icon\">₦</span>");
							__builder.OpenElement(328, "span");
							__builder.OpenElement(329, "b");
							__builder.AddContent(330, current.Plan?.Name ?? "Membership payment");
							__builder.CloseElement();
							__builder.OpenElement(331, "small");
							DateTimeOffset val = current.CreatedAt;
							val = val.ToLocalTime();
							__builder.AddContent(332, val.ToString("d MMM yyyy"));
							__builder.AddMarkupContent(333, " · ");
							__builder.AddContent(334, current.Method);
							__builder.CloseElement();
							__builder.CloseElement();
							__builder.OpenElement(335, "strong");
							__builder.AddMarkupContent(336, "₦");
							__builder.AddContent(337, current.AmountNgn.ToString("N0"));
							__builder.CloseElement();
							__builder.CloseElement();
						}
					}
					finally
					{
						((global::System.IDisposable)enumerator)?.Dispose();
					}
					__builder.CloseElement();
				}
				__builder.CloseElement();
				__builder.OpenElement(338, "article");
				__builder.AddAttribute(339, "class", "panel support-panel");
				__builder.AddMarkupContent(340, "<div class=\"support-art\">✳</div>");
				__builder.AddMarkupContent(341, "<span class=\"eyebrow\">WE’RE HERE FOR YOU</span>");
				__builder.AddMarkupContent(342, "<h2>Questions about your membership?</h2>");
				__builder.AddMarkupContent(343, "<p>Talk to your gym team about plans, payments or anything else.</p>");
				__builder.OpenElement(344, "button");
				__builder.AddAttribute(345, "class", "text-link");
				__builder.AddAttribute<MouseEventArgs>(346, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(Support)));
				__builder.AddMarkupContent(347, "Get in touch →");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.CloseElement();
			}
			else if (section == "Membership")
			{
				__builder.AddMarkupContent(348, "<section class=\"page-heading\"><span class=\"eyebrow\">YOUR PLAN</span><h1>My membership</h1><p>Everything you need to know about your plan and renewal.</p></section>");
				__builder.OpenElement(349, "article");
				__builder.AddAttribute(350, "class", "detail-card");
				__builder.OpenElement(351, "div");
				if (IsMembershipActive)
				{
					__builder.AddMarkupContent(352, "<span class=\"active-pill\"><i></i> ACTIVE MEMBER</span>");
				}
				else
				{
					__builder.AddMarkupContent(353, "<span class=\"inactive-pill\">PAYMENT REQUIRED</span>");
				}
				__builder.AddMarkupContent(354, "<small class=\"detail-label\">CURRENT MEMBERSHIP</small>");
				__builder.OpenElement(355, "h2");
				__builder.AddContent(356, MemberPlanName);
				__builder.CloseElement();
				__builder.AddMarkupContent(357, "<p>Full gym access · Renews every month</p>");
				__builder.CloseElement();
				__builder.OpenElement(358, "div");
				__builder.AddAttribute(359, "class", "detail-side");
				__builder.AddMarkupContent(360, "<small>RENEWAL DATE</small>");
				__builder.OpenElement(361, "b");
				__builder.AddContent(362, MembershipEndLabel);
				__builder.CloseElement();
				__builder.AddMarkupContent(363, "<em>Plan price is set by your gym</em>");
				__builder.OpenElement(364, "button");
				__builder.AddAttribute(365, "class", "primary-button");
				__builder.AddAttribute<MouseEventArgs>(366, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(Pay)));
				__builder.AddMarkupContent(367, "Pay with Paystack →");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(368, "<div class=\"plans-head\"><span class=\"eyebrow\">FIND YOUR RHYTHM</span><h2>Membership options</h2><p>Choose the duration that works for you. Your gym sets each plan’s price.</p></div>");
				__builder.OpenElement(369, "section");
				__builder.AddAttribute(370, "class", "plan-grid");
				__builder.AddMarkupContent(371, "<article class=\"plan-card\"><span class=\"plan-icon green\">◷</span><small>1 MONTH</small><h3>Monthly</h3><p>Flexible access, renewed monthly.</p><b>Gym-set price</b><span class=\"current-plan\">YOUR CURRENT PLAN</span></article>");
				__builder.OpenElement(372, "article");
				__builder.AddAttribute(373, "class", "plan-card");
				__builder.AddMarkupContent(374, "<span class=\"plan-icon amber\">◈</span>");
				__builder.AddMarkupContent(375, "<small>3 MONTHS</small>");
				__builder.AddMarkupContent(376, "<h3>Quarterly</h3>");
				__builder.AddMarkupContent(377, "<p>Three months of uninterrupted training.</p>");
				__builder.AddMarkupContent(378, "<b>Gym-set price</b>");
				__builder.OpenElement(379, "button");
				__builder.AddAttribute(380, "class", "outline-button");
				__builder.AddAttribute<MouseEventArgs>(381, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(Support)));
				__builder.AddMarkupContent(382, "Ask about this plan →");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(383, "article");
				__builder.AddAttribute(384, "class", "plan-card");
				__builder.AddMarkupContent(385, "<span class=\"plan-icon blue\">✳</span>");
				__builder.AddMarkupContent(386, "<small>12 MONTHS</small>");
				__builder.AddMarkupContent(387, "<h3>Annual</h3>");
				__builder.AddMarkupContent(388, "<p>A full year to build your routine.</p>");
				__builder.AddMarkupContent(389, "<b>Gym-set price</b>");
				__builder.OpenElement(390, "button");
				__builder.AddAttribute(391, "class", "outline-button");
				__builder.AddAttribute<MouseEventArgs>(392, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(Support)));
				__builder.AddMarkupContent(393, "Ask about this plan →");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(394, "<div class=\"info-banner\">ⓘ <span>Plan changes can be arranged with your gym team and take effect at your next renewal.</span></div>");
			}
			else if (section == "Payments")
			{
				__builder.AddMarkupContent(395, "<section class=\"page-heading\"><span class=\"eyebrow\">PAYMENT CENTRE</span><h1>Payments</h1><p>View your payment history and manage your next renewal.</p></section>");
				__builder.OpenElement(396, "article");
				__builder.AddAttribute(397, "class", "next-payment");
				__builder.AddMarkupContent(398, "<span class=\"renewal-icon\">↗</span>");
				__builder.OpenElement(399, "div");
				__builder.OpenElement(400, "small");
				__builder.AddAttribute(401, "class", "eyebrow");
				__builder.AddContent(402, IsMembershipActive ? ("RENEWAL · " + MembershipEndLabel.ToUpperInvariant()) : "PAYMENT REQUIRED");
				__builder.CloseElement();
				__builder.OpenElement(403, "h2");
				__builder.AddContent(404, MemberPlanName);
				__builder.AddContent(405, " renewal");
				__builder.CloseElement();
				__builder.AddMarkupContent(406, "<p>Your gym will confirm the plan price before you pay.</p>");
				__builder.CloseElement();
				__builder.OpenElement(407, "button");
				__builder.AddAttribute(408, "class", "primary-button");
				__builder.AddAttribute<MouseEventArgs>(409, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(Pay)));
				__builder.AddMarkupContent(410, "Pay with Paystack →");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(411, "<article class=\"panel history-panel\"><div class=\"panel-head\"><div><span class=\"eyebrow\">TRANSACTION RECORD</span><h2>Payment history</h2></div><small class=\"history-count\">0 payments</small></div><div class=\"empty-large\"><span class=\"empty-icon\">▤</span><div><b>No payments yet</b><p>Your completed payments and receipts will be listed here.</p></div></div></article>");
				__builder.AddMarkupContent(412, "<div class=\"security-note\"><span>⌑</span><div><b>Your payments are secure</b><p>Online payments are processed securely by Paystack. Your card details are never stored by PMR.</p></div></div>");
			}
			else if (section == "Check-in")
			{
				__builder.AddMarkupContent(413, "<section class=\"page-heading\"><span class=\"eyebrow\">GYM ATTENDANCE</span><h1>Check in</h1><p>Scan the QR code displayed at your gym when you arrive.</p></section>");
				__builder.OpenElement(414, "section");
				__builder.AddAttribute(415, "class", "checkin-layout");
				__builder.OpenElement(416, "article");
				__builder.AddAttribute(417, "class", "checkin-card");
				__builder.AddMarkupContent(418, "<div class=\"checkin-icon\">▦</div>");
				__builder.AddMarkupContent(419, "<span class=\"eyebrow\">MEMBER CHECK-IN</span>");
				__builder.AddMarkupContent(420, "<h2>Ready for your workout?</h2>");
				__builder.AddMarkupContent(421, "<p>Allow camera access, then point your phone at the gym’s check-in QR code. Keep the whole code inside the frame.</p>");
				__builder.OpenElement(422, "button");
				__builder.AddAttribute(423, "class", "primary-button");
				__builder.AddAttribute<MouseEventArgs>(424, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Func<global::System.Threading.Tasks.Task>)ScanForAttendance));
				__builder.AddContent(425, "Open QR scanner ");
				__builder.AddMarkupContent(426, "<span>→</span>");
				__builder.CloseElement();
				__builder.AddMarkupContent(427, "<small class=\"secure\">Camera access is only used while scanning.</small>");
				__builder.CloseElement();
				__builder.AddMarkupContent(428, "<aside class=\"panel checkin-help\"><span class=\"eyebrow\">HOW IT WORKS</span><h2>Three quick steps</h2><ol><li>Open the scanner when you arrive.</li><li>Scan the code at the front desk.</li><li>Your saved check-in appears in your attendance record for the gym administrator.</li></ol><div class=\"info-banner\">ⓘ The attendance record needs the gym’s shared online service. QR scans are not saved as attendance yet.</div></aside>");
				__builder.CloseElement();
			}
			else
			{
				__builder.AddMarkupContent(429, "<section class=\"page-heading\"><span class=\"eyebrow\">YOUR ACCOUNT</span><h1>My profile</h1><p>Keep your contact details up to date.</p></section>");
				__builder.OpenElement(430, "article");
				__builder.AddAttribute(431, "class", "panel profile-panel");
				__builder.OpenElement(432, "div");
				__builder.AddAttribute(433, "class", "profile-banner");
				__builder.AddMarkupContent(434, "<span class=\"profile-avatar\">M</span>");
				__builder.OpenElement(435, "div");
				__builder.AddMarkupContent(436, "<span class=\"eyebrow light\">LIFTDESK MEMBER</span>");
				__builder.OpenElement(437, "h2");
				__builder.AddContent(438, MemberDisplayName);
				__builder.CloseElement();
				__builder.OpenElement(439, "p");
				__builder.AddMarkupContent(440, "Member ID · ");
				__builder.AddContent(441, currentMember?.MemberCode ?? "—");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(442, "span");
				__builder.AddAttribute(443, "class", IsMembershipActive ? "active-pill" : "inactive-pill");
				__builder.AddContent(444, IsMembershipActive ? "ACTIVE" : "INACTIVE");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(445, "div");
				__builder.AddAttribute(446, "class", "profile-fields");
				__builder.OpenElement(447, "div");
				__builder.AddMarkupContent(448, "<small>FULL NAME</small>");
				__builder.OpenElement(449, "b");
				__builder.AddContent(450, MemberDisplayName);
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(451, "div");
				__builder.AddMarkupContent(452, "<small>EMAIL ADDRESS</small>");
				__builder.OpenElement(453, "b");
				__builder.AddContent(454, currentMember?.Email ?? "—");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(455, "div");
				__builder.AddMarkupContent(456, "<small>PHONE NUMBER</small>");
				__builder.OpenElement(457, "b");
				__builder.AddContent(458, currentMember?.Phone ?? "—");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.OpenElement(459, "div");
				__builder.AddMarkupContent(460, "<small>PASSWORD</small>");
				__builder.AddMarkupContent(461, "<b>••••••••</b>");
				__builder.OpenElement(462, "button");
				__builder.AddAttribute(463, "class", "text-link");
				__builder.AddAttribute<MouseEventArgs>(464, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, new Action(PasswordHelp)));
				__builder.AddMarkupContent(465, "Change password →");
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.CloseElement();
				__builder.AddMarkupContent(466, "<div class=\"profile-note\">ⓘ Profile editing will be available in your account settings.</div>");
				__builder.OpenElement(466, "button");
				__builder.AddAttribute(466, "class", "profile-logout");
				__builder.AddAttribute(466, "type", "button");
				__builder.AddAttribute<MouseEventArgs>(466, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Func<global::System.Threading.Tasks.Task>)Signout));
				__builder.AddMarkupContent(466, "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M10 17l5-5-5-5M15 12H3m9-9h6a3 3 0 0 1 3 3v12a3 3 0 0 1-3 3h-6\"/></svg><span>Sign out</span>");
				__builder.CloseElement();
				__builder.CloseElement();
			}
			__builder.AddMarkupContent(467, "<footer class=\"member-footer\"><span>PMR Member Portal</span><span>Here for your next milestone <b>✳</b></span></footer>");
			__builder.CloseElement();
			__builder.OpenElement(468, "nav");
			__builder.AddAttribute(469, "class", "mobile-nav");
			__builder.OpenElement(470, "button");
			__builder.AddAttribute(471, "class", (section == "Overview") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(472, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Overview");
			})));
			__builder.AddMarkupContent(473, "<span>⌂</span>Home");
			__builder.CloseElement();
			__builder.OpenElement(474, "button");
			__builder.AddAttribute(475, "class", (section == "Membership") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(476, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Membership");
			})));
			__builder.AddMarkupContent(477, "<span>◈</span>Plan");
			__builder.CloseElement();
			__builder.OpenElement(478, "button");
			__builder.AddAttribute(479, "class", (section == "Payments") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(480, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Payments");
			})));
			__builder.AddMarkupContent(481, "<span>▤</span>Payments");
			__builder.CloseElement();
			__builder.OpenElement(482, "button");
			__builder.AddAttribute(483, "class", (section == "Check-in") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(484, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Check-in");
			})));
			__builder.AddMarkupContent(485, "<span>▦</span>Check in");
			__builder.CloseElement();
			__builder.OpenElement(486, "button");
			__builder.AddAttribute(487, "class", (section == "Profile") ? "selected" : "");
			__builder.AddAttribute<MouseEventArgs>(488, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				Go("Profile");
			})));
			__builder.AddMarkupContent(489, "<span>◎</span>Profile");
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.CloseElement();
			__builder.CloseElement();
		}
		if (!string.IsNullOrEmpty(toast))
		{
			__builder.OpenElement(490, "div");
			__builder.AddAttribute(491, "class", "toast");
			__builder.AddAttribute(492, "role", "status");
			__builder.AddMarkupContent(493, "✳ ");
			__builder.AddContent(494, toast);
			__builder.AddContent(495, " ");
			__builder.OpenElement(496, "button");
			__builder.AddAttribute(497, "class", "toast-close");
			__builder.AddAttribute(498, "aria-label", "Dismiss");
			__builder.AddAttribute<MouseEventArgs>(499, "onclick", EventCallback.Factory.Create<MouseEventArgs>((object)this, (Action)([CompilerGenerated] () =>
			{
				toast = null;
			})));
			__builder.AddMarkupContent(500, "×");
			__builder.CloseElement();
			__builder.CloseElement();
		}
		__builder.CloseElement();
	}

    protected override async Task OnInitializedAsync()
    {
        try
        {
            currentMember = await Backend.GetCurrentMemberAsync();
            isSignedIn = currentMember is not null;
            if (isSignedIn && currentMember?.Role != "admin")
                memberPayments = await Backend.GetPaymentsAsync(100);
            if (currentMember?.Role == "admin")
                Navigation.NavigateTo("/admin");
        }
        catch
        {
            isSignedIn = false;
        }
    }

    private void Go(string value) => section = value;

    private async Task Signout()
    {
        try { await Backend.SignOutAsync(); } catch { }
        currentMember = null;
        isSignedIn = false;
        signingUp = false;
        section = "Overview";
    }

    private async Task Signin()
    {
        if (isSigningIn) return;
        isSigningIn = true;
        authError = null;
        try
        {
            await Backend.SignInWithUsernameAsync(loginUsername, loginPassword);
            currentMember = await Backend.GetCurrentMemberAsync();
            if (currentMember is null)
                throw new InvalidOperationException("Your account profile could not be found. Please contact the gym administrator.");
            if (currentMember.Role == "admin")
            {
                Navigation.NavigateTo("/admin");
                return;
            }
            memberPayments = await Backend.GetPaymentsAsync(100);
            isSignedIn = true;
            toast = "Welcome back, " + currentMember.FullName + "!";
        }
        catch (Exception ex)
        {
            authError = string.IsNullOrWhiteSpace(ex.Message) ? "Sign in failed. Check your connection and try again." : ex.Message;
            Notify(authError);
        }
        finally { isSigningIn = false; }
    }

    private async Task Signup(SignupForm.Submission form)
    {
        signupName = form.Name;
        signupUsername = form.Username;
        signupEmail = form.Email;
        signupPhone = form.Phone;
        signupPassword = form.Password;
        authError = null;
        try
        {
            awaitingEmailConfirmation = await Backend.SignUpMemberAsync(signupName, signupUsername, signupEmail, signupPhone, signupPassword);
            if (awaitingEmailConfirmation)
            {
                Notify("Check your email for a confirmation link, then sign in with your username.");
                return;
            }
            currentMember = await Backend.GetCurrentMemberAsync();
            if (currentMember is null)
                throw new InvalidOperationException("Your account was created but the member profile could not be loaded.");
            memberPayments = await Backend.GetPaymentsAsync(100);
            isSignedIn = true;
            Notify("Account created. Your membership becomes active after your first payment.");
        }
        catch (Exception ex)
        {
            authError = string.IsNullOrWhiteSpace(ex.Message)
                ? "Account creation failed. Check your connection and try again."
                : ex.Message;
            Notify(authError);
        }
    }

    private async Task ScanForAttendance()
    {
        try
        {
            var qrValue = await QrScanner.ScanAsync();
            if (!string.IsNullOrWhiteSpace(qrValue))
            {
                await Backend.RecordMemberCheckinAsync(qrValue);
                Notify("Attendance recorded. The gym administrator can now see your check-in.");
            }
        }
        catch (Exception ex) { Notify(ex.Message); }
    }

    private void Pay() => Notify("Paystack checkout will be connected in the payment setup phase.");
    private void PasswordHelp() => Notify("Password reset will be available when accounts are connected.");
    private void Support() => Notify("Your gym contact details will appear here.");
    private void Notify(string text) => toast = text;
}
