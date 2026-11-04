const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (request.method !== "POST") return jsonResponse({ message: "Method not allowed." }, 405);

  const invalidCredentials = () => jsonResponse({ message: "Invalid username or password." }, 400);

  try {
    const body = await request.json();
    const username = typeof body.username === "string" ? body.username.trim().toLowerCase() : "";
    const password = typeof body.password === "string" ? body.password : "";
    if (!/^[a-z0-9][a-z0-9._-]{2,29}$/.test(username) || password.length === 0) {
      return invalidCredentials();
    }

    const projectUrl = Deno.env.get("SUPABASE_URL");
    const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
    const publishableKey = request.headers.get("apikey");
    if (!projectUrl || !serviceRoleKey || !publishableKey) {
      return jsonResponse({ message: "Username sign-in is not configured." }, 500);
    }

    const profileUrl = new URL("/rest/v1/profiles", projectUrl);
    profileUrl.searchParams.set("select", "email");
    profileUrl.searchParams.set("username", `eq.${username}`);
    profileUrl.searchParams.set("limit", "1");

    const profileResponse = await fetch(profileUrl, {
      headers: {
        apikey: serviceRoleKey,
        Authorization: `Bearer ${serviceRoleKey}`,
      },
    });
    if (!profileResponse.ok) return jsonResponse({ message: "Username sign-in is temporarily unavailable." }, 503);

    const profiles = await profileResponse.json();
    const email = Array.isArray(profiles) && typeof profiles[0]?.email === "string"
      ? profiles[0].email.trim()
      : "";
    if (!email) return invalidCredentials();

    const authResponse = await fetch(new URL("/auth/v1/token?grant_type=password", projectUrl), {
      method: "POST",
      headers: {
        apikey: publishableKey,
        Authorization: `Bearer ${publishableKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    });

    if (!authResponse.ok) return invalidCredentials();
    return new Response(await authResponse.text(), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  } catch {
    return invalidCredentials();
  }
});
