-- LiftDesk shared gym records (Supabase Postgres)
-- Run in Supabase Dashboard > SQL Editor after creating the project.
-- QR codes are short-lived; each active member can check in once per Lagos calendar day.

create schema if not exists private;
create extension if not exists pgcrypto with schema extensions;

create table if not exists public.membership_plans (
    id uuid primary key default gen_random_uuid(),
    name text not null unique,
    duration_months smallint not null check (duration_months in (1, 3, 12)),
    price_ngn integer not null check (price_ngn >= 0),
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

insert into public.membership_plans (name, duration_months, price_ngn)
values ('Monthly', 1, 0), ('Quarterly', 3, 0), ('Annual', 12, 0)
on conflict (name) do nothing;

create table if not exists public.profiles (
    id uuid primary key references auth.users(id) on delete cascade,
    member_code text not null unique default ('LD-' || upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))),
    username text not null check (username = lower(username) and username ~ '^[a-z0-9][a-z0-9_.-]{2,29}$'),
    full_name text not null,
    email text not null,
    phone text unique,
    role text not null default 'member' check (role in ('member', 'admin')),
    current_plan_id uuid references public.membership_plans(id),
    membership_ends_on date,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists profiles_username_lower_uidx
    on public.profiles (lower(username));

create table if not exists public.payments (
    id uuid primary key default gen_random_uuid(),
    member_id uuid not null references public.profiles(id) on delete restrict,
    plan_id uuid not null references public.membership_plans(id) on delete restrict,
    amount_ngn integer not null check (amount_ngn >= 0),
    method text not null check (method in ('paystack', 'manual')),
    status text not null default 'pending' check (status in ('pending', 'successful', 'failed')),
    reference text unique,
    paid_at timestamptz,
    created_at timestamptz not null default now()
);

create table if not exists public.gym_checkin_qr_codes (
    token_hash bytea primary key,
    created_by uuid not null references public.profiles(id) on delete restrict,
    expires_at timestamptz not null,
    created_at timestamptz not null default now()
);

create table if not exists public.attendance (
    id bigint generated always as identity primary key,
    member_id uuid not null references public.profiles(id) on delete restrict,
    checkin_date date not null default ((now() at time zone 'Africa/Lagos')::date),
    checked_in_at timestamptz not null default now(),
    qr_token_hash bytea not null references public.gym_checkin_qr_codes(token_hash) on delete restrict,
    unique (member_id, checkin_date)
);

create index if not exists attendance_checked_in_at_idx on public.attendance (checked_in_at desc);
create index if not exists payments_member_created_at_idx on public.payments (member_id, created_at desc);
create index if not exists profiles_role_idx on public.profiles (role);

-- Keep member profile records in sync with new Supabase Auth users.
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
drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
after insert on auth.users
for each row execute function private.handle_new_auth_user();

-- Private authorization helpers. Keep the `private` schema out of Supabase's exposed API schemas.
create or replace function private.is_admin()
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select exists (
        select 1 from public.profiles p
        where p.id = (select auth.uid()) and p.role = 'admin'
    );
$$;

create or replace function private.has_active_membership(p_member_id uuid)
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select exists (
        select 1 from public.profiles p
        where p.id = p_member_id
          and p.role = 'member'
          and p.membership_ends_on >= (now() at time zone 'Africa/Lagos')::date
    );
$$;

create or replace function private.is_valid_checkin_code(p_token_hash bytea)
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select exists (
        select 1 from public.gym_checkin_qr_codes q
        where q.token_hash = p_token_hash and q.expires_at > now()
    );
$$;

revoke all on function private.is_admin() from public, anon;
revoke all on function private.has_active_membership(uuid) from public, anon;
revoke all on function private.is_valid_checkin_code(bytea) from public, anon;
grant usage on schema private to authenticated;
grant execute on function private.is_admin() to authenticated;
grant execute on function private.has_active_membership(uuid) to authenticated;
grant execute on function private.is_valid_checkin_code(bytea) to authenticated;

