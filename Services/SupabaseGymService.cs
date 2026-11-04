using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PIMP_MY_RIDE.Services;

public sealed class SupabaseGymService(HttpClient http)
{
    private const string AccessTokenKey = "liftdesk.supabase.access";
    private const string RefreshTokenKey = "liftdesk.supabase.refresh";
    private const string UserIdKey = "liftdesk.supabase.user";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private SupabaseConfiguration? _configuration;
    private readonly SemaphoreSlim _configurationLock = new(1, 1);

    public async Task<bool> SignUpMemberAsync(string fullName, string username, string email, string? phone, string password)
    {
        var normalizedUsername = NormalizeUsername(username);
        var body = JsonSerializer.Serialize(new
        {
            email = email.Trim(),
            password,
            data = new { full_name = fullName.Trim(), username = normalizedUsername, phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim() }
        }, JsonOptions);
        using var response = await SendAsync(HttpMethod.Post, "auth/v1/signup", body, authenticated: false);
        var json = await ReadJsonAsync(response);
        var session = ParseSession(json);
        if (session is not null)
        {
            await SaveSessionAsync(session);
            return false;
        }
        return true;
    }

    public async Task SignInWithUsernameAsync(string username, string password)
    {
        var body = JsonSerializer.Serialize(new { username = NormalizeUsername(username), password }, JsonOptions);
        using var response = await SendAsync(HttpMethod.Post, "functions/v1/username-signin", body, authenticated: false);
        var json = await ReadJsonAsync(response);
        var session = ParseSession(json) ?? throw new InvalidOperationException("Supabase did not return a sign-in session.");
        await SaveSessionAsync(session);
    }

