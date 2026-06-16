-- =============================================================================
-- Fale7 System - Supabase Schema & RPCs
-- لا اعتماد على باك اند قديم أو تخزين محلي؛ كل المصدر هو Supabase.
-- الربط (Linking): السعر المشترك عبر source_products فقط بين التطبيق والـ POS.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Extensions required by hashing/session helpers
-- -----------------------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS extensions;
CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA extensions;

-- -----------------------------------------------------------------------------
-- 1) طبقة السعر المشتركة (Linking)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.source_products (
  id TEXT PRIMARY KEY,
  price NUMERIC NOT NULL DEFAULT 0 CHECK (price >= 0),
  inventory_mode TEXT NOT NULL DEFAULT 'available',
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- -----------------------------------------------------------------------------
-- 1.1) المخزون الذكي المشترك
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.inventory_items (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  measure_type TEXT NOT NULL DEFAULT 'UNIT'
    CHECK (measure_type IN ('UNIT', 'WEIGHT', 'VOLUME')),
  base_unit TEXT NOT NULL DEFAULT 'piece'
    CHECK (base_unit IN ('piece', 'gram', 'milliliter')),
  display_unit TEXT NOT NULL DEFAULT 'piece'
    CHECK (display_unit IN ('piece', 'gram', 'kilogram', 'milliliter', 'liter')),
  quantity_on_hand NUMERIC NOT NULL DEFAULT 0 CHECK (quantity_on_hand >= 0),
  low_stock_threshold NUMERIC NOT NULL DEFAULT 0 CHECK (low_stock_threshold >= 0),
  is_active BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.inventory_item_links (
  id BIGSERIAL PRIMARY KEY,
  inventory_item_id TEXT NOT NULL REFERENCES public.inventory_items(id) ON DELETE CASCADE,
  entity_kind TEXT NOT NULL
    CHECK (entity_kind IN ('SOURCE_PRODUCT', 'ADDON_OPTION')),
  entity_id TEXT NOT NULL DEFAULT '',
  consumption_quantity NUMERIC NOT NULL DEFAULT 0 CHECK (consumption_quantity > 0),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (inventory_item_id, entity_kind, entity_id)
);

CREATE INDEX IF NOT EXISTS idx_inventory_item_links_entity
  ON public.inventory_item_links(entity_kind, entity_id);

ALTER TABLE public.source_products
  ADD COLUMN IF NOT EXISTS inventory_mode TEXT NOT NULL DEFAULT 'available';

DO $$
BEGIN
  IF to_regclass('public.addon_options') IS NOT NULL THEN
    ALTER TABLE public.addon_options
      ADD COLUMN IF NOT EXISTS inventory_mode TEXT NOT NULL DEFAULT 'available';
  END IF;
END;
$$;

UPDATE public.source_products
SET inventory_mode = 'available'
WHERE NULLIF(TRIM(COALESCE(inventory_mode, '')), '') IS NULL
   OR LOWER(TRIM(COALESCE(inventory_mode, ''))) NOT IN ('available', 'unavailable', 'linked');

DO $$
BEGIN
  IF to_regclass('public.addon_options') IS NOT NULL THEN
    UPDATE public.addon_options
    SET inventory_mode = 'available'
    WHERE NULLIF(TRIM(COALESCE(inventory_mode, '')), '') IS NULL
       OR LOWER(TRIM(COALESCE(inventory_mode, ''))) NOT IN ('available', 'unavailable', 'linked');
  END IF;
END;
$$;

UPDATE public.source_products sp
SET inventory_mode = 'linked',
    updated_at = now()
WHERE LOWER(TRIM(COALESCE(sp.inventory_mode, 'available'))) = 'available'
  AND EXISTS (
    SELECT 1
    FROM public.inventory_item_links l
    WHERE l.entity_kind = 'SOURCE_PRODUCT'
      AND l.entity_id = sp.id
  );

DO $$
BEGIN
  IF to_regclass('public.addon_options') IS NOT NULL THEN
    UPDATE public.addon_options o
    SET inventory_mode = 'linked'
    WHERE LOWER(TRIM(COALESCE(o.inventory_mode, 'available'))) = 'available'
      AND EXISTS (
        SELECT 1
        FROM public.inventory_item_links l
        WHERE l.entity_kind = 'ADDON_OPTION'
          AND l.entity_id = o.id
      );
  END IF;
END;
$$;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conrelid = 'public.source_products'::regclass
      AND conname = 'source_products_inventory_mode_check'
  ) THEN
    ALTER TABLE public.source_products
      ADD CONSTRAINT source_products_inventory_mode_check
      CHECK (inventory_mode IN ('available', 'unavailable', 'linked'));
  END IF;
END;
$$;

DO $$
BEGIN
  IF to_regclass('public.addon_options') IS NOT NULL
     AND NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conrelid = 'public.addon_options'::regclass
      AND conname = 'addon_options_inventory_mode_check'
  ) THEN
    ALTER TABLE public.addon_options
      ADD CONSTRAINT addon_options_inventory_mode_check
      CHECK (inventory_mode IN ('available', 'unavailable', 'linked'));
  END IF;
END;
$$;


-- -----------------------------------------------------------------------------
-- 2) كتالوج التطبيق (منفصل عن الـ POS)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.app_categories (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  sort_order INT NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS public.app_products (
  id TEXT PRIMARY KEY,
  category_id TEXT NOT NULL REFERENCES public.app_categories(id) ON DELETE CASCADE,
  source_product_id TEXT NOT NULL REFERENCES public.source_products(id) ON DELETE RESTRICT,
  name TEXT NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT '',
  image_url TEXT NOT NULL DEFAULT '',
  image_url_light TEXT NOT NULL DEFAULT '',
  image_url_dark TEXT NOT NULL DEFAULT '',
  enabled INT NOT NULL DEFAULT 1 CHECK (enabled IN (0, 1)),
  sort_order INT NOT NULL DEFAULT 0,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- -----------------------------------------------------------------------------
-- 3) كتالوج الـ POS (منفصل عن التطبيق - أسماء وترتيب وعرض مستقل)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.pos_categories (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  sort_order INT NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS public.pos_products (
  id TEXT PRIMARY KEY,
  pos_category_id TEXT NOT NULL REFERENCES public.pos_categories(id) ON DELETE CASCADE,
  source_product_id TEXT NOT NULL REFERENCES public.source_products(id) ON DELETE RESTRICT,
  name TEXT NOT NULL DEFAULT '',
  pos_display_name TEXT NOT NULL DEFAULT '',
  sort_order INT NOT NULL DEFAULT 0,
  enabled INT NOT NULL DEFAULT 1 CHECK (enabled IN (0, 1))
);

-- -----------------------------------------------------------------------------
-- 4) الإضافات (Addons) للتطبيق
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.addon_groups (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  image_url TEXT NOT NULL DEFAULT '',
  kind TEXT NOT NULL DEFAULT 'normal',
  mode TEXT NOT NULL DEFAULT 'multi',
  required BOOLEAN NOT NULL DEFAULT false,
  max_count INT NOT NULL DEFAULT 0 CHECK (max_count >= 0),
  enabled BOOLEAN NOT NULL DEFAULT true,
  sort_order INT NOT NULL DEFAULT 0,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.addon_options (
  id TEXT PRIMARY KEY,
  group_id TEXT NOT NULL REFERENCES public.addon_groups(id) ON DELETE CASCADE,
  name TEXT NOT NULL DEFAULT '',
  price NUMERIC NOT NULL DEFAULT 0 CHECK (price >= 0),
  inventory_mode TEXT NOT NULL DEFAULT 'available',
  enabled BOOLEAN NOT NULL DEFAULT true,
  image_url TEXT NOT NULL DEFAULT '',
  image_url_light TEXT NOT NULL DEFAULT '',
  image_url_dark TEXT NOT NULL DEFAULT '',
  sort_order INT NOT NULL DEFAULT 0,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE public.app_products
  ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();

ALTER TABLE public.addon_groups
  ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();

ALTER TABLE public.addon_options
  ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();

UPDATE public.app_products
SET updated_at = now()
WHERE updated_at IS NULL;

UPDATE public.addon_groups
SET updated_at = now()
WHERE updated_at IS NULL;

UPDATE public.addon_options
SET updated_at = now()
WHERE updated_at IS NULL;

CREATE OR REPLACE FUNCTION public.trg_touch_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
SET search_path = public
AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_touch_updated_at_app_products ON public.app_products;
CREATE TRIGGER trg_touch_updated_at_app_products
BEFORE UPDATE ON public.app_products
FOR EACH ROW
EXECUTE FUNCTION public.trg_touch_updated_at();

DROP TRIGGER IF EXISTS trg_touch_updated_at_addon_groups ON public.addon_groups;
CREATE TRIGGER trg_touch_updated_at_addon_groups
BEFORE UPDATE ON public.addon_groups
FOR EACH ROW
EXECUTE FUNCTION public.trg_touch_updated_at();

DROP TRIGGER IF EXISTS trg_touch_updated_at_addon_options ON public.addon_options;
CREATE TRIGGER trg_touch_updated_at_addon_options
BEFORE UPDATE ON public.addon_options
FOR EACH ROW
EXECUTE FUNCTION public.trg_touch_updated_at();

CREATE TABLE IF NOT EXISTS public.app_category_default_groups (
  category_id TEXT NOT NULL REFERENCES public.app_categories(id) ON DELETE CASCADE,
  group_id TEXT NOT NULL REFERENCES public.addon_groups(id) ON DELETE CASCADE,
  sort_order INT NOT NULL DEFAULT 0,
  PRIMARY KEY (category_id, group_id)
);

CREATE TABLE IF NOT EXISTS public.app_product_group_overrides (
  product_id TEXT NOT NULL REFERENCES public.app_products(id) ON DELETE CASCADE,
  group_id TEXT NOT NULL REFERENCES public.addon_groups(id) ON DELETE CASCADE,
  enabled BOOLEAN NOT NULL DEFAULT true,
  mode_override TEXT,
  required_override BOOLEAN,
  max_override INT,
  PRIMARY KEY (product_id, group_id)
);

-- -----------------------------------------------------------------------------
-- 5) الحسابات (كل السائقين والكاشير والأدمن في users فقط)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.users (
  id TEXT PRIMARY KEY,
  username TEXT,
  phone TEXT,
  email TEXT,
  display_name TEXT,
  is_active BOOLEAN NOT NULL DEFAULT true,
  role TEXT NOT NULL DEFAULT 'CUSTOMER' CHECK (role IN ('CUSTOMER', 'DRIVER', 'CASHIER', 'ADMIN', 'ADMIN_POS')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ضمان دعم ADMIN_POS حتى لو الجدول تم إنشاؤه سابقا بقيد أقدم.
DO $$
DECLARE
  c RECORD;
BEGIN
  FOR c IN
    SELECT conname
    FROM pg_constraint
    WHERE conrelid = 'public.users'::regclass
      AND contype = 'c'
      AND (
        conname ILIKE '%role%'
        OR pg_get_constraintdef(oid) ILIKE '%role%'
      )
  LOOP
    EXECUTE format('ALTER TABLE public.users DROP CONSTRAINT IF EXISTS %I', c.conname);
  END LOOP;

  ALTER TABLE public.users DROP CONSTRAINT IF EXISTS users_role_check;

  ALTER TABLE public.users
    ADD CONSTRAINT users_role_check
    CHECK (role IN ('CUSTOMER', 'DRIVER', 'CASHIER', 'ADMIN', 'ADMIN_POS'));
END;
$$;

CREATE OR REPLACE FUNCTION public.trg_users_normalize_unique()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_username TEXT := NULLIF(TRIM(COALESCE(NEW.username, '')), '');
  v_phone TEXT := NULLIF(TRIM(COALESCE(NEW.phone, '')), '');
  v_email TEXT := NULLIF(TRIM(LOWER(COALESCE(NEW.email, ''))), '');
BEGIN
  NEW.username := v_username;
  NEW.phone := v_phone;
  NEW.email := v_email;

  IF v_username IS NOT NULL AND EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id <> COALESCE(NEW.id, '')
      AND NULLIF(TRIM(COALESCE(u.username, '')), '') = v_username
  ) THEN
    RAISE EXCEPTION 'username_exists';
  END IF;

  IF v_phone IS NOT NULL AND EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id <> COALESCE(NEW.id, '')
      AND NULLIF(TRIM(COALESCE(u.phone, '')), '') = v_phone
  ) THEN
    RAISE EXCEPTION 'phone_exists';
  END IF;

  IF v_email IS NOT NULL AND EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id <> COALESCE(NEW.id, '')
      AND NULLIF(TRIM(LOWER(COALESCE(u.email, ''))), '') = v_email
  ) THEN
    RAISE EXCEPTION 'email_exists';
  END IF;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_users_normalize_unique ON public.users;
CREATE TRIGGER trg_users_normalize_unique
BEFORE INSERT OR UPDATE ON public.users
FOR EACH ROW
EXECUTE FUNCTION public.trg_users_normalize_unique();

CREATE TABLE IF NOT EXISTS public.user_login_secrets (
  user_id TEXT PRIMARY KEY REFERENCES public.users(id) ON DELETE CASCADE,
  password_hash TEXT,
  pin_hash TEXT,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.app_role_sessions (
  token_hash TEXT PRIMARY KEY,
  user_id TEXT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  expires_at TIMESTAMPTZ NOT NULL,
  revoked_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  last_seen_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_app_role_sessions_user_id
  ON public.app_role_sessions(user_id);

CREATE INDEX IF NOT EXISTS idx_app_role_sessions_expires_at
  ON public.app_role_sessions(expires_at);

-- توكنات Push (FCM) للأجهزة لكل مستخدم
CREATE TABLE IF NOT EXISTS public.user_push_tokens (
  id BIGSERIAL PRIMARY KEY,
  user_id TEXT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  device_id TEXT NOT NULL,
  platform TEXT NOT NULL DEFAULT 'android',
  fcm_token TEXT NOT NULL,
  app_role TEXT,
  app_version TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (user_id, device_id)
);

CREATE INDEX IF NOT EXISTS idx_user_push_tokens_user_id ON public.user_push_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_user_push_tokens_fcm_token ON public.user_push_tokens(fcm_token);

-- كل FCM token / device يجب أن يرتبط بمالك واحد حاليًا لتجنب الإشعارات المزدوجة أو الخاطئة.
DELETE FROM public.user_push_tokens a
USING public.user_push_tokens b
WHERE a.id < b.id
  AND (
    a.fcm_token = b.fcm_token
    OR a.device_id = b.device_id
  );

CREATE UNIQUE INDEX IF NOT EXISTS idx_user_push_tokens_device_id_unique
  ON public.user_push_tokens(device_id);

CREATE UNIQUE INDEX IF NOT EXISTS idx_user_push_tokens_fcm_token_unique
  ON public.user_push_tokens(fcm_token);

-- إزالة أي صفوف Push يتيمة (بيانات من سكيمات قديمة بدون FK صحيح).
DELETE FROM public.user_push_tokens t
WHERE NOT EXISTS (
  SELECT 1
  FROM public.users u
  WHERE u.id = t.user_id
);

-- التأكد من وجود FK user_push_tokens.user_id -> users.id حتى يكون المصدر الوحيد للهوية هو users.
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conrelid = 'public.user_push_tokens'::regclass
      AND contype = 'f'
      AND conname = 'user_push_tokens_user_id_fkey'
  ) THEN
    ALTER TABLE public.user_push_tokens
      ADD CONSTRAINT user_push_tokens_user_id_fkey
      FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;
  END IF;
END;
$$;

-- app_role يتم اشتقاقه دائمًا من users.role (لا نعتمد على أي قيمة مرسلة من التطبيقات).
CREATE OR REPLACE FUNCTION public.trg_user_push_tokens_sync_role()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_role TEXT;
BEGIN
  SELECT UPPER(COALESCE(u.role, 'CUSTOMER'))
  INTO v_role
  FROM public.users u
  WHERE u.id = NEW.user_id
  LIMIT 1;

  NEW.app_role := COALESCE(NULLIF(v_role, ''), 'CUSTOMER');
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_user_push_tokens_sync_role ON public.user_push_tokens;
CREATE TRIGGER trg_user_push_tokens_sync_role
BEFORE INSERT OR UPDATE ON public.user_push_tokens
FOR EACH ROW
EXECUTE FUNCTION public.trg_user_push_tokens_sync_role();

-- Backfill القيم الحالية من users.role.
UPDATE public.user_push_tokens t
SET app_role = UPPER(COALESCE(u.role, 'CUSTOMER')),
    updated_at = now()
FROM public.users u
WHERE u.id = t.user_id;



-- تنظيف جدول قديم (إن وجد) كان يسبب التباسًا في FCM.
DROP TABLE IF EXISTS public.device_push_tokens;

-- -----------------------------------------------------------------------------
-- 6) الأحياء وساعات العمل والكوبونات
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.hoods (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  fee NUMERIC NOT NULL DEFAULT 0 CHECK (fee >= 0)
);

CREATE TABLE IF NOT EXISTS public.app_work_hours (
  id INT PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  open_at_minutes INT NOT NULL DEFAULT 420 CHECK (open_at_minutes >= 0 AND open_at_minutes <= 1439),
  close_at_minutes INT NOT NULL DEFAULT 180 CHECK (close_at_minutes >= 0 AND close_at_minutes <= 1439)
);

INSERT INTO public.app_work_hours (id, open_at_minutes, close_at_minutes)
VALUES (1, 420, 180)
ON CONFLICT (id) DO NOTHING;
CREATE TABLE IF NOT EXISTS public.app_settings (
  id INT PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  appearance_preset TEXT NOT NULL DEFAULT 'normal',
  guest_appearance_preset TEXT NOT NULL DEFAULT 'normal',
  launcher_icon TEXT NOT NULL DEFAULT 'normal',
  share_web_base_url TEXT NOT NULL DEFAULT 'https://fale7.app',
  maintenance_mode BOOLEAN NOT NULL DEFAULT false,
  maintenance_allow_guest BOOLEAN NOT NULL DEFAULT false,
  updated_at_millis BIGINT NOT NULL DEFAULT (extract(epoch from now()) * 1000)::BIGINT,
  updated_by TEXT
);

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS appearance_preset TEXT NOT NULL DEFAULT 'normal';

ALTER TABLE public.app_settings
  ALTER COLUMN appearance_preset SET DEFAULT 'normal';

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS guest_appearance_preset TEXT NOT NULL DEFAULT 'normal';

ALTER TABLE public.app_settings
  ALTER COLUMN guest_appearance_preset SET DEFAULT 'normal';

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS launcher_icon TEXT NOT NULL DEFAULT 'normal';

ALTER TABLE public.app_settings
  ALTER COLUMN launcher_icon SET DEFAULT 'normal';

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS share_web_base_url TEXT NOT NULL DEFAULT 'https://fale7.app';

ALTER TABLE public.app_settings
  ALTER COLUMN share_web_base_url SET DEFAULT 'https://fale7.app';

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS maintenance_mode BOOLEAN NOT NULL DEFAULT false;

ALTER TABLE public.app_settings
  ALTER COLUMN maintenance_mode SET DEFAULT false;

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS maintenance_allow_guest BOOLEAN NOT NULL DEFAULT false;

ALTER TABLE public.app_settings
  ALTER COLUMN maintenance_allow_guest SET DEFAULT false;

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS updated_at_millis BIGINT NOT NULL DEFAULT (extract(epoch from now()) * 1000)::BIGINT;

ALTER TABLE public.app_settings
  ALTER COLUMN updated_at_millis SET DEFAULT (extract(epoch from now()) * 1000)::BIGINT;

ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS updated_by TEXT;

UPDATE public.app_settings
SET appearance_preset = 'normal'
WHERE appearance_preset IS NULL
   OR NULLIF(TRIM(COALESCE(appearance_preset, '')), '') IS NULL;

UPDATE public.app_settings
SET guest_appearance_preset = appearance_preset
WHERE guest_appearance_preset IS NULL
   OR NULLIF(TRIM(COALESCE(guest_appearance_preset, '')), '') IS NULL;

UPDATE public.app_settings
SET launcher_icon = 'normal'
WHERE launcher_icon IS NULL
   OR NULLIF(TRIM(COALESCE(launcher_icon, '')), '') IS NULL;

UPDATE public.app_settings
SET share_web_base_url = 'https://fale7.app'
WHERE share_web_base_url IS NULL
   OR NULLIF(TRIM(COALESCE(share_web_base_url, '')), '') IS NULL;

UPDATE public.app_settings
SET updated_at_millis = (extract(epoch from now()) * 1000)::BIGINT
WHERE updated_at_millis IS NULL;

UPDATE public.app_settings
SET maintenance_allow_guest = false
WHERE maintenance_allow_guest IS NULL;

INSERT INTO public.app_settings (
  id,
  appearance_preset,
  guest_appearance_preset,
  launcher_icon,
  share_web_base_url,
  maintenance_mode,
  maintenance_allow_guest,
  updated_at_millis
)
VALUES (
  1,
  'normal',
  'normal',
  'normal',
  'https://fale7.app',
  false,
  false,
  (extract(epoch from now()) * 1000)::BIGINT
)
ON CONFLICT (id) DO NOTHING;

-- إعدادات Push: يتم ضبطها من SQL (مرة واحدة)
-- ملاحظات مهمة:
-- 1) لا تضع أي سر حقيقي داخل هذا الملف أو داخل Git.
-- 2) المصادقة الحالية تعتمد على Edge secret فقط:
--    - Secret في Supabase Edge Functions باسم: PUSH_EDGE_SECRET
--    - نفس القيمة تُحفظ في: public.app_settings.push_edge_secret
-- 3) بديل JWT موجود تقنيًا عبر push_edge_jwt لكنه غير مستخدم حاليًا.
-- 4) بيانات Firebase الخاصة بالإرسال الخلفي يجب أن تكون في Edge secrets:
--    - FCM_SERVICE_ACCOUNT_JSON
--    - أو القيم المنفصلة: FCM_PROJECT_ID / FCM_CLIENT_EMAIL / FCM_PRIVATE_KEY
-- 5) مثال ضبط آمن بعد النشر:
--    update public.app_settings
--    set push_provider = 'EDGE_V1',
--        push_edge_url = 'https://<project-ref>.supabase.co/functions/v1/push-fcm-v1',
--        push_edge_secret = '<same value as PUSH_EDGE_SECRET>'
--    where id = 1;
ALTER TABLE public.app_settings
  ADD COLUMN IF NOT EXISTS push_enabled BOOLEAN NOT NULL DEFAULT true,
  ADD COLUMN IF NOT EXISTS fcm_server_key TEXT,
  ADD COLUMN IF NOT EXISTS push_provider TEXT NOT NULL DEFAULT 'EDGE_V1',
  ADD COLUMN IF NOT EXISTS push_edge_url TEXT,
  ADD COLUMN IF NOT EXISTS push_edge_secret TEXT,
  ADD COLUMN IF NOT EXISTS push_edge_jwt TEXT;

ALTER TABLE public.app_settings
  ALTER COLUMN push_enabled SET DEFAULT true;

ALTER TABLE public.app_settings
  ALTER COLUMN push_provider SET DEFAULT 'EDGE_V1';

UPDATE public.app_settings
SET push_enabled = true
WHERE id = 1
  AND push_enabled IS DISTINCT FROM true;

CREATE TABLE IF NOT EXISTS public.coupons (
  id TEXT PRIMARY KEY,
  code TEXT NOT NULL,
  title TEXT NOT NULL DEFAULT '',
  enabled BOOLEAN NOT NULL DEFAULT true,
  free_delivery BOOLEAN NOT NULL DEFAULT false,
  discount_amount NUMERIC NOT NULL DEFAULT 0 CHECK (discount_amount >= 0),
  discount_percent NUMERIC NOT NULL DEFAULT 0 CHECK (discount_percent >= 0 AND discount_percent <= 100),
  min_order_amount NUMERIC NOT NULL DEFAULT 0 CHECK (min_order_amount >= 0),
  per_user_limit INT NOT NULL DEFAULT 0 CHECK (per_user_limit >= 0),
  max_customers INT NOT NULL DEFAULT 0 CHECK (max_customers >= 0),
  sort_order INT NOT NULL DEFAULT 0
);

-- استثناءات وضع الصيانة:
-- ACCOUNT_ONLY: يسمح لحساب معين بالدخول من أي جهاز.
-- DEVICE_ONLY: يسمح لجهاز معين بالدخول مهما تغير الحساب عليه.
CREATE TABLE IF NOT EXISTS public.maintenance_mode_exceptions (
  id BIGSERIAL PRIMARY KEY,
  scope TEXT NOT NULL CHECK (scope IN ('ACCOUNT_ONLY', 'DEVICE_ONLY')),
  user_id TEXT REFERENCES public.users(id) ON DELETE SET NULL,
  device_id TEXT,
  device_platform TEXT,
  device_app_version TEXT,
  created_by TEXT,
  updated_by TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CHECK (
    (scope = 'ACCOUNT_ONLY' AND user_id IS NOT NULL)
    OR (scope = 'DEVICE_ONLY' AND NULLIF(TRIM(COALESCE(device_id, '')), '') IS NOT NULL)
  )
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_maintenance_mode_exceptions_account_unique
  ON public.maintenance_mode_exceptions(user_id, scope)
  WHERE user_id IS NOT NULL AND scope = 'ACCOUNT_ONLY';

CREATE UNIQUE INDEX IF NOT EXISTS idx_maintenance_mode_exceptions_device_unique
  ON public.maintenance_mode_exceptions(device_id, scope)
  WHERE device_id IS NOT NULL AND scope = 'DEVICE_ONLY';

CREATE INDEX IF NOT EXISTS idx_maintenance_mode_exceptions_created_at
  ON public.maintenance_mode_exceptions(created_at DESC);

-- -----------------------------------------------------------------------------
-- 7) الطلبات والإشعارات
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.app_orders (
  id TEXT PRIMARY KEY,
  order_type TEXT NOT NULL DEFAULT 'DELIVERY',
  status TEXT NOT NULL DEFAULT 'received',
  customer_user_id TEXT,
  customer_name TEXT,
  customer_phone TEXT,
  district TEXT,
  address_text TEXT,
  pos_address_id TEXT,
  mobile_address_id TEXT,
  driver_user_id TEXT,
  subtotal NUMERIC NOT NULL DEFAULT 0,
  delivery_fee NUMERIC NOT NULL DEFAULT 0,
  total NUMERIC NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  metadata JSONB
);

ALTER TABLE public.app_orders DROP CONSTRAINT IF EXISTS app_orders_order_type_check;
ALTER TABLE public.app_orders ADD CONSTRAINT app_orders_order_type_check 
CHECK (order_type IN ('DELIVERY', 'PICKUP', 'POS_TAKEAWAY', 'POS_DELIVERY', 'POS_PICKUP'));

CREATE INDEX IF NOT EXISTS idx_app_orders_customer_created_at
  ON public.app_orders(customer_user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_app_orders_driver_created_at
  ON public.app_orders(driver_user_id, created_at DESC);

CREATE TABLE IF NOT EXISTS public.inventory_order_reservations (
  id BIGSERIAL PRIMARY KEY,
  order_id TEXT NOT NULL REFERENCES public.app_orders(id) ON DELETE CASCADE,
  inventory_item_id TEXT NOT NULL REFERENCES public.inventory_items(id) ON DELETE RESTRICT,
  quantity NUMERIC NOT NULL DEFAULT 0 CHECK (quantity > 0),
  source_kind TEXT NOT NULL
    CHECK (source_kind IN ('SOURCE_PRODUCT', 'ADDON_OPTION')),
  source_id TEXT NOT NULL DEFAULT '',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  released_at TIMESTAMPTZ,
  UNIQUE (order_id, inventory_item_id, source_kind, source_id)
);

CREATE INDEX IF NOT EXISTS idx_inventory_order_reservations_order_id
  ON public.inventory_order_reservations(order_id);

CREATE TABLE IF NOT EXISTS public.notifications (
  id UUID PRIMARY KEY DEFAULT extensions.gen_random_uuid(),
  user_id TEXT,
  role_target TEXT NOT NULL,
  order_id TEXT,
  order_type TEXT,
  title TEXT NOT NULL DEFAULT '',
  message TEXT NOT NULL DEFAULT '',
  image_url TEXT,
  read BOOLEAN NOT NULL DEFAULT false,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE public.notifications
  ADD COLUMN IF NOT EXISTS image_url TEXT;

CREATE INDEX IF NOT EXISTS idx_notifications_user_created_at
  ON public.notifications(user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_notifications_role_created_at
  ON public.notifications(role_target, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_notifications_order_created_at
  ON public.notifications(order_id, created_at DESC);

-- -----------------------------------------------------------------------------
-- 8) الشكاوى والمقترحات (بين العميل والمدير)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.support_threads (
  id BIGSERIAL PRIMARY KEY,
  complaint_no BIGINT NOT NULL DEFAULT 0,
  customer_id TEXT NOT NULL,
  customer_name TEXT NOT NULL DEFAULT '',
  title TEXT NOT NULL DEFAULT '',
  status TEXT NOT NULL DEFAULT 'open' CHECK (status IN ('open', 'closed')),
  last_message TEXT NOT NULL DEFAULT '',
  admin_reply_count INT NOT NULL DEFAULT 0,
  unread_by_customer INT NOT NULL DEFAULT 0,
  unread_by_admin INT NOT NULL DEFAULT 0,
  created_at_millis BIGINT NOT NULL DEFAULT (extract(epoch from now()) * 1000)::BIGINT,
  updated_at_millis BIGINT NOT NULL DEFAULT (extract(epoch from now()) * 1000)::BIGINT
);

CREATE TABLE IF NOT EXISTS public.support_messages (
  id BIGSERIAL PRIMARY KEY,
  thread_id BIGINT NOT NULL REFERENCES public.support_threads(id) ON DELETE CASCADE,
  sender_role TEXT NOT NULL CHECK (sender_role IN ('customer', 'admin')),
  sender_id TEXT NOT NULL,
  sender_name TEXT NOT NULL DEFAULT '',
  body TEXT NOT NULL DEFAULT '',
  attachment_uri TEXT,
  reply_to_message_id BIGINT REFERENCES public.support_messages(id) ON DELETE SET NULL,
  created_at_millis BIGINT NOT NULL DEFAULT (extract(epoch from now()) * 1000)::BIGINT
);

CREATE INDEX IF NOT EXISTS idx_support_threads_customer ON public.support_threads(customer_id);
CREATE INDEX IF NOT EXISTS idx_support_messages_thread ON public.support_messages(thread_id);

-- -----------------------------------------------------------------------------
-- 9) العناوين: عناوين التطبيق (العميل يضيفها) وعناوين مسجلة من الكاشير
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.customer_addresses (
  id SERIAL PRIMARY KEY,
  customer_phone TEXT NOT NULL,
  customer_user_id TEXT REFERENCES public.users(id) ON DELETE CASCADE,
  label TEXT NOT NULL DEFAULT '',
  district_id TEXT NOT NULL,
  block TEXT NOT NULL DEFAULT '',
  street TEXT NOT NULL DEFAULT '',
  building TEXT NOT NULL DEFAULT '',
  apt TEXT NOT NULL DEFAULT '',
  note TEXT NOT NULL DEFAULT '',
  is_default BOOLEAN NOT NULL DEFAULT false,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE public.customer_addresses
  ADD COLUMN IF NOT EXISTS customer_user_id TEXT;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conrelid = 'public.customer_addresses'::regclass
      AND contype = 'f'
      AND conname = 'customer_addresses_customer_user_id_fkey'
  ) THEN
    ALTER TABLE public.customer_addresses
      ADD CONSTRAINT customer_addresses_customer_user_id_fkey
      FOREIGN KEY (customer_user_id) REFERENCES public.users(id) ON DELETE CASCADE;
  END IF;
END;
$$;

CREATE TABLE IF NOT EXISTS public.pos_registered_addresses (
  id SERIAL PRIMARY KEY,
  phone TEXT NOT NULL,
  label TEXT NOT NULL DEFAULT '',
  address_line TEXT NOT NULL DEFAULT '',
  registered_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  device_id TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS idx_customer_addresses_phone ON public.customer_addresses(customer_phone);
CREATE INDEX IF NOT EXISTS idx_customer_addresses_user_id ON public.customer_addresses(customer_user_id);
CREATE INDEX IF NOT EXISTS idx_pos_registered_addresses_phone ON public.pos_registered_addresses(phone);

-- Backfill customer_addresses.customer_user_id from legacy owner keys.
UPDATE public.customer_addresses ca
SET customer_user_id = u.id
FROM public.users u
WHERE ca.customer_user_id IS NULL
  AND ca.customer_phone = u.id;

UPDATE public.customer_addresses ca
SET customer_user_id = u.id
FROM public.users u
WHERE ca.customer_user_id IS NULL
  AND NULLIF(TRIM(COALESCE(u.phone, '')), '') IS NOT NULL
  AND ca.customer_phone = u.phone;

UPDATE public.customer_addresses ca
SET customer_user_id = u.id
FROM public.users u
WHERE ca.customer_user_id IS NULL
  AND NULLIF(TRIM(COALESCE(u.email, '')), '') IS NOT NULL
  AND LOWER(ca.customer_phone) = LOWER(u.email);

UPDATE public.customer_addresses ca
SET customer_user_id = u.id
FROM public.users u
WHERE ca.customer_user_id IS NULL
  AND NULLIF(TRIM(COALESCE(u.username, '')), '') IS NOT NULL
  AND ca.customer_phone = u.username;

CREATE OR REPLACE FUNCTION public.resolve_customer_address_owner_user_id(
  p_owner_key TEXT
)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_owner_key TEXT := NULLIF(TRIM(COALESCE(p_owner_key, '')), '');
  v_user_id TEXT;
BEGIN
  IF v_owner_key IS NULL THEN
    RETURN NULL;
  END IF;

  SELECT u.id
  INTO v_user_id
  FROM public.users u
  WHERE u.id = v_owner_key
     OR (
       NULLIF(TRIM(COALESCE(u.phone, '')), '') IS NOT NULL
       AND u.phone = v_owner_key
     )
     OR (
       NULLIF(TRIM(COALESCE(u.email, '')), '') IS NOT NULL
       AND LOWER(u.email) = LOWER(v_owner_key)
     )
     OR (
       NULLIF(TRIM(COALESCE(u.username, '')), '') IS NOT NULL
       AND u.username = v_owner_key
     )
  ORDER BY
    CASE
      WHEN u.id = v_owner_key THEN 1
      WHEN u.phone = v_owner_key THEN 2
      WHEN LOWER(COALESCE(u.email, '')) = LOWER(v_owner_key) THEN 3
      ELSE 4
    END,
    u.updated_at DESC NULLS LAST,
    u.created_at DESC NULLS LAST
  LIMIT 1;

  RETURN v_user_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.resolve_customer_address_owner_phone(
  p_user_id TEXT
)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_user_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  v_phone TEXT;
BEGIN
  IF v_user_id IS NULL THEN
    RETURN NULL;
  END IF;

  SELECT COALESCE(NULLIF(TRIM(COALESCE(u.phone, '')), ''), u.id)
  INTO v_phone
  FROM public.users u
  WHERE u.id = v_user_id
  LIMIT 1;

  RETURN NULLIF(TRIM(COALESCE(v_phone, '')), '');
END;
$$;

CREATE OR REPLACE FUNCTION public.trg_customer_addresses_sync_owner()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_owner_user_id TEXT := COALESCE(
    NULLIF(TRIM(COALESCE(NEW.customer_user_id, '')), ''),
    public.resolve_customer_address_owner_user_id(NEW.customer_phone)
  );
BEGIN
  IF v_owner_user_id IS NOT NULL THEN
    NEW.customer_user_id := v_owner_user_id;
    NEW.customer_phone := COALESCE(
      public.resolve_customer_address_owner_phone(v_owner_user_id),
      v_owner_user_id
    );
  ELSE
    NEW.customer_user_id := NULL;
    NEW.customer_phone := NULLIF(TRIM(COALESCE(NEW.customer_phone, '')), '');
  END IF;

  IF NEW.customer_phone IS NULL THEN
    RAISE EXCEPTION 'customer_owner_required';
  END IF;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_customer_addresses_sync_owner ON public.customer_addresses;
CREATE TRIGGER trg_customer_addresses_sync_owner
BEFORE INSERT OR UPDATE ON public.customer_addresses
FOR EACH ROW
EXECUTE FUNCTION public.trg_customer_addresses_sync_owner();

UPDATE public.customer_addresses ca
SET customer_phone = public.resolve_customer_address_owner_phone(ca.customer_user_id)
WHERE ca.customer_user_id IS NOT NULL
  AND COALESCE(public.resolve_customer_address_owner_phone(ca.customer_user_id), '') <> COALESCE(ca.customer_phone, '');

CREATE OR REPLACE FUNCTION public.resolve_app_order_customer_user_id(
  p_customer_user_id TEXT,
  p_customer_phone TEXT,
  p_metadata JSONB DEFAULT '{}'::jsonb
)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_customer_user_id TEXT := NULLIF(TRIM(COALESCE(p_customer_user_id, '')), '');
  v_customer_phone TEXT := NULLIF(TRIM(COALESCE(p_customer_phone, '')), '');
  v_metadata JSONB := COALESCE(p_metadata, '{}'::jsonb);
  v_candidate TEXT;
BEGIN
  v_candidate := COALESCE(
    v_customer_user_id,
    NULLIF(TRIM(COALESCE(v_metadata->>'customerUserId', '')), ''),
    NULLIF(TRIM(COALESCE(v_metadata->>'customer_user_id', '')), '')
  );

  IF v_candidate IS NOT NULL
     AND EXISTS (
       SELECT 1
       FROM public.users u
       WHERE u.id = v_candidate
       LIMIT 1
     ) THEN
    RETURN v_candidate;
  END IF;

  v_candidate := COALESCE(
    v_customer_phone,
    NULLIF(TRIM(COALESCE(v_metadata->>'customerPhone', '')), ''),
    NULLIF(TRIM(COALESCE(v_metadata->>'customer_phone', '')), '')
  );

  IF v_candidate IS NULL THEN
    RETURN NULL;
  END IF;

  RETURN public.resolve_customer_address_owner_user_id(v_candidate);
END;
$$;

CREATE OR REPLACE FUNCTION public.trg_app_orders_sync_customer_owner()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  NEW.customer_user_id := COALESCE(
    public.resolve_app_order_customer_user_id(
      NEW.customer_user_id,
      NEW.customer_phone,
      COALESCE(NEW.metadata, '{}'::jsonb)
    ),
    NULLIF(TRIM(COALESCE(NEW.customer_user_id, '')), '')
  );

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_app_orders_sync_customer_owner ON public.app_orders;
CREATE TRIGGER trg_app_orders_sync_customer_owner
BEFORE INSERT OR UPDATE ON public.app_orders
FOR EACH ROW
EXECUTE FUNCTION public.trg_app_orders_sync_customer_owner();

UPDATE public.app_orders o
SET customer_user_id = resolved.resolved_user_id
FROM (
  SELECT
    ao.id,
    public.resolve_app_order_customer_user_id(
      ao.customer_user_id,
      ao.customer_phone,
      COALESCE(ao.metadata, '{}'::jsonb)
    ) AS resolved_user_id
  FROM public.app_orders ao
) resolved
WHERE resolved.id = o.id
  AND resolved.resolved_user_id IS NOT NULL
  AND COALESCE(NULLIF(TRIM(COALESCE(o.customer_user_id, '')), ''), '') <> resolved.resolved_user_id;

-- -----------------------------------------------------------------------------
-- 10) استخدام الكوبونات (لاحصائيات per_user و max_customers)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.coupon_usage (
  coupon_id TEXT NOT NULL REFERENCES public.coupons(id) ON DELETE CASCADE,
  customer_id TEXT NOT NULL,
  order_id TEXT,
  used_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (coupon_id, customer_id, order_id)
);

-- -----------------------------------------------------------------------------
-- 10.1) حجوزات مخزون مؤقتة للعميل (تفاصيل المنتج / السلة)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.inventory_client_reservations (
  id BIGSERIAL PRIMARY KEY,
  reservation_key TEXT NOT NULL,
  scope TEXT NOT NULL CHECK (scope IN ('DETAILS', 'CART')),
  scope_ref TEXT NOT NULL DEFAULT '',
  inventory_item_id TEXT NOT NULL REFERENCES public.inventory_items(id) ON DELETE CASCADE,
  entity_kind TEXT NOT NULL
    CHECK (entity_kind IN ('SOURCE_PRODUCT', 'ADDON_OPTION')),
  entity_id TEXT NOT NULL DEFAULT '',
  quantity NUMERIC NOT NULL DEFAULT 0 CHECK (quantity > 0),
  expires_at TIMESTAMPTZ NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (reservation_key, scope, scope_ref, inventory_item_id, entity_kind, entity_id)
);

CREATE INDEX IF NOT EXISTS idx_inventory_client_reservations_inventory_item
  ON public.inventory_client_reservations(inventory_item_id, expires_at);

CREATE INDEX IF NOT EXISTS idx_inventory_client_reservations_scope
  ON public.inventory_client_reservations(reservation_key, scope, expires_at);

CREATE TABLE IF NOT EXISTS public.inventory_reservation_events (
  id INT PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

INSERT INTO public.inventory_reservation_events (id, updated_at)
VALUES (1, now())
ON CONFLICT (id) DO NOTHING;

DO $$
BEGIN
  IF EXISTS (
    SELECT 1
    FROM pg_publication
    WHERE pubname = 'supabase_realtime'
  ) THEN
    BEGIN
      ALTER PUBLICATION supabase_realtime
      ADD TABLE public.inventory_reservation_events;
    EXCEPTION
      WHEN duplicate_object THEN NULL;
      WHEN undefined_object THEN NULL;
    END;
  END IF;
END;
$$;

-- =============================================================================
-- RPCs
-- =============================================================================

-- صحة الباك اند
CREATE OR REPLACE FUNCTION public.api_backend_healthcheck()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  RETURN json_build_object('ok', true, 'source', 'supabase');
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_normalize_display_unit(p_unit TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
  v_unit TEXT := LOWER(NULLIF(TRIM(COALESCE(p_unit, '')), ''));
BEGIN
  IF v_unit IN ('piece', 'pieces', 'unit', 'units', 'count') THEN
    RETURN 'piece';
  END IF;
  IF v_unit IN ('gram', 'grams', 'g') THEN
    RETURN 'gram';
  END IF;
  IF v_unit IN ('kilogram', 'kilograms', 'kg', 'kilo') THEN
    RETURN 'kilogram';
  END IF;
  IF v_unit IN ('milliliter', 'milliliters', 'ml') THEN
    RETURN 'milliliter';
  END IF;
  IF v_unit IN ('liter', 'liters', 'l') THEN
    RETURN 'liter';
  END IF;
  RETURN 'piece';
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_base_unit_from_display_unit(p_unit TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
  v_unit TEXT := public.inventory_normalize_display_unit(p_unit);
BEGIN
  IF v_unit = 'kilogram' THEN RETURN 'gram'; END IF;
  IF v_unit = 'liter' THEN RETURN 'milliliter'; END IF;
  RETURN v_unit;
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_to_base_quantity(
  p_quantity NUMERIC,
  p_unit TEXT
)
RETURNS NUMERIC
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
  v_qty NUMERIC := COALESCE(p_quantity, 0);
  v_unit TEXT := public.inventory_normalize_display_unit(p_unit);
BEGIN
  IF v_qty <= 0 THEN
    RETURN 0;
  END IF;
  IF v_unit = 'kilogram' THEN RETURN v_qty * 1000; END IF;
  IF v_unit = 'liter' THEN RETURN v_qty * 1000; END IF;
  RETURN v_qty;
END;
$$;

CREATE OR REPLACE FUNCTION public.current_inventory_reservation_key()
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_headers JSONB := '{}'::jsonb;
  v_val TEXT;
BEGIN
  BEGIN
    v_headers := COALESCE(
      NULLIF(current_setting('request.headers', true), '')::jsonb,
      '{}'::jsonb
    );
  EXCEPTION WHEN OTHERS THEN
    v_headers := '{}'::jsonb;
  END;

  SELECT NULLIF(TRIM(j.value), '')
  INTO v_val
  FROM jsonb_each_text(v_headers) AS j(key, value)
  WHERE LOWER(j.key) IN (
      'x-fale7-reservation',
      'x_fale7_reservation',
      'reservation_key',
      'reservation-key'
    )
    AND NULLIF(TRIM(j.value), '') IS NOT NULL
  LIMIT 1;

  RETURN v_val;
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_active_reserved_quantity(
  p_inventory_item_id TEXT,
  p_exclude_reservation_key TEXT DEFAULT NULL,
  p_exclude_scope TEXT DEFAULT NULL
)
RETURNS NUMERIC
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_inventory_item_id TEXT := NULLIF(TRIM(COALESCE(p_inventory_item_id, '')), '');
  v_exclude_reservation_key TEXT := NULLIF(TRIM(COALESCE(p_exclude_reservation_key, '')), '');
  v_exclude_scope TEXT := UPPER(NULLIF(TRIM(COALESCE(p_exclude_scope, '')), ''));
  v_total NUMERIC := 0;
BEGIN
  IF v_inventory_item_id IS NULL THEN
    RETURN 0;
  END IF;

  SELECT COALESCE(SUM(r.quantity), 0)
  INTO v_total
  FROM public.inventory_client_reservations r
  WHERE r.inventory_item_id = v_inventory_item_id
    AND r.expires_at > now()
    AND (
      v_exclude_reservation_key IS NULL
      OR r.reservation_key <> v_exclude_reservation_key
      OR (
        v_exclude_scope IS NOT NULL
        AND UPPER(COALESCE(r.scope, '')) <> v_exclude_scope
      )
    );

  RETURN GREATEST(COALESCE(v_total, 0), 0);
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_cleanup_expired_client_reservations()
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_count INT := 0;
BEGIN
  DELETE FROM public.inventory_client_reservations
  WHERE expires_at <= now();

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN COALESCE(v_count, 0);
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_entity_available_units(
  p_entity_kind TEXT,
  p_entity_id TEXT
)
RETURNS NUMERIC
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_kind TEXT := UPPER(NULLIF(TRIM(COALESCE(p_entity_kind, '')), ''));
  v_entity_id TEXT := NULLIF(TRIM(COALESCE(p_entity_id, '')), '');
  v_mode TEXT := public.inventory_entity_mode(v_kind, v_entity_id);
  v_units NUMERIC;
  v_reservation_key TEXT := public.current_inventory_reservation_key();
BEGIN
  IF v_kind IS NULL OR v_entity_id IS NULL THEN
    RETURN NULL;
  END IF;

  IF v_mode = 'unavailable' THEN
    RETURN 0;
  END IF;
  IF v_mode = 'available' THEN
    RETURN NULL;
  END IF;

  SELECT MIN(
    FLOOR(
      CASE
        WHEN COALESCE(l.consumption_quantity, 0) <= 0 THEN 0
        ELSE GREATEST(
          COALESCE(i.quantity_on_hand, 0) - COALESCE(
            public.inventory_active_reserved_quantity(
              i.id,
              v_reservation_key
            ),
            0
          ),
          0
        ) / l.consumption_quantity
      END
    )
  )
  INTO v_units
  FROM public.inventory_item_links l
  JOIN public.inventory_items i
    ON i.id = l.inventory_item_id
  WHERE UPPER(COALESCE(l.entity_kind, '')) = v_kind
    AND l.entity_id = v_entity_id;

  RETURN COALESCE(v_units, 0);
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_entity_mode(
  p_entity_kind TEXT,
  p_entity_id TEXT
)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_kind TEXT := UPPER(NULLIF(TRIM(COALESCE(p_entity_kind, '')), ''));
  v_entity_id TEXT := NULLIF(TRIM(COALESCE(p_entity_id, '')), '');
  v_mode TEXT := 'available';
BEGIN
  IF v_kind IS NULL OR v_entity_id IS NULL THEN
    RETURN 'available';
  END IF;

  IF v_kind = 'SOURCE_PRODUCT' THEN
    SELECT LOWER(COALESCE(NULLIF(TRIM(COALESCE(sp.inventory_mode, '')), ''), 'available'))
    INTO v_mode
    FROM public.source_products sp
    WHERE sp.id = v_entity_id
    LIMIT 1;
  ELSIF v_kind = 'ADDON_OPTION' THEN
    SELECT LOWER(COALESCE(NULLIF(TRIM(COALESCE(o.inventory_mode, '')), ''), 'available'))
    INTO v_mode
    FROM public.addon_options o
    WHERE o.id = v_entity_id
    LIMIT 1;
  END IF;

  IF v_mode NOT IN ('available', 'unavailable', 'linked') THEN
    RETURN 'available';
  END IF;
  RETURN v_mode;
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_entity_status(
  p_entity_kind TEXT,
  p_entity_id TEXT
)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_kind TEXT := UPPER(NULLIF(TRIM(COALESCE(p_entity_kind, '')), ''));
  v_entity_id TEXT := NULLIF(TRIM(COALESCE(p_entity_id, '')), '');
  v_mode TEXT := public.inventory_entity_mode(v_kind, v_entity_id);
  v_has_links BOOLEAN := false;
  v_reservation_key TEXT := public.current_inventory_reservation_key();
BEGIN
  IF v_kind IS NULL OR v_entity_id IS NULL THEN
    RETURN 'available';
  END IF;

  IF v_mode = 'unavailable' THEN
    RETURN 'out_of_stock';
  END IF;
  IF v_mode = 'available' THEN
    RETURN 'available';
  END IF;

  SELECT EXISTS (
    SELECT 1
    FROM public.inventory_item_links l
    WHERE UPPER(COALESCE(l.entity_kind, '')) = v_kind
      AND l.entity_id = v_entity_id
  )
  INTO v_has_links;

  IF NOT COALESCE(v_has_links, false) THEN
    RETURN 'out_of_stock';
  END IF;

  IF EXISTS (
    SELECT 1
    FROM public.inventory_item_links l
    JOIN public.inventory_items i
      ON i.id = l.inventory_item_id
    WHERE UPPER(COALESCE(l.entity_kind, '')) = v_kind
      AND l.entity_id = v_entity_id
      AND (
        COALESCE(i.is_active, false) = false
        OR GREATEST(
          COALESCE(i.quantity_on_hand, 0) - COALESCE(
            public.inventory_active_reserved_quantity(
              i.id,
              v_reservation_key
            ),
            0
          ),
          0
        ) < COALESCE(l.consumption_quantity, 0)
      )
  ) THEN
    RETURN 'out_of_stock';
  END IF;

  IF EXISTS (
    SELECT 1
    FROM public.inventory_item_links l
    JOIN public.inventory_items i
      ON i.id = l.inventory_item_id
    WHERE UPPER(COALESCE(l.entity_kind, '')) = v_kind
      AND l.entity_id = v_entity_id
      AND GREATEST(
            COALESCE(i.quantity_on_hand, 0) - COALESCE(
              public.inventory_active_reserved_quantity(
                i.id,
                v_reservation_key
              ),
              0
            ),
            0
          )
          <= GREATEST(COALESCE(i.low_stock_threshold, 0), COALESCE(l.consumption_quantity, 0))
  ) THEN
    RETURN 'limited';
  END IF;

  RETURN 'available';
END;
$$;

CREATE OR REPLACE FUNCTION public.inventory_release_order_reservations(
  p_order_id TEXT
)
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_order_id TEXT := NULLIF(TRIM(COALESCE(p_order_id, '')), '');
  v_count INT := 0;
BEGIN
  IF v_order_id IS NULL THEN
    RETURN 0;
  END IF;

  WITH pending AS (
    SELECT inventory_item_id, SUM(quantity) AS total_qty
    FROM public.inventory_order_reservations
    WHERE order_id = v_order_id
      AND released_at IS NULL
    GROUP BY inventory_item_id
  )
  UPDATE public.inventory_items i
  SET
    quantity_on_hand = COALESCE(i.quantity_on_hand, 0) + pending.total_qty,
    updated_at = now()
  FROM pending
  WHERE i.id = pending.inventory_item_id;

  UPDATE public.inventory_order_reservations
  SET released_at = now()
  WHERE order_id = v_order_id
    AND released_at IS NULL;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN COALESCE(v_count, 0);
END;
$$;

-- قائمة القوائم للموبايل من كتالوج التطبيق + السعر من source_products
CREATE OR REPLACE FUNCTION public.api_android_menu_snapshot()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  j JSON;
BEGIN
  SELECT json_build_object(
    'categories', COALESCE((SELECT json_agg(json_build_object(
      'id', c.id, 'name', c.name, 'sortOrder', c.sort_order, 'enabled', true
    ) ORDER BY c.sort_order) FROM app_categories c), '[]'::json),
    'products', COALESCE((
      SELECT json_agg(json_build_object(
        'id', p.id, 'categoryId', p.category_id, 'name', p.name, 'description', p.description,
        'sourceProductId', p.source_product_id,
        'basePrice', (SELECT sp.price FROM source_products sp WHERE sp.id = p.source_product_id),
        'imageUrl', p.image_url, 'imageUrlLight', p.image_url_light, 'imageUrlDark', p.image_url_dark,
        'updatedAt', p.updated_at,
        'enabled', p.enabled = 1, 'sortOrder', p.sort_order,
        'inventoryMode', public.inventory_entity_mode('SOURCE_PRODUCT', p.source_product_id),
        'inventoryStatus', public.inventory_entity_status('SOURCE_PRODUCT', p.source_product_id),
        'inventoryRemaining', public.inventory_entity_available_units('SOURCE_PRODUCT', p.source_product_id)
      ) ORDER BY p.sort_order)
      FROM app_products p
    ), '[]'::json),
    'addonGroups', COALESCE((SELECT json_agg(json_build_object(
      'id', g.id, 'name', g.name, 'imageUrl', g.image_url, 'kind', g.kind, 'mode', g.mode,
      'required', g.required, 'max', g.max_count, 'enabled', g.enabled, 'order', g.sort_order,
      'updatedAt', g.updated_at,
      'options', COALESCE((SELECT json_agg(json_build_object(
        'id', o.id, 'name', o.name, 'price', o.price, 'enabled', o.enabled,
        'imageUrl', o.image_url, 'imageUrlLight', o.image_url_light, 'imageUrlDark', o.image_url_dark, 'order', o.sort_order,
        'updatedAt', o.updated_at,
        'inventoryMode', public.inventory_entity_mode('ADDON_OPTION', o.id),
        'inventoryStatus', public.inventory_entity_status('ADDON_OPTION', o.id),
        'inventoryRemaining', public.inventory_entity_available_units('ADDON_OPTION', o.id)
      ) ORDER BY o.sort_order) FROM addon_options o WHERE o.group_id = g.id), '[]'::json)
    ) ORDER BY g.sort_order) FROM addon_groups g WHERE g.enabled), '[]'::json),
    'categoryDefaults', COALESCE((
      SELECT json_object_agg(cd.category_id, cd.group_ids)
      FROM (
        SELECT category_id, json_agg(group_id ORDER BY sort_order) AS group_ids
        FROM app_category_default_groups
        GROUP BY category_id
      ) cd
    ), '{}'::json),
    'productOverrides', COALESCE((SELECT json_object_agg(product_id, json_build_object(
      'linkedGroups', (SELECT json_object_agg(group_id, json_build_object(
        'enabled', enabled, 'mode', COALESCE(mode_override, 'multi'), 'required', COALESCE(required_override, false), 'max', COALESCE(max_override, 0)
      )) FROM app_product_group_overrides WHERE app_product_group_overrides.product_id = po.product_id)
    )) FROM (SELECT DISTINCT product_id FROM app_product_group_overrides) po), '{}'::json),
    'hoods', COALESCE((SELECT json_agg(json_build_object('id', id, 'name', name, 'fee', fee)) FROM hoods), '[]'::json),
    'workHours', (SELECT json_build_object('openAtMinutes', open_at_minutes, 'closeAtMinutes', close_at_minutes) FROM app_work_hours LIMIT 1),
    'coupons', COALESCE((SELECT json_agg(json_build_object(
      'id', id, 'code', code, 'title', title, 'enabled', enabled, 'freeDelivery', free_delivery,
      'discountAmount', discount_amount, 'discountPercent', discount_percent, 'minOrderAmount', min_order_amount,
      'perUserLimit', per_user_limit, 'maxCustomers', max_customers, 'order', sort_order
    ) ORDER BY sort_order) FROM coupons WHERE enabled), '[]'::json)
  ) INTO j;
  RETURN j;
END;
$$;

-- قائمة الـ POS من pos_categories و pos_products مع السعر من source_products
CREATE OR REPLACE FUNCTION public.api_pos_menu_snapshot()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  j JSON;
BEGIN
  SELECT json_build_object(
    'categories', COALESCE((SELECT json_agg(json_build_object(
      'id', c.id, 'name', c.name, 'sortOrder', c.sort_order
    ) ORDER BY c.sort_order) FROM pos_categories c), '[]'::json),
    'products', COALESCE((
      SELECT json_agg(json_build_object(
        'id', p.id, 'categoryId', p.pos_category_id, 'name', p.name,
        'sourceProductId', p.source_product_id,
        'basePrice', (SELECT sp.price FROM source_products sp WHERE sp.id = p.source_product_id),
        'enabled', p.enabled = 1, 'sortOrder', p.sort_order, 'posDisplayName', p.pos_display_name,
        'inventoryMode', public.inventory_entity_mode('SOURCE_PRODUCT', p.source_product_id),
        'inventoryStatus', public.inventory_entity_status('SOURCE_PRODUCT', p.source_product_id),
        'inventoryRemaining', public.inventory_entity_available_units('SOURCE_PRODUCT', p.source_product_id)
      ) ORDER BY p.sort_order)
      FROM pos_products p
    ), '[]'::json)
  ) INTO j;
  RETURN j;
END;
$$;

-- سنابشوت منتجات التطبيق للـ POS (للعناصر المرتبطة - نفس الـ ID والسعر فقط)
CREATE OR REPLACE FUNCTION public.api_pos_app_products_snapshot()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  j JSON;
BEGIN
  SELECT COALESCE(
    json_agg(prod_row.product_json ORDER BY prod_row.sort_order),
    '[]'::json
  )
  INTO j
  FROM (
    SELECT
      p.sort_order,
      json_build_object(
        'id', p.id,
        'sourceProductId', p.source_product_id,
        'basePrice', sp.price,
        'name', p.name,
        'updatedAt', p.updated_at,
        'inventoryMode', public.inventory_entity_mode('SOURCE_PRODUCT', p.source_product_id),
        'inventoryStatus', public.inventory_entity_status('SOURCE_PRODUCT', p.source_product_id),
        'inventoryRemaining', public.inventory_entity_available_units('SOURCE_PRODUCT', p.source_product_id),
        'addonGroups', COALESCE((
          SELECT json_agg(
            json_build_object(
              'id', g.id,
              'nameAr', g.name,
              'multiSelect', COALESCE(ov.mode_override, g.mode, 'multi') = 'multi',
              'options', COALESCE((
                SELECT json_agg(
                  json_build_object(
                    'id', o.id,
                    'nameAr', o.name,
                    'extraPrice', o.price,
                    'updatedAt', o.updated_at,
                    'inventoryMode', public.inventory_entity_mode('ADDON_OPTION', o.id),
                    'inventoryStatus', public.inventory_entity_status('ADDON_OPTION', o.id),
                    'inventoryRemaining', public.inventory_entity_available_units('ADDON_OPTION', o.id)
                  )
                  ORDER BY o.sort_order, o.id
                )
                FROM addon_options o
                WHERE o.group_id = g.id
                  AND o.enabled = true
              ), '[]'::json)
            )
            ORDER BY eg.sort_order, g.sort_order, g.id
          )
          FROM (
            SELECT
              src.group_id,
              MIN(src.sort_order) AS sort_order
            FROM (
              SELECT cd.group_id, cd.sort_order
              FROM app_category_default_groups cd
              WHERE cd.category_id = p.category_id
              UNION ALL
              SELECT po.group_id, 100000
              FROM app_product_group_overrides po
              WHERE po.product_id = p.id
                AND COALESCE(po.enabled, true) = true
            ) src
            LEFT JOIN app_product_group_overrides po_disable
              ON po_disable.product_id = p.id
             AND po_disable.group_id = src.group_id
            WHERE COALESCE(po_disable.enabled, true) = true
            GROUP BY src.group_id
          ) eg
          JOIN addon_groups g
            ON g.id = eg.group_id
           AND g.enabled = true
          LEFT JOIN app_product_group_overrides ov
            ON ov.product_id = p.id
           AND ov.group_id = g.id
        ), '[]'::json)
      ) AS product_json
    FROM app_products p
    JOIN source_products sp ON sp.id = p.source_product_id
    WHERE p.enabled = 1
  ) prod_row;
  RETURN j;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_pos_app_orders_snapshot(
  p_limit INT DEFAULT 300
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_limit INT := GREATEST(LEAST(COALESCE(p_limit, 300), 1000), 1);
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN '[]'::json;
  END IF;

  RETURN COALESCE(
    (
      SELECT json_agg(json_build_object(
        'id', o.id,
        'order_type', o.order_type,
        'status', o.status,
        'customer_user_id', o.customer_user_id,
        'customer_name', o.customer_name,
        'customer_phone', o.customer_phone,
        'district', o.district,
        'address_text', o.address_text,
        'driver_user_id', o.driver_user_id,
        'subtotal', o.subtotal,
        'delivery_fee', o.delivery_fee,
        'total', o.total,
        'created_at', o.created_at,
        'updated_at', o.updated_at,
        'metadata', COALESCE(o.metadata, '{}'::jsonb)
      ) ORDER BY o.created_at DESC)
      FROM (
        SELECT o.*
        FROM public.app_orders o
        ORDER BY o.created_at DESC
        LIMIT v_limit
      ) o
    ),
    '[]'::json
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_pos_notifications_snapshot(
  p_limit INT DEFAULT 100
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_actor_user_id TEXT := public.current_app_user_id();
  v_limit INT := GREATEST(LEAST(COALESCE(p_limit, 100), 500), 1);
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'DRIVER', 'SERVICE_ROLE') THEN
    RETURN '[]'::json;
  END IF;

  RETURN COALESCE(
    (
      SELECT json_agg(json_build_object(
        'id', n.id,
        'user_id', n.user_id,
        'role_target', n.role_target,
        'order_id', n.order_id,
        'order_type', n.order_type,
        'title', n.title,
        'message', n.message,
        'read', n.read,
        'created_at', n.created_at
      ) ORDER BY n.created_at DESC)
      FROM (
        SELECT n.*
        FROM public.notifications n
        WHERE
          (
            v_actor_role IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE')
            AND n.user_id IS NULL
            AND UPPER(COALESCE(n.role_target, '')) = 'CASHIER'
          )
          OR (
            v_actor_role = 'CASHIER'
            AND (
              (v_actor_user_id IS NOT NULL AND n.user_id = v_actor_user_id)
              OR (
                n.user_id IS NULL
                AND UPPER(COALESCE(n.role_target, '')) = 'CASHIER'
              )
            )
          )
          OR (
            v_actor_role = 'DRIVER'
            AND (
              (v_actor_user_id IS NOT NULL AND n.user_id = v_actor_user_id)
              OR (
                n.user_id IS NULL
                AND UPPER(COALESCE(n.role_target, '')) IN ('DRIVER', 'DELIVERY')
              )
            )
          )
        ORDER BY n.created_at DESC
        LIMIT v_limit
      ) n
    ),
    '[]'::json
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_pos_inventory_snapshot()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  j JSON;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'forbidden');
  END IF;

  SELECT COALESCE(
    json_agg(
      json_build_object(
        'id', i.id,
        'name', i.name,
        'measureType', i.measure_type,
        'baseUnit', i.base_unit,
        'displayUnit', i.display_unit,
        'quantityOnHand', i.quantity_on_hand,
        'lowStockThreshold', i.low_stock_threshold,
        'active', i.is_active,
        'linkedCount', COALESCE((
          SELECT COUNT(*)
          FROM public.inventory_item_links l
          WHERE l.inventory_item_id = i.id
        ), 0),
        'linksSummary', COALESCE((
          SELECT string_agg(link_name, '، ' ORDER BY link_name)
          FROM (
            SELECT DISTINCT
              CASE
                WHEN l.entity_kind = 'SOURCE_PRODUCT' THEN COALESCE(
                  NULLIF(TRIM(ap.name), ''),
                  NULLIF(TRIM(pp.pos_display_name), ''),
                  NULLIF(TRIM(pp.name), ''),
                  'منتج بدون اسم'
                )
                ELSE COALESCE(
                  NULLIF(TRIM(g.name), '') || ' / ' || NULLIF(TRIM(o.name), ''),
                  NULLIF(TRIM(o.name), ''),
                  'إضافة بدون اسم'
                )
              END AS link_name
            FROM public.inventory_item_links l
            LEFT JOIN public.app_products ap
              ON l.entity_kind = 'SOURCE_PRODUCT'
             AND ap.source_product_id = l.entity_id
            LEFT JOIN public.pos_products pp
              ON l.entity_kind = 'SOURCE_PRODUCT'
             AND pp.source_product_id = l.entity_id
            LEFT JOIN public.addon_options o
              ON l.entity_kind = 'ADDON_OPTION'
             AND o.id = l.entity_id
            LEFT JOIN public.addon_groups g
              ON o.group_id = g.id
            WHERE l.inventory_item_id = i.id
          ) link_rows
          WHERE link_name IS NOT NULL AND TRIM(link_name) <> ''
        ), '')
      )
      ORDER BY i.name, i.id
    ),
    '[]'::json
  )
  INTO j
  FROM public.inventory_items i;

  RETURN j;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_pos_inventory_admin_snapshot()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  j JSON;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'forbidden');
  END IF;

  SELECT json_build_object(
    'inventoryItems', COALESCE((
      SELECT json_agg(json_build_object(
        'id', i.id,
        'name', i.name,
        'measureType', i.measure_type,
        'baseUnit', i.base_unit,
        'displayUnit', i.display_unit,
        'quantityOnHand', i.quantity_on_hand,
        'lowStockThreshold', i.low_stock_threshold,
        'active', i.is_active
      ) ORDER BY i.name, i.id)
      FROM public.inventory_items i
    ), '[]'::json),
    'inventoryLinks', COALESCE((
      SELECT json_agg(json_build_object(
        'inventoryItemId', l.inventory_item_id,
        'entityKind', l.entity_kind,
        'entityId', l.entity_id,
        'consumptionQuantity', l.consumption_quantity
      ) ORDER BY l.entity_kind, l.entity_id, l.inventory_item_id)
      FROM public.inventory_item_links l
    ), '[]'::json),
    'targets', COALESCE((
      SELECT json_agg(target_row.target_json ORDER BY target_row.sort_name, target_row.entity_kind, target_row.entity_id)
      FROM (
        SELECT
          LOWER(COALESCE(src.name, '')) AS sort_name,
          src.entity_kind,
          src.entity_id,
          json_build_object(
            'entityKind', src.entity_kind,
            'entityId', src.entity_id,
            'name', src.name,
            'groupName', src.group_name,
            'catalogScope', src.catalog_scope,
            'inventoryMode', src.inventory_mode,
            'inventoryStatus', src.inventory_status,
            'inventoryRemaining', src.inventory_remaining,
            'linkedCount', src.linked_count
          ) AS target_json
        FROM (
          SELECT
            'SOURCE_PRODUCT'::TEXT AS entity_kind,
            sp.id AS entity_id,
            COALESCE(
              MIN(NULLIF(TRIM(ap.name), '')),
              MIN(NULLIF(TRIM(pp.pos_display_name), '')),
              MIN(NULLIF(TRIM(pp.name), '')),
              'منتج بدون اسم'
            ) AS name,
            ''::TEXT AS group_name,
            CASE
              WHEN COUNT(DISTINCT ap.id) > 0 AND COUNT(DISTINCT pp.id) > 0 THEN 'both'
              WHEN COUNT(DISTINCT ap.id) > 0 THEN 'app'
              WHEN COUNT(DISTINCT pp.id) > 0 THEN 'pos'
              ELSE 'unassigned'
            END AS catalog_scope,
            public.inventory_entity_mode('SOURCE_PRODUCT', sp.id) AS inventory_mode,
            public.inventory_entity_status('SOURCE_PRODUCT', sp.id) AS inventory_status,
            public.inventory_entity_available_units('SOURCE_PRODUCT', sp.id) AS inventory_remaining,
            COUNT(DISTINCT l.id) AS linked_count
          FROM public.source_products sp
          LEFT JOIN public.app_products ap
            ON ap.source_product_id = sp.id
          LEFT JOIN public.pos_products pp
            ON pp.source_product_id = sp.id
          LEFT JOIN public.inventory_item_links l
            ON l.entity_kind = 'SOURCE_PRODUCT'
           AND l.entity_id = sp.id
          GROUP BY sp.id

          UNION ALL

          SELECT
            'ADDON_OPTION'::TEXT AS entity_kind,
            o.id AS entity_id,
            COALESCE(NULLIF(TRIM(o.name), ''), 'إضافة بدون اسم') AS name,
            COALESCE(NULLIF(TRIM(g.name), ''), '') AS group_name,
            'addon'::TEXT AS catalog_scope,
            public.inventory_entity_mode('ADDON_OPTION', o.id) AS inventory_mode,
            public.inventory_entity_status('ADDON_OPTION', o.id) AS inventory_status,
            public.inventory_entity_available_units('ADDON_OPTION', o.id) AS inventory_remaining,
            COUNT(DISTINCT l.id) AS linked_count
          FROM public.addon_options o
          LEFT JOIN public.addon_groups g
            ON g.id = o.group_id
          LEFT JOIN public.inventory_item_links l
            ON l.entity_kind = 'ADDON_OPTION'
           AND l.entity_id = o.id
          GROUP BY o.id, o.name, g.name
        ) src
      ) target_row
    ), '[]'::json)
  )
  INTO j;

  RETURN j;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_inventory_upsert_item(
  p_item JSONB DEFAULT '{}'::jsonb
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_item JSONB := COALESCE(p_item, '{}'::jsonb);
  v_id TEXT := NULLIF(TRIM(COALESCE(v_item->>'id', '')), '');
  v_name TEXT := NULLIF(TRIM(COALESCE(v_item->>'name', '')), '');
  v_display_unit TEXT := public.inventory_normalize_display_unit(v_item->>'displayUnit');
  v_base_unit TEXT := public.inventory_base_unit_from_display_unit(v_display_unit);
  v_measure_type TEXT := CASE
    WHEN v_base_unit = 'piece' THEN 'UNIT'
    WHEN v_base_unit = 'gram' THEN 'WEIGHT'
    ELSE 'VOLUME'
  END;
  v_quantity_on_hand NUMERIC := GREATEST(COALESCE((v_item->>'quantityOnHand')::NUMERIC, 0), 0);
  v_low_stock_threshold NUMERIC := GREATEST(COALESCE((v_item->>'lowStockThreshold')::NUMERIC, 0), 0);
  v_active BOOLEAN := COALESCE((v_item->>'active')::BOOLEAN, true);
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_name IS NULL THEN
    RETURN jsonb_build_object('ok', false, 'error', 'name_required');
  END IF;

  IF v_id IS NULL THEN
    v_id := 'stock_' || REPLACE(extensions.gen_random_uuid()::TEXT, '-', '');
  END IF;

  INSERT INTO public.inventory_items (
    id,
    name,
    measure_type,
    base_unit,
    display_unit,
    quantity_on_hand,
    low_stock_threshold,
    is_active,
    updated_at
  )
  VALUES (
    v_id,
    v_name,
    v_measure_type,
    v_base_unit,
    v_display_unit,
    v_quantity_on_hand,
    v_low_stock_threshold,
    v_active,
    now()
  )
  ON CONFLICT (id) DO UPDATE
  SET
    name = EXCLUDED.name,
    measure_type = EXCLUDED.measure_type,
    base_unit = EXCLUDED.base_unit,
    display_unit = EXCLUDED.display_unit,
    quantity_on_hand = EXCLUDED.quantity_on_hand,
    low_stock_threshold = EXCLUDED.low_stock_threshold,
    is_active = EXCLUDED.is_active,
    updated_at = now();

  RETURN jsonb_build_object('ok', true, 'itemId', v_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.api_inventory_delete_item(
  p_inventory_item_id TEXT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_inventory_item_id TEXT := NULLIF(TRIM(COALESCE(p_inventory_item_id, '')), '');
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_inventory_item_id IS NULL THEN
    RETURN jsonb_build_object('ok', false, 'error', 'inventory_item_id_required');
  END IF;

  DELETE FROM public.inventory_item_links
  WHERE inventory_item_id = v_inventory_item_id;

  DELETE FROM public.inventory_items
  WHERE id = v_inventory_item_id;

  RETURN jsonb_build_object('ok', true, 'deleted', FOUND);
END;
$$;

CREATE OR REPLACE FUNCTION public.api_inventory_set_entity_mode(
  p_entity_kind TEXT,
  p_entity_id TEXT,
  p_inventory_mode TEXT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_entity_kind TEXT := UPPER(NULLIF(TRIM(COALESCE(p_entity_kind, '')), ''));
  v_entity_id TEXT := NULLIF(TRIM(COALESCE(p_entity_id, '')), '');
  v_inventory_mode TEXT := LOWER(NULLIF(TRIM(COALESCE(p_inventory_mode, '')), ''));
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_entity_kind IS NULL OR v_entity_id IS NULL THEN
    RETURN jsonb_build_object('ok', false, 'error', 'entity_required');
  END IF;
  IF v_inventory_mode NOT IN ('available', 'unavailable', 'linked') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'inventory_mode_invalid');
  END IF;

  IF v_entity_kind = 'SOURCE_PRODUCT' THEN
    UPDATE public.source_products
    SET inventory_mode = v_inventory_mode,
        updated_at = now()
    WHERE id = v_entity_id;
  ELSIF v_entity_kind = 'ADDON_OPTION' THEN
    UPDATE public.addon_options
    SET inventory_mode = v_inventory_mode
    WHERE id = v_entity_id;
  ELSE
    RETURN jsonb_build_object('ok', false, 'error', 'entity_kind_invalid');
  END IF;

  RETURN jsonb_build_object('ok', true, 'updated', FOUND);
END;
$$;

CREATE OR REPLACE FUNCTION public.api_inventory_replace_entity_links(
  p_entity_kind TEXT,
  p_entity_id TEXT,
  p_links JSONB DEFAULT '[]'::jsonb
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_entity_kind TEXT := UPPER(NULLIF(TRIM(COALESCE(p_entity_kind, '')), ''));
  v_entity_id TEXT := NULLIF(TRIM(COALESCE(p_entity_id, '')), '');
  v_links JSONB := COALESCE(p_links, '[]'::jsonb);
  v_link JSONB;
  v_inventory_item_id TEXT;
  v_consumption_quantity NUMERIC;
  v_has_valid_links BOOLEAN := false;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_entity_kind NOT IN ('SOURCE_PRODUCT', 'ADDON_OPTION') OR v_entity_id IS NULL THEN
    RETURN jsonb_build_object('ok', false, 'error', 'entity_required');
  END IF;

  IF jsonb_typeof(v_links) IS DISTINCT FROM 'array' THEN
    RETURN jsonb_build_object('ok', false, 'error', 'links_invalid');
  END IF;

  DELETE FROM public.inventory_item_links
  WHERE entity_kind = v_entity_kind
    AND entity_id = v_entity_id;

  FOR v_link IN
    SELECT value
    FROM jsonb_array_elements(v_links)
  LOOP
    v_inventory_item_id := NULLIF(TRIM(COALESCE(v_link->>'inventoryItemId', '')), '');
    v_consumption_quantity := GREATEST(COALESCE((v_link->>'consumptionQuantity')::NUMERIC, 0), 0);

    IF v_inventory_item_id IS NULL OR v_consumption_quantity <= 0 THEN
      CONTINUE;
    END IF;

    v_has_valid_links := true;

    INSERT INTO public.inventory_item_links (
      inventory_item_id,
      entity_kind,
      entity_id,
      consumption_quantity,
      updated_at
    )
    VALUES (
      v_inventory_item_id,
      v_entity_kind,
      v_entity_id,
      v_consumption_quantity,
      now()
    )
    ON CONFLICT (inventory_item_id, entity_kind, entity_id)
    DO UPDATE SET
      consumption_quantity = EXCLUDED.consumption_quantity,
      updated_at = now();
  END LOOP;

  IF v_has_valid_links THEN
    IF v_entity_kind = 'SOURCE_PRODUCT' THEN
      UPDATE public.source_products
      SET inventory_mode = 'linked',
          updated_at = now()
      WHERE id = v_entity_id
        AND LOWER(TRIM(COALESCE(inventory_mode, 'available'))) = 'available';
    ELSIF v_entity_kind = 'ADDON_OPTION' THEN
      UPDATE public.addon_options
      SET inventory_mode = 'linked'
      WHERE id = v_entity_id
        AND LOWER(TRIM(COALESCE(inventory_mode, 'available'))) = 'available';
    END IF;
  END IF;

  RETURN jsonb_build_object('ok', true);
END;
$$;

CREATE OR REPLACE FUNCTION public.api_inventory_replace_client_reservations(
  p_scope TEXT,
  p_entries JSONB DEFAULT '[]'::jsonb
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_scope TEXT := UPPER(NULLIF(TRIM(COALESCE(p_scope, '')), ''));
  v_entries JSONB := COALESCE(p_entries, '[]'::jsonb);
  v_reservation_key TEXT := public.current_inventory_reservation_key();
  v_now TIMESTAMPTZ := now();
  v_expires_at TIMESTAMPTZ;
  v_entry JSONB;
  v_scope_ref TEXT;
  v_source_product_id TEXT;
  v_option_id TEXT;
  v_quantity INT;
  v_shortages JSONB;
  v_inserted_count INT := 0;
BEGIN
  IF v_scope NOT IN ('DETAILS', 'CART') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'scope_invalid');
  END IF;

  IF v_reservation_key IS NULL THEN
    RETURN jsonb_build_object('ok', true, 'skipped', true);
  END IF;

  IF jsonb_typeof(v_entries) IS DISTINCT FROM 'array' THEN
    RETURN jsonb_build_object('ok', false, 'error', 'entries_invalid');
  END IF;

  PERFORM public.inventory_cleanup_expired_client_reservations();

  CREATE TEMP TABLE IF NOT EXISTS pg_temp.tmp_client_reservation_needs (
    scope_ref TEXT NOT NULL,
    inventory_item_id TEXT NOT NULL,
    entity_kind TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    required_qty NUMERIC NOT NULL
  ) ON COMMIT DROP;
  TRUNCATE pg_temp.tmp_client_reservation_needs;

  FOR v_entry IN
    SELECT value
    FROM jsonb_array_elements(v_entries)
  LOOP
    v_scope_ref := COALESCE(
      NULLIF(TRIM(COALESCE(v_entry->>'scopeRef', '')), ''),
      NULLIF(TRIM(COALESCE(v_entry->>'scope_ref', '')), '')
    );
    v_source_product_id := COALESCE(
      NULLIF(TRIM(COALESCE(v_entry->>'sourceProductId', '')), ''),
      NULLIF(TRIM(COALESCE(v_entry->>'source_product_id', '')), '')
    );
    v_quantity := GREATEST(
      COALESCE(NULLIF(TRIM(COALESCE(v_entry->>'quantity', '')), '')::INT, 0),
      0
    );

    IF v_source_product_id IS NULL OR v_quantity <= 0 THEN
      CONTINUE;
    END IF;

    v_scope_ref := COALESCE(v_scope_ref, v_source_product_id);

    INSERT INTO pg_temp.tmp_client_reservation_needs (
      scope_ref,
      inventory_item_id,
      entity_kind,
      entity_id,
      required_qty
    )
    SELECT
      v_scope_ref,
      l.inventory_item_id,
      'SOURCE_PRODUCT',
      v_source_product_id,
      l.consumption_quantity * v_quantity
    FROM public.inventory_item_links l
    WHERE l.entity_kind = 'SOURCE_PRODUCT'
      AND l.entity_id = v_source_product_id;

    IF jsonb_typeof(v_entry->'optionIds') = 'array' THEN
      FOR v_option_id IN
        SELECT DISTINCT NULLIF(TRIM(value), '')
        FROM jsonb_array_elements_text(v_entry->'optionIds')
        WHERE NULLIF(TRIM(value), '') IS NOT NULL
      LOOP
        INSERT INTO pg_temp.tmp_client_reservation_needs (
          scope_ref,
          inventory_item_id,
          entity_kind,
          entity_id,
          required_qty
        )
        SELECT
          v_scope_ref,
          l.inventory_item_id,
          'ADDON_OPTION',
          v_option_id,
          l.consumption_quantity * v_quantity
        FROM public.inventory_item_links l
        WHERE l.entity_kind = 'ADDON_OPTION'
          AND l.entity_id = v_option_id;
      END LOOP;
    END IF;
  END LOOP;

  PERFORM 1
  FROM public.inventory_items i
  WHERE EXISTS (
    SELECT 1
    FROM pg_temp.tmp_client_reservation_needs need
    WHERE need.inventory_item_id = i.id
  )
  FOR UPDATE;

  SELECT jsonb_agg(jsonb_build_object(
    'inventoryItemId', shortage.inventory_item_id,
    'requiredQty', shortage.required_qty,
    'availableQty', shortage.available_qty
  ))
  INTO v_shortages
  FROM (
    SELECT
      need.inventory_item_id,
      SUM(need.required_qty) AS required_qty,
      MAX(
        GREATEST(
          COALESCE(i.quantity_on_hand, 0) - COALESCE(
            public.inventory_active_reserved_quantity(
              i.id,
              v_reservation_key,
              v_scope
            ),
            0
          ),
          0
        )
      ) AS available_qty
    FROM pg_temp.tmp_client_reservation_needs need
    JOIN public.inventory_items i
      ON i.id = need.inventory_item_id
    GROUP BY need.inventory_item_id
    HAVING BOOL_OR(COALESCE(i.is_active, false) = false)
       OR MAX(
            GREATEST(
              COALESCE(i.quantity_on_hand, 0) - COALESCE(
                public.inventory_active_reserved_quantity(
                  i.id,
                  v_reservation_key,
                  v_scope
                ),
                0
              ),
              0
            )
          ) < SUM(need.required_qty)
  ) shortage;

  IF v_shortages IS NOT NULL THEN
    RETURN jsonb_build_object(
      'ok', false,
      'code', 'inventory_out_of_stock',
      'shortages', v_shortages
    );
  END IF;

  DELETE FROM public.inventory_client_reservations
  WHERE reservation_key = v_reservation_key
    AND scope = v_scope;

  v_expires_at := v_now + CASE
    WHEN v_scope = 'DETAILS' THEN make_interval(mins => 2)
    ELSE make_interval(mins => 15)
  END;

  INSERT INTO public.inventory_client_reservations (
    reservation_key,
    scope,
    scope_ref,
    inventory_item_id,
    entity_kind,
    entity_id,
    quantity,
    expires_at,
    created_at,
    updated_at
  )
  SELECT
    v_reservation_key,
    v_scope,
    need.scope_ref,
    need.inventory_item_id,
    need.entity_kind,
    need.entity_id,
    SUM(need.required_qty) AS quantity,
    v_expires_at,
    v_now,
    v_now
  FROM pg_temp.tmp_client_reservation_needs need
  GROUP BY need.scope_ref, need.inventory_item_id, need.entity_kind, need.entity_id
  ON CONFLICT (reservation_key, scope, scope_ref, inventory_item_id, entity_kind, entity_id)
  DO UPDATE
  SET
    quantity = EXCLUDED.quantity,
    expires_at = EXCLUDED.expires_at,
    updated_at = v_now;

  GET DIAGNOSTICS v_inserted_count = ROW_COUNT;

  INSERT INTO public.inventory_reservation_events (id, updated_at)
  VALUES (1, v_now)
  ON CONFLICT (id) DO UPDATE
  SET updated_at = EXCLUDED.updated_at;

  RETURN jsonb_build_object(
    'ok', true,
    'count', COALESCE(v_inserted_count, 0)
  );
END;
$$;

-- -----------------------------------------------------------------------------
-- Request context helpers (role/user extraction + normalization)
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.api_request_header_value(p_keys TEXT[])
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_headers JSONB := '{}'::jsonb;
  v_keys TEXT[] := ARRAY[]::TEXT[];
  v_val TEXT;
BEGIN
  SELECT COALESCE(array_agg(LOWER(TRIM(k))), ARRAY[]::TEXT[])
  INTO v_keys
  FROM unnest(COALESCE(p_keys, ARRAY[]::TEXT[])) AS k
  WHERE NULLIF(TRIM(k), '') IS NOT NULL;

  IF COALESCE(array_length(v_keys, 1), 0) = 0 THEN
    RETURN NULL;
  END IF;

  BEGIN
    v_headers := COALESCE(NULLIF(current_setting('request.headers', true), '')::jsonb, '{}'::jsonb);
  EXCEPTION WHEN OTHERS THEN
    v_headers := '{}'::jsonb;
  END;

  SELECT NULLIF(TRIM(j.value), '')
  INTO v_val
  FROM jsonb_each_text(v_headers) AS j(key, value)
  WHERE LOWER(j.key) = ANY (v_keys)
    AND NULLIF(TRIM(j.value), '') IS NOT NULL
  LIMIT 1;

  RETURN v_val;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_request_claim_value(p_keys TEXT[])
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_claims JSONB := '{}'::jsonb;
  v_keys TEXT[] := ARRAY[]::TEXT[];
  v_val TEXT;
BEGIN
  SELECT COALESCE(array_agg(LOWER(TRIM(k))), ARRAY[]::TEXT[])
  INTO v_keys
  FROM unnest(COALESCE(p_keys, ARRAY[]::TEXT[])) AS k
  WHERE NULLIF(TRIM(k), '') IS NOT NULL;

  IF COALESCE(array_length(v_keys, 1), 0) = 0 THEN
    RETURN NULL;
  END IF;

  BEGIN
    v_claims := COALESCE(NULLIF(current_setting('request.jwt.claims', true), '')::jsonb, '{}'::jsonb);
  EXCEPTION WHEN OTHERS THEN
    v_claims := '{}'::jsonb;
  END;

  SELECT NULLIF(TRIM(j.value), '')
  INTO v_val
  FROM jsonb_each_text(v_claims) AS j(key, value)
  WHERE LOWER(j.key) = ANY (v_keys)
    AND NULLIF(TRIM(j.value), '') IS NOT NULL
  LIMIT 1;

  RETURN v_val;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_request_context_value(p_keys TEXT[])
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_val TEXT;
BEGIN
  v_val := public.api_request_header_value(p_keys);
  IF v_val IS NOT NULL THEN
    RETURN v_val;
  END IF;
  RETURN public.api_request_claim_value(p_keys);
END;
$$;

CREATE OR REPLACE FUNCTION public.normalize_app_role(p_role TEXT)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v TEXT := UPPER(TRIM(COALESCE(p_role, '')));
BEGIN
  v := REPLACE(v, '-', '_');
  v := REPLACE(v, ' ', '_');
  v := REGEXP_REPLACE(v, '_+', '_', 'g');

  IF v IN ('ADMINPOS', 'ADMIN_POS') THEN
    RETURN 'ADMIN_POS';
  ELSIF v IN ('ADMIN') THEN
    RETURN 'ADMIN';
  ELSIF v IN ('CUSTOMER', 'CLIENT') THEN
    RETURN 'CUSTOMER';
  ELSIF v IN ('DRIVER', 'DELIVERY') THEN
    RETURN 'DRIVER';
  ELSIF v IN ('CASHIER') THEN
    RETURN 'CASHIER';
  ELSIF v IN ('SERVICE_ROLE', 'SERVICEROLE') THEN
    RETURN 'SERVICE_ROLE';
  END IF;

  RETURN v;
END;
$$;

CREATE OR REPLACE FUNCTION public.account_role_supports_display_name_login(
  p_role TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_role TEXT := public.normalize_app_role(NULLIF(TRIM(COALESCE(p_role, '')), ''));
BEGIN
  RETURN v_role IN ('ADMIN_POS', 'ADMIN', 'SERVICE_ROLE', 'CASHIER', 'DRIVER');
END;
$$;

CREATE OR REPLACE FUNCTION public.account_identifier_match_rank(
  p_identifier TEXT,
  p_user_id TEXT,
  p_phone TEXT,
  p_email TEXT,
  p_username TEXT,
  p_display_name TEXT,
  p_role TEXT,
  p_allow_staff_display_name BOOLEAN DEFAULT true
)
RETURNS INT
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_identifier TEXT := NULLIF(TRIM(COALESCE(p_identifier, '')), '');
  v_identifier_lower TEXT := LOWER(COALESCE(v_identifier, ''));
BEGIN
  IF v_identifier IS NULL THEN
    RETURN 0;
  END IF;

  IF NULLIF(TRIM(COALESCE(p_user_id, '')), '') = v_identifier THEN
    RETURN 1;
  END IF;
  IF NULLIF(TRIM(COALESCE(p_phone, '')), '') = v_identifier THEN
    RETURN 2;
  END IF;
  IF NULLIF(TRIM(COALESCE(p_username, '')), '') = v_identifier THEN
    RETURN 3;
  END IF;
  IF NULLIF(TRIM(LOWER(COALESCE(p_email, ''))), '') = v_identifier_lower THEN
    RETURN 4;
  END IF;
  IF COALESCE(p_allow_staff_display_name, true) = true
     AND public.account_role_supports_display_name_login(p_role)
     AND NULLIF(TRIM(LOWER(COALESCE(p_display_name, ''))), '') = v_identifier_lower THEN
    RETURN 5;
  END IF;

  RETURN 0;
END;
$$;

CREATE OR REPLACE FUNCTION public.account_identifier_matches(
  p_identifier TEXT,
  p_user_id TEXT,
  p_phone TEXT,
  p_email TEXT,
  p_username TEXT,
  p_display_name TEXT,
  p_role TEXT,
  p_allow_staff_display_name BOOLEAN DEFAULT true
)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
BEGIN
  RETURN public.account_identifier_match_rank(
    p_identifier := p_identifier,
    p_user_id := p_user_id,
    p_phone := p_phone,
    p_email := p_email,
    p_username := p_username,
    p_display_name := p_display_name,
    p_role := p_role,
    p_allow_staff_display_name := p_allow_staff_display_name
  ) > 0;
END;
$$;

CREATE OR REPLACE FUNCTION public.current_app_session_token()
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  RETURN NULLIF(TRIM(COALESCE(
    public.api_request_header_value(
      ARRAY['x-fale7-session', 'x_fale7_session', 'session_token', 'session-token']
    ),
    ''
  )), '');
END;
$$;

CREATE OR REPLACE FUNCTION public.current_app_role()
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_claim_role TEXT := public.normalize_app_role(
    public.api_request_claim_value(ARRAY['role'])
  );
  v_user_id TEXT := public.current_app_user_id();
  v_role TEXT;
BEGIN
  IF v_claim_role = 'SERVICE_ROLE' THEN
    RETURN 'SERVICE_ROLE';
  END IF;

  IF v_user_id IS NULL THEN
    RETURN '';
  END IF;

  SELECT public.normalize_app_role(COALESCE(u.role, ''))
  INTO v_role
  FROM public.users u
  WHERE u.id = v_user_id
    AND COALESCE(u.is_active, true) = true
  LIMIT 1;

  RETURN COALESCE(v_role, '');
END;
$$;

CREATE OR REPLACE FUNCTION public.current_app_user_id()
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_claim_user_id TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['sub', 'id']),
    ''
  )), '');
  v_session_token TEXT := public.current_app_session_token();
  v_resolved_user_id TEXT;
BEGIN
  IF v_claim_user_id IS NOT NULL THEN
    SELECT u.id
    INTO v_resolved_user_id
    FROM public.users u
    WHERE u.id = v_claim_user_id
      AND COALESCE(u.is_active, true) = true
    LIMIT 1;

    IF v_resolved_user_id IS NOT NULL THEN
      RETURN v_resolved_user_id;
    END IF;
  END IF;

  IF v_session_token IS NOT NULL THEN
    SELECT s.user_id
    INTO v_resolved_user_id
    FROM public.app_role_sessions s
    JOIN public.users u ON u.id = s.user_id
    WHERE s.token_hash = public.hash_session_token(v_session_token)
      AND s.revoked_at IS NULL
      AND s.expires_at > now()
      AND COALESCE(u.is_active, true) = true
    ORDER BY s.expires_at DESC, s.created_at DESC
    LIMIT 1;

    IF v_resolved_user_id IS NOT NULL THEN
      RETURN v_resolved_user_id;
    END IF;
  END IF;

  RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.current_app_phone()
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_user_id TEXT := public.current_app_user_id();
  v_phone TEXT;
BEGIN
  IF v_user_id IS NULL THEN
    RETURN NULL;
  END IF;

  SELECT NULLIF(TRIM(COALESCE(u.phone, '')), '')
  INTO v_phone
  FROM public.users u
  WHERE u.id = v_user_id
  LIMIT 1;

  RETURN v_phone;
END;
$$;

CREATE OR REPLACE FUNCTION public.current_request_claim_linked_user_id()
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public, auth
AS $$
DECLARE
  v_claim_auth_user_id TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['sub', 'id']),
    ''
  )), '');
  v_claim_email TEXT := LOWER(NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['email']),
    ''
  )), ''));
  v_claim_phone TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['phone']),
    ''
  )), '');
  v_auth_email TEXT := NULL;
  v_auth_phone TEXT := NULL;
  v_auth_meta JSONB := '{}'::jsonb;
  v_candidate TEXT;
  v_resolved_user_id TEXT;
  v_candidates TEXT[];
BEGIN
  IF v_claim_auth_user_id IS NOT NULL THEN
    BEGIN
      SELECT
        LOWER(NULLIF(TRIM(COALESCE(au.email, '')), '')),
        NULLIF(TRIM(COALESCE(au.phone, '')), ''),
        COALESCE(au.raw_user_meta_data, '{}'::jsonb)
      INTO
        v_auth_email,
        v_auth_phone,
        v_auth_meta
      FROM auth.users au
      WHERE au.id::TEXT = v_claim_auth_user_id
      LIMIT 1;
    EXCEPTION WHEN OTHERS THEN
      v_auth_email := NULL;
      v_auth_phone := NULL;
      v_auth_meta := '{}'::jsonb;
    END;
  END IF;

  v_candidates := ARRAY[
    v_claim_auth_user_id,
    v_claim_email,
    v_claim_phone,
    v_auth_email,
    v_auth_phone,
    LOWER(NULLIF(TRIM(COALESCE(
      v_auth_meta->>'email',
      v_auth_meta->>'fale7_email',
      ''
    )), '')),
    NULLIF(TRIM(COALESCE(
      v_auth_meta->>'phone',
      v_auth_meta->>'fale7_phone',
      ''
    )), ''),
    NULLIF(TRIM(COALESCE(v_auth_meta->>'username', '')), '')
  ];

  FOREACH v_candidate IN ARRAY v_candidates
  LOOP
    v_candidate := NULLIF(TRIM(COALESCE(v_candidate, '')), '');
    IF v_candidate IS NULL THEN
      CONTINUE;
    END IF;

    v_resolved_user_id := public.resolve_customer_address_owner_user_id(v_candidate);
    IF v_resolved_user_id IS NOT NULL THEN
      RETURN v_resolved_user_id;
    END IF;
  END LOOP;

  RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_register_push_token(
  p_user_id TEXT,
  p_device_id TEXT,
  p_platform TEXT DEFAULT 'android',
  p_fcm_token TEXT DEFAULT NULL,
  p_app_version TEXT DEFAULT NULL
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_requested_user_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  v_request_user_id TEXT := public.current_app_user_id();
  v_claim_linked_user_id TEXT := public.current_request_claim_linked_user_id();
  v_actor_role TEXT := public.current_app_role();
  v_user_id TEXT;
  v_device_id TEXT := NULLIF(TRIM(COALESCE(p_device_id, '')), '');
  v_platform TEXT := COALESCE(NULLIF(TRIM(COALESCE(p_platform, '')), ''), 'android');
  v_fcm_token TEXT := NULLIF(TRIM(COALESCE(p_fcm_token, '')), '');
  v_app_version TEXT := NULLIF(TRIM(COALESCE(p_app_version, '')), '');
BEGIN
  IF v_requested_user_id IS NULL THEN
    RETURN json_build_object('ok', false, 'error', 'user_id_required');
  END IF;
  IF v_device_id IS NULL THEN
    RETURN json_build_object('ok', false, 'error', 'device_id_required');
  END IF;
  IF v_fcm_token IS NULL THEN
    RETURN json_build_object('ok', false, 'error', 'fcm_token_required');
  END IF;

  IF v_actor_role IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    v_user_id := v_requested_user_id;
  ELSE
    v_user_id := COALESCE(v_request_user_id, v_claim_linked_user_id);
    IF v_user_id IS NULL THEN
      RETURN json_build_object('ok', false, 'error', 'forbidden');
    END IF;
    IF v_requested_user_id <> v_user_id THEN
      RETURN json_build_object('ok', false, 'error', 'forbidden');
    END IF;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM public.users u WHERE u.id = v_user_id) THEN
    RETURN json_build_object('ok', false, 'error', 'user_not_found');
  END IF;

  DELETE FROM public.user_push_tokens
  WHERE (
      device_id = v_device_id
      OR fcm_token = v_fcm_token
    )
    AND (
      user_id <> v_user_id
      OR COALESCE(device_id, '') <> v_device_id
    );

  INSERT INTO public.user_push_tokens (
    user_id, device_id, platform, fcm_token, app_version
  )
  VALUES (
    v_user_id, v_device_id, v_platform, v_fcm_token, v_app_version
  )
  ON CONFLICT (user_id, device_id)
  DO UPDATE SET
    platform = EXCLUDED.platform,
    fcm_token = EXCLUDED.fcm_token,
    app_version = EXCLUDED.app_version,
    updated_at = now();

  RETURN json_build_object('ok', true, 'userId', v_user_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.user_auth_provider_key(
  p_user_id TEXT
)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public, auth
AS $$
DECLARE
  v_user_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  v_providers TEXT[] := ARRAY[]::TEXT[];
  v_role TEXT := '';
BEGIN
  IF v_user_id IS NULL THEN
    RETURN 'unknown';
  END IF;

  BEGIN
    SELECT COALESCE(array_agg(DISTINCT LOWER(COALESCE(i.provider, ''))), ARRAY[]::TEXT[])
    INTO v_providers
    FROM auth.identities i
    WHERE i.user_id::TEXT = v_user_id
      AND NULLIF(TRIM(COALESCE(i.provider, '')), '') IS NOT NULL;
  EXCEPTION WHEN OTHERS THEN
    v_providers := ARRAY[]::TEXT[];
  END;

  SELECT UPPER(COALESCE(u.role, ''))
  INTO v_role
  FROM public.users u
  WHERE u.id = v_user_id
  LIMIT 1;

  IF 'google' = ANY (v_providers) THEN
    RETURN 'google_auth';
  END IF;
  IF 'email' = ANY (v_providers) THEN
    RETURN 'email_password';
  END IF;
  IF v_role IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'DRIVER') THEN
    RETURN 'manual_credentials';
  END IF;

  RETURN 'unknown';
END;
$$;

CREATE OR REPLACE FUNCTION public.user_auth_provider_label(
  p_provider_key TEXT
)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
SET search_path = public
AS $$
DECLARE
  v_key TEXT := LOWER(NULLIF(TRIM(COALESCE(p_provider_key, '')), ''));
BEGIN
  IF v_key = 'google_auth' THEN
    RETURN 'Google Auth';
  END IF;
  IF v_key = 'email_password' THEN
    RETURN 'Email/Password';
  END IF;
  IF v_key = 'manual_credentials' THEN
    RETURN 'Manual Credentials';
  END IF;
  RETURN 'Unknown';
END;
$$;

CREATE OR REPLACE FUNCTION public.api_admin_search_maintenance_candidates(
  p_identifier TEXT,
  p_limit INT DEFAULT 20
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, auth
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_identifier TEXT := NULLIF(TRIM(COALESCE(p_identifier, '')), '');
  v_identifier_lower TEXT := LOWER(COALESCE(v_identifier, ''));
  v_identifier_digits TEXT := regexp_replace(COALESCE(v_identifier, ''), '\D', '', 'g');
  v_limit INT := GREATEST(LEAST(COALESCE(p_limit, 20), 50), 1);
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN '[]'::json;
  END IF;
  IF v_identifier IS NULL THEN
    RETURN '[]'::json;
  END IF;

  RETURN COALESCE(
    (
      SELECT json_agg(candidate_row.payload ORDER BY candidate_row.rank_score, candidate_row.updated_at DESC NULLS LAST, candidate_row.created_at DESC NULLS LAST)
      FROM (
        SELECT
          u.updated_at,
          u.created_at,
          CASE
            WHEN v_identifier_lower LIKE '%@%'
                 AND LOWER(COALESCE(u.email, '')) = v_identifier_lower THEN 1
            WHEN v_identifier_digits <> ''
                 AND regexp_replace(COALESCE(u.phone, ''), '\D', '', 'g') = v_identifier_digits THEN 2
            WHEN v_identifier_lower LIKE '%@%'
                 AND LOWER(COALESCE(u.email, '')) LIKE v_identifier_lower || '%' THEN 3
            WHEN v_identifier_digits <> ''
                 AND regexp_replace(COALESCE(u.phone, ''), '\D', '', 'g') LIKE v_identifier_digits || '%' THEN 4
            ELSE 9
          END AS rank_score,
          json_build_object(
            'userId', u.id,
            'displayName', COALESCE(NULLIF(TRIM(COALESCE(u.display_name, '')), ''), NULLIF(TRIM(COALESCE(u.username, '')), ''), 'بدون اسم'),
            'phone', NULLIF(TRIM(COALESCE(u.phone, '')), ''),
            'email', NULLIF(TRIM(COALESCE(u.email, '')), ''),
            'role', UPPER(COALESCE(u.role, 'CUSTOMER')),
            'providerKey', public.user_auth_provider_key(u.id),
            'providerLabel', public.user_auth_provider_label(public.user_auth_provider_key(u.id)),
            'latestDeviceId', tok.device_id,
            'latestDevicePlatform', tok.platform,
            'latestDeviceAppVersion', tok.app_version,
            'latestDeviceUpdatedAt', tok.updated_at
          ) AS payload
        FROM public.users u
        LEFT JOIN LATERAL (
          SELECT
            t.device_id,
            t.platform,
            t.app_version,
            t.updated_at
          FROM public.user_push_tokens t
          WHERE t.user_id = u.id
            AND NULLIF(TRIM(COALESCE(t.device_id, '')), '') IS NOT NULL
          ORDER BY t.updated_at DESC, t.id DESC
          LIMIT 1
        ) tok ON true
        WHERE (
          (
            v_identifier_lower LIKE '%@%'
            AND (
              LOWER(COALESCE(u.email, '')) = v_identifier_lower
              OR LOWER(COALESCE(u.email, '')) LIKE '%' || v_identifier_lower || '%'
            )
          )
          OR (
            v_identifier_digits <> ''
            AND regexp_replace(COALESCE(u.phone, ''), '\D', '', 'g') LIKE '%' || v_identifier_digits || '%'
          )
        )
        ORDER BY rank_score, u.updated_at DESC NULLS LAST, u.created_at DESC NULLS LAST
        LIMIT v_limit
      ) candidate_row
    ),
    '[]'::json
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_admin_list_maintenance_exceptions()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, auth
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN '[]'::json;
  END IF;

  DELETE FROM public.maintenance_mode_exceptions
  WHERE scope = 'ACCOUNT_ONLY'
    AND user_id IS NULL;

  RETURN COALESCE(
    (
      SELECT json_agg(json_build_object(
        'id', e.id,
        'scope', e.scope,
        'userId', e.user_id,
        'deviceId', e.device_id,
        'devicePlatform', e.device_platform,
        'deviceAppVersion', e.device_app_version,
        'displayName', COALESCE(NULLIF(TRIM(COALESCE(u.display_name, '')), ''), NULLIF(TRIM(COALESCE(u.username, '')), ''), 'بدون اسم'),
        'phone', NULLIF(TRIM(COALESCE(u.phone, '')), ''),
        'email', NULLIF(TRIM(COALESCE(u.email, '')), ''),
        'role', UPPER(COALESCE(u.role, 'CUSTOMER')),
        'providerKey', CASE
          WHEN e.user_id IS NULL THEN 'unknown'
          ELSE public.user_auth_provider_key(e.user_id)
        END,
        'providerLabel', CASE
          WHEN e.user_id IS NULL THEN public.user_auth_provider_label('unknown')
          ELSE public.user_auth_provider_label(public.user_auth_provider_key(e.user_id))
        END,
        'createdAt', e.created_at,
        'updatedAt', e.updated_at,
        'createdBy', e.created_by,
        'updatedBy', e.updated_by
      ) ORDER BY e.created_at DESC, e.id DESC)
      FROM public.maintenance_mode_exceptions e
      LEFT JOIN public.users u
        ON u.id = e.user_id
    ),
    '[]'::json
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_admin_upsert_maintenance_exception(
  p_scope TEXT,
  p_user_id TEXT DEFAULT NULL,
  p_device_id TEXT DEFAULT NULL,
  p_device_platform TEXT DEFAULT NULL,
  p_device_app_version TEXT DEFAULT NULL
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_actor_user_id TEXT := public.current_app_user_id();
  v_scope TEXT := UPPER(NULLIF(TRIM(COALESCE(p_scope, '')), ''));
  v_user_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  v_device_id TEXT := NULLIF(TRIM(COALESCE(p_device_id, '')), '');
  v_device_platform TEXT := NULLIF(TRIM(COALESCE(p_device_platform, '')), '');
  v_device_app_version TEXT := NULLIF(TRIM(COALESCE(p_device_app_version, '')), '');
  v_exception_id BIGINT;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_scope NOT IN ('ACCOUNT_ONLY', 'DEVICE_ONLY') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'scope_invalid');
  END IF;

  IF v_scope = 'ACCOUNT_ONLY' THEN
    IF v_user_id IS NULL THEN
      RETURN jsonb_build_object('ok', false, 'error', 'user_id_required');
    END IF;

    UPDATE public.maintenance_mode_exceptions
    SET
      updated_at = now(),
      updated_by = v_actor_user_id,
      user_id = v_user_id
    WHERE scope = 'ACCOUNT_ONLY'
      AND user_id = v_user_id
    RETURNING id INTO v_exception_id;

    IF v_exception_id IS NULL THEN
      INSERT INTO public.maintenance_mode_exceptions (
        scope,
        user_id,
        created_by,
        updated_by
      )
      VALUES (
        'ACCOUNT_ONLY',
        v_user_id,
        v_actor_user_id,
        v_actor_user_id
      )
      RETURNING id INTO v_exception_id;
    END IF;
  ELSE
    IF v_device_id IS NULL THEN
      RETURN jsonb_build_object('ok', false, 'error', 'device_id_required');
    END IF;

    UPDATE public.maintenance_mode_exceptions
    SET
      user_id = COALESCE(v_user_id, user_id),
      device_platform = COALESCE(v_device_platform, device_platform),
      device_app_version = COALESCE(v_device_app_version, device_app_version),
      updated_at = now(),
      updated_by = v_actor_user_id
    WHERE scope = 'DEVICE_ONLY'
      AND device_id = v_device_id
    RETURNING id INTO v_exception_id;

    IF v_exception_id IS NULL THEN
      INSERT INTO public.maintenance_mode_exceptions (
        scope,
        user_id,
        device_id,
        device_platform,
        device_app_version,
        created_by,
        updated_by
      )
      VALUES (
        'DEVICE_ONLY',
        v_user_id,
        v_device_id,
        v_device_platform,
        v_device_app_version,
        v_actor_user_id,
        v_actor_user_id
      )
      RETURNING id INTO v_exception_id;
    END IF;
  END IF;

  RETURN jsonb_build_object('ok', true, 'exceptionId', v_exception_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.api_admin_delete_maintenance_exception(
  p_exception_id BIGINT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  DELETE FROM public.maintenance_mode_exceptions
  WHERE id = p_exception_id;

  RETURN jsonb_build_object('ok', true, 'deleted', FOUND);
END;
$$;

CREATE OR REPLACE FUNCTION public.api_is_maintenance_bypassed(
  p_device_id TEXT DEFAULT NULL
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_user_id TEXT := public.current_app_user_id();
  v_device_id TEXT := NULLIF(TRIM(COALESCE(p_device_id, '')), '');
  v_reason TEXT;
  v_allow_guest BOOLEAN := false;
BEGIN
  SELECT COALESCE(s.maintenance_allow_guest, false)
  INTO v_allow_guest
  FROM public.app_settings s
  WHERE s.id = 1
  LIMIT 1;

  IF v_user_id IS NOT NULL
     AND EXISTS (
       SELECT 1
       FROM public.maintenance_mode_exceptions e
       WHERE e.scope = 'ACCOUNT_ONLY'
         AND e.user_id = v_user_id
     ) THEN
    v_reason := 'ACCOUNT_ONLY';
  ELSIF v_user_id IS NULL
     AND COALESCE(v_allow_guest, false) = true THEN
    v_reason := 'GUEST';
  ELSIF v_device_id IS NOT NULL
     AND EXISTS (
       SELECT 1
       FROM public.maintenance_mode_exceptions e
       WHERE e.scope = 'DEVICE_ONLY'
         AND e.device_id = v_device_id
     ) THEN
    v_reason := 'DEVICE_ONLY';
  ELSE
    v_reason := NULL;
  END IF;

  RETURN json_build_object(
    'ok', true,
    'allowed', v_reason IS NOT NULL,
    'reason', CASE
      WHEN v_reason IS NULL THEN NULL
      WHEN v_reason = 'GUEST' THEN 'guest'
      ELSE LOWER(v_reason)
    END
  );
END;
$$;

-- جلب الـ Store الكامل للأدمن (توافق رجعي: drivers و cashiers من users)
CREATE OR REPLACE FUNCTION public.api_admin_get_store(p_store_key TEXT DEFAULT 'fale7-main')
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  j JSON;
  drivers_json JSON;
  cashiers_json JSON;
  v_actor_role TEXT := public.current_app_role();
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'forbidden');
  END IF;

  SELECT json_agg(json_build_object(
    'id', u.id, 'name', COALESCE(u.display_name, u.username, ''), 'phone', COALESCE(u.phone, ''),
    'password', '', 'status', CASE WHEN u.is_active THEN 'active' ELSE 'inactive' END
  )) INTO drivers_json FROM users u WHERE u.role = 'DRIVER';
  SELECT json_agg(json_build_object(
    'id', u.id, 'name', COALESCE(u.display_name, u.username, ''), 'password', '', 'pin', ''
  )) INTO cashiers_json FROM users u WHERE u.role = 'CASHIER';

  SELECT json_build_object(
    'categories', COALESCE((SELECT json_agg(json_build_object('id', id, 'name', name, 'order', sort_order) ORDER BY sort_order, id) FROM app_categories), '[]'::json),
    'products', COALESCE((SELECT json_agg(json_build_object(
      'id', p.id, 'catId', p.category_id, 'name', p.name, 'desc', p.description, 'price', (SELECT sp.price FROM source_products sp WHERE sp.id = p.source_product_id),
      'img', p.image_url, 'imgLight', p.image_url_light, 'imgDark', p.image_url_dark, 'enabled', p.enabled, 'order', p.sort_order,
      'inventoryMode', public.inventory_entity_mode('SOURCE_PRODUCT', p.source_product_id)
    ) ORDER BY p.sort_order, p.id) FROM app_products p), '[]'::json),
    'addonGroups', COALESCE((SELECT json_agg(json_build_object(
      'id', g.id, 'name', g.name, 'img', g.image_url, 'kind', g.kind, 'mode', g.mode,
      'required', g.required, 'max', g.max_count, 'enabled', g.enabled, 'order', g.sort_order,
      'options', COALESCE((SELECT json_agg(json_build_object('id', o.id, 'name', o.name, 'price', o.price, 'enabled', o.enabled, 'img', o.image_url, 'imgLight', o.image_url_light, 'imgDark', o.image_url_dark, 'order', o.sort_order, 'inventoryMode', public.inventory_entity_mode('ADDON_OPTION', o.id)) ORDER BY o.sort_order, o.id) FROM addon_options o WHERE o.group_id = g.id), '[]'::json)
    ) ORDER BY g.sort_order, g.id) FROM addon_groups g), '[]'::json),
    'categoryDefaults', COALESCE((
      SELECT json_object_agg(cd.category_id, cd.group_ids)
      FROM (
        SELECT category_id, json_agg(group_id ORDER BY sort_order) AS group_ids
        FROM app_category_default_groups
        GROUP BY category_id
      ) cd
    ), '{}'::json),
    'productOptions', COALESCE((SELECT json_object_agg(product_id, json_build_object('linkedGroups', (SELECT json_object_agg(group_id, json_build_object('enabled', enabled, 'mode', COALESCE(mode_override, 'multi'), 'required', COALESCE(required_override, false), 'max', COALESCE(max_override, 0))) FROM app_product_group_overrides o2 WHERE o2.product_id = o.product_id))) FROM (SELECT DISTINCT product_id FROM app_product_group_overrides) o), '{}'::json),
    'hoods', COALESCE((SELECT json_agg(json_build_object('id', id, 'name', name, 'fee', fee)) FROM hoods), '[]'::json),
    'workHours', (SELECT json_build_object('openAtMinutes', open_at_minutes, 'closeAtMinutes', close_at_minutes) FROM app_work_hours LIMIT 1),
    'coupons', COALESCE((SELECT json_agg(json_build_object('id', id, 'code', code, 'title', title, 'enabled', enabled, 'freeDelivery', free_delivery, 'discountAmount', discount_amount, 'discountPercent', discount_percent, 'minOrderAmount', min_order_amount, 'perUserLimit', per_user_limit, 'maxCustomers', max_customers, 'order', sort_order) ORDER BY sort_order, id) FROM coupons), '[]'::json),
    'drivers', COALESCE(drivers_json, '[]'::json),
    'cashiers', COALESCE(cashiers_json, '[]'::json),
    'posCategories', COALESCE((SELECT json_agg(json_build_object('id', id, 'name', name, 'order', sort_order) ORDER BY sort_order, id) FROM pos_categories), '[]'::json),
    'posProducts', COALESCE((SELECT json_agg(json_build_object(
      'id', p.id, 'posCategoryId', p.pos_category_id, 'sourceProductId', p.source_product_id, 'name', p.name, 'posDisplayName', p.pos_display_name, 'order', p.sort_order, 'enabled', p.enabled
    ) ORDER BY p.sort_order, p.id) FROM pos_products p), '[]'::json),
    'inventoryItems', COALESCE((SELECT json_agg(json_build_object(
      'id', i.id,
      'name', i.name,
      'measureType', i.measure_type,
      'baseUnit', i.base_unit,
      'displayUnit', i.display_unit,
      'quantityOnHand', i.quantity_on_hand,
      'lowStockThreshold', i.low_stock_threshold,
      'active', i.is_active
    ) ORDER BY i.name, i.id) FROM inventory_items i), '[]'::json),
    'inventoryLinks', COALESCE((SELECT json_agg(json_build_object(
      'inventoryItemId', l.inventory_item_id,
      'entityKind', l.entity_kind,
      'entityId', l.entity_id,
      'consumptionQuantity', l.consumption_quantity
    ) ORDER BY l.inventory_item_id, l.entity_kind, l.entity_id) FROM inventory_item_links l), '[]'::json)
  ) INTO j;
  RETURN j;
END;
$$;

-- مزامنة الـ Store إلى الجداول فقط (جداول + فانكشن سوبا بيز؛ لا replace_store)
CREATE OR REPLACE FUNCTION public.api_admin_sync_store(p_store JSONB DEFAULT '{}')
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  cat JSONB;
  prd JSONB;
  grp JSONB;
  opt JSONB;
  pos_cat JSONB;
  pos_prd JSONB;
  h JSONB;
  cpn JSONB;
  drv JSONB;
  cash JSONB;
  drv_password TEXT;
  cash_password TEXT;
  cash_pin TEXT;
  sid TEXT;
  cat_id TEXT;
  gid TEXT;
  pid TEXT;
  wh JSONB;
  cat_defaults JSONB;
  po JSONB;
  linked JSONB;
  ord INT;
  opt_ord INT;
  v_actor_role TEXT := public.current_app_role();
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  ord := 0;
  opt_ord := 0;
  -- 1) source_products من products (id = product.id, price = product.price)
  FOR prd IN SELECT * FROM jsonb_array_elements(p_store->'products')
  LOOP
    sid := prd->>'id';
    IF sid IS NOT NULL AND sid <> '' THEN
      INSERT INTO source_products (id, price, inventory_mode, updated_at)
      VALUES (
        sid,
        COALESCE((prd->>'price')::numeric, 0),
        COALESCE(NULLIF(TRIM(COALESCE(prd->>'inventoryMode', '')), ''), 'available'),
        now()
      )
      ON CONFLICT (id) DO UPDATE
      SET
        price = EXCLUDED.price,
        inventory_mode = EXCLUDED.inventory_mode,
        updated_at = now();
    END IF;
  END LOOP;
  -- من posProducts نضيف source_product_id إن لم يكن من products
  FOR pos_prd IN SELECT * FROM jsonb_array_elements(p_store->'posProducts')
  LOOP
    sid := pos_prd->>'sourceProductId';
    IF sid IS NOT NULL AND sid <> '' AND NOT EXISTS (SELECT 1 FROM source_products WHERE id = sid) THEN
      INSERT INTO source_products (id, price, updated_at) VALUES (sid, 0, now()) ON CONFLICT (id) DO NOTHING;
    END IF;
  END LOOP;

  -- 2) حذف التبعيات ثم إعادة الإدراج
  DELETE FROM app_product_group_overrides;
  DELETE FROM app_category_default_groups;
  DELETE FROM addon_options;
  DELETE FROM app_products;
  DELETE FROM app_categories;
  DELETE FROM pos_products;
  DELETE FROM pos_categories;
  DELETE FROM addon_groups;
  DELETE FROM hoods;
  DELETE FROM coupons;

  FOR cat IN SELECT * FROM jsonb_array_elements(p_store->'categories')
  LOOP
    INSERT INTO app_categories (id, name, sort_order) VALUES (
      cat->>'id', COALESCE(cat->>'name',''), COALESCE((cat->>'order')::int, 0)
    );
  END LOOP;

  FOR prd IN SELECT * FROM jsonb_array_elements(p_store->'products')
  LOOP
    sid := prd->>'id';
    IF sid IS NULL OR sid = '' THEN CONTINUE; END IF;
    INSERT INTO app_products (id, category_id, source_product_id, name, description, image_url, image_url_light, image_url_dark, enabled, sort_order)
    VALUES (
      sid, COALESCE(prd->>'catId',''), sid,
      COALESCE(prd->>'name',''), COALESCE(prd->>'desc',''),
      COALESCE(prd->>'img',''), COALESCE(prd->>'imgLight',''), COALESCE(prd->>'imgDark',''),
      COALESCE((prd->>'enabled')::int, 1), ord
    );
    ord := ord + 1;
  END LOOP;

  FOR pos_cat IN SELECT * FROM jsonb_array_elements(p_store->'posCategories')
  LOOP
    INSERT INTO pos_categories (id, name, sort_order) VALUES (
      pos_cat->>'id', COALESCE(pos_cat->>'name',''), COALESCE((pos_cat->>'order')::int, 0)
    );
  END LOOP;

  FOR pos_prd IN SELECT * FROM jsonb_array_elements(p_store->'posProducts')
  LOOP
    pid := pos_prd->>'id';
    IF pid IS NULL OR pid = '' THEN CONTINUE; END IF;
    INSERT INTO pos_products (id, pos_category_id, source_product_id, name, pos_display_name, sort_order, enabled)
    VALUES (
      pid, COALESCE(pos_prd->>'posCategoryId',''), COALESCE(pos_prd->>'sourceProductId', pid),
      COALESCE(pos_prd->>'name',''), COALESCE(pos_prd->>'posDisplayName',''),
      COALESCE((pos_prd->>'order')::int, 0), COALESCE((pos_prd->>'enabled')::int, 1)
    );
  END LOOP;

  FOR grp IN SELECT * FROM jsonb_array_elements(p_store->'addonGroups')
  LOOP
    gid := grp->>'id';
    IF gid IS NULL OR gid = '' THEN CONTINUE; END IF;
    INSERT INTO addon_groups (id, name, image_url, kind, mode, required, max_count, enabled, sort_order)
    VALUES (gid, COALESCE(grp->>'name',''), COALESCE(grp->>'img',''), COALESCE(grp->>'kind','normal'),
      COALESCE(grp->>'mode','multi'), COALESCE((grp->>'required')::boolean, false),
      COALESCE((grp->>'max')::int, 0), COALESCE((grp->>'enabled')::boolean, true), COALESCE((grp->>'order')::int, 0));
    opt_ord := 0;
    FOR opt IN SELECT * FROM jsonb_array_elements(grp->'options')
    LOOP
      INSERT INTO addon_options (id, group_id, name, price, inventory_mode, enabled, image_url, image_url_light, image_url_dark, sort_order)
      VALUES (opt->>'id', gid, COALESCE(opt->>'name',''), COALESCE((opt->>'price')::numeric, 0),
        COALESCE(NULLIF(TRIM(COALESCE(opt->>'inventoryMode', '')), ''), 'available'),
        COALESCE((opt->>'enabled')::boolean, true), COALESCE(opt->>'img',''), COALESCE(opt->>'imgLight',''), COALESCE(opt->>'imgDark',''), opt_ord);
      opt_ord := opt_ord + 1;
    END LOOP;
  END LOOP;

  cat_defaults := p_store->'categoryDefaults';
  IF jsonb_typeof(cat_defaults) = 'object' THEN
    FOR cat_id IN SELECT * FROM jsonb_object_keys(cat_defaults)
    LOOP
      FOR gid IN SELECT * FROM jsonb_array_elements_text(cat_defaults->cat_id)
      LOOP
        INSERT INTO app_category_default_groups (category_id, group_id, sort_order) VALUES (cat_id, gid, 0);
      END LOOP;
    END LOOP;
  END IF;

  po := p_store->'productOptions';
  IF jsonb_typeof(po) = 'object' THEN
    FOR pid IN SELECT * FROM jsonb_object_keys(po)
    LOOP
      linked := po->pid->'linkedGroups';
      IF jsonb_typeof(linked) = 'object' THEN
        FOR gid IN SELECT * FROM jsonb_object_keys(linked)
        LOOP
          INSERT INTO app_product_group_overrides (product_id, group_id, enabled, mode_override, required_override, max_override)
          VALUES (pid, gid,
            COALESCE((linked->gid->>'enabled')::boolean, true),
            linked->gid->>'mode', (linked->gid->>'required')::boolean, (linked->gid->>'max')::int);
        END LOOP;
      END IF;
    END LOOP;
  END IF;

  FOR h IN SELECT * FROM jsonb_array_elements(p_store->'hoods')
  LOOP
    INSERT INTO hoods (id, name, fee) VALUES (h->>'id', COALESCE(h->>'name',''), COALESCE((h->>'fee')::numeric, 0));
  END LOOP;

  wh := p_store->'workHours';
  IF wh IS NOT NULL AND jsonb_typeof(wh) = 'object' THEN
    INSERT INTO app_work_hours (id, open_at_minutes, close_at_minutes) VALUES (1,
      COALESCE((wh->>'openAtMinutes')::int, 420), COALESCE((wh->>'closeAtMinutes')::int, 180))
    ON CONFLICT (id) DO UPDATE SET open_at_minutes = EXCLUDED.open_at_minutes, close_at_minutes = EXCLUDED.close_at_minutes;
  END IF;

  FOR cpn IN SELECT * FROM jsonb_array_elements(p_store->'coupons')
  LOOP
    INSERT INTO coupons (id, code, title, enabled, free_delivery, discount_amount, discount_percent, min_order_amount, per_user_limit, max_customers, sort_order)
    VALUES (cpn->>'id', COALESCE(cpn->>'code',''), COALESCE(cpn->>'title',''), COALESCE((cpn->>'enabled')::boolean, true),
      COALESCE((cpn->>'freeDelivery')::boolean, false), COALESCE((cpn->>'discountAmount')::numeric, 0), COALESCE((cpn->>'discountPercent')::numeric, 0),
      COALESCE((cpn->>'minOrderAmount')::numeric, 0), COALESCE((cpn->>'perUserLimit')::int, 0), COALESCE((cpn->>'maxCustomers')::int, 0), COALESCE((cpn->>'order')::int, 0));
  END LOOP;

  -- users: drivers و cashiers (upsert فقط؛ لا حذف باقي المستخدمين)
  FOR drv IN SELECT * FROM jsonb_array_elements(p_store->'drivers')
  LOOP
    sid := drv->>'id';
    IF sid IS NOT NULL AND sid <> '' THEN
      drv_password := NULLIF(COALESCE(drv->>'password', ''), '');
      INSERT INTO users (id, username, display_name, phone, role, is_active, updated_at)
      VALUES (
        sid,
        COALESCE(NULLIF(drv->>'phone', ''), NULLIF(drv->>'name', ''), sid),
        COALESCE(drv->>'name',''),
        COALESCE(drv->>'phone',''),
        'DRIVER',
        (drv->>'status') <> 'inactive',
        now()
      )
      ON CONFLICT (id) DO UPDATE
      SET
        username = EXCLUDED.username,
        display_name = EXCLUDED.display_name,
        phone = EXCLUDED.phone,
        is_active = EXCLUDED.is_active,
        updated_at = now();
      INSERT INTO user_login_secrets (user_id, password_hash, updated_at)
      VALUES (
        sid,
        CASE
          WHEN drv_password IS NULL THEN NULL
          ELSE public.hash_app_secret(drv_password)
        END,
        now()
      )
      ON CONFLICT (user_id) DO UPDATE
      SET
        password_hash = CASE
          WHEN drv_password IS NULL THEN user_login_secrets.password_hash
          ELSE public.hash_app_secret(drv_password)
        END,
        updated_at = now();
    END IF;
  END LOOP;
  FOR cash IN SELECT * FROM jsonb_array_elements(p_store->'cashiers')
  LOOP
    sid := cash->>'id';
    IF sid IS NOT NULL AND sid <> '' THEN
      cash_password := NULLIF(COALESCE(cash->>'password', ''), '');
      cash_pin := NULLIF(COALESCE(cash->>'pin', ''), '');
      INSERT INTO users (id, username, display_name, role, is_active, updated_at)
      VALUES (
        sid,
        COALESCE(NULLIF(cash->>'name', ''), sid),
        COALESCE(cash->>'name',''),
        'CASHIER',
        true,
        now()
      )
      ON CONFLICT (id) DO UPDATE
      SET
        username = EXCLUDED.username,
        display_name = EXCLUDED.display_name,
        updated_at = now();
      INSERT INTO user_login_secrets (user_id, password_hash, pin_hash, updated_at)
      VALUES (
        sid,
        CASE
          WHEN cash_password IS NULL THEN NULL
          ELSE public.hash_app_secret(cash_password)
        END,
        CASE
          WHEN cash_pin IS NULL THEN NULL
          ELSE public.hash_app_secret(cash_pin)
        END,
        now()
      )
      ON CONFLICT (user_id) DO UPDATE
      SET
        password_hash = CASE
          WHEN cash_password IS NULL THEN user_login_secrets.password_hash
          ELSE public.hash_app_secret(cash_password)
        END,
        pin_hash = CASE
          WHEN cash_pin IS NULL THEN user_login_secrets.pin_hash
          ELSE public.hash_app_secret(cash_pin)
        END,
        updated_at = now();
    END IF;
  END LOOP;

  RETURN p_store;
END;
$$;

CREATE OR REPLACE FUNCTION public.hash_app_secret(p_value TEXT)
RETURNS TEXT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_value TEXT := NULLIF(COALESCE(p_value, ''), '');
BEGIN
  IF v_value IS NULL THEN
    RETURN NULL;
  END IF;
  RETURN extensions.crypt(v_value, extensions.gen_salt('bf', 12));
END;
$$;

CREATE OR REPLACE FUNCTION public.hash_session_token(p_value TEXT)
RETURNS TEXT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_value TEXT := NULLIF(COALESCE(p_value, ''), '');
BEGIN
  IF v_value IS NULL THEN
    RETURN NULL;
  END IF;
  RETURN encode(extensions.digest(v_value, 'sha256'), 'hex');
END;
$$;

CREATE OR REPLACE FUNCTION public.verify_app_secret(
  p_value TEXT,
  p_hash TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_value TEXT := COALESCE(p_value, '');
  v_hash TEXT := NULLIF(TRIM(COALESCE(p_hash, '')), '');
BEGIN
  IF v_hash IS NULL THEN
    RETURN false;
  END IF;
  IF v_hash ~ '^\$2[abxy]?\$' THEN
    RETURN extensions.crypt(v_value, v_hash) = v_hash;
  END IF;
  RETURN false;
END;
$$;

-- إنشاء حساب عميل (جداول public.users و user_login_secrets فقط)
CREATE OR REPLACE FUNCTION public.api_customer_signup(
  p_phone TEXT,
  p_email TEXT,
  p_display_name TEXT,
  p_password TEXT,
  p_user_id TEXT
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  new_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  u RECORD;
BEGIN
  p_phone := NULLIF(TRIM(COALESCE(p_phone, '')), '');
  p_email := NULLIF(TRIM(LOWER(COALESCE(p_email, ''))), '');
  p_display_name := COALESCE(NULLIF(TRIM(COALESCE(p_display_name, '')), ''), 'User');

  IF p_password IS NULL OR LENGTH(COALESCE(p_password, '')) < 6 THEN
    RETURN json_build_object('ok', false, 'error', 'password_too_short');
  END IF;
  IF (p_phone IS NULL OR p_phone = '') AND (p_email IS NULL OR p_email = '') THEN
    RETURN json_build_object('ok', false, 'error', 'phone_or_email_required');
  END IF;

  -- NO SECURITY GUARD HERE - anon is allowed to signup with p_user_id
  IF new_id IS NOT NULL THEN
    SELECT * INTO u FROM public.users WHERE id = new_id LIMIT 1;

    IF FOUND THEN
      IF UPPER(COALESCE(u.role, 'CUSTOMER')) <> 'CUSTOMER' THEN
        RETURN json_build_object('ok', false, 'error', 'user_id_conflict');
      END IF;
      IF p_phone IS NOT NULL AND EXISTS (
        SELECT 1 FROM public.users WHERE phone = p_phone AND id <> new_id
      ) THEN
        RETURN json_build_object('ok', false, 'error', 'phone_exists');
      END IF;
      IF p_email IS NOT NULL AND EXISTS (
        SELECT 1 FROM public.users WHERE email = p_email AND id <> new_id
      ) THEN
        RETURN json_build_object('ok', false, 'error', 'email_exists');
      END IF;

      UPDATE public.users
      SET
        username     = COALESCE(p_phone, p_email, username),
        phone        = p_phone,
        email        = p_email,
        display_name = p_display_name,
        is_active    = true,
        role         = 'CUSTOMER',
        updated_at   = now()
      WHERE id = new_id;

      INSERT INTO public.user_login_secrets (user_id, password_hash, updated_at)
      VALUES (new_id, public.hash_app_secret(p_password), now())
      ON CONFLICT (user_id) DO UPDATE
        SET password_hash = EXCLUDED.password_hash, updated_at = now();

      SELECT * INTO u FROM public.users WHERE id = new_id;
      RETURN json_build_object(
        'ok', true,
        'user', json_build_object(
          'id', u.id, 'username', u.username, 'phone', u.phone,
          'email', u.email, 'displayName', u.display_name,
          'isActive', u.is_active, 'role', u.role
        )
      );
    END IF;
  END IF;

  IF p_phone IS NOT NULL AND EXISTS (SELECT 1 FROM public.users WHERE phone = p_phone) THEN
    RETURN json_build_object('ok', false, 'error', 'phone_exists');
  END IF;
  IF p_email IS NOT NULL AND EXISTS (SELECT 1 FROM public.users WHERE email = p_email) THEN
    RETURN json_build_object('ok', false, 'error', 'email_exists');
  END IF;

  new_id := COALESCE(new_id, extensions.gen_random_uuid()::TEXT);
  INSERT INTO public.users (id, username, phone, email, display_name, is_active, role, updated_at)
  VALUES (
    new_id, COALESCE(p_phone, p_email, new_id),
    p_phone, p_email, p_display_name, true, 'CUSTOMER', now()
  );

  INSERT INTO public.user_login_secrets (user_id, password_hash, updated_at)
  VALUES (new_id, public.hash_app_secret(p_password), now())
  ON CONFLICT (user_id) DO UPDATE
    SET password_hash = EXCLUDED.password_hash, updated_at = now();

  SELECT * INTO u FROM public.users WHERE id = new_id;
  RETURN json_build_object(
    'ok', true,
    'user', json_build_object(
      'id', u.id, 'username', u.username, 'phone', u.phone,
      'email', u.email, 'displayName', u.display_name,
      'isActive', u.is_active, 'role', u.role
    )
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_customer_signup(
  p_phone TEXT DEFAULT NULL,
  p_email TEXT DEFAULT NULL,
  p_display_name TEXT DEFAULT NULL,
  p_password TEXT DEFAULT NULL
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  RETURN public.api_customer_signup(
    p_phone := p_phone,
    p_email := p_email,
    p_display_name := p_display_name,
    p_password := p_password,
    p_user_id := NULL
  );
END;
$$;

CREATE TABLE IF NOT EXISTS public.auth_login_rate_limits (
  scope_key TEXT PRIMARY KEY,
  scope_kind TEXT NOT NULL CHECK (scope_kind IN ('IDENTIFIER', 'IP')),
  failed_attempts INT NOT NULL DEFAULT 0 CHECK (failed_attempts >= 0),
  first_failed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  last_failed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  blocked_until TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_auth_login_rate_limits_blocked_until
  ON public.auth_login_rate_limits(blocked_until);

CREATE OR REPLACE FUNCTION public.api_request_ip_address()
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_raw TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_header_value(
      ARRAY['cf-connecting-ip', 'x-real-ip', 'x-forwarded-for']
    ),
    ''
  )), '');
BEGIN
  IF v_raw IS NULL THEN
    RETURN NULL;
  END IF;

  IF POSITION(',' IN v_raw) > 0 THEN
    v_raw := split_part(v_raw, ',', 1);
  END IF;

  RETURN NULLIF(TRIM(v_raw), '');
END;
$$;

CREATE OR REPLACE FUNCTION public.auth_rate_limit_key(
  p_scope_kind TEXT,
  p_raw_value TEXT
)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
SET search_path = public, extensions
AS $$
DECLARE
  v_scope_kind TEXT := UPPER(NULLIF(TRIM(COALESCE(p_scope_kind, '')), ''));
  v_raw_value TEXT := NULLIF(TRIM(COALESCE(p_raw_value, '')), '');
BEGIN
  IF v_scope_kind IS NULL OR v_raw_value IS NULL THEN
    RETURN NULL;
  END IF;

  RETURN encode(
    extensions.digest(v_scope_kind || ':' || LOWER(v_raw_value), 'sha256'),
    'hex'
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.auth_cleanup_login_rate_limits()
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_deleted INT := 0;
BEGIN
  DELETE FROM public.auth_login_rate_limits rl
  WHERE rl.last_failed_at < now() - interval '7 days'
    AND (
      rl.blocked_until IS NULL
      OR rl.blocked_until <= now()
    );

  GET DIAGNOSTICS v_deleted = ROW_COUNT;
  RETURN COALESCE(v_deleted, 0);
END;
$$;

CREATE OR REPLACE FUNCTION public.auth_login_is_blocked(
  p_identifier TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_identifier_key TEXT := public.auth_rate_limit_key(
    'IDENTIFIER',
    LOWER(NULLIF(TRIM(COALESCE(p_identifier, '')), ''))
  );
  v_ip_key TEXT := public.auth_rate_limit_key(
    'IP',
    public.api_request_ip_address()
  );
BEGIN
  PERFORM public.auth_cleanup_login_rate_limits();

  RETURN EXISTS (
    SELECT 1
    FROM public.auth_login_rate_limits rl
    WHERE rl.scope_key IN (v_identifier_key, v_ip_key)
      AND rl.blocked_until IS NOT NULL
      AND rl.blocked_until > now()
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.auth_register_login_failure(
  p_identifier TEXT
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_window INTERVAL := interval '15 minutes';
  v_block_for INTERVAL := interval '15 minutes';
  v_identifier_key TEXT := public.auth_rate_limit_key(
    'IDENTIFIER',
    LOWER(NULLIF(TRIM(COALESCE(p_identifier, '')), ''))
  );
  v_ip_key TEXT := public.auth_rate_limit_key(
    'IP',
    public.api_request_ip_address()
  );
BEGIN
  IF v_identifier_key IS NOT NULL THEN
    INSERT INTO public.auth_login_rate_limits (
      scope_key,
      scope_kind,
      failed_attempts,
      first_failed_at,
      last_failed_at,
      blocked_until
    )
    VALUES (
      v_identifier_key,
      'IDENTIFIER',
      1,
      now(),
      now(),
      NULL
    )
    ON CONFLICT (scope_key) DO UPDATE
    SET
      failed_attempts = CASE
        WHEN public.auth_login_rate_limits.last_failed_at < now() - v_window THEN 1
        ELSE LEAST(public.auth_login_rate_limits.failed_attempts + 1, 1000000)
      END,
      first_failed_at = CASE
        WHEN public.auth_login_rate_limits.last_failed_at < now() - v_window THEN now()
        ELSE public.auth_login_rate_limits.first_failed_at
      END,
      last_failed_at = now(),
      blocked_until = CASE
        WHEN public.auth_login_rate_limits.last_failed_at < now() - v_window THEN NULL
        WHEN public.auth_login_rate_limits.failed_attempts + 1 >= 6 THEN now() + v_block_for
        ELSE public.auth_login_rate_limits.blocked_until
      END;
  END IF;

  IF v_ip_key IS NOT NULL THEN
    INSERT INTO public.auth_login_rate_limits (
      scope_key,
      scope_kind,
      failed_attempts,
      first_failed_at,
      last_failed_at,
      blocked_until
    )
    VALUES (
      v_ip_key,
      'IP',
      1,
      now(),
      now(),
      NULL
    )
    ON CONFLICT (scope_key) DO UPDATE
    SET
      failed_attempts = CASE
        WHEN public.auth_login_rate_limits.last_failed_at < now() - v_window THEN 1
        ELSE LEAST(public.auth_login_rate_limits.failed_attempts + 1, 1000000)
      END,
      first_failed_at = CASE
        WHEN public.auth_login_rate_limits.last_failed_at < now() - v_window THEN now()
        ELSE public.auth_login_rate_limits.first_failed_at
      END,
      last_failed_at = now(),
      blocked_until = CASE
        WHEN public.auth_login_rate_limits.last_failed_at < now() - v_window THEN NULL
        WHEN public.auth_login_rate_limits.failed_attempts + 1 >= 20 THEN now() + v_block_for
        ELSE public.auth_login_rate_limits.blocked_until
      END;
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION public.auth_clear_login_failures(
  p_identifier TEXT
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_identifier_key TEXT := public.auth_rate_limit_key(
    'IDENTIFIER',
    LOWER(NULLIF(TRIM(COALESCE(p_identifier, '')), ''))
  );
  v_ip_key TEXT := public.auth_rate_limit_key(
    'IP',
    public.api_request_ip_address()
  );
BEGIN
  DELETE FROM public.auth_login_rate_limits rl
  WHERE rl.scope_key IN (v_identifier_key, v_ip_key);
END;
$$;

-- تسجيل الدخول بالهاتف/البريد وكلمة المرور.
-- ملاحظة: حسابات الـ staff في الـ POS قد تعتمد PIN بدل password،
-- لذلك نقبل password_hash أو pin_hash للحسابات غير CUSTOMER.
-- وندعم أيضًا display_name للـ staff لأن بعض الكاشير لديهم username داخلي مولد
-- بينما المستخدم الفعلي يعرف اسم العرض فقط.
CREATE OR REPLACE FUNCTION public.api_verify_phone_password_login(p_phone TEXT, p_password TEXT)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  u RECORD;
  secret RECORD;
  ident TEXT;
BEGIN
  ident := NULLIF(TRIM(COALESCE(p_phone, '')), '');
  IF ident IS NULL OR p_password IS NULL OR TRIM(COALESCE(p_password, '')) = '' THEN
    RETURN json_build_object('ok', false, 'error', 'invalid_input');
  END IF;

  FOR u IN
    SELECT *
    FROM users
    WHERE is_active = true
      AND UPPER(COALESCE(role, '')) IN ('CUSTOMER', 'ADMIN_POS', 'ADMIN', 'SERVICE_ROLE', 'CASHIER', 'DRIVER')
      AND public.account_identifier_matches(
        p_identifier := ident,
        p_user_id := id,
        p_phone := phone,
        p_email := email,
        p_username := username,
        p_display_name := display_name,
        p_role := role,
        p_allow_staff_display_name := true
      )
    ORDER BY
      CASE UPPER(COALESCE(role, ''))
        WHEN 'ADMIN_POS' THEN 1
        WHEN 'SERVICE_ROLE' THEN 2
        WHEN 'ADMIN' THEN 3
        WHEN 'CASHIER' THEN 4
        WHEN 'DRIVER' THEN 5
        WHEN 'CUSTOMER' THEN 6
        ELSE 9
      END,
      public.account_identifier_match_rank(
        p_identifier := ident,
        p_user_id := id,
        p_phone := phone,
        p_email := email,
        p_username := username,
        p_display_name := display_name,
        p_role := role,
        p_allow_staff_display_name := true
      ),
      updated_at DESC NULLS LAST,
      created_at DESC NULLS LAST
  LOOP
    SELECT * INTO secret FROM user_login_secrets WHERE user_id = u.id;
    IF secret.user_id IS NULL THEN
      CONTINUE;
    END IF;

    IF (
      secret.password_hash IS NOT NULL
      AND secret.password_hash <> ''
      AND LOWER(secret.password_hash) !~ '^[0-9a-f]{32}$'
      AND public.verify_app_secret(p_password, secret.password_hash)
    ) THEN
      NULL;
    ELSIF (
      UPPER(COALESCE(u.role, '')) IN ('ADMIN_POS', 'ADMIN', 'SERVICE_ROLE', 'CASHIER', 'DRIVER')
      AND secret.pin_hash IS NOT NULL
      AND secret.pin_hash <> ''
      AND LOWER(secret.pin_hash) !~ '^[0-9a-f]{32}$'
      AND public.verify_app_secret(p_password, secret.pin_hash)
    ) THEN
      NULL;
    ELSE
      CONTINUE;
    END IF;

    RETURN json_build_object(
      'ok', true,
      'user', json_build_object(
        'id', u.id, 'username', u.username, 'phone', u.phone, 'email', u.email,
        'displayName', u.display_name, 'isActive', u.is_active, 'role', u.role
      )
    );
  END LOOP;

  RETURN json_build_object('ok', false, 'error', 'invalid_credentials');
END;
$$;

CREATE OR REPLACE FUNCTION public.api_issue_role_session(
  p_phone TEXT,
  p_password TEXT,
  p_ttl_hours INT DEFAULT 336
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_identifier TEXT := NULLIF(TRIM(COALESCE(p_phone, '')), '');
  v_was_blocked BOOLEAN := false;
  v_login JSON;
  v_user RECORD;
  v_user_id TEXT;
  v_role TEXT;
  v_ttl_hours INT := GREATEST(LEAST(COALESCE(p_ttl_hours, 24), 24 * 14), 1);
  v_raw_token TEXT;
  v_expires_at TIMESTAMPTZ;
BEGIN
  IF v_identifier IS NULL OR p_password IS NULL OR TRIM(COALESCE(p_password, '')) = '' THEN
    RETURN json_build_object('ok', false, 'error', 'invalid_input');
  END IF;

  v_was_blocked := public.auth_login_is_blocked(v_identifier);

  DELETE FROM public.app_role_sessions
  WHERE revoked_at IS NOT NULL
     OR expires_at <= now();

  v_login := public.api_verify_phone_password_login(p_phone, p_password);
  IF COALESCE(v_login->>'ok', 'false') NOT IN ('true', 'TRUE') THEN
    IF v_was_blocked THEN
      RETURN json_build_object('ok', false, 'error', 'too_many_attempts');
    END IF;

    PERFORM public.auth_register_login_failure(v_identifier);
    IF public.auth_login_is_blocked(v_identifier) THEN
      RETURN json_build_object('ok', false, 'error', 'too_many_attempts');
    END IF;
    RETURN json_build_object('ok', false, 'error', 'invalid_credentials');
  END IF;

  v_user_id := NULLIF(TRIM(COALESCE(v_login->'user'->>'id', '')), '');
  IF v_user_id IS NULL THEN
    RETURN json_build_object('ok', false, 'error', 'user_not_found');
  END IF;

  SELECT *
  INTO v_user
  FROM public.users
  WHERE id = v_user_id
  LIMIT 1;

  IF NOT FOUND OR COALESCE(v_user.is_active, true) <> true THEN
    RETURN json_build_object('ok', false, 'error', 'user_not_found');
  END IF;

  v_role := public.normalize_app_role(COALESCE(v_user.role, ''));
  IF v_role IS NULL OR v_role = '' THEN
    RETURN json_build_object('ok', false, 'error', 'role_missing');
  END IF;

  v_raw_token := encode(extensions.gen_random_bytes(32), 'hex');
  v_expires_at := now() + make_interval(hours => v_ttl_hours);

  INSERT INTO public.app_role_sessions (
    token_hash,
    user_id,
    expires_at,
    revoked_at,
    created_at,
    last_seen_at
  )
  VALUES (
    public.hash_session_token(v_raw_token),
    v_user_id,
    v_expires_at,
    NULL,
    now(),
    now()
  );

  PERFORM public.auth_clear_login_failures(v_identifier);

  RETURN json_build_object(
    'ok', true,
    'sessionToken', v_raw_token,
    'expiresAtMillis', (extract(epoch FROM v_expires_at) * 1000)::BIGINT,
    'user', json_build_object(
      'id', v_user.id,
      'username', v_user.username,
      'phone', v_user.phone,
      'email', v_user.email,
      'displayName', v_user.display_name,
      'isActive', v_user.is_active,
      'role', v_user.role
    )
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_role_session_logout(p_token TEXT DEFAULT NULL)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_token TEXT := NULLIF(TRIM(COALESCE(p_token, public.current_app_session_token(), '')), '');
BEGIN
  IF v_token IS NULL THEN
    RETURN json_build_object('ok', true);
  END IF;

  UPDATE public.app_role_sessions
  SET revoked_at = now()
  WHERE token_hash = public.hash_session_token(v_token)
    AND revoked_at IS NULL;

  RETURN json_build_object('ok', true);
END;
$$;

-- البحث في users عبر RPC (بديل آمن عن القراءة المباشرة من /rest/v1/users)
CREATE OR REPLACE FUNCTION public.api_users_search(
  p_identifier TEXT DEFAULT NULL,
  p_id TEXT DEFAULT NULL,
  p_role TEXT DEFAULT NULL,
  p_limit INT DEFAULT 50
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_actor_user_id TEXT := public.current_app_user_id();
  v_identifier TEXT := NULLIF(TRIM(COALESCE(p_identifier, '')), '');
  v_id TEXT := NULLIF(TRIM(COALESCE(p_id, '')), '');
  v_role_filter TEXT := public.normalize_app_role(NULLIF(TRIM(COALESCE(p_role, '')), ''));
  v_limit INT := GREATEST(LEAST(COALESCE(p_limit, 50), 500), 1);
  v_is_admin_actor BOOLEAN := v_actor_role IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE');
  v_claim_auth_user_id TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['sub', 'id']),
    ''
  )), '');
  v_claim_email TEXT := LOWER(NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['email']),
    ''
  )), ''));
  v_claim_phone TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['phone']),
    ''
  )), '');
  v_auth_email TEXT := NULL;
  v_auth_phone TEXT := NULL;
  v_auth_meta JSONB := '{}'::jsonb;
  v_auth_meta_email TEXT := NULL;
  v_auth_meta_phone TEXT := NULL;
  v_allow_claim_identity_lookup BOOLEAN := false;
BEGIN
  IF v_claim_auth_user_id IS NOT NULL THEN
    BEGIN
      SELECT
        LOWER(NULLIF(TRIM(COALESCE(au.email, '')), '')),
        NULLIF(TRIM(COALESCE(au.phone, '')), ''),
        COALESCE(au.raw_user_meta_data, '{}'::jsonb)
      INTO
        v_auth_email,
        v_auth_phone,
        v_auth_meta
      FROM auth.users au
      WHERE au.id::TEXT = v_claim_auth_user_id
      LIMIT 1;
    EXCEPTION WHEN OTHERS THEN
      v_auth_email := NULL;
      v_auth_phone := NULL;
      v_auth_meta := '{}'::jsonb;
    END;
  END IF;

  v_auth_meta_email := LOWER(NULLIF(TRIM(COALESCE(
    v_auth_meta->>'email',
    v_auth_meta->>'fale7_email',
    ''
  )), ''));
  v_auth_meta_phone := NULLIF(TRIM(COALESCE(
    v_auth_meta->>'phone',
    v_auth_meta->>'fale7_phone',
    ''
  )), '');

  v_allow_claim_identity_lookup := v_actor_user_id IS NULL
    AND (
      v_claim_email IS NOT NULL
      OR v_claim_phone IS NOT NULL
      OR v_auth_email IS NOT NULL
      OR v_auth_phone IS NOT NULL
      OR v_auth_meta_email IS NOT NULL
      OR v_auth_meta_phone IS NOT NULL
    );

  RETURN COALESCE(
    (
      SELECT json_agg(json_build_object(
        'id', u.id, 'username', u.username, 'phone', u.phone, 'email', u.email,
        'displayName', u.display_name, 'isActive', u.is_active, 'role', u.role
      ))
      FROM (
        SELECT u.*
        FROM public.users u
        WHERE
          (
            v_is_admin_actor
            OR (
              v_actor_user_id IS NOT NULL
              AND u.id = v_actor_user_id
            )
            OR (
              v_actor_user_id IS NULL
              AND COALESCE(u.is_active, true) = true
              AND public.normalize_app_role(COALESCE(u.role, '')) = 'CUSTOMER'
            )
            OR (
              v_allow_claim_identity_lookup
              AND (
                (v_claim_email IS NOT NULL AND LOWER(NULLIF(TRIM(COALESCE(u.email, '')), '')) = v_claim_email)
                OR (v_claim_phone IS NOT NULL AND NULLIF(TRIM(COALESCE(u.phone, '')), '') = v_claim_phone)
                OR (v_auth_email IS NOT NULL AND LOWER(NULLIF(TRIM(COALESCE(u.email, '')), '')) = v_auth_email)
                OR (v_auth_phone IS NOT NULL AND NULLIF(TRIM(COALESCE(u.phone, '')), '') = v_auth_phone)
                OR (v_auth_meta_email IS NOT NULL AND LOWER(NULLIF(TRIM(COALESCE(u.email, '')), '')) = v_auth_meta_email)
                OR (v_auth_meta_phone IS NOT NULL AND NULLIF(TRIM(COALESCE(u.phone, '')), '') = v_auth_meta_phone)
              )
            )
          )
          AND (v_id IS NULL OR u.id = v_id)
          AND (
            v_identifier IS NULL
            OR public.account_identifier_matches(
              p_identifier := v_identifier,
              p_user_id := u.id,
              p_phone := u.phone,
              p_email := u.email,
              p_username := u.username,
              p_display_name := u.display_name,
              p_role := u.role,
              p_allow_staff_display_name := (v_is_admin_actor OR (v_actor_user_id IS NOT NULL AND u.id = v_actor_user_id))
            )
          )
          AND (v_role_filter IS NULL OR UPPER(COALESCE(u.role, '')) = v_role_filter)
        ORDER BY
          CASE
            WHEN v_id IS NOT NULL AND u.id = v_id THEN 0
            ELSE 1
          END,
          CASE
            WHEN v_identifier IS NULL THEN 0
            ELSE public.account_identifier_match_rank(
              p_identifier := v_identifier,
              p_user_id := u.id,
              p_phone := u.phone,
              p_email := u.email,
              p_username := u.username,
              p_display_name := u.display_name,
              p_role := u.role,
              p_allow_staff_display_name := (v_is_admin_actor OR (v_actor_user_id IS NOT NULL AND u.id = v_actor_user_id))
            )
          END,
          u.updated_at DESC,
          u.created_at DESC
        LIMIT CASE
          WHEN NOT v_is_admin_actor AND v_actor_user_id IS NOT NULL THEN 1
          ELSE v_limit
        END
      ) u
    ),
    '[]'::json
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_public_account_lookup(
  p_identifier TEXT DEFAULT NULL,
  p_id TEXT DEFAULT NULL
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_identifier TEXT := NULLIF(TRIM(COALESCE(p_identifier, '')), '');
  v_id TEXT := NULLIF(TRIM(COALESCE(p_id, '')), '');
BEGIN
  IF v_identifier IS NULL AND v_id IS NULL THEN
    RETURN '[]'::json;
  END IF;

  RETURN COALESCE(
    (
      SELECT json_agg(json_build_object(
        'id', u.id,
        'username', COALESCE(u.username, ''),
        'displayName', COALESCE(u.display_name, COALESCE(u.username, '')),
        'isActive', COALESCE(u.is_active, true),
        'role', COALESCE(u.role, 'CUSTOMER')
      ))
      FROM (
        SELECT u.*
        FROM public.users u
        WHERE COALESCE(u.is_active, true) = true
          AND UPPER(COALESCE(u.role, '')) = 'CUSTOMER'
          AND (
            (v_id IS NOT NULL AND u.id = v_id)
            OR (
              v_identifier IS NOT NULL
              AND public.account_identifier_matches(
                p_identifier := v_identifier,
                p_user_id := u.id,
                p_phone := u.phone,
                p_email := u.email,
                p_username := u.username,
                p_display_name := u.display_name,
                p_role := u.role,
                p_allow_staff_display_name := false
              )
            )
          )
        ORDER BY
          CASE
            WHEN v_id IS NOT NULL AND u.id = v_id THEN 0
            ELSE 1
          END,
          CASE
            WHEN v_identifier IS NULL THEN 0
            ELSE public.account_identifier_match_rank(
              p_identifier := v_identifier,
              p_user_id := u.id,
              p_phone := u.phone,
              p_email := u.email,
              p_username := u.username,
              p_display_name := u.display_name,
              p_role := u.role,
              p_allow_staff_display_name := false
            )
          END,
          u.updated_at DESC NULLS LAST,
          u.created_at DESC NULLS LAST
        LIMIT 1
      ) u
    ),
    '[]'::json
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_public_login_identifier_resolve(
  p_identifier TEXT DEFAULT NULL
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_identifier TEXT := NULLIF(TRIM(COALESCE(p_identifier, '')), '');
  v_result JSON;
BEGIN
  IF v_identifier IS NULL THEN
    RETURN json_build_object('ok', false);
  END IF;

  PERFORM public.auth_cleanup_login_rate_limits();

  IF public.auth_login_is_blocked(v_identifier) THEN
    RETURN json_build_object('ok', false, 'error', 'rate_limited');
  END IF;

  SELECT json_build_object(
    'ok', true,
    'id', u.id,
    'email', u.email
  )
  INTO v_result
  FROM (
    SELECT u.id, u.email
    FROM public.users u
    WHERE COALESCE(u.is_active, true) = true
      AND UPPER(COALESCE(u.role, '')) = 'CUSTOMER'
      AND NULLIF(TRIM(LOWER(COALESCE(u.email, ''))), '') IS NOT NULL
      AND public.account_identifier_matches(
        p_identifier := v_identifier,
        p_user_id := u.id,
        p_phone := u.phone,
        p_email := u.email,
        p_username := u.username,
        p_display_name := u.display_name,
        p_role := u.role,
        p_allow_staff_display_name := false
      )
    ORDER BY u.updated_at DESC NULLS LAST, u.created_at DESC NULLS LAST
    LIMIT 1
  ) u;

  RETURN COALESCE(v_result, json_build_object('ok', false));
END;
$$;

CREATE OR REPLACE FUNCTION public.is_valid_complaints_object_name(p_name TEXT)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_name TEXT := NULLIF(TRIM(COALESCE(p_name, '')), '');
BEGIN
  IF v_name IS NULL THEN
    RETURN false;
  END IF;

  RETURN v_name ~ '^support/[A-Za-z0-9_-]{1,64}/[A-Za-z0-9._-]{1,160}$';
END;
$$;

CREATE OR REPLACE FUNCTION public.complaints_object_owner_user_id(p_name TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
SET search_path = public
AS $$
DECLARE
  v_name TEXT := NULLIF(TRIM(COALESCE(p_name, '')), '');
BEGIN
  IF v_name IS NULL THEN
    RETURN NULL;
  END IF;

  RETURN NULLIF(
    substring(v_name FROM '^support/([A-Za-z0-9_-]{1,64})/'),
    ''
  );
END;
$$;

-- POS: snapshot of staff credentials (cashiers/admins/drivers) for offline hash login sync.
CREATE OR REPLACE FUNCTION public.api_pos_cashier_credentials_snapshot(
  p_include_inactive BOOLEAN DEFAULT true,
  p_limit INT DEFAULT 2000
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_limit INT := GREATEST(LEAST(COALESCE(p_limit, 2000), 5000), 1);
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN '[]'::json;
  END IF;

  RETURN COALESCE(
    (
      SELECT json_agg(json_build_object(
        'id', u.id,
        'username', COALESCE(u.username, ''),
        'display_name', COALESCE(u.display_name, ''),
        'displayName', COALESCE(u.display_name, ''),
        'phone', COALESCE(u.phone, ''),
        'email', COALESCE(u.email, ''),
        'role', UPPER(COALESCE(u.role, '')),
        'is_active', COALESCE(u.is_active, false),
        'isActive', COALESCE(u.is_active, false),
        'updated_at', COALESCE(u.updated_at, u.created_at),
        'updatedAt', COALESCE(u.updated_at, u.created_at),
        'secret_updated_at', s.updated_at,
        'secretUpdatedAt', s.updated_at
      ))
      FROM (
        SELECT u.*
        FROM public.users u
        WHERE UPPER(COALESCE(u.role, '')) IN ('CASHIER', 'ADMIN_POS', 'ADMIN', 'DRIVER')
          AND (COALESCE(p_include_inactive, true) OR u.is_active = true)
        ORDER BY u.updated_at DESC NULLS LAST, u.created_at DESC NULLS LAST
        LIMIT v_limit
      ) u
      LEFT JOIN public.user_login_secrets s
        ON s.user_id = u.id
    ),
    '[]'::json
  );
END;
$$;

-- POS / admin: upsert staff credentials. Prefer raw password/pin and hash on server.
-- p_password_hash is retained only to reject legacy MD5 callers explicitly.
CREATE OR REPLACE FUNCTION public.api_pos_upsert_staff_credential(
  p_user_id TEXT,
  p_username TEXT DEFAULT NULL,
  p_display_name TEXT DEFAULT NULL,
  p_phone TEXT DEFAULT NULL,
  p_email TEXT DEFAULT NULL,
  p_role TEXT DEFAULT NULL,
  p_is_active BOOLEAN DEFAULT true,
  p_password_hash TEXT DEFAULT NULL,
  p_password TEXT DEFAULT NULL,
  p_pin TEXT DEFAULT NULL
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_user_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  v_username TEXT := NULLIF(TRIM(COALESCE(p_username, '')), '');
  v_display_name TEXT := NULLIF(TRIM(COALESCE(p_display_name, '')), '');
  v_phone TEXT := NULLIF(TRIM(COALESCE(p_phone, '')), '');
  v_email TEXT := NULLIF(TRIM(LOWER(COALESCE(p_email, ''))), '');
  v_role TEXT := public.normalize_app_role(NULLIF(TRIM(COALESCE(p_role, '')), ''));
  v_is_active BOOLEAN := COALESCE(p_is_active, true);
  v_password_hash TEXT := NULLIF(TRIM(COALESCE(p_password_hash, '')), '');
  v_password TEXT := NULLIF(TRIM(COALESCE(p_password, '')), '');
  v_pin TEXT := NULLIF(TRIM(COALESCE(p_pin, '')), '');
  v_resolved_password_hash TEXT;
  v_resolved_pin_hash TEXT;
  v_policy_role TEXT := public.current_app_role();
  v_user_exists BOOLEAN := false;
  v_has_new_credentials BOOLEAN := false;
  v_existing_username TEXT;
  v_existing_display_name TEXT;
  v_existing_phone TEXT;
  v_existing_email TEXT;
  v_existing_role TEXT;
BEGIN
  IF v_policy_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_user_id IS NULL THEN
    RETURN json_build_object('ok', false, 'error', 'user_id_required');
  END IF;
  IF v_password_hash IS NOT NULL THEN
    RETURN json_build_object('ok', false, 'error', 'password_hash_unsupported');
  END IF;

  SELECT
    u.username,
    u.display_name,
    u.phone,
    u.email,
    u.role
  INTO
    v_existing_username,
    v_existing_display_name,
    v_existing_phone,
    v_existing_email,
    v_existing_role
  FROM public.users u
  WHERE u.id = v_user_id
  LIMIT 1;

  v_user_exists := FOUND;
  IF NOT v_user_exists AND v_password IS NULL AND v_pin IS NULL THEN
    RETURN json_build_object('ok', false, 'error', 'credential_required');
  END IF;

  v_resolved_password_hash := CASE
    WHEN v_password IS NOT NULL THEN public.hash_app_secret(v_password)
    ELSE NULL
  END;
  v_resolved_pin_hash := CASE
    WHEN v_pin IS NOT NULL THEN public.hash_app_secret(v_pin)
    ELSE NULL
  END;
  v_has_new_credentials := v_resolved_password_hash IS NOT NULL OR v_resolved_pin_hash IS NOT NULL;

  IF v_role IS NULL OR v_role = '' THEN
    v_role := public.normalize_app_role(COALESCE(v_existing_role, ''));
  END IF;
  IF v_role IS NULL OR v_role = '' THEN
    v_role := 'CASHIER';
  END IF;
  IF v_role NOT IN ('CASHIER', 'DRIVER', 'ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'role_invalid');
  END IF;

  IF v_username IS NULL THEN
    v_username := COALESCE(
      v_phone,
      v_email,
      NULLIF(TRIM(COALESCE(v_existing_username, '')), ''),
      v_user_id
    );
  END IF;
  IF v_display_name IS NULL THEN
    v_display_name := COALESCE(
      NULLIF(TRIM(COALESCE(v_existing_display_name, '')), ''),
      v_username
    );
  END IF;

  IF v_username IS NOT NULL AND EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id <> v_user_id
      AND NULLIF(TRIM(COALESCE(u.username, '')), '') = v_username
  ) THEN
    RETURN json_build_object('ok', false, 'error', 'username_exists');
  END IF;

  IF v_phone IS NOT NULL AND EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id <> v_user_id
      AND NULLIF(TRIM(COALESCE(u.phone, '')), '') = v_phone
  ) THEN
    RETURN json_build_object('ok', false, 'error', 'phone_exists');
  END IF;

  IF v_email IS NOT NULL AND EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id <> v_user_id
      AND LOWER(NULLIF(TRIM(COALESCE(u.email, '')), '')) = v_email
  ) THEN
    RETURN json_build_object('ok', false, 'error', 'email_exists');
  END IF;

  INSERT INTO public.users (id, username, display_name, phone, email, role, is_active, updated_at)
  VALUES (v_user_id, v_username, v_display_name, v_phone, v_email, v_role, v_is_active, now())
  ON CONFLICT (id) DO UPDATE
  SET
    username = COALESCE(EXCLUDED.username, public.users.username),
    display_name = COALESCE(EXCLUDED.display_name, public.users.display_name),
    phone = COALESCE(EXCLUDED.phone, public.users.phone),
    email = COALESCE(EXCLUDED.email, public.users.email),
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = now();

  IF v_has_new_credentials THEN
    INSERT INTO public.user_login_secrets (user_id, password_hash, pin_hash, updated_at)
    VALUES (v_user_id, v_resolved_password_hash, v_resolved_pin_hash, now())
    ON CONFLICT (user_id) DO UPDATE
    SET
      password_hash = COALESCE(EXCLUDED.password_hash, public.user_login_secrets.password_hash),
      pin_hash = COALESCE(v_resolved_pin_hash, public.user_login_secrets.pin_hash),
      updated_at = now();
  END IF;

  -- Reset identifier-based lockouts after an admin updates staff credentials.
  PERFORM public.auth_clear_login_failures(v_user_id);
  PERFORM public.auth_clear_login_failures(v_username);
  PERFORM public.auth_clear_login_failures(v_phone);
  PERFORM public.auth_clear_login_failures(v_email);
  PERFORM public.auth_clear_login_failures(v_display_name);
  IF v_user_exists THEN
    PERFORM public.auth_clear_login_failures(v_existing_username);
    PERFORM public.auth_clear_login_failures(v_existing_phone);
    PERFORM public.auth_clear_login_failures(v_existing_email);
    PERFORM public.auth_clear_login_failures(v_existing_display_name);
  END IF;

  RETURN json_build_object('ok', true, 'userId', v_user_id, 'role', v_role);
END;
$$;

CREATE OR REPLACE FUNCTION public.api_pos_delete_staff_user(
  p_user_id TEXT,
  p_allow_admin BOOLEAN DEFAULT false
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_actor_user_id TEXT := public.current_app_user_id();
  v_user_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  v_allow_admin BOOLEAN := COALESCE(p_allow_admin, false);
  v_target RECORD;
  v_target_role TEXT := '';
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_user_id IS NULL THEN
    RETURN json_build_object('ok', false, 'error', 'user_id_required');
  END IF;

  SELECT *
  INTO v_target
  FROM public.users u
  WHERE u.id = v_user_id
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN json_build_object('ok', true, 'deleted', false, 'userId', v_user_id);
  END IF;

  v_target_role := public.normalize_app_role(COALESCE(v_target.role, ''));
  IF v_target_role NOT IN ('CASHIER', 'DRIVER', 'ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'staff_only');
  END IF;

  IF NOT v_allow_admin
     AND v_target_role IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'protected_role');
  END IF;

  IF v_actor_role <> 'SERVICE_ROLE'
     AND v_actor_user_id IS NOT NULL
     AND v_actor_user_id = v_user_id THEN
    RETURN json_build_object('ok', false, 'error', 'cannot_delete_self');
  END IF;

  PERFORM public.auth_clear_login_failures(v_user_id);
  PERFORM public.auth_clear_login_failures(v_target.username);
  PERFORM public.auth_clear_login_failures(v_target.phone);
  PERFORM public.auth_clear_login_failures(v_target.email);

  DELETE FROM public.maintenance_mode_exceptions
  WHERE user_id = v_user_id;

  DELETE FROM public.notifications
  WHERE user_id = v_user_id;

  IF v_target_role = 'DRIVER' THEN
    UPDATE public.app_orders
    SET
      driver_user_id = NULL,
      updated_at = now(),
      metadata = jsonb_set(
        COALESCE(metadata, '{}'::jsonb),
        '{driverRemovedUserId}',
        to_jsonb(v_user_id),
        true
      )
    WHERE driver_user_id = v_user_id;
  END IF;

  DELETE FROM public.user_push_tokens
  WHERE user_id = v_user_id;

  DELETE FROM public.app_role_sessions
  WHERE user_id = v_user_id;

  DELETE FROM public.user_login_secrets
  WHERE user_id = v_user_id;

  DELETE FROM public.users
  WHERE id = v_user_id;

  RETURN json_build_object(
    'ok', true,
    'deleted', FOUND,
    'userId', v_user_id,
    'role', v_target_role
  );
END;
$$;

-- ========== الشكاوى والمقترحات ==========
CREATE OR REPLACE FUNCTION public.api_support_create_complaint(
  p_customer_id TEXT,
  p_customer_name TEXT,
  p_message TEXT,
  p_attachment_uri TEXT DEFAULT NULL
)
RETURNS BIGINT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  new_id BIGINT;
  no BIGINT;
  v_actor_role TEXT := public.current_app_role();
BEGIN
  IF v_actor_role NOT IN ('CUSTOMER', 'ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN 0;
  END IF;
  IF p_customer_id IS NULL OR TRIM(p_customer_id) = '' THEN
    RETURN 0;
  END IF;
  IF v_actor_role = 'CUSTOMER' AND NOT public.is_same_request_user(p_customer_id) THEN
    RETURN 0;
  END IF;

  SELECT COALESCE(MAX(complaint_no), 0) + 1 INTO no FROM support_threads;
  INSERT INTO support_threads (complaint_no, customer_id, customer_name, title, last_message, unread_by_admin, created_at_millis, updated_at_millis)
  VALUES (no, p_customer_id, p_customer_name, 'Complaint #' || no, p_message, 1,
    (extract(epoch from now()) * 1000)::BIGINT, (extract(epoch from now()) * 1000)::BIGINT)
  RETURNING id INTO new_id;
  INSERT INTO support_messages (thread_id, sender_role, sender_id, sender_name, body, attachment_uri, created_at_millis)
  VALUES (new_id, 'customer', p_customer_id, p_customer_name, p_message, p_attachment_uri, (extract(epoch from now()) * 1000)::BIGINT);
  RETURN new_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_support_send_message(
  p_thread_id BIGINT,
  p_sender_role TEXT,
  p_sender_id TEXT,
  p_sender_name TEXT,
  p_body TEXT,
  p_attachment_uri TEXT DEFAULT NULL,
  p_reply_to_id BIGINT DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  t RECORD;
  v_actor_role TEXT := public.current_app_role();
  v_sender_role TEXT := LOWER(TRIM(COALESCE(p_sender_role, '')));
BEGIN
  SELECT * INTO t FROM support_threads WHERE id = p_thread_id;
  IF t.id IS NULL THEN RETURN false; END IF;

  IF v_sender_role = 'admin' THEN
    IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
      RETURN false;
    END IF;
    UPDATE support_threads SET admin_reply_count = admin_reply_count + 1, unread_by_customer = unread_by_customer + 1,
      last_message = p_body, updated_at_millis = (extract(epoch from now()) * 1000)::BIGINT WHERE id = p_thread_id;
  ELSE
    IF v_actor_role <> 'CUSTOMER' THEN
      RETURN false;
    END IF;
    IF NOT public.is_same_request_user(p_sender_id) THEN
      RETURN false;
    END IF;
    IF t.customer_id IS NULL OR t.customer_id <> p_sender_id THEN
      RETURN false;
    END IF;
    UPDATE support_threads SET unread_by_admin = unread_by_admin + 1,
      last_message = p_body, updated_at_millis = (extract(epoch from now()) * 1000)::BIGINT WHERE id = p_thread_id;
  END IF;
  INSERT INTO support_messages (thread_id, sender_role, sender_id, sender_name, body, attachment_uri, reply_to_message_id, created_at_millis)
  VALUES (p_thread_id, v_sender_role, p_sender_id, p_sender_name, p_body, p_attachment_uri, p_reply_to_id, (extract(epoch from now()) * 1000)::BIGINT);
  RETURN true;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_support_close_thread(p_thread_id BIGINT)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  IF public.current_app_role() NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN false;
  END IF;
  -- Deleting the thread removes all messages via ON DELETE CASCADE.
  DELETE FROM support_threads WHERE id = p_thread_id;
  RETURN FOUND;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_support_mark_read_customer(p_thread_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_actor_user_id TEXT := public.current_app_user_id();
BEGIN
  IF v_actor_role IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    UPDATE support_threads SET unread_by_customer = 0 WHERE id = p_thread_id;
    RETURN;
  END IF;
  IF v_actor_role = 'CUSTOMER' AND v_actor_user_id IS NOT NULL THEN
    UPDATE support_threads
    SET unread_by_customer = 0
    WHERE id = p_thread_id
      AND customer_id = v_actor_user_id;
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_support_mark_read_admin(p_thread_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  IF public.current_app_role() NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN;
  END IF;
  UPDATE support_threads SET unread_by_admin = 0 WHERE id = p_thread_id;
END;
$$;

-- قائمة خيوط الشكاوى للعميل
CREATE OR REPLACE FUNCTION public.api_support_threads_for_customer(p_customer_id TEXT)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
BEGIN
  IF p_customer_id IS NULL OR TRIM(p_customer_id) = '' THEN
    RETURN '[]'::json;
  END IF;
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE')
     AND NOT public.is_same_request_user(p_customer_id) THEN
    RETURN '[]'::json;
  END IF;

  RETURN COALESCE(
    (SELECT json_agg(json_build_object(
      'id', id, 'complaintNo', complaint_no, 'customerId', customer_id, 'customerName', customer_name,
      'title', title, 'createdAtMillis', created_at_millis, 'updatedAtMillis', updated_at_millis,
      'lastMessage', last_message, 'adminReplyCount', admin_reply_count,
      'unreadByCustomerCount', unread_by_customer, 'unreadByAdminCount', unread_by_admin
    ) ORDER BY updated_at_millis DESC)
    FROM support_threads WHERE customer_id = p_customer_id),
    '[]'::json
  );
END;
$$;

-- قائمة كل الخيوط (للأدمن)
CREATE OR REPLACE FUNCTION public.api_support_all_threads()
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  IF public.current_app_role() NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN '[]'::json;
  END IF;
  RETURN COALESCE(
    (SELECT json_agg(json_build_object(
      'id', id, 'complaintNo', complaint_no, 'customerId', customer_id, 'customerName', customer_name,
      'title', title, 'createdAtMillis', created_at_millis, 'updatedAtMillis', updated_at_millis,
      'lastMessage', last_message, 'adminReplyCount', admin_reply_count,
      'unreadByCustomerCount', unread_by_customer, 'unreadByAdminCount', unread_by_admin
    ) ORDER BY updated_at_millis DESC)
    FROM support_threads),
    '[]'::json
  );
END;
$$;

-- رسائل خيط معين
CREATE OR REPLACE FUNCTION public.api_support_messages(p_thread_id BIGINT)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_actor_user_id TEXT := public.current_app_user_id();
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    IF v_actor_role <> 'CUSTOMER' OR v_actor_user_id IS NULL THEN
      RETURN '[]'::json;
    END IF;
    IF NOT EXISTS (
      SELECT 1
      FROM support_threads st
      WHERE st.id = p_thread_id
        AND st.customer_id = v_actor_user_id
    ) THEN
      RETURN '[]'::json;
    END IF;
  END IF;

  RETURN COALESCE(
    (SELECT json_agg(json_build_object(
      'id', id, 'threadId', thread_id, 'senderRole', sender_role, 'senderId', sender_id,
      'senderName', sender_name, 'body', body, 'attachmentUri', attachment_uri,
      'replyToMessageId', reply_to_message_id, 'createdAtMillis', created_at_millis
    ) ORDER BY created_at_millis)
    FROM support_messages WHERE thread_id = p_thread_id),
    '[]'::json
  );
END;
$$;

-- ========= Push Notifications (FCM عبر Backend فقط) =========

-- محاولة تفعيل pg_net (مطلوب لإرسال HTTP من داخل Postgres إلى FCM).
DO $$
BEGIN
  CREATE EXTENSION IF NOT EXISTS pg_net;
EXCEPTION WHEN OTHERS THEN
  -- قد لا يتوفر الامتداد في بعض البيئات؛ الدوال أدناه ستكمل بدون كسر السكيما.
  NULL;
END;
$$;

-- إرسال Push لكل توكنات مستخدم محدد (يستخدم FCM legacy server key من app_settings).

-- Remove the legacy overload without p_image_url. If it remains in an upgraded
-- database, named-argument calls that omit p_image_url become ambiguous.
DROP FUNCTION IF EXISTS public.api_push_fcm_to_user_tokens(
  TEXT,
  TEXT,
  TEXT,
  TEXT,
  TEXT,
  TEXT
);

CREATE OR REPLACE FUNCTION public.api_push_fcm_to_user_tokens(
  p_user_id TEXT,
  p_title TEXT,
  p_message TEXT,
  p_order_id TEXT DEFAULT NULL,
  p_order_type TEXT DEFAULT NULL,
  p_status TEXT DEFAULT NULL,
  p_image_url TEXT DEFAULT NULL
)
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_push_enabled BOOLEAN := false;
  v_server_key TEXT;
  v_push_mode TEXT := 'EDGE_V1';
  v_notification_id TEXT := NULLIF(
    TRIM(COALESCE(current_setting('app.current_notification_id', true), '')),
    ''
  );
  v_headers JSONB;
  v_payload JSONB;
  v_token TEXT;
  v_sent INT := 0;
BEGIN
  IF p_user_id IS NULL OR TRIM(p_user_id) = '' THEN
    RETURN 0;
  END IF;

  SELECT
    COALESCE(push_enabled, true),
    NULLIF(TRIM(COALESCE(fcm_server_key, '')), ''),
    COALESCE(NULLIF(TRIM(COALESCE(push_provider, '')), ''), 'EDGE_V1')
  INTO v_push_enabled, v_server_key, v_push_mode
  FROM public.app_settings
  WHERE id = 1
  LIMIT 1;

  -- Log diagnostic info if push is disabled
  IF NOT COALESCE(v_push_enabled, true) THEN
    INSERT INTO public.push_delivery_logs(target, error_msg, response_body)
    VALUES (
      p_user_id,
      'push_disabled_globally',
      jsonb_build_object('push_enabled', v_push_enabled)::TEXT
    );
    RETURN 0;
  END IF;
  v_push_mode := UPPER(COALESCE(v_push_mode, 'EDGE_V1'));
  IF v_push_mode = 'LEGACY' AND (v_server_key IS NULL OR v_server_key = '') THEN
    RETURN 0;
  END IF;

  IF v_push_mode = 'LEGACY' THEN
    v_headers := jsonb_build_object(
      'Content-Type', 'application/json',
      'Authorization', 'key=' || v_server_key
    );
  ELSE
    v_headers := jsonb_build_object(
      'Content-Type', 'application/json'
    );
  END IF;

  FOR v_token IN
    SELECT DISTINCT
      t.fcm_token
    FROM public.user_push_tokens t
    WHERE t.user_id = p_user_id
      AND NULLIF(TRIM(COALESCE(t.fcm_token, '')), '') IS NOT NULL
  LOOP
    v_payload := jsonb_build_object(
       'to', v_token,
       'data_only', false,
       'priority', 'high',
       'content_available', true,
       'mutable_content', true,
       'notification', jsonb_strip_nulls(jsonb_build_object(
         'title', NULLIF(TRIM(COALESCE(p_title, '')), ''),
         'body', NULLIF(TRIM(COALESCE(p_message, '')), ''),
         'imageUrl', COALESCE(NULLIF(TRIM(COALESCE(p_image_url, '')), ''), NULLIF(TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')), ''))
       )),
       'data', jsonb_strip_nulls(jsonb_build_object(
         'notification_id', v_notification_id,
         'order_id', NULLIF(TRIM(COALESCE(p_order_id, '')), ''),
         'order_type', NULLIF(TRIM(COALESCE(p_order_type, '')), ''),
         'status', NULLIF(TRIM(COALESCE(p_status, '')), ''),
         'title', COALESCE(p_title, ''),
         'body', COALESCE(p_message, ''),
         'image_url', COALESCE(NULLIF(TRIM(COALESCE(p_image_url, '')), ''), NULLIF(TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')), ''))
       ))
     );

    IF public.api_push_http_post_fcm(
      p_headers := v_headers,
      p_payload := v_payload
    ) THEN
      v_sent := v_sent + 1;
    END IF;
  END LOOP;

  RETURN v_sent;
END;
$$;

-- Remove the legacy overload without p_image_url for the same reason.
DROP FUNCTION IF EXISTS public.api_push_fcm_to_role_tokens(
  TEXT,
  TEXT,
  TEXT,
  TEXT,
  TEXT,
  TEXT
);

-- إرسال Push حسب role_target (مثال: DRIVER / DELIVERY -> كل السائقين النشطين).
CREATE OR REPLACE FUNCTION public.api_push_fcm_to_role_tokens(
  p_role_target TEXT,
  p_title TEXT,
  p_message TEXT,
  p_order_id TEXT DEFAULT NULL,
  p_order_type TEXT DEFAULT NULL,
  p_status TEXT DEFAULT NULL,
  p_image_url TEXT DEFAULT NULL
)
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_push_enabled BOOLEAN := false;
  v_server_key TEXT;
  v_push_mode TEXT := 'EDGE_V1';
  v_notification_id TEXT := NULLIF(
    TRIM(COALESCE(current_setting('app.current_notification_id', true), '')),
    ''
  );
  v_headers JSONB;
  v_payload JSONB;
  v_token TEXT;
  v_token_role TEXT;
  v_sent INT := 0;
  v_role TEXT := UPPER(TRIM(COALESCE(p_role_target, '')));
BEGIN
  IF v_role = '' THEN
    RETURN 0;
  END IF;

  SELECT
    COALESCE(push_enabled, true),
    NULLIF(TRIM(COALESCE(fcm_server_key, '')), ''),
    COALESCE(NULLIF(TRIM(COALESCE(push_provider, '')), ''), 'EDGE_V1')
  INTO v_push_enabled, v_server_key, v_push_mode
  FROM public.app_settings
  WHERE id = 1
  LIMIT 1;

  -- Log diagnostic info if push is disabled
  IF NOT COALESCE(v_push_enabled, true) THEN
    INSERT INTO public.push_delivery_logs(target, error_msg, response_body)
    VALUES (
      p_role,
      'push_disabled_globally',
      jsonb_build_object('push_enabled', v_push_enabled)::TEXT
    );
    RETURN 0;
  END IF;
  v_push_mode := UPPER(COALESCE(v_push_mode, 'EDGE_V1'));
  IF v_push_mode = 'LEGACY' AND (v_server_key IS NULL OR v_server_key = '') THEN
    RETURN 0;
  END IF;

  IF v_push_mode = 'LEGACY' THEN
    v_headers := jsonb_build_object(
      'Content-Type', 'application/json',
      'Authorization', 'key=' || v_server_key
    );
  ELSE
    v_headers := jsonb_build_object(
      'Content-Type', 'application/json'
    );
  END IF;

  FOR v_token, v_token_role IN
    SELECT DISTINCT
      t.fcm_token,
      UPPER(COALESCE(u.role, 'CUSTOMER'))
    FROM public.user_push_tokens t
    JOIN public.users u ON u.id = t.user_id
    WHERE u.is_active = true
      AND NULLIF(TRIM(COALESCE(t.fcm_token, '')), '') IS NOT NULL
      AND (
        (v_role IN ('DELIVERY', 'DRIVER') AND UPPER(COALESCE(u.role, '')) IN ('DRIVER', 'ADMIN_POS'))
        OR (v_role = 'CASHIER' AND UPPER(COALESCE(u.role, '')) IN ('CASHIER', 'ADMIN_POS'))
        OR (v_role NOT IN ('DELIVERY', 'DRIVER', 'CASHIER') AND UPPER(COALESCE(u.role, '')) = v_role)
      )
  LOOP
    v_payload := jsonb_build_object(
      'to', v_token,
      'data_only', false,
      'priority', 'high',
      'content_available', true,
      'mutable_content', true,
      'notification', jsonb_strip_nulls(jsonb_build_object(
        'title', NULLIF(TRIM(COALESCE(p_title, '')), ''),
        'body', NULLIF(TRIM(COALESCE(p_message, '')), ''),
        'imageUrl', COALESCE(NULLIF(TRIM(COALESCE(p_image_url, '')), ''), NULLIF(TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')), ''))
      )),
      'data', jsonb_strip_nulls(jsonb_build_object(
        'notification_id', v_notification_id,
        'order_id', NULLIF(TRIM(COALESCE(p_order_id, '')), ''),
        'order_type', NULLIF(TRIM(COALESCE(p_order_type, '')), ''),
        'status', NULLIF(TRIM(COALESCE(p_status, '')), ''),
        'title', COALESCE(p_title, ''),
        'body', COALESCE(p_message, ''),
        'image_url', COALESCE(NULLIF(TRIM(COALESCE(p_image_url, '')), ''), NULLIF(TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')), '')),
        'user_role', COALESCE(v_token_role, 'CUSTOMER')
      ))
    );
    IF public.api_push_http_post_fcm(
      p_headers := v_headers,
      p_payload := v_payload
    ) THEN
      v_sent := v_sent + 1;
    END IF;
  END LOOP;

  RETURN v_sent;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_push_fcm_to_topic(
  p_topic TEXT,
  p_title TEXT,
  p_message TEXT,
  p_order_id TEXT DEFAULT NULL,
  p_order_type TEXT DEFAULT NULL,
  p_status TEXT DEFAULT NULL
)
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_push_enabled BOOLEAN := false;
  v_server_key TEXT;
  v_push_mode TEXT := 'EDGE_V1';
  v_notification_id TEXT := NULLIF(
    TRIM(COALESCE(current_setting('app.current_notification_id', true), '')),
    ''
  );
  v_headers JSONB;
  v_payload JSONB;
  v_topic TEXT;
BEGIN
  v_topic := NULLIF(TRIM(COALESCE(p_topic, '')), '');
  IF v_topic IS NULL THEN
    RETURN 0;
  END IF;

  v_topic := regexp_replace(v_topic, '[^A-Za-z0-9\-_.~%]', '_', 'g');
  IF v_topic = '' THEN
    RETURN 0;
  END IF;

  SELECT
    COALESCE(push_enabled, true),
    NULLIF(TRIM(COALESCE(fcm_server_key, '')), ''),
    COALESCE(NULLIF(TRIM(COALESCE(push_provider, '')), ''), 'EDGE_V1')
  INTO v_push_enabled, v_server_key, v_push_mode
  FROM public.app_settings
  WHERE id = 1
  LIMIT 1;

  -- Log diagnostic info if push is disabled
  IF NOT COALESCE(v_push_enabled, true) THEN
    INSERT INTO public.push_delivery_logs(target, error_msg, response_body)
    VALUES (
      p_topic,
      'push_disabled_globally',
      jsonb_build_object('push_enabled', v_push_enabled)::TEXT
    );
    RETURN 0;
  END IF;
  v_push_mode := UPPER(COALESCE(v_push_mode, 'EDGE_V1'));
  IF v_push_mode = 'LEGACY' AND (v_server_key IS NULL OR v_server_key = '') THEN
    RETURN 0;
  END IF;

  IF v_push_mode = 'LEGACY' THEN
    v_headers := jsonb_build_object(
      'Content-Type', 'application/json',
      'Authorization', 'key=' || v_server_key
    );
  ELSE
    v_headers := jsonb_build_object(
      'Content-Type', 'application/json'
    );
  END IF;

  v_payload := jsonb_build_object(
    'to', '/topics/' || v_topic,
    'data_only', false,
    'priority', 'high',
    'content_available', true,
    'notification', jsonb_strip_nulls(jsonb_build_object(
      'title', NULLIF(TRIM(COALESCE(p_title, '')), ''),
      'body', NULLIF(TRIM(COALESCE(p_message, '')), ''),
      'image', NULLIF(TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')), '')
    )),
    'data', jsonb_strip_nulls(jsonb_build_object(
      'notification_id', v_notification_id,
      'order_id', NULLIF(TRIM(COALESCE(p_order_id, '')), ''),
      'order_type', NULLIF(TRIM(COALESCE(p_order_type, '')), ''),
      'status', NULLIF(TRIM(COALESCE(p_status, '')), ''),
      'title', COALESCE(p_title, ''),
      'body', COALESCE(p_message, ''),
      'image_url', NULLIF(TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')), ''),
      'user_role', CASE WHEN v_topic LIKE 'role_%' THEN UPPER(SUBSTRING(v_topic FROM 6)) ELSE 'CUSTOMER' END
    ))
  );

  IF public.api_push_http_post_fcm(
    p_headers := v_headers,
    p_payload := v_payload
  ) THEN
    RETURN 1;
  END IF;
  RETURN 0;
END;
$$;



CREATE OR REPLACE FUNCTION public.api_push_collect_recent_responses(
  p_limit INT DEFAULT 100
)
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  r RECORD;
  v_status INT;
  v_body TEXT;
  v_updated INT := 0;
  v_unregistered BOOLEAN := false;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN 0;
  END IF;

  FOR r IN
    SELECT l.id, l.request_id, COALESCE(l.target, '') AS target
    FROM public.push_delivery_logs l
    WHERE l.request_id IS NOT NULL
      AND (
        l.status_code IS NULL
        OR (
          l.status_code = 202
          AND COALESCE(l.error_msg, '') = 'queued_async_pending'
        )
      )
    ORDER BY l.id DESC
    LIMIT GREATEST(COALESCE(p_limit, 100), 1)
  LOOP
    BEGIN
      EXECUTE 'SELECT status_code, content::text FROM net._http_response WHERE id = $1'
      INTO v_status, v_body
      USING r.request_id;
    EXCEPTION WHEN OTHERS THEN
      v_status := NULL;
      v_body := NULL;
    END;

    IF v_status IS NULL THEN
      CONTINUE;
    END IF;

    v_unregistered := (
      v_status = 404
      AND COALESCE(v_body, '') ILIKE '%UNREGISTERED%'
      AND LEFT(r.target, 8) <> '/topics/'
      AND POSITION(':' IN r.target) > 0
    );

    IF v_unregistered THEN
      DELETE FROM public.user_push_tokens
      WHERE fcm_token = r.target;
    END IF;

    UPDATE public.push_delivery_logs
    SET
      status_code = v_status,
      error_msg = CASE
        WHEN v_status BETWEEN 200 AND 299 THEN NULL
        WHEN v_unregistered THEN 'unregistered_token'
        ELSE 'http_' || v_status::TEXT
      END,
      response_body = CASE
        WHEN v_body IS NULL OR TRIM(v_body) = '' THEN response_body
        ELSE LEFT(v_body, 4000)
      END
    WHERE id = r.id
      AND (
        status_code IS NULL
        OR (
          status_code = 202
          AND COALESCE(error_msg, '') = 'queued_async_pending'
        )
      );

    IF FOUND THEN
      v_updated := v_updated + 1;
    END IF;
  END LOOP;

  RETURN v_updated;
END;
$$;

-- إدراج إشعار داخل جدول notifications + إرسال Push (إن توفر user_id).
CREATE OR REPLACE FUNCTION public.api_emit_order_notification(
  p_user_id TEXT,
  p_role_target TEXT,
  p_order_id TEXT,
  p_order_type TEXT,
  p_title TEXT,
  p_message TEXT,
  p_status TEXT DEFAULT NULL
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_user_id TEXT := NULLIF(TRIM(COALESCE(p_user_id, '')), '');
  v_role TEXT := UPPER(TRIM(COALESCE(p_role_target, '')));
  v_notification_id TEXT;
  v_user_tokens INT := 0;
  v_user_sent INT := 0;
  v_user_topic_sent INT := 0;
  v_role_sent INT := 0;
  v_user_topic TEXT;
  v_role_topic TEXT;
  v_recent_duplicate_exists BOOLEAN := false;
BEGIN
  IF v_role = 'CUSTOMER' AND v_user_id IS NULL THEN
    RETURN;
  END IF;

  SELECT EXISTS (
    SELECT 1
    FROM public.notifications n
    WHERE COALESCE(n.user_id, '') = COALESCE(v_user_id, '')
      AND UPPER(COALESCE(n.role_target, '')) = COALESCE(v_role, '')
      AND COALESCE(n.order_id, '') = COALESCE(NULLIF(TRIM(COALESCE(p_order_id, '')), ''), '')
      AND COALESCE(n.order_type, '') = COALESCE(NULLIF(TRIM(COALESCE(p_order_type, '')), ''), '')
      AND COALESCE(n.title, '') = COALESCE(p_title, '')
      AND COALESCE(n.message, '') = COALESCE(p_message, '')
      AND n.created_at >= now() - interval '90 seconds'
  )
  INTO v_recent_duplicate_exists;

  IF COALESCE(v_recent_duplicate_exists, false) THEN
    RETURN;
  END IF;

  BEGIN
    INSERT INTO public.notifications (user_id, role_target, order_id, order_type, title, message, read, created_at)
    VALUES (
      v_user_id,
      COALESCE(NULLIF(TRIM(COALESCE(p_role_target, '')), ''), 'CUSTOMER'),
      NULLIF(TRIM(COALESCE(p_order_id, '')), ''),
      NULLIF(TRIM(COALESCE(p_order_type, '')), ''),
      COALESCE(p_title, ''),
      COALESCE(p_message, ''),
      false,
      now()
    )
    RETURNING id::TEXT INTO v_notification_id;
  EXCEPTION WHEN OTHERS THEN
    NULL;
  END;

  PERFORM set_config(
    'app.current_notification_id',
    COALESCE(v_notification_id, ''),
    true
  );

  IF v_user_id IS NOT NULL THEN
    v_user_topic := 'u_' || regexp_replace(
      v_user_id,
      '[^A-Za-z0-9\-_.~%]',
      '_',
      'g'
    );

    SELECT COUNT(*)
    INTO v_user_tokens
    FROM public.user_push_tokens t
    WHERE t.user_id = v_user_id
      AND NULLIF(TRIM(COALESCE(t.fcm_token, '')), '') IS NOT NULL;

    IF v_user_tokens > 0 THEN
      BEGIN
        v_user_sent := COALESCE(public.api_push_fcm_to_user_tokens(
          p_user_id := v_user_id,
          p_title := p_title,
          p_message := p_message,
          p_order_id := p_order_id,
          p_order_type := p_order_type,
          p_status := p_status,
          p_image_url := NULLIF(
            TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')),
            ''
          )
        ), 0);
        
        -- ✅ FIX: Log if push failed
        IF v_user_sent = 0 THEN
          INSERT INTO public.push_delivery_logs(target, error_msg, response_body)
          VALUES (
            v_user_id,
            'user_push_dispatch_failed_zero_sent',
            jsonb_build_object('user_id', v_user_id, 'tokens_count', v_user_tokens)::TEXT
          );
        END IF;
      EXCEPTION
      WHEN query_canceled THEN
        v_user_sent := 0;
        INSERT INTO public.push_delivery_logs(target, error_msg)
        VALUES (v_user_id, 'user_push_query_canceled');
      WHEN OTHERS THEN
        v_user_sent := 0;
        INSERT INTO public.push_delivery_logs(target, error_msg, response_body)
        VALUES (v_user_id, 'user_push_exception: ' || SQLSTATE, SQLERRM);
      END;
    END IF;

    IF v_role = 'CUSTOMER'
       AND v_user_sent = 0
       AND v_user_topic IS NOT NULL
       AND v_user_topic <> '' THEN
      BEGIN
        v_user_topic_sent := COALESCE(public.api_push_fcm_to_topic(
          p_topic := v_user_topic,
          p_title := p_title,
          p_message := p_message,
          p_order_id := p_order_id,
          p_order_type := p_order_type,
          p_status := p_status
        ), 0);
      EXCEPTION
      WHEN query_canceled THEN
        v_user_topic_sent := 0;
      WHEN OTHERS THEN
        v_user_topic_sent := 0;
      END;
    END IF;
  END IF;

  IF v_role <> '' AND v_user_id IS NULL THEN
    BEGIN
      -- Primary path: send to role tokens from DB (no dependency on topic subscriptions).
      v_role_sent := COALESCE(public.api_push_fcm_to_role_tokens(
        p_role_target := v_role,
        p_title := p_title,
        p_message := p_message,
        p_order_id := p_order_id,
        p_order_type := p_order_type,
        p_status := p_status,
        p_image_url := NULLIF(
          TRIM(COALESCE(current_setting('app.current_notification_image_url', true), '')),
          ''
        )
      ), 0);
      
      -- ✅ FIX: Log if role push failed
      IF v_role_sent = 0 THEN
        INSERT INTO public.push_delivery_logs(target, error_msg, response_body)
        VALUES (
          v_role,
          'role_push_dispatch_failed_zero_sent',
          jsonb_build_object('role', v_role, 'order_id', p_order_id)::TEXT
        );
      END IF;
    EXCEPTION
    WHEN query_canceled THEN
      v_role_sent := 0;
      INSERT INTO public.push_delivery_logs(target, error_msg)
      VALUES (v_role, 'role_push_query_canceled');
    WHEN OTHERS THEN
      v_role_sent := 0;
      INSERT INTO public.push_delivery_logs(target, error_msg, response_body)
      VALUES (v_role, 'role_push_exception: ' || SQLSTATE, SQLERRM);
    END;

    IF v_role_sent = 0 AND v_role IN ('DRIVER', 'DELIVERY', 'CASHIER', 'ADMIN', 'ADMIN_POS') THEN
      v_role_topic := 'role_' || CASE
        WHEN v_role = 'DELIVERY' THEN 'DRIVER'
        ELSE v_role
      END;

      BEGIN
        PERFORM public.api_push_fcm_to_topic(
          p_topic := v_role_topic,
          p_title := p_title,
          p_message := p_message,
          p_order_id := p_order_id,
          p_order_type := p_order_type,
          p_status := p_status
        );
      EXCEPTION
      WHEN query_canceled THEN
        NULL;
      WHEN OTHERS THEN
        NULL;
      END;
    END IF;
  END IF;
EXCEPTION
WHEN query_canceled THEN
  NULL;
WHEN OTHERS THEN
  NULL;
END;
$$;

-- -----------------------------------------------------------------------------
-- App orders RPCs used by POS (status update + driver assignment)
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.api_create_app_order_with_inventory(
  p_payload JSONB DEFAULT '{}'::jsonb
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_payload JSONB := COALESCE(p_payload, '{}'::jsonb);
  v_metadata JSONB := COALESCE(v_payload->'metadata', '{}'::jsonb);
  v_items JSONB := COALESCE(v_payload->'items', v_metadata->'items', '[]'::jsonb);
  v_actor_role TEXT := public.current_app_role();
  v_actor_user_id TEXT := public.current_app_user_id();
  v_order_id TEXT := NULLIF(TRIM(COALESCE(v_payload->>'id', '')), '');
  v_order_type TEXT := UPPER(NULLIF(TRIM(COALESCE(v_payload->>'order_type', '')), ''));
  v_status TEXT := UPPER(NULLIF(TRIM(COALESCE(v_payload->>'status', '')), ''));
  v_customer_user_id TEXT := NULLIF(TRIM(COALESCE(v_payload->>'customer_user_id', '')), '');
  v_customer_name TEXT := NULLIF(TRIM(COALESCE(v_payload->>'customer_name', '')), '');
  v_customer_phone TEXT := NULLIF(TRIM(COALESCE(v_payload->>'customer_phone', '')), '');
  v_district TEXT := NULLIF(TRIM(COALESCE(v_payload->>'district', '')), '');
  v_address_text TEXT := NULLIF(TRIM(COALESCE(v_payload->>'address_text', '')), '');
  v_pos_address_id TEXT := NULLIF(TRIM(COALESCE(v_payload->>'pos_address_id', '')), '');
  v_mobile_address_id TEXT := NULLIF(TRIM(COALESCE(v_payload->>'mobile_address_id', '')), '');
  v_driver_user_id TEXT := NULLIF(TRIM(COALESCE(v_payload->>'driver_user_id', '')), '');
  v_subtotal NUMERIC := 0;
  v_delivery_fee NUMERIC := 0;
  v_total NUMERIC := 0;
  v_priced_subtotal NUMERIC := 0;
  v_priced_delivery_fee NUMERIC := 0;
  v_priced_discount NUMERIC := 0;
  v_item_index INT := 0;
  v_item JSONB;
  v_line_qty INT;
  v_product_id TEXT;
  v_source_product_id TEXT;
  v_option_id TEXT;
  v_line_base_price NUMERIC := 0;
  v_line_options_price NUMERIC := 0;
  v_line_unit_price NUMERIC := 0;
  v_address_id_text TEXT := NULLIF(TRIM(COALESCE(v_metadata->>'addressId', '')), '');
  v_district_id TEXT := NULLIF(TRIM(COALESCE(v_metadata->>'districtId', '')), '');
  v_shortages JSONB;
  v_unavailable_items JSONB;
  v_coupon_id TEXT := NULLIF(TRIM(COALESCE(v_metadata->>'couponId', '')), '');
  v_coupon_code TEXT := UPPER(NULLIF(TRIM(COALESCE(v_metadata->>'couponCode', '')), ''));
  v_coupon_record RECORD;
  v_coupon_used_count INT := 0;
  v_coupon_used_customers INT := 0;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'CUSTOMER', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'code', 'forbidden');
  END IF;

  IF v_order_type IS NULL THEN
    v_order_type := 'DELIVERY';
  END IF;
  IF v_status IS NULL THEN
    v_status := 'RECEIVED';
  END IF;

  IF v_actor_role = 'CUSTOMER' THEN
    IF v_actor_user_id IS NULL THEN
      RETURN jsonb_build_object('ok', false, 'code', 'forbidden');
    END IF;
    IF v_customer_user_id IS NULL THEN
      v_customer_user_id := v_actor_user_id;
    END IF;
    IF v_customer_user_id <> v_actor_user_id THEN
      RETURN jsonb_build_object('ok', false, 'code', 'forbidden');
    END IF;
  END IF;

  IF jsonb_typeof(v_items) IS DISTINCT FROM 'array' THEN
    RETURN jsonb_build_object('ok', false, 'code', 'invalid_items');
  END IF;

  IF v_order_id IS NULL THEN
    v_order_id := NULLIF(TRIM(COALESCE(v_metadata->>'localOrderId', '')), '');
  END IF;
  IF v_order_id IS NULL THEN
    v_order_id := 'ord_' || REPLACE(extensions.gen_random_uuid()::TEXT, '-', '');
  END IF;

  IF EXISTS (SELECT 1 FROM public.app_orders WHERE id = v_order_id) THEN
    RETURN jsonb_build_object('ok', true, 'orderId', v_order_id, 'existing', true);
  END IF;

  CREATE TEMP TABLE IF NOT EXISTS pg_temp.tmp_inventory_needs (
    inventory_item_id TEXT NOT NULL,
    source_kind TEXT NOT NULL,
    source_id TEXT NOT NULL,
    required_qty NUMERIC NOT NULL
  ) ON COMMIT DROP;
  TRUNCATE pg_temp.tmp_inventory_needs;

  CREATE TEMP TABLE IF NOT EXISTS pg_temp.tmp_order_line_products (
    product_id TEXT,
    source_product_id TEXT,
    item_name TEXT,
    qty INT NOT NULL DEFAULT 1
  ) ON COMMIT DROP;
  TRUNCATE pg_temp.tmp_order_line_products;

  CREATE TEMP TABLE IF NOT EXISTS pg_temp.tmp_order_line_options (
    option_id TEXT PRIMARY KEY
  ) ON COMMIT DROP;
  TRUNCATE pg_temp.tmp_order_line_options;

  CREATE TEMP TABLE IF NOT EXISTS pg_temp.tmp_order_line_prices (
    item_index INT PRIMARY KEY,
    unit_price NUMERIC NOT NULL DEFAULT 0
  ) ON COMMIT DROP;
  TRUNCATE pg_temp.tmp_order_line_prices;

  FOR v_item_index, v_item IN
    SELECT ordinality::INT, value
    FROM jsonb_array_elements(v_items) WITH ORDINALITY
  LOOP
    v_line_qty := GREATEST(COALESCE((v_item->>'qty')::INT, 1), 1);
    v_product_id := NULLIF(TRIM(COALESCE(v_item->>'productId', '')), '');
    v_source_product_id := NULLIF(TRIM(COALESCE(v_item->>'sourceProductId', '')), '');

    IF v_product_id IS NOT NULL THEN
      SELECT ap.source_product_id
      INTO v_source_product_id
      FROM public.app_products ap
      WHERE ap.id = v_product_id
      LIMIT 1;

      IF v_source_product_id IS NULL THEN
        SELECT pp.source_product_id
        INTO v_source_product_id
        FROM public.pos_products pp
        WHERE pp.id = v_product_id
        LIMIT 1;
      END IF;
    END IF;

    IF v_source_product_id IS NULL AND v_product_id IS NOT NULL THEN
      IF v_source_product_id IS NULL THEN
        v_source_product_id := v_product_id;
      END IF;
    END IF;

    INSERT INTO pg_temp.tmp_order_line_products (
      product_id,
      source_product_id,
      item_name,
      qty
    )
    VALUES (
      v_product_id,
      v_source_product_id,
      NULLIF(TRIM(COALESCE(v_item->>'name', '')), ''),
      v_line_qty
    );

    IF v_source_product_id IS NOT NULL THEN
      INSERT INTO pg_temp.tmp_inventory_needs (inventory_item_id, source_kind, source_id, required_qty)
      SELECT
        l.inventory_item_id,
        'SOURCE_PRODUCT',
        v_source_product_id,
        l.consumption_quantity * v_line_qty
      FROM public.inventory_item_links l
      WHERE l.entity_kind = 'SOURCE_PRODUCT'
        AND l.entity_id = v_source_product_id;
    END IF;

    FOR v_option_id IN
      SELECT DISTINCT option_id
      FROM (
        SELECT NULLIF(TRIM(COALESCE(v_item->>'breadId', '')), '') AS option_id
        UNION ALL
        SELECT NULLIF(TRIM(value), '')
        FROM jsonb_array_elements_text(COALESCE(v_item->'sauceIds', '[]'::jsonb))
        UNION ALL
        SELECT NULLIF(TRIM(value), '')
        FROM jsonb_array_elements_text(COALESCE(v_item->'optionIds', '[]'::jsonb))
        UNION ALL
        SELECT NULLIF(TRIM(value), '')
        FROM jsonb_array_elements_text(COALESCE(v_item->'fixedOptionIds', '[]'::jsonb))
      ) option_rows
      WHERE option_id IS NOT NULL
    LOOP
      INSERT INTO pg_temp.tmp_order_line_options (option_id)
      VALUES (v_option_id)
      ON CONFLICT (option_id) DO NOTHING;

      INSERT INTO pg_temp.tmp_inventory_needs (inventory_item_id, source_kind, source_id, required_qty)
      SELECT
        l.inventory_item_id,
        'ADDON_OPTION',
        v_option_id,
        l.consumption_quantity * v_line_qty
      FROM public.inventory_item_links l
      WHERE l.entity_kind = 'ADDON_OPTION'
        AND l.entity_id = v_option_id;
    END LOOP;

    SELECT COALESCE(sp.price, 0)
    INTO v_line_base_price
    FROM public.source_products sp
    WHERE sp.id = v_source_product_id
    LIMIT 1;

    SELECT COALESCE(SUM(o.price), 0)
    INTO v_line_options_price
    FROM public.addon_options o
    WHERE o.id IN (
      SELECT DISTINCT option_id
      FROM (
        SELECT NULLIF(TRIM(COALESCE(v_item->>'breadId', '')), '') AS option_id
        UNION ALL
        SELECT NULLIF(TRIM(value), '')
        FROM jsonb_array_elements_text(COALESCE(v_item->'sauceIds', '[]'::jsonb))
        UNION ALL
        SELECT NULLIF(TRIM(value), '')
        FROM jsonb_array_elements_text(COALESCE(v_item->'optionIds', '[]'::jsonb))
        UNION ALL
        SELECT NULLIF(TRIM(value), '')
        FROM jsonb_array_elements_text(COALESCE(v_item->'fixedOptionIds', '[]'::jsonb))
      ) option_rows
      WHERE option_id IS NOT NULL
    );

    v_line_unit_price := GREATEST(
      COALESCE(v_line_base_price, 0) + COALESCE(v_line_options_price, 0),
      0
    );
    v_priced_subtotal := v_priced_subtotal + (v_line_unit_price * v_line_qty);

    INSERT INTO pg_temp.tmp_order_line_prices (item_index, unit_price)
    VALUES (v_item_index, v_line_unit_price)
    ON CONFLICT (item_index) DO UPDATE
    SET unit_price = EXCLUDED.unit_price;
  END LOOP;

  PERFORM 1
  FROM public.source_products sp
  WHERE EXISTS (
    SELECT 1
    FROM pg_temp.tmp_order_line_products p
    WHERE p.source_product_id = sp.id
  )
  FOR UPDATE;

  PERFORM 1
  FROM public.app_products ap
  WHERE EXISTS (
    SELECT 1
    FROM pg_temp.tmp_order_line_products p
    WHERE p.product_id = ap.id
  )
  FOR UPDATE;

  PERFORM 1
  FROM public.addon_options ao
  WHERE EXISTS (
    SELECT 1
    FROM pg_temp.tmp_order_line_options o
    WHERE o.option_id = ao.id
  )
  FOR UPDATE;

  SELECT jsonb_agg(unavailable.item_json)
  INTO v_unavailable_items
  FROM (
    SELECT DISTINCT jsonb_build_object(
      'kind', 'product',
      'productId', p.product_id,
      'sourceProductId', p.source_product_id,
      'name', COALESCE(
        p.item_name,
        NULLIF(TRIM(COALESCE(ap.name, '')), ''),
        'منتج غير متاح'
      ),
      'reason', CASE
        WHEN v_actor_role = 'CUSTOMER' AND p.product_id IS NULL THEN 'missing_product_id'
        WHEN p.source_product_id IS NULL THEN 'missing_source_product'
        WHEN sp_src.id IS NULL THEN 'missing_source_product'
        WHEN ap.id IS NOT NULL AND COALESCE(ap.enabled, 0) <> 1 THEN 'disabled'
        WHEN public.inventory_entity_status('SOURCE_PRODUCT', p.source_product_id) = 'out_of_stock' THEN 'out_of_stock'
        ELSE 'unavailable'
      END
    ) AS item_json
    FROM pg_temp.tmp_order_line_products p
    LEFT JOIN public.app_products ap
      ON ap.id = p.product_id
    LEFT JOIN public.source_products sp_src
      ON sp_src.id = p.source_product_id
    WHERE (v_actor_role = 'CUSTOMER' AND p.product_id IS NULL)
       OR p.source_product_id IS NULL
       OR sp_src.id IS NULL
       OR (ap.id IS NOT NULL AND COALESCE(ap.enabled, 0) <> 1)
       OR public.inventory_entity_status('SOURCE_PRODUCT', p.source_product_id) = 'out_of_stock'

    UNION ALL

    SELECT DISTINCT jsonb_build_object(
      'kind', 'addon_option',
      'optionId', o.option_id,
      'name', COALESCE(NULLIF(TRIM(COALESCE(ao.name, '')), ''), 'إضافة غير متاحة'),
      'reason', CASE
        WHEN ao.id IS NULL THEN 'missing_option'
        WHEN COALESCE(ao.enabled, false) = false THEN 'disabled'
        WHEN public.inventory_entity_status('ADDON_OPTION', o.option_id) = 'out_of_stock' THEN 'out_of_stock'
        ELSE 'unavailable'
      END
    ) AS item_json
    FROM pg_temp.tmp_order_line_options o
    LEFT JOIN public.addon_options ao
      ON ao.id = o.option_id
    WHERE ao.id IS NULL
       OR COALESCE(ao.enabled, false) = false
       OR public.inventory_entity_status('ADDON_OPTION', o.option_id) = 'out_of_stock'
  ) unavailable;

  IF v_unavailable_items IS NOT NULL THEN
    RETURN jsonb_build_object(
      'ok', false,
      'code', 'items_unavailable',
      'items', v_unavailable_items
    );
  END IF;

  SELECT COALESCE(
    jsonb_agg(
      jsonb_set(
        item_rows.item_json,
        '{price}',
        to_jsonb(COALESCE(line_price.unit_price, 0)),
        true
      )
      ORDER BY item_rows.item_index
    ),
    '[]'::jsonb
  )
  INTO v_items
  FROM (
    SELECT ordinality::INT AS item_index, value AS item_json
    FROM jsonb_array_elements(v_items) WITH ORDINALITY
  ) item_rows
  LEFT JOIN pg_temp.tmp_order_line_prices line_price
    ON line_price.item_index = item_rows.item_index;

  IF v_order_type = 'DELIVERY' THEN
    IF v_address_id_text IS NOT NULL
       AND v_address_id_text ~ '^[0-9]+$' THEN
      SELECT
        ca.district_id,
        COALESCE(
          NULLIF(TRIM(COALESCE(h.name, '')), ''),
          v_district
        ),
        COALESCE(
          NULLIF(
            TRIM(
              CONCAT_WS(
                ' - ',
                CASE
                  WHEN NULLIF(TRIM(COALESCE(ca.block, '')), '') IS NOT NULL
                    THEN 'المجاورة ' || TRIM(ca.block)
                  ELSE NULL
                END,
                CASE
                  WHEN NULLIF(TRIM(COALESCE(ca.street, '')), '') IS NOT NULL
                    THEN 'الشارع ' || TRIM(ca.street)
                  ELSE NULL
                END,
                CASE
                  WHEN NULLIF(TRIM(COALESCE(ca.building, '')), '') IS NOT NULL
                    THEN 'العمارة ' || TRIM(ca.building)
                  ELSE NULL
                END,
                CASE
                  WHEN NULLIF(TRIM(COALESCE(ca.apt, '')), '') IS NOT NULL
                    THEN 'الشقة ' || TRIM(ca.apt)
                  ELSE NULL
                END,
                CASE
                  WHEN NULLIF(TRIM(COALESCE(ca.note, '')), '') IS NOT NULL
                    THEN 'ملاحظة ' || TRIM(ca.note)
                  ELSE NULL
                END
              )
            ),
            ''
          ),
          v_address_text
        )
      INTO
        v_district_id,
        v_district,
        v_address_text
      FROM public.customer_addresses ca
      LEFT JOIN public.hoods h
        ON h.id = ca.district_id
      WHERE ca.id = v_address_id_text::INT
        AND (
          v_actor_role IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE')
          OR public.is_same_request_user(ca.customer_user_id)
        )
      LIMIT 1;
    END IF;

    IF v_actor_role = 'CUSTOMER'
       AND (v_address_id_text IS NULL OR v_district_id IS NULL) THEN
      RETURN jsonb_build_object('ok', false, 'code', 'delivery_address_required');
    END IF;

    IF v_district_id IS NULL THEN
      v_district_id := NULLIF(TRIM(COALESCE(v_metadata->>'districtId', '')), '');
    END IF;

    IF v_district_id IS NOT NULL THEN
      SELECT
        COALESCE(h.fee, 0),
        COALESCE(NULLIF(TRIM(COALESCE(h.name, '')), ''), v_district)
      INTO
        v_priced_delivery_fee,
        v_district
      FROM public.hoods h
      WHERE h.id = v_district_id
      LIMIT 1;
    END IF;
  ELSE
    v_priced_delivery_fee := 0;
  END IF;

  v_priced_subtotal := GREATEST(COALESCE(v_priced_subtotal, 0), 0);
  v_priced_delivery_fee := GREATEST(COALESCE(v_priced_delivery_fee, 0), 0);

  IF v_coupon_id IS NOT NULL THEN
    SELECT
      c.id,
      c.code,
      c.enabled,
      c.free_delivery,
      c.discount_amount,
      c.discount_percent,
      c.min_order_amount,
      c.per_user_limit,
      c.max_customers
    INTO v_coupon_record
    FROM public.coupons c
    WHERE c.id = v_coupon_id
    FOR UPDATE;

    IF NOT FOUND THEN
      RETURN jsonb_build_object('ok', false, 'code', 'coupon_invalid');
    END IF;

    IF COALESCE(v_coupon_record.enabled, false) = false THEN
      RETURN jsonb_build_object('ok', false, 'code', 'coupon_unavailable');
    END IF;

    IF v_coupon_code IS NOT NULL
       AND UPPER(TRIM(COALESCE(v_coupon_record.code, ''))) <> v_coupon_code THEN
      RETURN jsonb_build_object('ok', false, 'code', 'coupon_invalid');
    END IF;

    IF v_customer_user_id IS NULL THEN
      RETURN jsonb_build_object('ok', false, 'code', 'coupon_customer_required');
    END IF;

    IF COALESCE(v_coupon_record.min_order_amount, 0) > 0
       AND v_priced_subtotal < COALESCE(v_coupon_record.min_order_amount, 0) THEN
      RETURN jsonb_build_object('ok', false, 'code', 'coupon_min_order_not_met');
    END IF;

    SELECT COUNT(DISTINCT usage_rows.usage_ref)
    INTO v_coupon_used_count
    FROM (
      SELECT COALESCE(
        NULLIF(TRIM(COALESCE(cu.order_id, '')), ''),
        'usage:' || cu.customer_id || ':' || cu.used_at::TEXT
      ) AS usage_ref
      FROM public.coupon_usage cu
      WHERE cu.coupon_id = v_coupon_id
        AND cu.customer_id = v_customer_user_id

      UNION

      SELECT o.id AS usage_ref
      FROM public.app_orders o
      WHERE o.customer_user_id = v_customer_user_id
        AND COALESCE(o.metadata->>'couponId', '') = v_coupon_id
    ) usage_rows;

    SELECT COUNT(DISTINCT customer_rows.customer_id)
    INTO v_coupon_used_customers
    FROM (
      SELECT cu.customer_id
      FROM public.coupon_usage cu
      WHERE cu.coupon_id = v_coupon_id

      UNION

      SELECT o.customer_user_id AS customer_id
      FROM public.app_orders o
      WHERE COALESCE(o.metadata->>'couponId', '') = v_coupon_id
        AND NULLIF(TRIM(COALESCE(o.customer_user_id, '')), '') IS NOT NULL
    ) customer_rows;

    IF COALESCE(v_coupon_record.per_user_limit, 0) > 0
       AND v_coupon_used_count >= COALESCE(v_coupon_record.per_user_limit, 0) THEN
      RETURN jsonb_build_object('ok', false, 'code', 'coupon_per_user_limit_reached');
    END IF;

    IF COALESCE(v_coupon_record.max_customers, 0) > 0
       AND v_coupon_used_count = 0
       AND v_coupon_used_customers >= COALESCE(v_coupon_record.max_customers, 0) THEN
      RETURN jsonb_build_object('ok', false, 'code', 'coupon_max_customers_reached');
    END IF;

    IF COALESCE(v_coupon_record.free_delivery, false) = true
       AND v_order_type = 'DELIVERY' THEN
      v_priced_delivery_fee := 0;
    END IF;

    IF COALESCE(v_coupon_record.discount_amount, 0) > 0 THEN
      v_priced_discount := COALESCE(v_coupon_record.discount_amount, 0);
    ELSIF COALESCE(v_coupon_record.discount_percent, 0) > 0 THEN
      v_priced_discount := v_priced_subtotal
        * (LEAST(GREATEST(COALESCE(v_coupon_record.discount_percent, 0), 0), 100) / 100.0);
    END IF;
  END IF;

  v_priced_discount := LEAST(
    GREATEST(COALESCE(v_priced_discount, 0), 0),
    v_priced_subtotal
  );
  v_subtotal := GREATEST(COALESCE(v_priced_subtotal, 0), 0);
  v_delivery_fee := GREATEST(COALESCE(v_priced_delivery_fee, 0), 0);
  v_total := GREATEST(v_subtotal + v_delivery_fee - v_priced_discount, 0);

  PERFORM 1
  FROM public.inventory_items i
  WHERE EXISTS (
    SELECT 1
    FROM pg_temp.tmp_inventory_needs need
    WHERE need.inventory_item_id = i.id
  )
  FOR UPDATE;

  SELECT jsonb_agg(jsonb_build_object(
    'inventoryItemId', shortage.inventory_item_id,
    'name', shortage.name,
    'requiredQty', shortage.required_qty,
    'availableQty', shortage.available_qty
  ))
  INTO v_shortages
  FROM (
    SELECT
      i.id AS inventory_item_id,
      i.name,
      SUM(n.required_qty) AS required_qty,
      MAX(i.quantity_on_hand) AS available_qty
    FROM pg_temp.tmp_inventory_needs n
    JOIN public.inventory_items i
      ON i.id = n.inventory_item_id
    GROUP BY i.id, i.name
    HAVING BOOL_OR(COALESCE(i.is_active, false) = false)
       OR MAX(COALESCE(i.quantity_on_hand, 0)) < SUM(n.required_qty)
  ) shortage;

  IF v_shortages IS NOT NULL THEN
    RETURN jsonb_build_object(
      'ok', false,
      'code', 'inventory_out_of_stock',
      'shortages', v_shortages
    );
  END IF;

  v_metadata := jsonb_set(v_metadata, '{items}', v_items, true);
  v_metadata := jsonb_set(v_metadata, '{pricing}', jsonb_build_object(
    'subtotal', v_subtotal,
    'deliveryFee', v_delivery_fee,
    'discount', v_priced_discount,
    'total', v_total
  ), true);

  INSERT INTO public.app_orders (
    id,
    order_type,
    status,
    customer_user_id,
    customer_name,
    customer_phone,
    district,
    address_text,
    pos_address_id,
    mobile_address_id,
    driver_user_id,
    subtotal,
    delivery_fee,
    total,
    metadata
  )
  VALUES (
    v_order_id,
    v_order_type,
    v_status,
    v_customer_user_id,
    v_customer_name,
    v_customer_phone,
    v_district,
    v_address_text,
    v_pos_address_id,
    v_mobile_address_id,
    v_driver_user_id,
    GREATEST(v_subtotal, 0),
    GREATEST(v_delivery_fee, 0),
    GREATEST(v_total, 0),
    v_metadata
  );

  IF v_coupon_id IS NOT NULL AND v_customer_user_id IS NOT NULL THEN
    INSERT INTO public.coupon_usage (
      coupon_id,
      customer_id,
      order_id
    )
    VALUES (
      v_coupon_id,
      v_customer_user_id,
      v_order_id
    )
    ON CONFLICT (coupon_id, customer_id, order_id) DO NOTHING;
  END IF;

  UPDATE public.inventory_items i
  SET
    quantity_on_hand = COALESCE(i.quantity_on_hand, 0) - need.required_qty,
    updated_at = now()
  FROM (
    SELECT inventory_item_id, SUM(required_qty) AS required_qty
    FROM pg_temp.tmp_inventory_needs
    GROUP BY inventory_item_id
  ) need
  WHERE i.id = need.inventory_item_id;

  INSERT INTO public.inventory_order_reservations (
    order_id,
    inventory_item_id,
    quantity,
    source_kind,
    source_id
  )
  SELECT
    v_order_id,
    need.inventory_item_id,
    SUM(need.required_qty) AS quantity,
    need.source_kind,
    need.source_id
  FROM pg_temp.tmp_inventory_needs need
  GROUP BY need.inventory_item_id, need.source_kind, need.source_id
  ON CONFLICT (order_id, inventory_item_id, source_kind, source_id)
  DO UPDATE
  SET
    quantity = EXCLUDED.quantity,
    released_at = NULL;

  RETURN jsonb_build_object(
    'ok', true,
    'orderId', v_order_id,
    'status', v_status,
    'subtotal', v_subtotal,
    'deliveryFee', v_delivery_fee,
    'discount', v_priced_discount,
    'total', v_total
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.normalize_app_order_type(p_order_type TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
  v_type TEXT := UPPER(NULLIF(TRIM(COALESCE(p_order_type, '')), ''));
BEGIN
  IF v_type IN ('DELIVERY', 'POS_DELIVERY') THEN
    RETURN 'DELIVERY';
  END IF;
  IF v_type IN ('PICKUP', 'POS_PICKUP') THEN
    RETURN 'PICKUP';
  END IF;
  IF v_type IN ('TAKEAWAY', 'POS_TAKEAWAY') THEN
    RETURN 'TAKEAWAY';
  END IF;
  RETURN v_type;
END;
$$;

CREATE OR REPLACE FUNCTION public.app_order_status_stage(
  p_order_type TEXT,
  p_status TEXT
)
RETURNS INT
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
  v_type TEXT := public.normalize_app_order_type(p_order_type);
  v_status TEXT := UPPER(COALESCE(TRIM(p_status), ''));
BEGIN
  IF v_type = 'DELIVERY' THEN
    IF v_status IN ('', 'RECEIVED', 'PENDING') THEN RETURN 1; END IF;
    IF v_status IN ('PREPARING', 'NO_DRIVER') THEN RETURN 2; END IF;
    IF v_status = 'DRIVER_ASSIGNED' THEN RETURN 3; END IF;
    IF v_status = 'DRIVER_PICKED' THEN RETURN 4; END IF;
    IF v_status = 'ON_ROAD' THEN RETURN 5; END IF;
    IF v_status IN ('DONE', 'DELIVERED') THEN RETURN 6; END IF;
    IF v_status IN ('CANCELLED', 'CANCELED', 'REJECTED') THEN RETURN 98; END IF;
    IF v_status = 'MIGRATED' THEN RETURN 99; END IF;
    RETURN 0;
  END IF;

  IF v_type = 'PICKUP' THEN
    IF v_status IN ('', 'RECEIVED', 'PENDING', 'ACCEPTED') THEN RETURN 1; END IF;
    IF v_status = 'PREPARING' THEN RETURN 2; END IF;
    IF v_status = 'READY' THEN RETURN 3; END IF;
    IF v_status IN ('DONE', 'DELIVERED') THEN RETURN 4; END IF;
    IF v_status IN ('CANCELLED', 'CANCELED', 'REJECTED') THEN RETURN 98; END IF;
    IF v_status = 'MIGRATED' THEN RETURN 99; END IF;
    RETURN 0;
  END IF;

  RETURN 0;
END;
$$;

CREATE OR REPLACE FUNCTION public.can_app_order_transition(
  p_order_type TEXT,
  p_from_status TEXT,
  p_to_status TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
  v_type TEXT := public.normalize_app_order_type(p_order_type);
  v_from TEXT := UPPER(COALESCE(TRIM(p_from_status), ''));
  v_to TEXT := UPPER(COALESCE(TRIM(p_to_status), ''));
BEGIN
  IF v_to = '' THEN
    RETURN false;
  END IF;

  IF v_from = v_to THEN
    RETURN true;
  END IF;

  IF v_to = 'MIGRATED' THEN
    RETURN v_from IN ('DONE', 'DELIVERED', 'CANCELLED', 'CANCELED', 'REJECTED');
  END IF;

  IF v_type = 'DELIVERY' THEN
    RETURN (v_from IN ('', 'RECEIVED', 'PENDING') AND v_to IN ('PREPARING', 'NO_DRIVER', 'CANCELLED', 'CANCELED', 'REJECTED'))
      OR (v_from = 'PREPARING' AND v_to IN ('DRIVER_ASSIGNED', 'NO_DRIVER', 'CANCELLED', 'CANCELED', 'REJECTED'))
      OR (v_from = 'NO_DRIVER' AND v_to IN ('PREPARING', 'DRIVER_ASSIGNED', 'CANCELLED', 'CANCELED', 'REJECTED'))
      OR (v_from = 'DRIVER_ASSIGNED' AND v_to IN ('DRIVER_PICKED', 'NO_DRIVER', 'CANCELLED', 'CANCELED', 'REJECTED'))
      OR (v_from = 'DRIVER_PICKED' AND v_to IN ('ON_ROAD', 'CANCELLED', 'CANCELED', 'REJECTED'))
      OR (v_from = 'ON_ROAD' AND v_to IN ('DELIVERED', 'DONE', 'CANCELLED', 'CANCELED', 'REJECTED'));
  END IF;

  IF v_type = 'PICKUP' THEN
    RETURN (v_from IN ('', 'RECEIVED', 'PENDING', 'ACCEPTED') AND v_to IN ('PREPARING', 'CANCELLED', 'CANCELED', 'REJECTED'))
      OR (v_from = 'PREPARING' AND v_to IN ('READY', 'CANCELLED', 'CANCELED', 'REJECTED'))
      OR (v_from = 'READY' AND v_to IN ('DELIVERED', 'DONE', 'CANCELLED', 'CANCELED', 'REJECTED'));
  END IF;

  RETURN false;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_update_app_order_status(
  p_order_id TEXT,
  p_to_status TEXT,
  p_actor_role TEXT DEFAULT NULL,
  p_actor_user_id TEXT DEFAULT NULL,
  p_mark_kitchen_printed BOOLEAN DEFAULT NULL,
  p_mark_driver_receipt_printed BOOLEAN DEFAULT NULL,
  p_mark_partner_receipt_printed BOOLEAN DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_order_id TEXT := NULLIF(TRIM(COALESCE(p_order_id, '')), '');
  v_to_status TEXT := UPPER(NULLIF(TRIM(COALESCE(p_to_status, '')), ''));
  v_old_status TEXT := '';
  v_order_type TEXT := '';
  v_driver_user_id TEXT;
  v_normalized_order_type TEXT := '';
  v_meta JSONB;
  v_request_actor_role TEXT := NULLIF(public.current_app_role(), '');
  v_actor_role TEXT := v_request_actor_role;
  v_request_actor_user_id TEXT := public.current_app_user_id();
  v_actor_user_id TEXT := COALESCE(
    v_request_actor_user_id,
    CASE
      WHEN v_request_actor_role = 'SERVICE_ROLE' THEN NULLIF(TRIM(COALESCE(p_actor_user_id, '')), '')
      ELSE NULL
    END
  );
  v_record_actor_user_id TEXT;
BEGIN
  IF v_order_id IS NULL OR v_to_status IS NULL THEN
    RETURN false;
  END IF;

  IF v_actor_role IS NULL
     OR v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'DRIVER', 'SERVICE_ROLE') THEN
    RETURN false;
  END IF;

  SELECT
    UPPER(COALESCE(status, '')),
    UPPER(COALESCE(order_type, '')),
    driver_user_id,
    COALESCE(metadata, '{}'::jsonb)
  INTO
    v_old_status,
    v_order_type,
    v_driver_user_id,
    v_meta
  FROM public.app_orders
  WHERE id = v_order_id
  FOR UPDATE;

  IF NOT FOUND THEN
    RETURN false;
  END IF;

  v_normalized_order_type := public.normalize_app_order_type(v_order_type);
  IF NOT public.can_app_order_transition(v_order_type, v_old_status, v_to_status) THEN
    RETURN false;
  END IF;

  IF v_actor_role = 'DRIVER' THEN
    IF v_actor_user_id IS NULL OR v_actor_user_id <> v_driver_user_id THEN
      RETURN false;
    END IF;
    IF v_normalized_order_type <> 'DELIVERY' THEN
      RETURN false;
    END IF;
    IF NOT public.can_driver_transition_order_status(v_old_status, v_to_status) THEN
      RETURN false;
    END IF;
  END IF;

  IF v_normalized_order_type = 'DELIVERY'
     AND v_to_status IN ('DRIVER_ASSIGNED', 'DRIVER_PICKED', 'ON_ROAD', 'DELIVERED', 'DONE')
     AND NULLIF(TRIM(COALESCE(v_driver_user_id, '')), '') IS NULL THEN
    RETURN false;
  END IF;

  IF p_mark_kitchen_printed IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{kitchen_printed}', to_jsonb(p_mark_kitchen_printed), true);
  END IF;
  IF p_mark_driver_receipt_printed IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{driver_receipt_printed}', to_jsonb(p_mark_driver_receipt_printed), true);
  END IF;
  IF p_mark_partner_receipt_printed IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{partner_receipt_printed}', to_jsonb(p_mark_partner_receipt_printed), true);
  END IF;

  v_meta := jsonb_set(v_meta, '{last_actor_role}', to_jsonb(v_actor_role), true);
  v_record_actor_user_id := v_actor_user_id;
  IF v_record_actor_user_id IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{last_actor_user_id}', to_jsonb(v_record_actor_user_id), true);
  END IF;

  UPDATE public.app_orders
  SET
    status = v_to_status,
    updated_at = now(),
    metadata = v_meta
  WHERE id = v_order_id;

  IF NOT FOUND THEN
    RETURN false;
  END IF;

  IF v_to_status <> v_old_status
     AND v_to_status IN ('CANCELLED', 'CANCELED', 'REJECTED') THEN
    PERFORM public.inventory_release_order_reservations(v_order_id);
  END IF;

  RETURN true;
END;
$$;

-- Backward-compat wrapper: accepts a single JSON/JSONB body.
CREATE OR REPLACE FUNCTION public.api_update_app_order_status(p_payload JSONB)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  RETURN public.api_update_app_order_status(
    p_order_id := p_payload->>'p_order_id',
    p_to_status := p_payload->>'p_to_status',
    p_actor_role := p_payload->>'p_actor_role',
    p_actor_user_id := p_payload->>'p_actor_user_id',
    p_mark_kitchen_printed := CASE
      WHEN p_payload ? 'p_mark_kitchen_printed' THEN (p_payload->>'p_mark_kitchen_printed')::BOOLEAN
      ELSE NULL
    END,
    p_mark_driver_receipt_printed := CASE
      WHEN p_payload ? 'p_mark_driver_receipt_printed' THEN (p_payload->>'p_mark_driver_receipt_printed')::BOOLEAN
      ELSE NULL
    END,
    p_mark_partner_receipt_printed := CASE
      WHEN p_payload ? 'p_mark_partner_receipt_printed' THEN (p_payload->>'p_mark_partner_receipt_printed')::BOOLEAN
      ELSE NULL
    END
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.api_assign_app_order_driver(
  p_order_id TEXT,
  p_driver_user_id TEXT,
  p_driver_name TEXT DEFAULT NULL,
  p_driver_phone TEXT DEFAULT NULL,
  p_actor_role TEXT DEFAULT NULL,
  p_mark_driver_receipt_printed BOOLEAN DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_order_id TEXT := NULLIF(TRIM(COALESCE(p_order_id, '')), '');
  v_driver_user_id TEXT := NULLIF(TRIM(COALESCE(p_driver_user_id, '')), '');
  v_old_status TEXT := '';
  v_order_type TEXT := '';
  v_normalized_order_type TEXT := '';
  v_meta JSONB;
  v_actor_role TEXT := NULLIF(public.current_app_role(), '');
  v_actor_user_id TEXT := public.current_app_user_id();
  v_driver_is_valid BOOLEAN := false;
  v_record_actor_user_id TEXT;
BEGIN
  IF v_order_id IS NULL OR v_driver_user_id IS NULL THEN
    RETURN false;
  END IF;

  IF v_actor_role IS NULL
     OR v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'CASHIER', 'SERVICE_ROLE') THEN
    RETURN false;
  END IF;

  SELECT
    UPPER(COALESCE(status, '')),
    UPPER(COALESCE(order_type, '')),
    COALESCE(metadata, '{}'::jsonb)
  INTO
    v_old_status,
    v_order_type,
    v_meta
  FROM public.app_orders
  WHERE id = v_order_id
  FOR UPDATE;

  IF NOT FOUND THEN
    RETURN false;
  END IF;

  v_normalized_order_type := public.normalize_app_order_type(v_order_type);
  IF v_normalized_order_type <> 'DELIVERY' THEN
    RETURN false;
  END IF;
  IF v_old_status NOT IN ('PREPARING', 'NO_DRIVER', 'DRIVER_ASSIGNED') THEN
    RETURN false;
  END IF;

  SELECT EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id = v_driver_user_id
      AND COALESCE(u.is_active, true) = true
      AND UPPER(COALESCE(u.role, '')) IN ('DRIVER', 'ADMIN_POS')
  )
  INTO v_driver_is_valid;

  IF NOT COALESCE(v_driver_is_valid, false) THEN
    RETURN false;
  END IF;

  v_meta := jsonb_set(v_meta, '{driver_user_id}', to_jsonb(v_driver_user_id), true);
  IF NULLIF(TRIM(COALESCE(p_driver_name, '')), '') IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{driver_name}', to_jsonb(TRIM(p_driver_name)), true);
  END IF;
  IF NULLIF(TRIM(COALESCE(p_driver_phone, '')), '') IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{driver_phone}', to_jsonb(TRIM(p_driver_phone)), true);
  END IF;
  IF p_mark_driver_receipt_printed IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{driver_receipt_printed}', to_jsonb(p_mark_driver_receipt_printed), true);
  END IF;
  v_meta := jsonb_set(v_meta, '{last_actor_role}', to_jsonb(v_actor_role), true);
  v_record_actor_user_id := v_actor_user_id;
  IF v_record_actor_user_id IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{last_actor_user_id}', to_jsonb(v_record_actor_user_id), true);
  END IF;

  UPDATE public.app_orders
  SET
    driver_user_id = v_driver_user_id,
    status = 'DRIVER_ASSIGNED',
    updated_at = now(),
    metadata = v_meta
  WHERE id = v_order_id;

  IF NOT FOUND THEN
    RETURN false;
  END IF;

  RETURN true;
END;
$$;

-- Backward-compat wrapper: accepts a single JSON/JSONB body.
CREATE OR REPLACE FUNCTION public.api_assign_app_order_driver(p_payload JSONB)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  RETURN public.api_assign_app_order_driver(
    p_order_id := p_payload->>'p_order_id',
    p_driver_user_id := p_payload->>'p_driver_user_id',
    p_driver_name := p_payload->>'p_driver_name',
    p_driver_phone := p_payload->>'p_driver_phone',
    p_actor_role := p_payload->>'p_actor_role',
    p_mark_driver_receipt_printed := CASE
      WHEN p_payload ? 'p_mark_driver_receipt_printed' THEN (p_payload->>'p_mark_driver_receipt_printed')::BOOLEAN
      ELSE NULL
    END
  );
END;
$$;

CREATE OR REPLACE FUNCTION public.trg_app_orders_validate_transition()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_order_type TEXT := COALESCE(NEW.order_type, OLD.order_type, '');
  v_normalized_order_type TEXT := public.normalize_app_order_type(v_order_type);
  v_old_status TEXT := UPPER(COALESCE(OLD.status, ''));
  v_new_status TEXT := UPPER(COALESCE(NEW.status, OLD.status, ''));
BEGIN
  IF TG_OP <> 'UPDATE' THEN
    RETURN NEW;
  END IF;

  IF v_new_status = v_old_status
     AND COALESCE(NEW.driver_user_id, '') = COALESCE(OLD.driver_user_id, '') THEN
    RETURN NEW;
  END IF;

  IF NOT public.can_app_order_transition(v_order_type, v_old_status, v_new_status) THEN
    RAISE EXCEPTION 'invalid_app_order_status_transition';
  END IF;

  IF v_normalized_order_type = 'DELIVERY'
     AND v_new_status IN ('DRIVER_ASSIGNED', 'DRIVER_PICKED', 'ON_ROAD', 'DELIVERED', 'DONE')
     AND NULLIF(TRIM(COALESCE(NEW.driver_user_id, '')), '') IS NULL THEN
    RAISE EXCEPTION 'driver_required_for_status';
  END IF;

  RETURN NEW;
END;
$$;

-- Trigger: توحيد منطق إشعارات حالات app_orders (إنشاء + تحديث).
CREATE OR REPLACE FUNCTION public.trg_app_orders_emit_notifications()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_raw_order_type TEXT;
  v_order_type TEXT;
  v_status TEXT;
  v_old_status TEXT := '';
  v_order_ref TEXT;
  v_driver_changed BOOLEAN := false;
BEGIN
  v_raw_order_type := UPPER(COALESCE(NEW.order_type, ''));
  v_order_type := public.normalize_app_order_type(NEW.order_type);
  v_status := UPPER(COALESCE(NEW.status, ''));
  v_order_ref := COALESCE(
    NULLIF(TRIM(COALESCE(NEW.metadata->>'localOrderId', '')), ''),
    NULLIF(TRIM(COALESCE(NEW.id, '')), ''),
    '---'
  );

  IF TG_OP = 'INSERT' THEN
    IF v_raw_order_type = 'DELIVERY' THEN
      PERFORM public.api_emit_order_notification(
        NULL, 'CASHIER', NEW.id, NEW.order_type,
        'طلب دليفري جديد',
        'يوجد طلب دليفري جديد رقم #' || v_order_ref,
        'RECEIVED'
      );
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'تم تسجيل طلبك',
          'تم تسجيل طلب الدليفري رقم #' || v_order_ref || '.',
          'RECEIVED'
        );
      END IF;
    ELSIF v_raw_order_type = 'PICKUP' THEN
      PERFORM public.api_emit_order_notification(
        NULL, 'CASHIER', NEW.id, NEW.order_type,
        'طلب استلام جديد',
        'يوجد طلب استلام من الفرع رقم #' || v_order_ref,
        'RECEIVED'
      );
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'تم تسجيل طلبك',
          'تم تسجيل طلب الاستلام رقم #' || v_order_ref || '.',
          'RECEIVED'
        );
      END IF;
    END IF;
    RETURN NEW;
  END IF;

  IF TG_OP <> 'UPDATE' THEN
    RETURN NEW;
  END IF;

  v_old_status := UPPER(COALESCE(OLD.status, ''));
  v_driver_changed := COALESCE(NEW.driver_user_id, '') <> COALESCE(OLD.driver_user_id, '');

  IF v_status = v_old_status AND NOT v_driver_changed THEN
    RETURN NEW;
  END IF;

  IF v_status <> v_old_status
     AND v_status IN ('CANCELLED', 'CANCELED', 'REJECTED') THEN
    PERFORM public.inventory_release_order_reservations(NEW.id);
  END IF;

  IF v_order_type = 'DELIVERY' THEN
    -- معلق -> قيد التجهيز
    IF v_status = 'PREPARING' AND v_old_status IN ('', 'RECEIVED', 'PENDING', 'NO_DRIVER') THEN
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'طلبك قيد التجهيز',
          'أوردرك قيد التجهيز.',
          v_status
        );
      END IF;

    -- بدون سائق: لا إشعار
    ELSIF v_status = 'NO_DRIVER' THEN
      NULL;

    -- تم تعيين سائق
    ELSIF v_status = 'DRIVER_ASSIGNED'
      AND (v_status <> v_old_status OR v_driver_changed) THEN
      IF NEW.driver_user_id IS NOT NULL AND TRIM(NEW.driver_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.driver_user_id, 'DRIVER', NEW.id, NEW.order_type,
          'أوردر جاهز للاستلام',
          'أوردر رقم #' || v_order_ref || ' جاهز للاستلام.',
          v_status
        );
      END IF;
      IF NEW.customer_user_id IS NOT NULL
         AND TRIM(NEW.customer_user_id) <> ''
         AND v_old_status IN ('PREPARING', 'NO_DRIVER') THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'تم تجهيز أوردرك',
          'تم تجهيز أوردرك.',
          v_status
        );
      END IF;

    -- السائق استلم الطلب
    ELSIF v_status = 'DRIVER_PICKED' AND v_status <> v_old_status THEN
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'السائق استلم طلبك',
          'السائق استلم طلبك وهو في طريقه للتوصيل.',
          v_status
        );
      END IF;

    -- السائق في الطريق
    ELSIF v_status = 'ON_ROAD' AND v_status <> v_old_status THEN
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'السائق في الطريق إليك',
          'السائق في الطريق إليك الآن.',
          v_status
        );
      END IF;

    -- تم التسليم
    ELSIF (v_status = 'DELIVERED' OR v_status = 'DONE') AND v_status <> v_old_status THEN
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'تم تسليم طلبك',
          'تم تسليم طلبك بنجاح.',
          v_status
        );
      END IF;
    END IF;

  ELSIF v_order_type = 'PICKUP' THEN
    -- معلق -> قيد التجهيز
    IF v_status = 'PREPARING' AND v_old_status IN ('', 'RECEIVED', 'PENDING', 'ACCEPTED') THEN
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'تم استلام طلبك',
          'تم استلام طلبك ويجري تجهيزه.',
          v_status
        );
      END IF;

    -- قيد التجهيز -> جاهز للاستلام
    ELSIF v_status = 'READY' AND v_old_status IN ('PREPARING', 'ACCEPTED') THEN
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'طلبك جاهز للاستلام',
          'تم تجهيز طلبك وهو جاهز للاستلام.',
          v_status
        );
      END IF;

    -- جاهز للاستلام -> تم التسليم
    ELSIF (v_status = 'DELIVERED' OR v_status = 'DONE') AND v_old_status IN ('READY', 'PREPARING', 'ACCEPTED') THEN
      IF NEW.customer_user_id IS NOT NULL AND TRIM(NEW.customer_user_id) <> '' THEN
        PERFORM public.api_emit_order_notification(
          NEW.customer_user_id, 'CUSTOMER', NEW.id, NEW.order_type,
          'تم تسليم طلبك',
          'تم تسليم طلبك.',
          v_status
        );
      END IF;
    END IF;
  END IF;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_app_orders_emit_notifications ON public.app_orders;
DROP TRIGGER IF EXISTS trg_app_orders_emit_notifications_ins ON public.app_orders;
DROP TRIGGER IF EXISTS trg_app_orders_emit_notifications_upd ON public.app_orders;
DROP TRIGGER IF EXISTS trg_app_orders_validate_transition ON public.app_orders;

CREATE TRIGGER trg_app_orders_validate_transition
BEFORE UPDATE OF status, driver_user_id ON public.app_orders
FOR EACH ROW
EXECUTE FUNCTION public.trg_app_orders_validate_transition();

CREATE TRIGGER trg_app_orders_emit_notifications_ins
AFTER INSERT ON public.app_orders
FOR EACH ROW
EXECUTE FUNCTION public.trg_app_orders_emit_notifications();

CREATE TRIGGER trg_app_orders_emit_notifications_upd
AFTER UPDATE OF status, driver_user_id ON public.app_orders
FOR EACH ROW
EXECUTE FUNCTION public.trg_app_orders_emit_notifications();

-- منح الصلاحيات للـ anon/authenticated حسب الحاجة
GRANT EXECUTE ON FUNCTION public.api_backend_healthcheck() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_android_menu_snapshot() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_menu_snapshot() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_app_products_snapshot() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_app_orders_snapshot(INT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_notifications_snapshot(INT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_inventory_snapshot() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_inventory_admin_snapshot() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_inventory_upsert_item(JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_inventory_delete_item(TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_inventory_set_entity_mode(TEXT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_inventory_replace_entity_links(TEXT, TEXT, JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_inventory_replace_client_reservations(TEXT, JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_create_app_order_with_inventory(JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_update_app_order_status(TEXT, TEXT, TEXT, TEXT, BOOLEAN, BOOLEAN, BOOLEAN) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_update_app_order_status(JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_assign_app_order_driver(TEXT, TEXT, TEXT, TEXT, TEXT, BOOLEAN) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_assign_app_order_driver(JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_admin_get_store(TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_admin_sync_store(JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_customer_signup(TEXT, TEXT, TEXT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_customer_signup(TEXT, TEXT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_issue_role_session(TEXT, TEXT, INT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_role_session_logout(TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_users_search(TEXT, TEXT, TEXT, INT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_public_account_lookup(TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_public_login_identifier_resolve(TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_cashier_credentials_snapshot(BOOLEAN, INT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_upsert_staff_credential(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, BOOLEAN, TEXT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_delete_staff_user(TEXT, BOOLEAN) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_register_push_token(TEXT, TEXT, TEXT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_create_complaint(TEXT, TEXT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_send_message(BIGINT, TEXT, TEXT, TEXT, TEXT, TEXT, BIGINT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_close_thread(BIGINT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_mark_read_customer(BIGINT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_mark_read_admin(BIGINT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_threads_for_customer(TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_all_threads() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_support_messages(BIGINT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_admin_search_maintenance_candidates(TEXT, INT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_admin_list_maintenance_exceptions() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_admin_upsert_maintenance_exception(TEXT, TEXT, TEXT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_admin_delete_maintenance_exception(BIGINT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_is_maintenance_bypassed(TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_push_diagnostics(TEXT, TEXT) TO anon, authenticated;

-- -----------------------------------------------------------------------------
-- PostgREST visibility for direct table-based backend (mobile + admin + POS)
-- -----------------------------------------------------------------------------
GRANT USAGE ON SCHEMA public TO anon, authenticated;
-- Required for SERIAL/BIGSERIAL defaults (nextval) to avoid permission denied.
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.source_products TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.inventory_items TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.inventory_item_links TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.pos_categories TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.pos_products TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.app_categories TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.app_products TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.addon_groups TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.addon_options TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.app_category_default_groups TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.app_product_group_overrides TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.hoods TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.app_work_hours TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.coupons TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.app_orders TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.inventory_order_reservations TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.inventory_reservation_events TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.notifications TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.coupon_usage TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.app_settings TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.customer_addresses TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.pos_registered_addresses TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.support_threads TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.support_messages TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.users TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.user_login_secrets TO anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.user_push_tokens TO anon, authenticated;

-- -----------------------------------------------------------------------------
-- Admin write policies for core management tables (categories/products/addons/
-- districts + cashier/driver rows).
-- NOTE: This project uses anon key flows plus app role sessions; policies rely
-- on current_app_role()/current_app_user_id() rather than trusting raw headers.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.is_admin_policy_request()
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
BEGIN
  RETURN public.current_app_role() IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE');
END;
$$;

DO $$
DECLARE
  t TEXT;
  p RECORD;
  p_select TEXT;
  p_write TEXT;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'app_categories',
    'app_products',
    'addon_groups',
    'addon_options',
    'app_category_default_groups',
    'app_product_group_overrides',
    'hoods'
  ]
  LOOP
    p_select := t || '_select_all';
    p_write := t || '_admin_write';

    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t);
    FOR p IN
      SELECT pol.policyname
      FROM pg_policies pol
      WHERE pol.schemaname = 'public'
        AND pol.tablename = t
    LOOP
      EXECUTE format('DROP POLICY IF EXISTS %I ON public.%I', p.policyname, t);
    END LOOP;

    EXECUTE format(
      'CREATE POLICY %I ON public.%I FOR SELECT TO anon, authenticated USING (true)',
      p_select,
      t
    );
    EXECUTE format(
      'CREATE POLICY %I ON public.%I FOR ALL TO anon, authenticated USING (public.is_admin_policy_request()) WITH CHECK (public.is_admin_policy_request())',
      p_write,
      t
    );
  END LOOP;
END;
$$;

DO $$
DECLARE
  p RECORD;
BEGIN
  ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;
  FOR p IN
    SELECT pol.policyname
    FROM pg_policies pol
    WHERE pol.schemaname = 'public'
      AND pol.tablename = 'users'
  LOOP
    EXECUTE format('DROP POLICY IF EXISTS %I ON public.users', p.policyname);
  END LOOP;
END;
$$;

DROP POLICY IF EXISTS "users_select_all" ON public.users;
CREATE POLICY users_select_all
ON public.users
FOR SELECT
TO anon, authenticated
USING (true);

DROP POLICY IF EXISTS "users_write_admin_or_customer" ON public.users;
CREATE POLICY users_write_admin_or_customer
ON public.users
FOR ALL
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR UPPER(COALESCE(role, 'CUSTOMER')) = 'CUSTOMER'
)
WITH CHECK (
  public.is_admin_policy_request()
  OR UPPER(COALESCE(role, 'CUSTOMER')) = 'CUSTOMER'
);

-- -----------------------------------------------------------------------------
-- Security hardening overrides for sensitive data
-- -----------------------------------------------------------------------------
-- 1) app_settings contains push credentials; expose only appearance fields to clients.
REVOKE ALL ON TABLE public.app_settings FROM anon, authenticated;
GRANT SELECT (id, appearance_preset, guest_appearance_preset, launcher_icon, share_web_base_url, maintenance_mode, maintenance_allow_guest, updated_at_millis, updated_by)
  ON TABLE public.app_settings TO anon, authenticated;
GRANT INSERT (id, appearance_preset, guest_appearance_preset, launcher_icon, share_web_base_url, maintenance_mode, maintenance_allow_guest, updated_at_millis, updated_by)
  ON TABLE public.app_settings TO anon, authenticated;
GRANT UPDATE (appearance_preset, guest_appearance_preset, launcher_icon, share_web_base_url, maintenance_mode, maintenance_allow_guest, updated_at_millis, updated_by)
  ON TABLE public.app_settings TO anon, authenticated;

-- 2) never expose password hashes.
REVOKE SELECT ON TABLE public.user_login_secrets FROM anon, authenticated;
GRANT INSERT, UPDATE, DELETE ON TABLE public.user_login_secrets TO anon, authenticated;

-- 3) role sessions, push tokens and delivery logs are internal-only (access via SECURITY DEFINER RPCs).
REVOKE ALL ON TABLE public.app_role_sessions FROM anon, authenticated;
REVOKE ALL ON TABLE public.user_push_tokens FROM anon, authenticated;
REVOKE ALL ON TABLE public.maintenance_mode_exceptions FROM anon, authenticated;
REVOKE ALL ON TABLE public.inventory_client_reservations FROM anon, authenticated;
REVOKE ALL ON TABLE public.auth_login_rate_limits FROM anon, authenticated;
REVOKE INSERT, UPDATE, DELETE ON TABLE public.app_orders FROM anon, authenticated;
REVOKE INSERT, UPDATE, DELETE ON TABLE public.notifications FROM anon, authenticated;
REVOKE INSERT, UPDATE, DELETE ON TABLE public.coupon_usage FROM anon, authenticated;
REVOKE INSERT, UPDATE, DELETE ON TABLE public.inventory_order_reservations FROM anon, authenticated;
REVOKE INSERT, UPDATE, DELETE ON TABLE public.inventory_reservation_events FROM anon, authenticated;

-- Final role-based RLS model (ADMIN / ADMIN_POS / CASHIER / DRIVER / CUSTOMER)
CREATE OR REPLACE FUNCTION public.is_cashier_policy_request()
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
BEGIN
  RETURN public.current_app_role() = 'CASHIER';
END;
$$;

CREATE OR REPLACE FUNCTION public.is_driver_policy_request()
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
BEGIN
  RETURN public.current_app_role() = 'DRIVER';
END;
$$;

CREATE OR REPLACE FUNCTION public.is_customer_policy_request()
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
BEGIN
  RETURN public.current_app_role() = 'CUSTOMER';
END;
$$;

CREATE OR REPLACE FUNCTION public.is_cashier_notification_scope(
  p_user_id TEXT,
  p_role_target TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_request_user_id TEXT := public.current_app_user_id();
BEGIN
  RETURN public.is_cashier_policy_request()
    AND (
      (
        v_request_user_id IS NOT NULL
        AND p_user_id IS NOT NULL
        AND TRIM(v_request_user_id) <> ''
        AND TRIM(p_user_id) <> ''
        AND v_request_user_id = p_user_id
      )
      OR (
        p_user_id IS NULL
        AND UPPER(COALESCE(p_role_target, '')) = 'CASHIER'
      )
    );
END;
$$;

CREATE OR REPLACE FUNCTION public.is_same_request_user(p_user_id TEXT)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_request_user_id TEXT := public.current_app_user_id();
BEGIN
  RETURN v_request_user_id IS NOT NULL
    AND p_user_id IS NOT NULL
    AND TRIM(v_request_user_id) <> ''
    AND TRIM(p_user_id) <> ''
    AND v_request_user_id = p_user_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.is_same_request_phone(p_phone TEXT)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_request_phone TEXT := public.current_app_phone();
BEGIN
  RETURN v_request_phone IS NOT NULL
    AND p_phone IS NOT NULL
    AND TRIM(v_request_phone) <> ''
    AND TRIM(p_phone) <> ''
    AND v_request_phone = p_phone;
END;
$$;

CREATE OR REPLACE FUNCTION public.is_current_request_legacy_owner_key(
  p_owner_key TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_owner_key TEXT := NULLIF(TRIM(COALESCE(p_owner_key, '')), '');
  v_request_user_id TEXT := public.current_app_user_id();
  v_request_phone TEXT;
  v_request_email TEXT;
  v_request_username TEXT;
BEGIN
  IF v_owner_key IS NULL OR v_request_user_id IS NULL THEN
    RETURN false;
  END IF;

  SELECT
    NULLIF(TRIM(COALESCE(u.phone, '')), ''),
    NULLIF(TRIM(LOWER(COALESCE(u.email, ''))), ''),
    NULLIF(TRIM(COALESCE(u.username, '')), '')
  INTO
    v_request_phone,
    v_request_email,
    v_request_username
  FROM public.users u
  WHERE u.id = v_request_user_id
  LIMIT 1;

  RETURN v_owner_key = v_request_user_id
    OR (v_request_phone IS NOT NULL AND v_owner_key = v_request_phone)
    OR (v_request_email IS NOT NULL AND LOWER(v_owner_key) = v_request_email)
    OR (v_request_username IS NOT NULL AND v_owner_key = v_request_username);
END;
$$;

CREATE OR REPLACE FUNCTION public.can_driver_transition_order_status(
  p_from_status TEXT,
  p_to_status TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
STABLE
SET search_path = public
AS $$
DECLARE
  v_from TEXT := UPPER(NULLIF(TRIM(COALESCE(p_from_status, '')), ''));
  v_to TEXT := UPPER(NULLIF(TRIM(COALESCE(p_to_status, '')), ''));
BEGIN
  RETURN (v_from = 'DRIVER_ASSIGNED' AND v_to = 'DRIVER_PICKED')
    OR (v_from = 'DRIVER_PICKED' AND v_to = 'ON_ROAD')
    OR (v_from = 'ON_ROAD' AND v_to IN ('DELIVERED', 'DONE'));
END;
$$;

DO $$
DECLARE
  t TEXT;
  p RECORD;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'source_products',
    'inventory_items',
    'inventory_item_links',
    'pos_categories',
    'pos_products',
    'app_categories',
    'app_products',
    'addon_groups',
    'addon_options',
    'app_category_default_groups',
    'app_product_group_overrides',
    'hoods',
    'app_work_hours',
    'coupons',
    'app_settings',
    'users',
    'user_login_secrets',
    'app_role_sessions',
    'app_orders',
    'notifications',
    'coupon_usage',
    'customer_addresses',
    'pos_registered_addresses',
    'support_threads',
    'support_messages',
    'user_push_tokens',
    'inventory_order_reservations',
    'inventory_reservation_events'
  ]
  LOOP
    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t);
    FOR p IN
      SELECT pol.policyname
      FROM pg_policies pol
      WHERE pol.schemaname = 'public'
        AND pol.tablename = t
    LOOP
      EXECUTE format('DROP POLICY IF EXISTS %I ON public.%I', p.policyname, t);
    END LOOP;
  END LOOP;
END;
$$;

DO $$
DECLARE
  t TEXT;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'source_products',
    'inventory_items',
    'inventory_item_links',
    'pos_categories',
    'pos_products',
    'app_categories',
    'app_products',
    'addon_groups',
    'addon_options',
    'app_category_default_groups',
    'app_product_group_overrides',
    'hoods',
    'app_work_hours',
    'coupons',
    'inventory_reservation_events'
  ]
  LOOP
    EXECUTE format(
      'CREATE POLICY %I ON public.%I FOR SELECT TO anon, authenticated USING (true)',
      t || '_select_all',
      t
    );
    EXECUTE format(
      'CREATE POLICY %I ON public.%I FOR ALL TO anon, authenticated USING (public.is_admin_policy_request()) WITH CHECK (public.is_admin_policy_request())',
      t || '_admin_write',
      t
    );
  END LOOP;
END;
$$;

DROP POLICY IF EXISTS "inventory_items_select_staff" ON public.inventory_items;
CREATE POLICY inventory_items_select_staff
ON public.inventory_items
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
);

DROP POLICY IF EXISTS "inventory_item_links_select_staff" ON public.inventory_item_links;
CREATE POLICY inventory_item_links_select_staff
ON public.inventory_item_links
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
);

DROP POLICY IF EXISTS "inventory_order_reservations_select_staff" ON public.inventory_order_reservations;
CREATE POLICY inventory_order_reservations_select_staff
ON public.inventory_order_reservations
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
);

DROP POLICY IF EXISTS "inventory_order_reservations_admin_write" ON public.inventory_order_reservations;
CREATE POLICY inventory_order_reservations_admin_write
ON public.inventory_order_reservations
FOR ALL
TO anon, authenticated
USING (public.is_admin_policy_request())
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "app_settings_select_all" ON public.app_settings;
CREATE POLICY app_settings_select_all
ON public.app_settings
FOR SELECT
TO anon, authenticated
USING (true);

DROP POLICY IF EXISTS "app_settings_admin_write" ON public.app_settings;
CREATE POLICY app_settings_admin_write
ON public.app_settings
FOR ALL
TO anon, authenticated
USING (public.is_admin_policy_request())
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "users_select_by_role" ON public.users;
CREATE POLICY users_select_by_role
ON public.users
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR (
    public.is_cashier_policy_request()
    AND UPPER(COALESCE(role, '')) IN ('DRIVER', 'CASHIER', 'ADMIN', 'ADMIN_POS')
  )
  OR (
    public.is_driver_policy_request()
    AND (
      public.is_same_request_user(id)
      OR UPPER(COALESCE(role, '')) = 'DRIVER'
    )
  )
  OR (
    public.is_customer_policy_request()
    AND (
      public.is_same_request_user(id)
      OR (is_active = true AND UPPER(COALESCE(role, '')) = 'DRIVER')
    )
  )
);

DROP POLICY IF EXISTS "users_admin_write" ON public.users;
CREATE POLICY users_admin_write
ON public.users
FOR ALL
TO anon, authenticated
USING (public.is_admin_policy_request())
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "users_self_update" ON public.users;
CREATE POLICY users_self_update
ON public.users
FOR UPDATE
TO anon, authenticated
USING (false)
WITH CHECK (false);

DROP POLICY IF EXISTS "users_self_delete" ON public.users;
CREATE POLICY users_self_delete
ON public.users
FOR DELETE
TO anon, authenticated
USING (false);

DROP POLICY IF EXISTS "user_login_secrets_admin_write" ON public.user_login_secrets;
CREATE POLICY user_login_secrets_admin_write
ON public.user_login_secrets
FOR ALL
TO anon, authenticated
USING (public.is_admin_policy_request())
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "app_orders_select_by_role" ON public.app_orders;
CREATE POLICY app_orders_select_by_role
ON public.app_orders
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
  OR (public.is_driver_policy_request() AND public.is_same_request_user(driver_user_id))
  OR (public.is_customer_policy_request() AND public.is_same_request_user(customer_user_id))
);

DROP POLICY IF EXISTS "app_orders_insert_by_role" ON public.app_orders;
CREATE POLICY app_orders_insert_by_role
ON public.app_orders
FOR INSERT
TO anon, authenticated
WITH CHECK (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
);

DROP POLICY IF EXISTS "app_orders_update_by_role" ON public.app_orders;
CREATE POLICY app_orders_update_by_role
ON public.app_orders
FOR UPDATE
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
)
WITH CHECK (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
);

DROP POLICY IF EXISTS "app_orders_delete_admin" ON public.app_orders;
CREATE POLICY app_orders_delete_admin
ON public.app_orders
FOR DELETE
TO anon, authenticated
USING (public.is_admin_policy_request());

DROP POLICY IF EXISTS "notifications_select_by_role" ON public.notifications;
CREATE POLICY notifications_select_by_role
ON public.notifications
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_notification_scope(user_id, role_target)
  OR (
    public.is_driver_policy_request()
    AND (
      public.is_same_request_user(user_id)
      OR (user_id IS NULL AND UPPER(COALESCE(role_target, '')) IN ('DRIVER', 'DELIVERY'))
    )
  )
  OR (
    public.is_customer_policy_request()
    AND (
      public.is_same_request_user(user_id)
    )
  )
);

DROP POLICY IF EXISTS "notifications_insert_staff" ON public.notifications;
CREATE POLICY notifications_insert_staff
ON public.notifications
FOR INSERT
TO anon, authenticated
WITH CHECK (
  public.is_admin_policy_request()
  OR public.is_cashier_notification_scope(user_id, role_target)
);

DROP POLICY IF EXISTS "notifications_update_by_role" ON public.notifications;
CREATE POLICY notifications_update_by_role
ON public.notifications
FOR UPDATE
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_notification_scope(user_id, role_target)
  OR public.is_same_request_user(user_id)
)
WITH CHECK (
  public.is_admin_policy_request()
  OR public.is_cashier_notification_scope(user_id, role_target)
  OR public.is_same_request_user(user_id)
);

DROP POLICY IF EXISTS "notifications_delete_staff" ON public.notifications;
CREATE POLICY notifications_delete_staff
ON public.notifications
FOR DELETE
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_notification_scope(user_id, role_target)
);

DROP POLICY IF EXISTS "coupon_usage_select_by_role" ON public.coupon_usage;
CREATE POLICY coupon_usage_select_by_role
ON public.coupon_usage
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
  OR (public.is_customer_policy_request() AND public.is_same_request_user(customer_id))
);

DROP POLICY IF EXISTS "coupon_usage_insert_by_role" ON public.coupon_usage;
CREATE POLICY coupon_usage_insert_by_role
ON public.coupon_usage
FOR INSERT
TO anon, authenticated
WITH CHECK (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
  OR (public.is_customer_policy_request() AND public.is_same_request_user(customer_id))
);

DROP POLICY IF EXISTS "coupon_usage_delete_staff" ON public.coupon_usage;
CREATE POLICY coupon_usage_delete_staff
ON public.coupon_usage
FOR DELETE
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
);

DROP POLICY IF EXISTS "customer_addresses_select_by_role" ON public.customer_addresses;
CREATE POLICY customer_addresses_select_by_role
ON public.customer_addresses
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
  OR public.is_same_request_user(customer_user_id)
  OR (
    customer_user_id IS NULL
    AND public.is_current_request_legacy_owner_key(customer_phone)
  )
);

DROP POLICY IF EXISTS "customer_addresses_write_by_role" ON public.customer_addresses;
CREATE POLICY customer_addresses_write_by_role
ON public.customer_addresses
FOR ALL
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
  OR public.is_same_request_user(customer_user_id)
)
WITH CHECK (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
  OR public.is_same_request_user(customer_user_id)
);

DROP POLICY IF EXISTS "pos_registered_addresses_select_by_role" ON public.pos_registered_addresses;
CREATE POLICY pos_registered_addresses_select_by_role
ON public.pos_registered_addresses
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
  OR public.is_same_request_phone(phone)
);

DROP POLICY IF EXISTS "pos_registered_addresses_write_staff" ON public.pos_registered_addresses;
CREATE POLICY pos_registered_addresses_write_staff
ON public.pos_registered_addresses
FOR ALL
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
)
WITH CHECK (
  public.is_admin_policy_request()
  OR public.is_cashier_policy_request()
);

DROP POLICY IF EXISTS "support_threads_select_by_role" ON public.support_threads;
CREATE POLICY support_threads_select_by_role
ON public.support_threads
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR (public.is_customer_policy_request() AND public.is_same_request_user(customer_id))
);

DROP POLICY IF EXISTS "support_threads_insert_by_role" ON public.support_threads;
CREATE POLICY support_threads_insert_by_role
ON public.support_threads
FOR INSERT
TO anon, authenticated
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "support_threads_update_by_role" ON public.support_threads;
CREATE POLICY support_threads_update_by_role
ON public.support_threads
FOR UPDATE
TO anon, authenticated
USING (public.is_admin_policy_request())
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "support_threads_delete_admin" ON public.support_threads;
CREATE POLICY support_threads_delete_admin
ON public.support_threads
FOR DELETE
TO anon, authenticated
USING (public.is_admin_policy_request());

DROP POLICY IF EXISTS "support_messages_select_by_role" ON public.support_messages;
CREATE POLICY support_messages_select_by_role
ON public.support_messages
FOR SELECT
TO anon, authenticated
USING (
  public.is_admin_policy_request()
  OR EXISTS (
    SELECT 1
    FROM public.support_threads st
    WHERE st.id = support_messages.thread_id
      AND public.is_customer_policy_request()
      AND public.is_same_request_user(st.customer_id)
  )
);

DROP POLICY IF EXISTS "support_messages_insert_by_role" ON public.support_messages;
CREATE POLICY support_messages_insert_by_role
ON public.support_messages
FOR INSERT
TO anon, authenticated
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "support_messages_update_admin" ON public.support_messages;
CREATE POLICY support_messages_update_admin
ON public.support_messages
FOR UPDATE
TO anon, authenticated
USING (public.is_admin_policy_request())
WITH CHECK (public.is_admin_policy_request());

DROP POLICY IF EXISTS "support_messages_delete_admin" ON public.support_messages;
CREATE POLICY support_messages_delete_admin
ON public.support_messages
FOR DELETE
TO anon, authenticated
USING (public.is_admin_policy_request());

-- -----------------------------------------------------------------------------
-- Push observability and robust delivery checks
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.push_delivery_logs (
  id BIGSERIAL PRIMARY KEY,
  request_id BIGINT,
  target TEXT,
  status_code INT,
  error_msg TEXT,
  response_body TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_push_delivery_logs_created_at
  ON public.push_delivery_logs(created_at DESC);

CREATE OR REPLACE FUNCTION public.api_default_push_edge_url(
  p_function_name TEXT DEFAULT 'push-fcm-v1'
)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_function_name TEXT := COALESCE(
    NULLIF(TRIM(COALESCE(p_function_name, '')), ''),
    'push-fcm-v1'
  );
  v_host TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_header_value(ARRAY['x-forwarded-host', 'host']),
    ''
  )), '');
  v_origin TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_header_value(ARRAY['origin']),
    ''
  )), '');
  v_referer TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_header_value(ARRAY['referer']),
    ''
  )), '');
  v_issuer TEXT := NULLIF(TRIM(COALESCE(
    public.api_request_claim_value(ARRAY['iss']),
    ''
  )), '');
  v_base_url TEXT;
BEGIN
  IF v_origin IS NOT NULL AND v_origin ~* '^https?://' THEN
    v_base_url := substring(v_origin FROM '^(https?://[^/]+)');
  ELSIF v_issuer IS NOT NULL AND v_issuer ~* '^https?://' THEN
    v_base_url := regexp_replace(v_issuer, '/auth/v1/?$', '', 'i');
  ELSIF v_referer IS NOT NULL AND v_referer ~* '^https?://' THEN
    v_base_url := substring(v_referer FROM '^(https?://[^/]+)');
  ELSIF v_host IS NOT NULL THEN
    v_base_url := CASE
      WHEN v_host ~* '^https?://' THEN substring(v_host FROM '^(https?://[^/]+)')
      ELSE 'https://' || v_host
    END;
  END IF;

  IF v_base_url IS NULL OR TRIM(v_base_url) = '' THEN
    RETURN NULL;
  END IF;

  v_base_url := regexp_replace(v_base_url, '/rest/v1/?$', '', 'i');
  v_base_url := regexp_replace(v_base_url, '/auth/v1/?$', '', 'i');
  v_base_url := regexp_replace(v_base_url, '/functions/v1/?$', '', 'i');
  v_base_url := regexp_replace(v_base_url, '/+$', '');

  RETURN v_base_url || '/functions/v1/' || v_function_name;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_push_http_post_fcm(
  p_headers JSONB,
  p_payload JSONB
)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_request_id BIGINT;
  v_push_mode TEXT := 'EDGE_V1';
  v_server_key TEXT;
  v_server_key_is_json BOOLEAN := false;
  v_can_legacy BOOLEAN := false;
  v_edge_url TEXT;
  v_edge_secret TEXT;
  v_edge_jwt TEXT;
  v_default_edge_url TEXT;
  v_send_url TEXT;
  v_send_headers JSONB := '{}'::jsonb;
  v_send_payload JSONB := COALESCE(p_payload, '{}'::jsonb);
  v_target TEXT := COALESCE(NULLIF(TRIM(COALESCE(p_payload->>'to', '')), ''), '');
  v_status_code INT;
  v_response_body TEXT;
  v_probe_status INT;
  v_probe_body TEXT;
  v_probe_try INT := 0;
  v_force_legacy BOOLEAN := false;
  v_error_label TEXT := NULL;
  v_force_sync_probe BOOLEAN := false;
BEGIN
  v_force_sync_probe := (
    NULLIF(TRIM(COALESCE(p_payload->'notification'->>'image', '')), '') IS NOT NULL
    OR NULLIF(TRIM(COALESCE(p_payload->'notification'->>'imageUrl', '')), '') IS NOT NULL
    OR NULLIF(TRIM(COALESCE(p_payload->'data'->>'image_url', '')), '') IS NOT NULL
    OR NULLIF(TRIM(COALESCE(p_payload->'data'->>'imageUrl', '')), '') IS NOT NULL
  );

  SELECT
    NULLIF(TRIM(COALESCE(push_provider, '')), ''),
    NULLIF(TRIM(COALESCE(fcm_server_key, '')), ''),
    NULLIF(TRIM(COALESCE(push_edge_url, '')), ''),
    NULLIF(TRIM(COALESCE(push_edge_secret, '')), ''),
    NULLIF(TRIM(COALESCE(push_edge_jwt, '')), '')
  INTO v_push_mode, v_server_key, v_edge_url, v_edge_secret, v_edge_jwt
  FROM public.app_settings
  WHERE id = 1
  LIMIT 1;

  v_default_edge_url := public.api_default_push_edge_url('push-fcm-v1');
  IF (v_edge_url IS NULL OR v_edge_url = '')
     AND v_default_edge_url IS NOT NULL THEN
    v_edge_url := v_default_edge_url;
  END IF;

  v_server_key_is_json := (v_server_key IS NOT NULL AND LEFT(TRIM(v_server_key), 1) = '{');
  v_can_legacy := (v_server_key IS NOT NULL AND v_server_key <> '' AND NOT v_server_key_is_json);

  IF v_push_mode IS NULL OR v_push_mode = '' THEN
    IF v_edge_url IS NOT NULL AND v_edge_url <> '' THEN
      v_push_mode := 'EDGE_V1';
    ELSIF v_can_legacy THEN
      v_push_mode := 'LEGACY';
    ELSE
      v_push_mode := 'EDGE_V1';
    END IF;
  ELSE
    v_push_mode := UPPER(v_push_mode);
  END IF;

  IF v_push_mode NOT IN ('EDGE_V1', 'LEGACY') THEN
    IF v_edge_url IS NOT NULL AND v_edge_url <> '' THEN
      v_push_mode := 'EDGE_V1';
    ELSIF v_can_legacy THEN
      v_push_mode := 'LEGACY';
    ELSE
      INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
      VALUES (
        NULL,
        v_target,
        NULL,
        'push_provider_unsupported',
        jsonb_build_object('mode', v_push_mode)::TEXT
      );
      RETURN false;
    END IF;
  END IF;

  IF v_push_mode = 'EDGE_V1' THEN
    IF v_edge_url IS NULL OR v_edge_url = '' THEN
      IF v_can_legacy THEN
        v_push_mode := 'LEGACY';
      ELSE
        -- Try to auto-resolve Edge URL before failing
        v_edge_url := public.api_default_push_edge_url('push-fcm-v1');
        IF v_edge_url IS NULL OR v_edge_url = '' THEN
          INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
          VALUES (
            NULL,
            v_target,
            NULL,
            'edge_url_missing_and_unresolvable',
            jsonb_build_object('mode', v_push_mode, 'attempted_auto_resolve', true)::TEXT
          );
          RETURN false;
        END IF;
      END IF;
    END IF;
    
    IF v_push_mode = 'EDGE_V1' THEN
      v_send_url := v_edge_url;
      v_send_headers := jsonb_build_object('Content-Type', 'application/json');
      IF v_edge_secret IS NOT NULL AND v_edge_secret <> '' THEN
        v_send_headers := v_send_headers || jsonb_build_object('x-push-secret', v_edge_secret);
      END IF;
      IF v_edge_jwt IS NOT NULL AND v_edge_jwt <> '' THEN
        v_send_headers := v_send_headers
          || jsonb_build_object('Authorization', 'Bearer ' || v_edge_jwt)
          || jsonb_build_object('apikey', v_edge_jwt);
      END IF;
      v_send_payload := jsonb_strip_nulls(jsonb_build_object(
        'legacy_payload', COALESCE(p_payload, '{}'::jsonb),
        'service_account_json', CASE
          WHEN v_server_key_is_json THEN v_server_key
          ELSE NULL
        END
      ));

      BEGIN
        EXECUTE 'SELECT net.http_post($1::text, $2::jsonb, $3::jsonb, $4::jsonb, $5::integer)'
        INTO v_request_id
        USING
          v_send_url,
          COALESCE(v_send_payload, '{}'::jsonb),
          '{}'::jsonb,
          COALESCE(v_send_headers, '{}'::jsonb),
          10000;
      EXCEPTION WHEN OTHERS THEN
        v_request_id := NULL;
        v_error_label := 'net.http_post_failed';
      END;

      IF v_request_id IS NULL THEN
        IF v_can_legacy THEN
          v_force_legacy := true;
          v_error_label := COALESCE(v_error_label, 'edge_request_id_missing');
        ELSE
          INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
          VALUES (
            NULL,
            v_target,
            NULL,
            COALESCE(v_error_label, 'edge_request_id_missing'),
            jsonb_build_object('mode', 'EDGE_V1', 'url', v_send_url)::TEXT
          );
          RETURN false;
        END IF;
      ELSE
        IF NOT v_force_sync_probe THEN
          INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
          VALUES (
            v_request_id,
            v_target,
            202,
            'queued_async_pending',
            jsonb_build_object(
              'mode', 'EDGE_V1',
              'url', v_send_url,
              'request_id', v_request_id
            )::TEXT
          );
          RETURN true;
        END IF;

        v_probe_status := NULL;
        v_probe_body := NULL;
        v_probe_try := 0;
        LOOP
          BEGIN
            EXECUTE 'SELECT status_code, content::text FROM net._http_response WHERE id = $1'
            INTO v_probe_status, v_probe_body
            USING v_request_id;
          EXCEPTION WHEN OTHERS THEN
            v_probe_status := NULL;
            v_probe_body := NULL;
          END;

          EXIT WHEN v_probe_status IS NOT NULL OR v_probe_try >= 7;
          v_probe_try := v_probe_try + 1;
          PERFORM pg_sleep(0.1);
        END LOOP;

        IF v_probe_status IS NULL THEN
          INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
          VALUES (
            v_request_id,
            v_target,
            202,
            'queued_async_pending',
            jsonb_build_object(
              'mode', 'EDGE_V1',
              'url', v_send_url,
              'request_id', v_request_id
            )::TEXT
          );
          RETURN true;
        END IF;

        INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
        VALUES (
          v_request_id,
          v_target,
          v_probe_status,
          CASE
            WHEN v_probe_status BETWEEN 200 AND 299 THEN NULL
            ELSE 'http_' || v_probe_status::TEXT
          END,
          CASE
            WHEN v_probe_body IS NULL OR TRIM(v_probe_body) = '' THEN
              jsonb_build_object(
                'mode', 'EDGE_V1',
                'url', v_send_url,
                'request_id', v_request_id
              )::TEXT
            ELSE LEFT(v_probe_body, 4000)
          END
        );
        RETURN (v_probe_status BETWEEN 200 AND 299);
      END IF;
    END IF;
  END IF;

  IF v_push_mode = 'LEGACY' OR v_force_legacy THEN
    IF NOT v_can_legacy THEN
      INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
      VALUES (
        NULL,
        v_target,
        NULL,
        'legacy_server_key_missing',
        jsonb_build_object('mode', v_push_mode)::TEXT
      );
      RETURN false;
    END IF;
    v_push_mode := 'LEGACY';
    v_send_url := 'https://fcm.googleapis.com/fcm/send';
    v_send_headers := COALESCE(p_headers, '{}'::jsonb)
      || jsonb_build_object('Authorization', 'key=' || v_server_key)
      || jsonb_build_object('Content-Type', 'application/json');
    v_send_payload := COALESCE(p_payload, '{}'::jsonb);

    BEGIN
      EXECUTE 'SELECT net.http_post($1::text, $2::jsonb, $3::jsonb, $4::jsonb, $5::integer)'
      INTO v_request_id
      USING
        v_send_url,
        COALESCE(v_send_payload, '{}'::jsonb),
        '{}'::jsonb,
        COALESCE(v_send_headers, '{}'::jsonb),
        10000;
    EXCEPTION WHEN OTHERS THEN
      INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
      VALUES (
        NULL,
        v_target,
        NULL,
        COALESCE(v_error_label, 'net.http_post_failed'),
        jsonb_build_object(
          'mode', v_push_mode,
          'url', v_send_url,
          'error', SQLERRM
        )::TEXT
      );
      RETURN false;
    END;

    IF v_request_id IS NULL THEN
      INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
      VALUES (
        NULL,
        v_target,
        NULL,
        COALESCE(v_error_label, 'no_request_id'),
        jsonb_build_object('mode', v_push_mode, 'url', v_send_url)::TEXT
      );
      RETURN false;
    END IF;

    IF NOT v_force_sync_probe THEN
      INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
      VALUES (
        v_request_id,
        v_target,
        NULL,
        'queued',
        jsonb_build_object('mode', v_push_mode, 'url', v_send_url)::TEXT
      );
      RETURN true;
    END IF;

    v_probe_status := NULL;
    v_probe_body := NULL;
    v_probe_try := 0;
    LOOP
      BEGIN
        EXECUTE 'SELECT status_code, content::text FROM net._http_response WHERE id = $1'
        INTO v_probe_status, v_probe_body
        USING v_request_id;
      EXCEPTION WHEN OTHERS THEN
        v_probe_status := NULL;
        v_probe_body := NULL;
      END;

      EXIT WHEN v_probe_status IS NOT NULL OR v_probe_try >= 7;
      v_probe_try := v_probe_try + 1;
      PERFORM pg_sleep(0.1);
    END LOOP;

    IF v_probe_status IS NULL THEN
      INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
      VALUES (
        v_request_id,
        v_target,
        NULL,
        'queued',
        jsonb_build_object('mode', v_push_mode, 'url', v_send_url)::TEXT
      );
      RETURN true;
    END IF;

    INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
    VALUES (
      v_request_id,
      v_target,
      v_probe_status,
      CASE
        WHEN v_probe_status BETWEEN 200 AND 299 THEN NULL
        ELSE 'http_' || v_probe_status::TEXT
      END,
      CASE
        WHEN v_probe_body IS NULL OR TRIM(v_probe_body) = '' THEN
          jsonb_build_object('mode', v_push_mode, 'url', v_send_url)::TEXT
        ELSE LEFT(v_probe_body, 4000)
      END
    );
    RETURN (v_probe_status BETWEEN 200 AND 299);
  END IF;

  INSERT INTO public.push_delivery_logs(request_id, target, status_code, error_msg, response_body)
  VALUES (
    NULL,
    v_target,
    NULL,
    'push_dispatch_skipped',
    jsonb_build_object('mode', v_push_mode)::TEXT
  );
  RETURN false;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_push_diagnostics(
  p_user_id TEXT DEFAULT NULL,
  p_role_target TEXT DEFAULT NULL
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_push_enabled BOOLEAN := false;
  v_has_server_key BOOLEAN := false;
  v_has_service_account_json BOOLEAN := false;
  v_push_mode TEXT := 'LEGACY';
  v_edge_configured BOOLEAN := false;
  v_edge_jwt_configured BOOLEAN := false;
  v_edge_secret_configured BOOLEAN := false;
  v_edge_url TEXT;
  v_resolved_edge_url TEXT;
  v_pg_net_available BOOLEAN := false;
  v_total_tokens INT := 0;
  v_user_tokens INT := 0;
  v_role_tokens INT := 0;
  v_role TEXT := UPPER(TRIM(COALESCE(p_role_target, '')));
  v_last_status INT;
  v_last_error TEXT;
  v_last_target TEXT;
  v_last_response_body TEXT;
  v_last_created TIMESTAMPTZ;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN json_build_object('ok', false, 'error', 'forbidden');
  END IF;

  PERFORM public.api_push_collect_recent_responses(100);

  SELECT
    COALESCE(push_enabled, false),
    NULLIF(TRIM(COALESCE(fcm_server_key, '')), '') IS NOT NULL,
    COALESCE(NULLIF(TRIM(COALESCE(push_provider, '')), ''), 'EDGE_V1'),
    NULLIF(TRIM(COALESCE(push_edge_url, '')), ''),
    NULLIF(TRIM(COALESCE(push_edge_jwt, '')), '') IS NOT NULL,
    NULLIF(TRIM(COALESCE(push_edge_secret, '')), '') IS NOT NULL,
    (
      NULLIF(TRIM(COALESCE(fcm_server_key, '')), '') IS NOT NULL
      AND LEFT(TRIM(COALESCE(fcm_server_key, '')), 1) = '{'
    )
  INTO
    v_push_enabled,
    v_has_server_key,
    v_push_mode,
    v_edge_url,
    v_edge_jwt_configured,
    v_edge_secret_configured,
    v_has_service_account_json
  FROM public.app_settings
  WHERE id = 1
  LIMIT 1;

  v_edge_configured := v_edge_url IS NOT NULL AND v_edge_url <> '';
  v_resolved_edge_url := COALESCE(
    v_edge_url,
    public.api_default_push_edge_url('push-fcm-v1')
  );

  v_pg_net_available := (
    to_regprocedure('net.http_post(text,jsonb,jsonb,jsonb,integer)') IS NOT NULL
    OR to_regprocedure('net.http_post(text,jsonb,jsonb,jsonb)') IS NOT NULL
    OR to_regprocedure('net.http_post(text,jsonb,jsonb)') IS NOT NULL
  );

  SELECT COUNT(*)
  INTO v_total_tokens
  FROM public.user_push_tokens t
  WHERE NULLIF(TRIM(COALESCE(t.fcm_token, '')), '') IS NOT NULL;

  IF p_user_id IS NOT NULL AND TRIM(p_user_id) <> '' THEN
    SELECT COUNT(*)
    INTO v_user_tokens
    FROM public.user_push_tokens t
    WHERE t.user_id = p_user_id
      AND NULLIF(TRIM(COALESCE(t.fcm_token, '')), '') IS NOT NULL;
  END IF;

  IF v_role = '' THEN
    SELECT COUNT(DISTINCT t.fcm_token)
    INTO v_role_tokens
    FROM public.user_push_tokens t
    JOIN public.users u ON u.id = t.user_id
    WHERE u.is_active = true
      AND NULLIF(TRIM(COALESCE(t.fcm_token, '')), '') IS NOT NULL;
  ELSE
    SELECT COUNT(DISTINCT t.fcm_token)
    INTO v_role_tokens
    FROM public.user_push_tokens t
    JOIN public.users u ON u.id = t.user_id
    WHERE u.is_active = true
      AND NULLIF(TRIM(COALESCE(t.fcm_token, '')), '') IS NOT NULL
      AND (
        (v_role = 'DELIVERY' AND UPPER(COALESCE(u.role, '')) IN ('DRIVER', 'ADMIN_POS'))
        OR (v_role = 'DRIVER' AND UPPER(COALESCE(u.role, '')) = 'DRIVER')
        OR (v_role = 'CASHIER' AND UPPER(COALESCE(u.role, '')) IN ('CASHIER', 'ADMIN_POS'))
        OR (v_role NOT IN ('DELIVERY', 'DRIVER', 'CASHIER') AND UPPER(COALESCE(u.role, '')) = v_role)
      );
  END IF;

  SELECT status_code, error_msg, target, response_body, created_at
  INTO v_last_status, v_last_error, v_last_target, v_last_response_body, v_last_created
  FROM public.push_delivery_logs
  ORDER BY id DESC
  LIMIT 1;

  RETURN json_build_object(
    'pushEnabled', v_push_enabled,
    'hasServerKey', v_has_server_key,
    'pushMode', v_push_mode,
    'edgeConfigured', v_edge_configured,
    'edgeSecretConfigured', v_edge_secret_configured,
    'edgeJwtConfigured', v_edge_jwt_configured,
    'resolvedPushEdgeUrl', v_resolved_edge_url,
    'hasServiceAccountJson', v_has_service_account_json,
    'pgNetAvailable', v_pg_net_available,
    'totalTokenCount', v_total_tokens,
    'userTokenCount', v_user_tokens,
    'roleTokenCount', v_role_tokens,
    'latestPushStatus', v_last_status,
    'latestPushError', v_last_error,
    'latestPushTarget', v_last_target,
    'latestPushResponseBody', v_last_response_body,
    'latestPushAt', v_last_created
  );
END;
$$;

REVOKE ALL ON TABLE public.push_delivery_logs FROM anon, authenticated;
ALTER TABLE public.push_delivery_logs ENABLE ROW LEVEL SECURITY;
GRANT EXECUTE ON FUNCTION public.api_push_diagnostics(TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.current_app_user_id() TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.current_app_role() TO anon, authenticated;
REVOKE EXECUTE ON FUNCTION public.api_verify_phone_password_login(TEXT, TEXT) FROM anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_verify_phone_password_login(TEXT, TEXT) TO service_role;

REVOKE EXECUTE ON FUNCTION public.api_push_http_post_fcm(JSONB, JSONB) FROM anon, authenticated;
REVOKE EXECUTE ON FUNCTION public.api_push_collect_recent_responses(INT) FROM anon, authenticated;
REVOKE EXECUTE ON FUNCTION public.api_push_fcm_to_user_tokens(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) FROM anon, authenticated;
REVOKE EXECUTE ON FUNCTION public.api_push_fcm_to_role_tokens(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) FROM anon, authenticated;
REVOKE EXECUTE ON FUNCTION public.api_push_fcm_to_topic(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) FROM anon, authenticated;
REVOKE EXECUTE ON FUNCTION public.api_emit_order_notification(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) FROM anon, authenticated;

GRANT EXECUTE ON FUNCTION public.api_push_http_post_fcm(JSONB, JSONB) TO service_role;
GRANT EXECUTE ON FUNCTION public.api_push_collect_recent_responses(INT) TO service_role;
GRANT EXECUTE ON FUNCTION public.api_push_fcm_to_user_tokens(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.api_push_fcm_to_role_tokens(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.api_push_fcm_to_topic(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.api_emit_order_notification(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT) TO service_role;

-- -----------------------------------------------------------------------------
-- Storage: complaints attachments (image + video)
-- -----------------------------------------------------------------------------
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'complaints',
  'complaints',
  false,
  15728640,
  ARRAY[
    'image/jpeg',
    'image/png',
    'image/webp',
    'image/heic',
    'image/heif',
    'image/jpg',
    'image/tiff',
    'image/bmp',
    'video/mp4',
    'video/quicktime',
    'video/webm',
    'video/x-matroska',
    'video/3gpp'
  ]
)
ON CONFLICT (id) DO UPDATE
SET
  public = EXCLUDED.public,
  file_size_limit = EXCLUDED.file_size_limit,
  allowed_mime_types = EXCLUDED.allowed_mime_types;

DO $$
BEGIN
  DROP POLICY IF EXISTS "complaints_objects_select" ON storage.objects;
CREATE POLICY complaints_objects_select
ON storage.objects
  FOR SELECT
  TO anon, authenticated
  USING (false);

  DROP POLICY IF EXISTS "complaints_objects_insert" ON storage.objects;
CREATE POLICY complaints_objects_insert
ON storage.objects
  FOR INSERT
  TO anon, authenticated
  WITH CHECK (false);

  DROP POLICY IF EXISTS "complaints_objects_update" ON storage.objects;
CREATE POLICY complaints_objects_update
ON storage.objects
  FOR UPDATE
  TO anon, authenticated
  USING (false)
  WITH CHECK (false);

  DROP POLICY IF EXISTS "complaints_objects_delete" ON storage.objects;
CREATE POLICY complaints_objects_delete
ON storage.objects
  FOR DELETE
  TO anon, authenticated
  USING (false);
END;
$$;

-- Ensure Realtime publication includes order and notification tables.
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'supabase_realtime') THEN
    IF NOT EXISTS (
      SELECT 1
      FROM pg_publication_tables
      WHERE pubname = 'supabase_realtime'
        AND schemaname = 'public'
        AND tablename = 'inventory_items'
    ) THEN
      ALTER PUBLICATION supabase_realtime ADD TABLE public.inventory_items;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM pg_publication_tables
      WHERE pubname = 'supabase_realtime'
        AND schemaname = 'public'
        AND tablename = 'inventory_item_links'
    ) THEN
      ALTER PUBLICATION supabase_realtime ADD TABLE public.inventory_item_links;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM pg_publication_tables
      WHERE pubname = 'supabase_realtime'
        AND schemaname = 'public'
        AND tablename = 'app_orders'
    ) THEN
      ALTER PUBLICATION supabase_realtime ADD TABLE public.app_orders;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM pg_publication_tables
      WHERE pubname = 'supabase_realtime'
        AND schemaname = 'public'
        AND tablename = 'notifications'
    ) THEN
      ALTER PUBLICATION supabase_realtime ADD TABLE public.notifications;
    END IF;
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION public.api_admin_send_notification(
  p_user_ids JSONB DEFAULT '[]'::jsonb,
  p_role_target TEXT DEFAULT NULL,
  p_title TEXT DEFAULT '',
  p_message TEXT DEFAULT '',
  p_image_url TEXT DEFAULT NULL,
  p_targeting_meta JSONB DEFAULT '{}'::jsonb,
  p_test_only BOOLEAN DEFAULT false
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_title TEXT := NULLIF(TRIM(COALESCE(p_title, '')), '');
  v_message TEXT := NULLIF(TRIM(COALESCE(p_message, '')), '');
  v_image_url TEXT := NULLIF(TRIM(COALESCE(p_image_url, '')), '');
  v_role_target TEXT := UPPER(NULLIF(TRIM(COALESCE(p_role_target, '')), ''));
  v_targeting_meta JSONB := COALESCE(p_targeting_meta, '{}'::jsonb);
  v_requested_user_id TEXT;
  v_inserted_count INT := 0;
  v_push_sent_count INT := 0;
  v_inserted_user_ids JSONB := '[]'::jsonb;
  v_notification_message TEXT;
  v_target_role_for_row TEXT;
BEGIN
  IF v_actor_role NOT IN ('ADMIN', 'ADMIN_POS', 'SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok', false, 'error', 'forbidden');
  END IF;

  IF v_title IS NULL THEN
    RETURN jsonb_build_object('ok', false, 'error', 'title_required');
  END IF;

  IF v_message IS NULL THEN
    RETURN jsonb_build_object('ok', false, 'error', 'message_required');
  END IF;

  IF jsonb_typeof(COALESCE(p_user_ids, '[]'::jsonb)) IS DISTINCT FROM 'array' THEN
    RETURN jsonb_build_object('ok', false, 'error', 'user_ids_must_be_array');
  END IF;

  IF COALESCE(jsonb_array_length(COALESCE(p_user_ids, '[]'::jsonb)), 0) = 0 THEN
    RETURN jsonb_build_object('ok', false, 'error', 'target_users_required');
  END IF;

  v_notification_message := v_message;

  PERFORM set_config(
    'app.current_notification_image_url',
    COALESCE(v_image_url, ''),
    true
  );

  FOR v_requested_user_id IN
    SELECT DISTINCT NULLIF(TRIM(value #>> '{}'), '')
    FROM jsonb_array_elements(COALESCE(p_user_ids, '[]'::jsonb))
  LOOP
    IF v_requested_user_id IS NULL THEN
      CONTINUE;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM public.users u
      WHERE u.id = v_requested_user_id
        AND COALESCE(u.is_active, true) = true
    ) THEN
      CONTINUE;
    END IF;

    v_target_role_for_row := COALESCE(
      v_role_target,
      UPPER(COALESCE((SELECT role FROM public.users WHERE id = v_requested_user_id LIMIT 1), 'CUSTOMER'))
    );

    INSERT INTO public.notifications (
      user_id,
      role_target,
      order_id,
      order_type,
      title,
      message,
      image_url,
      read
    )
    VALUES (
      v_requested_user_id,
      v_target_role_for_row,
      NULL,
      CASE WHEN p_test_only THEN 'ADMIN_TEST' ELSE 'ADMIN_BROADCAST' END,
      v_title,
      CASE
        WHEN v_target_role_for_row = 'CASHIER' THEN v_message
        WHEN v_image_url IS NOT NULL THEN v_message || E'\n\n[image]' || v_image_url
        ELSE v_notification_message
      END,
      v_image_url,
      false
    );

    v_inserted_count := v_inserted_count + 1;
    v_inserted_user_ids := v_inserted_user_ids || to_jsonb(v_requested_user_id);

    BEGIN
      v_push_sent_count := v_push_sent_count + COALESCE(
        public.api_push_fcm_to_user_tokens(
          p_user_id := v_requested_user_id,
          p_title := v_title,
          p_message := v_message,
          p_order_id := NULL,
          p_order_type := CASE WHEN p_test_only THEN 'ADMIN_TEST' ELSE 'ADMIN_BROADCAST' END,
          p_status := NULL,
          p_image_url := v_image_url
        ),
        0
      );
    EXCEPTION
      WHEN OTHERS THEN
        NULL;
    END;
  END LOOP;

  RETURN jsonb_build_object(
    'ok', true,
    'inserted', v_inserted_count,
    'push_sent', v_push_sent_count,
    'user_ids', v_inserted_user_ids,
    'test_only', p_test_only
  );
END;
$$;

REVOKE ALL ON FUNCTION public.api_admin_send_notification(JSONB, TEXT, TEXT, TEXT, TEXT, JSONB, BOOLEAN) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.api_admin_send_notification(JSONB, TEXT, TEXT, TEXT, TEXT, JSONB, BOOLEAN) TO anon, authenticated;

-- Force PostgREST to refresh schema cache after DDL/GRANT changes.
NOTIFY pgrst, 'reload schema';

-- --- BEGIN SCHEMA PATCH CONSOLIDATION ---

-- =============================================================================
-- Fale7 POS - Schema Patch v2.0
-- يُضاف هذا الملف بعد supabase_schema.sql الحالي
-- يعالج: kitchen_print_count | order sequence | idempotency | locks
-- =============================================================================

-- ---------------------------------------------------------------------------
-- 1) kitchen_print_count في app_orders
-- ---------------------------------------------------------------------------
ALTER TABLE public.app_orders
  ADD COLUMN IF NOT EXISTS kitchen_print_count INT NOT NULL DEFAULT 0;

-- إعادة ضبط للأوردرات القديمة التي طُبعت بالفعل (من metadata)
UPDATE public.app_orders
SET kitchen_print_count = 1
WHERE kitchen_print_count = 0
  AND COALESCE((metadata->>'kitchenPrinted')::boolean, false) = true;

-- RPC ذري للطباعة - يضمن عدم التكرار بين جهازين
CREATE OR REPLACE FUNCTION public.api_pos_increment_kitchen_print(
  p_order_id      TEXT,
  p_allow_reprint BOOLEAN DEFAULT false
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role  TEXT := public.current_app_role();
  v_new_count   INT;
BEGIN
  IF v_actor_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  -- زيادة ذرية
  UPDATE public.app_orders
  SET kitchen_print_count = kitchen_print_count + 1,
      updated_at           = now()
  WHERE id = p_order_id
  RETURNING kitchen_print_count INTO v_new_count;

  IF NOT FOUND THEN
    RETURN jsonb_build_object('ok',false,'error','order_not_found');
  END IF;

  -- الطباعة الثانية فأكثر تحتاج موافقة صريحة
  IF v_new_count > 1 AND NOT COALESCE(p_allow_reprint,false) THEN
    -- تراجع
    UPDATE public.app_orders
    SET kitchen_print_count = kitchen_print_count - 1
    WHERE id = p_order_id;
    RETURN jsonb_build_object(
      'ok',        false,
      'error',     'kitchen_already_printed',
      'printCount', v_new_count - 1
    );
  END IF;

  RETURN jsonb_build_object(
    'ok',        true,
    'printCount', v_new_count,
    'isReprint',  v_new_count > 1
  );
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_increment_kitchen_print(TEXT,BOOLEAN) TO anon, authenticated;

-- ---------------------------------------------------------------------------
-- 2) Sequence موحد للأوردرات على مستوى الفرع
-- ---------------------------------------------------------------------------
CREATE SEQUENCE IF NOT EXISTS public.pos_order_branch_seq
  START WITH 1
  INCREMENT BY 1
  NO CYCLE
  CACHE 1;

GRANT USAGE ON SEQUENCE public.pos_order_branch_seq TO anon, authenticated;

CREATE OR REPLACE FUNCTION public.api_pos_next_order_number()
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
  v_next       BIGINT;
BEGIN
  IF v_actor_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  v_next := nextval('public.pos_order_branch_seq');

  RETURN jsonb_build_object(
    'ok',           true,
    'orderNumber',  v_next,
    'orderNumberStr', lpad(v_next::text, 6, '0')
  );
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_next_order_number() TO anon, authenticated;

-- ---------------------------------------------------------------------------
-- 3) Idempotency Keys (Offline/Sync)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.pos_idempotency_keys (
  key            TEXT PRIMARY KEY,
  operation_type TEXT NOT NULL,
  item_key       TEXT NOT NULL DEFAULT '',
  device_id      TEXT NOT NULL DEFAULT '',
  result         JSONB,
  created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
  expires_at     TIMESTAMPTZ NOT NULL DEFAULT (now() + INTERVAL '7 days')
);

CREATE INDEX IF NOT EXISTS idx_pos_idempotency_expires
  ON public.pos_idempotency_keys(expires_at);

-- تنظيف دوري للمفاتيح المنتهية
CREATE OR REPLACE FUNCTION public.api_pos_cleanup_idempotency_keys()
RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_count INT;
BEGIN
  DELETE FROM public.pos_idempotency_keys WHERE expires_at < now();
  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$;

-- RPC إنشاء أوردر مع Idempotency
CREATE OR REPLACE FUNCTION public.api_pos_create_order_idempotent(
    p_idempotency_key text,
    p_order_payload jsonb
)
RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_inserted boolean := false;
    v_existing jsonb;
    v_result jsonb;
BEGIN
    -- 1. Atomic key registration lock
    INSERT INTO public.pos_idempotency_keys (key, operation_type, item_key, device_id, created_at, expires_at)
    VALUES (
        p_idempotency_key,
        COALESCE(p_order_payload->>'operationType', 'create_order'),
        COALESCE(p_order_payload->>'itemKey', p_order_payload->>'id', ''),
        COALESCE(p_order_payload->>'deviceId', ''),
        now(),
        (now() + INTERVAL '7 days')
    )
    ON CONFLICT (key) DO NOTHING
    RETURNING true INTO v_inserted;

    -- 2. Handle duplicate/race-conditions
    IF NOT v_inserted THEN
        SELECT result INTO v_existing FROM public.pos_idempotency_keys WHERE key = p_idempotency_key;
        IF v_existing IS NOT NULL THEN RETURN v_existing; END IF;
        RETURN jsonb_build_object('ok', true, 'idempotent', true, 'status', 'pending');
    END IF;

    -- 3. Direct execution pass (Fixing Bug 2 cleanly)
    BEGIN
        v_result := public.api_create_app_order_with_inventory(p_order_payload);
    EXCEPTION WHEN OTHERS THEN
        DELETE FROM public.pos_idempotency_keys WHERE key = p_idempotency_key;
        RAISE;
    END;

    -- 4. Persist result state
    UPDATE public.pos_idempotency_keys SET result = v_result WHERE key = p_idempotency_key;
    RETURN v_result;
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_create_order_idempotent(TEXT,JSONB) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_cleanup_idempotency_keys() TO anon, authenticated;

-- RLS
ALTER TABLE public.pos_idempotency_keys ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "pos_idempotency_keys_staff" ON public.pos_idempotency_keys;
CREATE POLICY pos_idempotency_keys_staff
ON public.pos_idempotency_keys
FOR ALL TO anon, authenticated
USING (public.is_admin_policy_request() OR public.is_cashier_policy_request())
WITH CHECK (public.is_admin_policy_request() OR public.is_cashier_policy_request());

-- ---------------------------------------------------------------------------
-- 4) Shared Shifts removed
-- ---------------------------------------------------------------------------
ALTER TABLE public.app_orders
  DROP COLUMN IF EXISTS pos_shift_id;

DROP FUNCTION IF EXISTS public.api_pos_open_shift(NUMERIC,TEXT);
DROP FUNCTION IF EXISTS public.api_pos_get_active_shift();
DROP FUNCTION IF EXISTS public.api_pos_close_shift(TEXT,NUMERIC);
DROP TABLE IF EXISTS public.pos_shifts CASCADE;

-- ---------------------------------------------------------------------------
-- 5) Order Edit Locks - قفل شاشة تعديل التوصيل لجهاز واحد
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.order_edit_locks (
  order_id             TEXT PRIMARY KEY REFERENCES public.app_orders(id) ON DELETE CASCADE,
  locked_by_device_id  TEXT NOT NULL,
  locked_by_user_id    TEXT REFERENCES public.users(id) ON DELETE SET NULL,
  lock_type            TEXT NOT NULL DEFAULT 'delivery_address'
    CHECK (lock_type IN ('delivery_address','full')),
  locked_at            TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- محاولة قفل أوردر (ذرية)
CREATE OR REPLACE FUNCTION public.api_pos_try_lock_order(
  p_order_id  TEXT,
  p_device_id TEXT,
  p_lock_type TEXT DEFAULT 'delivery_address'
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_user_id   TEXT := public.current_app_user_id();
  v_actor_role      TEXT := public.current_app_role();
  v_existing_device TEXT;
  v_inserted        BOOLEAN := false;
BEGIN
  IF v_actor_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  -- تنظيف أقفال قديمة (> 5 دقائق) لهذا الأوردر
  DELETE FROM public.order_edit_locks
  WHERE order_id = p_order_id
    AND locked_at < now() - INTERVAL '5 minutes';

  -- محاولة إدراج ذري
  INSERT INTO public.order_edit_locks
    (order_id, locked_by_device_id, locked_by_user_id, lock_type)
  VALUES
    (p_order_id, p_device_id, v_actor_user_id, COALESCE(p_lock_type,'delivery_address'))
  ON CONFLICT (order_id) DO NOTHING
  RETURNING true INTO v_inserted;

  IF v_inserted THEN
    RETURN jsonb_build_object('ok',true,'locked',true);
  END IF;

  -- القفل موجود - فحص مالكه
  SELECT locked_by_device_id INTO v_existing_device
  FROM public.order_edit_locks
  WHERE order_id = p_order_id;

  IF v_existing_device = p_device_id THEN
    -- نفس الجهاز → جدِّد
    UPDATE public.order_edit_locks
    SET locked_at = now()
    WHERE order_id = p_order_id AND locked_by_device_id = p_device_id;
    RETURN jsonb_build_object('ok',true,'locked',true,'renewed',true);
  END IF;

  RETURN jsonb_build_object(
    'ok',     false,
    'locked', false,
    'error',  'order_locked_by_another_device',
    'message','جاري تعديل بيانات التوصيل من جهاز آخر'
  );
END;
$$;

-- تحرير القفل
CREATE OR REPLACE FUNCTION public.api_pos_release_order_lock(
  p_order_id  TEXT,
  p_device_id TEXT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  DELETE FROM public.order_edit_locks
  WHERE order_id = p_order_id
    AND locked_by_device_id = p_device_id;
  RETURN jsonb_build_object('ok',true,'released',FOUND);
END;
$$;

-- تحرير جميع أقفال جهاز (عند تسجيل الخروج / انهيار التطبيق)
CREATE OR REPLACE FUNCTION public.api_pos_release_all_locks_for_device(
  p_device_id TEXT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_count INT;
BEGIN
  DELETE FROM public.order_edit_locks
  WHERE locked_by_device_id = p_device_id;
  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN jsonb_build_object('ok',true,'releasedCount',v_count);
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_try_lock_order(TEXT,TEXT,TEXT)     TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_release_order_lock(TEXT,TEXT)      TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.api_pos_release_all_locks_for_device(TEXT) TO anon, authenticated;

ALTER TABLE public.order_edit_locks ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "order_edit_locks_staff" ON public.order_edit_locks;
CREATE POLICY order_edit_locks_staff
ON public.order_edit_locks FOR ALL TO anon, authenticated
USING (public.is_admin_policy_request() OR public.is_cashier_policy_request())
WITH CHECK (public.is_admin_policy_request() OR public.is_cashier_policy_request());

-- ---------------------------------------------------------------------------
-- 6) Realtime - تفعيل البث على الجداول الحرجة
-- ---------------------------------------------------------------------------
-- شغِّل هذا من لوحة Supabase → Database → Replication
-- ALTER PUBLICATION supabase_realtime ADD TABLE public.app_orders;
-- ALTER PUBLICATION supabase_realtime ADD TABLE public.order_edit_locks;


-- --- BEGIN IDEMPOTENCY RPCS PATCH CONSOLIDATION ---

-- =============================================================================
-- supabase_idempotency_rpcs_patch.sql
-- مسار الملف: supabase/supabase_idempotency_rpcs_patch.sql
-- يُضاف بعد supabase_schema_patch.sql
-- RPCs الناقصة: update_order_status | update_delivery_address |
--               mark_order_prints   | assign_driver
-- =============================================================================

-- ---------------------------------------------------------------------------
-- helper: فحص الـ idempotency key قبل أي عملية
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public._pos_check_and_reserve_idempotency_key(
  p_key TEXT,
  p_op  TEXT
)
RETURNS BOOLEAN   -- true = نفِّذ العملية, false = مكررة (أعد النتيجة)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_inserted BOOLEAN := false;
BEGIN
  IF NULLIF(TRIM(COALESCE(p_key,'')), '') IS NULL THEN
    RETURN TRUE;   -- بدون key → نفِّذ دائماً
  END IF;

  INSERT INTO public.pos_idempotency_keys (key, operation_type, device_id)
  VALUES (p_key, COALESCE(p_op,'op'), '')
  ON CONFLICT (key) DO NOTHING
  RETURNING TRUE INTO v_inserted;

  RETURN COALESCE(v_inserted, FALSE);
END;
$$;

-- ---------------------------------------------------------------------------
-- 1. تحديث حالة الأوردر مع Idempotency
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.api_pos_update_order_status_idempotent(
  p_order_id        TEXT,
  p_to_status       TEXT,
  p_idempotency_key TEXT DEFAULT NULL,
  p_actor_role      TEXT DEFAULT NULL,
  p_actor_user_id   TEXT DEFAULT NULL
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_caller_role TEXT := public.current_app_role();
  v_should_exec BOOLEAN;
  v_old_status  TEXT;
BEGIN
  IF v_caller_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  -- فحص idempotency
  SELECT public._pos_check_and_reserve_idempotency_key(
           p_idempotency_key, 'update_status'
         ) INTO v_should_exec;

  IF NOT v_should_exec THEN
    RETURN jsonb_build_object('ok',true,'idempotent',true,'error','idempotent_duplicate');
  END IF;

  -- قراءة الحالة الحالية
  SELECT status INTO v_old_status
  FROM public.app_orders
  WHERE id = p_order_id;

  IF NOT FOUND THEN
    DELETE FROM public.pos_idempotency_keys WHERE key = p_idempotency_key;
    RETURN jsonb_build_object('ok',false,'error','order_not_found');
  END IF;

  -- تحديث الحالة
  UPDATE public.app_orders
  SET status     = p_to_status,
      updated_at = now(),
      metadata   = jsonb_set(
                     COALESCE(metadata,'{}'),
                     '{lastStatusUpdatedBy}',
                     to_jsonb(COALESCE(p_actor_role,''))
                   )
  WHERE id = p_order_id;

  RETURN jsonb_build_object(
    'ok',        true,
    'oldStatus', v_old_status,
    'newStatus', p_to_status
  );
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_update_order_status_idempotent(TEXT,TEXT,TEXT,TEXT,TEXT)
  TO anon, authenticated;

-- ---------------------------------------------------------------------------
-- 2. تحديث عنوان التوصيل مع Idempotency
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.api_pos_update_delivery_address(
  p_order_id         TEXT,
  p_idempotency_key  TEXT    DEFAULT NULL,
  p_address_id       TEXT    DEFAULT NULL,
  p_district         TEXT    DEFAULT NULL,
  p_district_id      TEXT    DEFAULT NULL,
  p_delivery_fee     INT     DEFAULT NULL,
  p_address_block    TEXT    DEFAULT NULL,
  p_address_street   TEXT    DEFAULT NULL,
  p_address_building TEXT    DEFAULT NULL,
  p_address_apartment TEXT   DEFAULT NULL,
  p_address_floor    TEXT    DEFAULT NULL,
  p_address_note     TEXT    DEFAULT NULL
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_caller_role TEXT := public.current_app_role();
  v_should_exec BOOLEAN;
  v_current_meta JSONB;
  v_new_meta     JSONB;
BEGIN
  IF v_caller_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  SELECT public._pos_check_and_reserve_idempotency_key(
    p_idempotency_key, 'update_delivery_address'
  ) INTO v_should_exec;

  IF NOT v_should_exec THEN
    RETURN jsonb_build_object('ok',true,'idempotent',true,'error','idempotent_duplicate');
  END IF;

  SELECT metadata INTO v_current_meta
  FROM public.app_orders
  WHERE id = p_order_id;

  IF NOT FOUND THEN
    DELETE FROM public.pos_idempotency_keys WHERE key = p_idempotency_key;
    RETURN jsonb_build_object('ok',false,'error','order_not_found');
  END IF;

  -- دمج بيانات العنوان في metadata
  v_new_meta := COALESCE(v_current_meta, '{}');

  IF p_address_id       IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{addressId}',       to_jsonb(p_address_id)); END IF;
  IF p_district         IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{district}',         to_jsonb(p_district)); END IF;
  IF p_district_id      IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{districtId}',       to_jsonb(p_district_id)); END IF;
  IF p_delivery_fee     IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{deliveryFee}',      to_jsonb(p_delivery_fee)); END IF;
  IF p_address_block    IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{addressBlock}',     to_jsonb(p_address_block)); END IF;
  IF p_address_street   IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{addressStreet}',    to_jsonb(p_address_street)); END IF;
  IF p_address_building IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{addressBuilding}',  to_jsonb(p_address_building)); END IF;
  IF p_address_apartment IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{addressApartment}',to_jsonb(p_address_apartment)); END IF;
  IF p_address_floor    IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{addressFloor}',     to_jsonb(p_address_floor)); END IF;
  IF p_address_note     IS NOT NULL THEN v_new_meta := jsonb_set(v_new_meta,'{addressNote}',      to_jsonb(p_address_note)); END IF;

  UPDATE public.app_orders
  SET metadata   = v_new_meta,
      updated_at = now()
  WHERE id = p_order_id;

  -- تحرير القفل بعد التعديل
  DELETE FROM public.order_edit_locks WHERE order_id = p_order_id;

  RETURN jsonb_build_object('ok',true);
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_update_delivery_address(TEXT,TEXT,TEXT,TEXT,TEXT,INT,TEXT,TEXT,TEXT,TEXT,TEXT,TEXT)
  TO anon, authenticated;

-- ---------------------------------------------------------------------------
-- 3. تسجيل حالات الطباعة مع Idempotency
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.api_pos_mark_order_prints(
  p_order_id                     TEXT,
  p_idempotency_key              TEXT    DEFAULT NULL,
  p_mark_kitchen_printed         BOOLEAN DEFAULT NULL,
  p_mark_driver_receipt_printed  BOOLEAN DEFAULT NULL,
  p_mark_partner_receipt_printed BOOLEAN DEFAULT NULL
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_caller_role TEXT := public.current_app_role();
  v_should_exec BOOLEAN;
  v_meta        JSONB;
BEGIN
  IF v_caller_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  SELECT public._pos_check_and_reserve_idempotency_key(
    p_idempotency_key, 'mark_prints'
  ) INTO v_should_exec;

  IF NOT v_should_exec THEN
    RETURN jsonb_build_object('ok',true,'idempotent',true,'error','idempotent_duplicate');
  END IF;

  SELECT metadata INTO v_meta
  FROM public.app_orders
  WHERE id = p_order_id;

  IF NOT FOUND THEN
    DELETE FROM public.pos_idempotency_keys WHERE key = p_idempotency_key;
    RETURN jsonb_build_object('ok',false,'error','order_not_found');
  END IF;

  v_meta := COALESCE(v_meta,'{}');

  IF p_mark_kitchen_printed IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{kitchenPrinted}',        to_jsonb(p_mark_kitchen_printed));
    v_meta := jsonb_set(v_meta, '{kitchenPrintedAt}',      to_jsonb(now()::text));
  END IF;
  IF p_mark_driver_receipt_printed IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{driverReceiptPrinted}',  to_jsonb(p_mark_driver_receipt_printed));
    v_meta := jsonb_set(v_meta, '{driverReceiptPrintedAt}',to_jsonb(now()::text));
  END IF;
  IF p_mark_partner_receipt_printed IS NOT NULL THEN
    v_meta := jsonb_set(v_meta, '{partnerReceiptPrinted}', to_jsonb(p_mark_partner_receipt_printed));
  END IF;

  UPDATE public.app_orders
  SET metadata   = v_meta,
      updated_at = now()
  WHERE id = p_order_id;

  RETURN jsonb_build_object('ok',true);
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_mark_order_prints(TEXT,TEXT,BOOLEAN,BOOLEAN,BOOLEAN)
  TO anon, authenticated;

-- ---------------------------------------------------------------------------
-- 4. تعيين سائق مع Idempotency
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.api_pos_assign_driver(
  p_order_id        TEXT,
  p_idempotency_key TEXT DEFAULT NULL,
  p_driver_user_id  TEXT DEFAULT NULL,
  p_driver_name     TEXT DEFAULT NULL
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_caller_role TEXT := public.current_app_role();
  v_should_exec BOOLEAN;
  v_meta        JSONB;
BEGIN
  IF v_caller_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  SELECT public._pos_check_and_reserve_idempotency_key(
    p_idempotency_key, 'assign_driver'
  ) INTO v_should_exec;

  IF NOT v_should_exec THEN
    RETURN jsonb_build_object('ok',true,'idempotent',true,'error','idempotent_duplicate');
  END IF;

  SELECT metadata INTO v_meta
  FROM public.app_orders
  WHERE id = p_order_id;

  IF NOT FOUND THEN
    DELETE FROM public.pos_idempotency_keys WHERE key = p_idempotency_key;
    RETURN jsonb_build_object('ok',false,'error','order_not_found');
  END IF;

  v_meta := COALESCE(v_meta,'{}');
  IF p_driver_user_id IS NOT NULL THEN v_meta := jsonb_set(v_meta,'{driverUserId}', to_jsonb(p_driver_user_id)); END IF;
  IF p_driver_name    IS NOT NULL THEN v_meta := jsonb_set(v_meta,'{driverName}',   to_jsonb(p_driver_name)); END IF;
  v_meta := jsonb_set(v_meta,'{driverAssignedAt}', to_jsonb(now()::text));

  UPDATE public.app_orders
  SET metadata   = v_meta,
      updated_at = now()
  WHERE id = p_order_id;

  RETURN jsonb_build_object('ok',true,'driverName',p_driver_name);
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_assign_driver(TEXT,TEXT,TEXT,TEXT)
  TO anon, authenticated;

-- ---------------------------------------------------------------------------
-- 5. تنظيف تلقائي لـ idempotency keys المنتهية (Cron - يُشغَّل يومياً)
-- ---------------------------------------------------------------------------
-- شغِّل هذا من Supabase → Edge Functions أو pg_cron
-- SELECT cron.schedule('cleanup-idempotency-keys','0 3 * * *',
--   'SELECT public.api_pos_cleanup_idempotency_keys()');


-- ---------------------------------------------------------------------------
-- api_pos_reset_device_counters
-- ---------------------------------------------------------------------------
DROP FUNCTION api_pos_reset_device_counters();
CREATE OR REPLACE FUNCTION public.api_pos_reset_device_counters()
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_actor_role TEXT := public.current_app_role();
BEGIN
  IF v_actor_role NOT IN ('ADMIN','ADMIN_POS','CASHIER','SERVICE_ROLE') THEN
    RETURN jsonb_build_object('ok',false,'error','forbidden');
  END IF;

  ALTER SEQUENCE public.pos_order_branch_seq RESTART WITH 1;
  DELETE FROM public.pos_idempotency_keys;

  RETURN jsonb_build_object('ok',true);
END;
$$;

GRANT EXECUTE ON FUNCTION public.api_pos_reset_device_counters() TO anon, authenticated;
