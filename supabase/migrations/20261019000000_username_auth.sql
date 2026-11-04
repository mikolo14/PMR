-- Add a case-insensitive username for app sign-in and make phone optional.
-- Email/password remains Supabase Auth's credential; username resolution is handled by
-- the private `username-signin` Edge Function, never by an anon-accessible profile query.

alter table public.profiles
    add column if not exists username text;

alter table public.profiles
    alter column phone drop not null;

update public.profiles
set phone = null
where phone = '';

update public.profiles p
set username = 'member-' || substr(replace(p.id::text, '-', ''), 1, 16)
from auth.users u
where p.id = u.id
  and p.username is null;

alter table public.profiles
    alter column username set not null;

create unique index if not exists profiles_username_lower_uidx
    on public.profiles (lower(username));

alter table public.profiles
    drop constraint if exists profiles_username_format_check;

alter table public.profiles
    add constraint profiles_username_format_check
    check (username = lower(username) and username ~ '^[a-z0-9][a-z0-9_.-]{2,29}$');

create or replace function private.handle_new_auth_user()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
    insert into public.profiles (id, username, full_name, email, phone)
    values (
        new.id,
        coalesce(
            nullif(lower(btrim(new.raw_user_meta_data ->> 'username')), ''),
            'member-' || substr(replace(new.id::text, '-', ''), 1, 10)
        ),
        coalesce(nullif(new.raw_user_meta_data ->> 'full_name', ''), 'New member'),
        coalesce(nullif(new.raw_user_meta_data ->> 'email', ''), new.email, ''),
        nullif(coalesce(new.phone, new.raw_user_meta_data ->> 'phone', ''), '')
    );
    return new;
end;
$$;

revoke all on function private.handle_new_auth_user() from public, anon, authenticated;