    public async Task SignOutAsync()
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Post, "auth/v1/logout", "{}", authenticated: true, retryOnUnauthorized: false);
        }
        finally
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(UserIdKey);
        }
    }

    public async Task<MemberProfile?> GetCurrentMemberAsync()
    {
        var userId = await SecureStorage.Default.GetAsync(UserIdKey);
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var url = $"rest/v1/profiles?id=eq.{Uri.EscapeDataString(userId)}&select=id,member_code,username,full_name,email,phone,role,current_plan_id,membership_ends_on,membership_plans(id,name,duration_months,price_ngn,is_active)&limit=1";
        using var response = await SendAsync(HttpMethod.Get, url, authenticated: true);
        using var json = await ReadJsonAsync(response);
        var rows = json.RootElement;
        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0) return null;
        return rows[0].Deserialize<MemberProfile>(JsonOptions);
    }

    public async Task RecordMemberCheckinAsync(string scannedToken)
    {
        var body = JsonSerializer.Serialize(new { p_token = scannedToken }, JsonOptions);
        using var response = await SendAsync(HttpMethod.Post, "rest/v1/rpc/record_member_checkin", body, authenticated: true);
        using var _ = await ReadJsonAsync(response);
    }

    public async Task<string> GenerateGymCheckinQrAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "rest/v1/rpc/generate_gym_checkin_qr", "{}", authenticated: true);
        using var json = await ReadJsonAsync(response);
        if (json.RootElement.ValueKind == JsonValueKind.String)
            return json.RootElement.GetString()!;
        if (json.RootElement.ValueKind == JsonValueKind.Array && json.RootElement.GetArrayLength() > 0)
            return json.RootElement[0].GetString() ?? throw new InvalidOperationException("Supabase returned an empty QR code.");
        throw new InvalidOperationException("Supabase returned an invalid QR code response.");
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetAttendanceAsync(int limit = 100)
    {
        var url = $"rest/v1/attendance?select=id,member_id,checkin_date,checked_in_at,profiles(full_name,member_code)&order=checked_in_at.desc&limit={Math.Clamp(limit, 1, 500)}";
        using var response = await SendAsync(HttpMethod.Get, url, authenticated: true);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.Deserialize<List<AttendanceRecord>>(JsonOptions) ?? [];
    }

    public async Task<IReadOnlyList<MemberProfile>> GetMembersAsync(int limit = 500)
    {
        var url = $"rest/v1/profiles?select=id,member_code,username,full_name,email,phone,role,current_plan_id,membership_ends_on,membership_plans(id,name,duration_months,price_ngn,is_active)&order=created_at.desc&limit={Math.Clamp(limit, 1, 1000)}";
        using var response = await SendAsync(HttpMethod.Get, url, authenticated: true);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.Deserialize<List<MemberProfile>>(JsonOptions) ?? [];
    }

    public async Task<IReadOnlyList<PaymentRecord>> GetPaymentsAsync(int limit = 500)
    {
        var url = $"rest/v1/payments?select=id,member_id,plan_id,amount_ngn,method,status,reference,paid_at,created_at,profiles(full_name,member_code),membership_plans(name)&order=created_at.desc&limit={Math.Clamp(limit, 1, 1000)}";
        using var response = await SendAsync(HttpMethod.Get, url, authenticated: true);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.Deserialize<List<PaymentRecord>>(JsonOptions) ?? [];
    }

    public async Task<IReadOnlyList<MembershipPlan>> GetPlansAsync()
    {
        const string url = "rest/v1/membership_plans?select=id,name,duration_months,price_ngn,is_active&order=duration_months.asc";
        using var response = await SendAsync(HttpMethod.Get, url, authenticated: true);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.Deserialize<List<MembershipPlan>>(JsonOptions) ?? [];
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? jsonBody = null, bool authenticated = false, bool retryOnUnauthorized = true)
    {
        var config = await GetConfigurationAsync();
        var response = await SendOnceAsync(config, method, path, jsonBody, authenticated);
        if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated && retryOnUnauthorized)
        {
            response.Dispose();
            if (await RefreshSessionAsync(config))
                response = await SendOnceAsync(config, method, path, jsonBody, authenticated);
            else
                throw new InvalidOperationException("Your session expired. Please sign in again.");
        }
        return response;
    }

    private async Task<HttpResponseMessage> SendOnceAsync(SupabaseConfiguration config, HttpMethod method, string path, string? jsonBody, bool authenticated)
    {
        using var request = new HttpRequestMessage(method, new Uri(config.ProjectUrl.TrimEnd('/') + "/" + path.TrimStart('/')));
        request.Headers.TryAddWithoutValidation("apikey", config.PublishableKey);
        var token = authenticated ? await SecureStorage.Default.GetAsync(AccessTokenKey) : null;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrWhiteSpace(token) ? config.PublishableKey : token);
        if (jsonBody is not null)
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            var friendly = ExtractErrorMessage(message);
            var statusCode = (int)response.StatusCode;
            response.Dispose();
            throw new SupabaseRequestException(friendly, statusCode);
        }
        return response;
    }

    private async Task<bool> RefreshSessionAsync(SupabaseConfiguration config)
    {
        var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;
        try
        {
            var body = JsonSerializer.Serialize(new { refresh_token = refreshToken }, JsonOptions);
            using var response = await SendOnceAsync(config, HttpMethod.Post, "auth/v1/token?grant_type=refresh_token", body, authenticated: false);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var session = ParseSession(json);
            if (session is null) return false;
            await SaveSessionAsync(session);
            return true;
        }
        catch
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(UserIdKey);
            return false;
        }
    }

    private async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static SupabaseSession? ParseSession(JsonDocument json)
    {
        var root = json.RootElement;
        if (!root.TryGetProperty("access_token", out var access) || access.ValueKind != JsonValueKind.String) return null;
        var refresh = root.TryGetProperty("refresh_token", out var refreshProperty) ? refreshProperty.GetString() : null;
        var userId = root.TryGetProperty("user", out var user) && user.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(refresh) || string.IsNullOrWhiteSpace(userId)) return null;
        return new SupabaseSession(access.GetString()!, refresh, userId);
    }

    private static async Task SaveSessionAsync(SupabaseSession session)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, session.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, session.RefreshToken);
        await SecureStorage.Default.SetAsync(UserIdKey, session.UserId);
    }

    private async Task<SupabaseConfiguration> GetConfigurationAsync()
    {
        if (_configuration is not null) return _configuration;
        await _configurationLock.WaitAsync();
        try
        {
            if (_configuration is not null) return _configuration;
            await using var stream = await FileSystem.OpenAppPackageFileAsync("supabase-config.json");
            var config = await JsonSerializer.DeserializeAsync<SupabaseConfiguration>(stream, JsonOptions);
            if (config is null || string.IsNullOrWhiteSpace(config.ProjectUrl) || string.IsNullOrWhiteSpace(config.PublishableKey))
                throw new InvalidOperationException("Add your Supabase Project URL and publishable key to Resources/Raw/supabase-config.json first.");
            if (!Uri.TryCreate(config.ProjectUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Supabase Project URL must be a valid HTTPS URL.");
            _configuration = config;
            return config;
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    private static string NormalizeUsername(string input)
    {
        var username = input.Trim().ToLowerInvariant();
        if (username.Length is < 3 or > 30
            || !char.IsAsciiLetterOrDigit(username[0])
            || username.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
            throw new ArgumentException("Use a username with 3–30 letters, numbers, dots, underscores or hyphens; it must start with a letter or number.", nameof(input));
        return username;
    }

    private static string ExtractErrorMessage(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            foreach (var key in new[] { "msg", "message", "error_description", "error" })
                if (document.RootElement.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.String)
                    return property.GetString() ?? "The request failed.";
        }
        catch (JsonException) { }
        return string.IsNullOrWhiteSpace(json) ? "The request failed. Check your internet connection and try again." : json;
    }

    private sealed record SupabaseSession(string AccessToken, string RefreshToken, string UserId);
}

public sealed record SupabaseConfiguration(
    [property: JsonPropertyName("ProjectUrl")] string ProjectUrl,
    [property: JsonPropertyName("PublishableKey")] string PublishableKey);

public sealed record MemberProfile(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("member_code")] string MemberCode,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("phone")] string Phone,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("current_plan_id")] string? CurrentPlanId,
    [property: JsonPropertyName("membership_ends_on")] DateOnly? MembershipEndsOn,
    [property: JsonPropertyName("membership_plans")] MembershipPlan? Plan);

public sealed record AttendanceRecord(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("checkin_date")] DateOnly CheckinDate,
    [property: JsonPropertyName("checked_in_at")] DateTimeOffset CheckedInAt,
    [property: JsonPropertyName("profiles")] AttendanceMember? Member);

public sealed record AttendanceMember(
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("member_code")] string MemberCode);

public sealed class SupabaseRequestException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record PaymentRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("member_id")] string MemberId,
    [property: JsonPropertyName("plan_id")] string PlanId,
    [property: JsonPropertyName("amount_ngn")] int AmountNgn,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("reference")] string? Reference,
    [property: JsonPropertyName("paid_at")] DateTimeOffset? PaidAt,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("profiles")] AttendanceMember? Member,
    [property: JsonPropertyName("membership_plans")] MembershipPlan? Plan);

public sealed record MembershipPlan(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("duration_months")] short DurationMonths,
    [property: JsonPropertyName("price_ngn")] int PriceNgn,
    [property: JsonPropertyName("is_active")] bool IsActive);
