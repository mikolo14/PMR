-- Migration: Switch member codes to sequential PMR-001-GYM format
-- Run this in Supabase Dashboard → SQL Editor on your live project.
-- Safe to run even if the profiles table already has rows.

-- 1. Create the sequence (starts at 1)
create sequence if not exists public.member_code_seq start 1 increment 1;

-- 2. If you already have members with old LD-XXXXXXXX codes, update the
--    sequence to start after the current count so there are no gaps.
--    This sets it to (existing member count + 1).
select setval('public.member_code_seq', coalesce((select count(*) from public.profiles), 0) + 1, false);

-- 3. Backfill existing members with sequential codes in signup order.
--    Each existing member gets PMR-001-GYM, PMR-002-GYM etc by created_at.
with numbered as (
    select id,
           row_number() over (order by created_at asc) as rn
    from public.profiles
)
update public.profiles p
set member_code = 'PMR-' || lpad(n.rn::text, 3, '0') || '-GYM'
from numbered n
where p.id = n.id;

-- 4. Replace the trigger function to use the sequence for all new signups.
create or replace function private.handle_new_auth_user()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_seq bigint;
begin
    v_seq := nextval('public.member_code_seq');
    insert into public.profiles (id, member_code, username, full_name, email, phone)
    values (
        new.id,
        'PMR-' || lpad(v_seq::text, 3, '0') || '-GYM',
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

-- 5. Verify — should show PMR-001-GYM, PMR-002-GYM etc.
select member_code, username, full_name, created_at
from public.profiles
order by created_at asc;
