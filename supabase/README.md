# Supabase setup for LiftDesk

The app uses Supabase for shared records and authentication. The member and admin screens require a configured Supabase project.

## Create the database

1. Create a Supabase project and save its database password privately.
2. Open **SQL Editor** in the Supabase dashboard.
3. Paste and run `schema.sql`.
4. In **Project Settings → API**, copy the Project URL and the publishable/anon client key. Never put the `service_role` key in the mobile app.
5. Keep **Email** authentication enabled and email confirmation configured for your project. Member sign-in uses a username, resolved privately by the `username-signin` Edge Function to the member's email/password account. Phone/SMS authentication is not used by this flow.
6. Create the first administrator through Supabase Auth, then set that user's `profiles.role` to `admin` in the SQL editor. Do not let people choose their own admin role in the app.

## QR check-in flow

1. An authenticated administrator generates a QR token with `generate_gym_checkin_qr()`.
2. The admin display renders the returned token as a QR code. The token expires after two minutes; only its SHA-256 hash is stored.
3. An authenticated member scans the token and calls `record_member_checkin(p_token)`.
4. The database requires an active membership, the current QR token, and no earlier check-in by that member on the same Lagos calendar day.
5. The admin attendance table can read all check-ins; members can read only their own. Row Level Security enforces these rules in the database.

## Important

- Configure plan prices instead of the zero-valued starter prices before allowing payment.
- Membership activation after Paystack or manual payment must be performed by a trusted server-side function. Do not activate a membership from the phone app or expose a Paystack secret/service-role key in the client.
- `schema.sql` establishes the database and secure check-in RPCs. Configure the Supabase project URL/publishable key, apply the username migration and deploy the username sign-in function before using member authentication.

## Username sign-in

For an existing project, apply `migrations/20261019000000_username_auth.sql` in the Supabase SQL Editor. For a new project, `schema.sql` includes the username column and optional phone number.

Deploy the username lookup function from the repository root with the Supabase CLI:

```powershell
supabase link --project-ref iiaiodoivkxiknahfbnm
supabase db push
supabase functions deploy username-signin
```

The function uses the server-side `SUPABASE_SERVICE_ROLE_KEY` only to look up the username. It never returns the matched email; it passes the email and password to Supabase Auth and returns only the normal Auth session. Do not put the service-role key in the app or share it in chat.

New accounts use email/password Auth, with a username stored in `profiles`. Email confirmation remains controlled by **Authentication → Sign In / Providers**; this removes SMS verification, but does not turn off email confirmation. Existing phone-only Auth users need a confirmed email attached to their Auth account before they can use username sign-in. The migration assigns their profile a generated username such as `member-<id>` but does not bypass email verification or alter their Auth credentials.
