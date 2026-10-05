CREATE SEQUENCE IF NOT EXISTS work_order_number_seq START WITH 1001;

CREATE TABLE IF NOT EXISTS users (
    user_id uuid PRIMARY KEY,
    email varchar(254) NOT NULL,
    full_name varchar(120) NOT NULL,
    role varchar(20) NOT NULL,
    password_hash varchar(256) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_users_role CHECK (role IN ('Admin', 'Advisor', 'Mechanic'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON users (lower(email));

CREATE TABLE IF NOT EXISTS customers (
    customer_id uuid PRIMARY KEY,
    full_name varchar(120) NOT NULL,
    email varchar(254) NOT NULL,
    phone varchar(25) NOT NULL,
    address varchar(250),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_customers_email ON customers (lower(email));

CREATE INDEX IF NOT EXISTS ix_customers_full_name ON customers (lower(full_name));

CREATE TABLE IF NOT EXISTS vehicles (
    vehicle_id uuid PRIMARY KEY,
    customer_id uuid NOT NULL REFERENCES customers (customer_id),
    license_plate varchar(12) NOT NULL,
    make varchar(60) NOT NULL,
    model varchar(60) NOT NULL,
    year integer NOT NULL,
    vin varchar(17),
    color varchar(30),
    mileage_km integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_vehicles_year CHECK (year BETWEEN 1950 AND 2100),
    CONSTRAINT ck_vehicles_mileage CHECK (mileage_km >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_vehicles_license_plate ON vehicles (upper(license_plate));

CREATE INDEX IF NOT EXISTS ix_vehicles_customer_id ON vehicles (customer_id);

CREATE TABLE IF NOT EXISTS work_orders (
    work_order_id uuid PRIMARY KEY,
    order_number integer NOT NULL DEFAULT nextval('work_order_number_seq'),
    customer_id uuid NOT NULL REFERENCES customers (customer_id),
    vehicle_id uuid NOT NULL REFERENCES vehicles (vehicle_id),
    description varchar(500) NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Received',
    assigned_mechanic_id uuid REFERENCES users (user_id),
    mileage_km integer NOT NULL DEFAULT 0,
    total_amount numeric(12, 2) NOT NULL DEFAULT 0,
    created_by_user_id uuid NOT NULL REFERENCES users (user_id),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz,
    CONSTRAINT ux_work_orders_order_number UNIQUE (order_number),
    CONSTRAINT ck_work_orders_status CHECK (status IN ('Received', 'Diagnosed', 'Approved', 'InProgress', 'Completed', 'Delivered', 'Cancelled')),
    CONSTRAINT ck_work_orders_total CHECK (total_amount >= 0)
);

CREATE INDEX IF NOT EXISTS ix_work_orders_status ON work_orders (status);

CREATE INDEX IF NOT EXISTS ix_work_orders_mechanic ON work_orders (assigned_mechanic_id);

CREATE INDEX IF NOT EXISTS ix_work_orders_created_at ON work_orders (created_at DESC);

CREATE INDEX IF NOT EXISTS ix_work_orders_customer_id ON work_orders (customer_id);

CREATE INDEX IF NOT EXISTS ix_work_orders_completed_at ON work_orders (completed_at);

CREATE TABLE IF NOT EXISTS work_order_items (
    line_item_id uuid PRIMARY KEY,
    work_order_id uuid NOT NULL REFERENCES work_orders (work_order_id) ON DELETE CASCADE,
    item_type varchar(10) NOT NULL,
    description varchar(200) NOT NULL,
    quantity numeric(10, 2) NOT NULL,
    unit_price numeric(12, 2) NOT NULL,
    line_total numeric(12, 2) NOT NULL,
    added_by_user_id uuid NOT NULL REFERENCES users (user_id),
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_work_order_items_type CHECK (item_type IN ('Service', 'Part')),
    CONSTRAINT ck_work_order_items_quantity CHECK (quantity > 0),
    CONSTRAINT ck_work_order_items_price CHECK (unit_price >= 0)
);

CREATE INDEX IF NOT EXISTS ix_work_order_items_order ON work_order_items (work_order_id);

CREATE TABLE IF NOT EXISTS work_order_history (
    history_id uuid PRIMARY KEY,
    work_order_id uuid NOT NULL REFERENCES work_orders (work_order_id) ON DELETE CASCADE,
    from_status varchar(20),
    to_status varchar(20) NOT NULL,
    changed_by_user_id uuid NOT NULL REFERENCES users (user_id),
    changed_at timestamptz NOT NULL DEFAULT now(),
    correlation_id varchar(64) NOT NULL,
    note varchar(500)
);

CREATE INDEX IF NOT EXISTS ix_work_order_history_order ON work_order_history (work_order_id, changed_at);

CREATE INDEX IF NOT EXISTS ix_work_order_history_correlation ON work_order_history (correlation_id);

CREATE TABLE IF NOT EXISTS idempotency_keys (
    idempotency_key varchar(128) NOT NULL,
    user_id uuid NOT NULL REFERENCES users (user_id),
    request_hash varchar(64) NOT NULL,
    work_order_id uuid REFERENCES work_orders (work_order_id),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (idempotency_key, user_id)
);

COMMENT ON TABLE users IS 'Staff accounts that can sign in: administrators, service advisors and mechanics.';
COMMENT ON COLUMN users.role IS 'Authorization role: Admin, Advisor or Mechanic.';
COMMENT ON COLUMN users.password_hash IS 'PBKDF2-SHA256 hash in the format PBKDF2-SHA256$iterations$salt$hash.';

COMMENT ON TABLE customers IS 'People or companies that bring vehicles to the workshop.';
COMMENT ON COLUMN customers.is_active IS 'Inactive customers cannot receive new work orders.';

COMMENT ON TABLE vehicles IS 'Vehicles owned by a customer. License plates are unique across the workshop.';
COMMENT ON COLUMN vehicles.mileage_km IS 'Last known odometer reading in kilometers.';

COMMENT ON TABLE work_orders IS 'Repair jobs. The status column is advanced only through atomic conditional updates.';
COMMENT ON COLUMN work_orders.order_number IS 'Human readable sequential number shown to staff as WO-nnnn.';
COMMENT ON COLUMN work_orders.status IS 'Received, Diagnosed, Approved, InProgress, Completed, Delivered or Cancelled.';
COMMENT ON COLUMN work_orders.assigned_mechanic_id IS 'Mechanic responsible for the job; mechanics can only see and advance their own orders.';
COMMENT ON COLUMN work_orders.total_amount IS 'Sum of line totals, maintained atomically by the server.';
COMMENT ON COLUMN work_orders.completed_at IS 'Moment the order reached the Completed status.';

COMMENT ON TABLE work_order_items IS 'Service and part lines billed on a work order.';
COMMENT ON COLUMN work_order_items.line_total IS 'quantity multiplied by unit_price, rounded to two decimals by the server.';

COMMENT ON TABLE work_order_history IS 'Audit trail of every status transition and mechanic assignment.';
COMMENT ON COLUMN work_order_history.changed_by_user_id IS 'User taken from the authenticated token, never from the request body.';
COMMENT ON COLUMN work_order_history.correlation_id IS 'X-Correlation-Id of the request that produced the change, used to cross-reference logs.';

COMMENT ON TABLE idempotency_keys IS 'Idempotency-Key values already used to create work orders, scoped per user.';
COMMENT ON COLUMN idempotency_keys.request_hash IS 'SHA-256 fingerprint of the original request payload.';