alter table public.profiles enable row level security;
alter table public.membership_plans enable row level security;
alter table public.payments enable row level security;
alter table public.gym_checkin_qr_codes enable row level security;
alter table public.attendance enable row level security;

revoke all on public.profiles, public.membership_plans, public.payments, public.gym_checkin_qr_codes, public.attendance from anon, authenticated;
grant select on public.profiles, public.membership_plans, public.payments, public.attendance to authenticated;
grant update (full_name, email) on public.profiles to authenticated;
grant insert (token_hash, created_by, expires_at) on public.gym_checkin_qr_codes to authenticated;
grant insert (member_id, qr_token_hash) on public.attendance to authenticated;
grant usage, select on sequence public.attendance_id_seq to authenticated;

create policy "Profiles are visible to their member and gym admins"
on public.profiles for select to authenticated
using (id = (select auth.uid()) or (select private.is_admin()));

create policy "Members can edit their own contact details"
on public.profiles for update to authenticated
using (id = (select auth.uid()))
with check (id = (select auth.uid()));

create policy "Active membership plans are visible to members"
on public.membership_plans for select to authenticated
using (is_active or (select private.is_admin()));

create policy "Gym admins manage membership plans"
on public.membership_plans for all to authenticated
using ((select private.is_admin()))
with check ((select private.is_admin()));
grant insert, update, delete on public.membership_plans to authenticated;

create policy "Members and admins can view payment history"
on public.payments for select to authenticated
using (member_id = (select auth.uid()) or (select private.is_admin()));

create policy "Only admins can record a manual payment"
on public.payments for insert to authenticated
with check ((select private.is_admin()));
grant insert on public.payments to authenticated;

create policy "Admins can create short-lived gym QR codes"
on public.gym_checkin_qr_codes for insert to authenticated
with check ((select private.is_admin()) and created_by = (select auth.uid()));

create policy "Members and admins can view attendance"
on public.attendance for select to authenticated
using (member_id = (select auth.uid()) or (select private.is_admin()));

create policy "Members can only check in for themselves with a live QR code"
on public.attendance for insert to authenticated
with check (
    member_id = (select auth.uid())
    and (select private.has_active_membership((select auth.uid())))
    and (select private.is_valid_checkin_code(qr_token_hash))
);

-- Admin dashboard calls this RPC to create a random QR value. Only the hash is stored.
create or replace function public.generate_gym_checkin_qr()
returns text
language plpgsql
security invoker
set search_path = ''
as $$
declare
    v_token text;
begin
    if not (select private.is_admin()) then
        raise exception 'Only a gym administrator can create a check-in QR code.' using errcode = '42501';
    end if;
    v_token := encode(extensions.gen_random_bytes(32), 'hex');
    insert into public.gym_checkin_qr_codes (token_hash, created_by, expires_at)
    values (extensions.digest(convert_to(v_token, 'UTF8'), 'sha256'), (select auth.uid()), now() + interval '2 minutes');
    return v_token;
end;
$$;

-- Member app calls this after scanning. RLS validates the member, subscription, QR expiry, and duplicate day.
create or replace function public.record_member_checkin(p_token text)
returns public.attendance
language plpgsql
security invoker
set search_path = ''
as $$
declare
    v_record public.attendance;
    v_hash bytea;
begin
    if (select auth.uid()) is null then
        raise exception 'Please sign in before checking in.' using errcode = '42501';
    end if;
    if p_token is null or length(p_token) < 32 then
        raise exception 'This QR code is not valid.' using errcode = '22023';
    end if;
    v_hash := extensions.digest(convert_to(p_token, 'UTF8'), 'sha256');
    insert into public.attendance (member_id, qr_token_hash)
    values ((select auth.uid()), v_hash)
    returning * into v_record;
    return v_record;
end;
$$;

revoke all on function public.generate_gym_checkin_qr() from public, anon;
revoke all on function public.record_member_checkin(text) from public, anon;
grant execute on function public.generate_gym_checkin_qr() to authenticated;
grant execute on function public.record_member_checkin(text) to authenticated;
