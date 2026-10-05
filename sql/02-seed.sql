INSERT INTO users (user_id, email, full_name, role, password_hash) VALUES
('a0000000-0000-0000-0000-000000000001', 'admin@example.com', 'Marcus Hale', 'Admin', 'PBKDF2-SHA256$310000$bAYh62rTBKidZMHlPwX1Hw==$R4F8LbzdJUD7k3Ob4VB5TzZksftBSJQdy9+pFd/Wq28='),
('a0000000-0000-0000-0000-000000000002', 'advisor@example.com', 'Sofia Bennett', 'Advisor', 'PBKDF2-SHA256$310000$tnFnIgH8VoScR/kRa7OcZg==$iSLze9nW+CiuNAc7MEjQ8C/AeP7pO/p8F58bXLkFdNc='),
('a0000000-0000-0000-0000-000000000003', 'mechanic1@example.com', 'Daniel Cruz', 'Mechanic', 'PBKDF2-SHA256$310000$83NhSaGDMazSm0D7W+G2dg==$WTH2Kf7OmFxVg6r78vadIkEqPiGiKCaKQH+pVa/INdw='),
('a0000000-0000-0000-0000-000000000004', 'mechanic2@example.com', 'Tom Okafor', 'Mechanic', 'PBKDF2-SHA256$310000$V+Og7s1FQVfnRpvwZgiIiQ==$xk7+HVSLcOapzhMANOpdyjz6UdPBGTGV/f8CxfAbOzU=')
ON CONFLICT DO NOTHING;

INSERT INTO customers (customer_id, full_name, email, phone, address, created_at, updated_at)
SELECT ('c0000000-0000-0000-0000-' || lpad(seed.n::text, 12, '0'))::uuid, seed.full_name, seed.email, seed.phone, seed.address, now() - seed.days_ago * interval '1 day', now() - seed.days_ago * interval '1 day'
FROM (VALUES
(1, 'Olivia Martin', 'olivia.martin@example.com', '+1 555 0101', '214 Maple Street, Springfield', 210),
(2, 'Liam Carter', 'liam.carter@example.com', '+1 555 0102', '88 Harbor Road, Springfield', 180),
(3, 'Emma Davis', 'emma.davis@example.com', '+1 555 0103', '19 Cedar Lane, Riverton', 150),
(4, 'Noah Wilson', 'noah.wilson@example.com', '+1 555 0104', '402 Oak Avenue, Riverton', 120),
(5, 'Ava Thompson', 'ava.thompson@example.com', '+1 555 0105', '7 Willow Court, Springfield', 95),
(6, 'Ethan Brooks', 'ethan.brooks@example.com', '+1 555 0106', '560 Pine Boulevard, Lakeside', 70),
(7, 'Mia Rodriguez', 'mia.rodriguez@example.com', '+1 555 0107', '33 Elm Terrace, Lakeside', 45),
(8, 'Lucas Foster', 'lucas.foster@example.com', '+1 555 0108', '151 Birch Way, Springfield', 20)
) AS seed (n, full_name, email, phone, address, days_ago)
ON CONFLICT DO NOTHING;

INSERT INTO vehicles (vehicle_id, customer_id, license_plate, make, model, year, vin, color, mileage_km, created_at)
SELECT ('b0000000-0000-0000-0000-' || lpad(seed.n::text, 12, '0'))::uuid, ('c0000000-0000-0000-0000-' || lpad(seed.customer_n::text, 12, '0'))::uuid, seed.license_plate, seed.make, seed.model, seed.year, seed.vin, seed.color, seed.mileage_km, now() - seed.days_ago * interval '1 day'
FROM (VALUES
(1, 1, 'KDP-4821', 'Toyota', 'Corolla', 2019, '2T1BURHE0KC123401', 'Silver', 68400, 200),
(2, 1, 'TRM-9023', 'Honda', 'CR-V', 2016, '5J6RM4H50GL123402', 'Blue', 121300, 200),
(3, 2, 'FHL-3317', 'Ford', 'F-150', 2021, '1FTEW1EP5MK123403', 'Black', 45900, 170),
(4, 3, 'VWG-7740', 'Volkswagen', 'Golf', 2018, 'WVWZZZAUZJW123404', 'White', 83200, 140),
(5, 4, 'TSL-1156', 'Tesla', 'Model 3', 2020, '5YJ3E1EA7LF123405', 'Red', 59800, 110),
(6, 5, 'SBR-6082', 'Subaru', 'Outback', 2015, '4S4BRBCC5F3123406', 'Green', 143700, 90),
(7, 6, 'BMW-2294', 'BMW', '330i', 2017, 'WBA8B9C50HK123407', 'Gray', 97500, 65),
(8, 7, 'HYU-5538', 'Hyundai', 'Tucson', 2022, 'KM8J3CA46NU123408', 'Orange', 27100, 40),
(9, 8, 'CHV-8901', 'Chevrolet', 'Silverado', 2014, '3GCUKREC4EG123409', 'Brown', 189600, 18),
(10, 3, 'MZD-4467', 'Mazda', '3', 2012, 'JM1BL1V74C1123410', 'Silver', 162400, 140)
) AS seed (n, customer_n, license_plate, make, model, year, vin, color, mileage_km, days_ago)
ON CONFLICT DO NOTHING;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM work_orders WHERE work_order_id = 'd0000000-0000-0000-0000-000000000001') THEN
        RETURN;
    END IF;

    INSERT INTO work_orders (work_order_id, order_number, customer_id, vehicle_id, description, status, assigned_mechanic_id, mileage_km, created_by_user_id, created_at, updated_at)
    SELECT
        ('d0000000-0000-0000-0000-' || lpad(seed.n::text, 12, '0'))::uuid,
        1000 + seed.n,
        ('c0000000-0000-0000-0000-' || lpad(seed.customer_n::text, 12, '0'))::uuid,
        ('b0000000-0000-0000-0000-' || lpad(seed.vehicle_n::text, 12, '0'))::uuid,
        seed.description,
        seed.status,
        CASE seed.mechanic_n WHEN 1 THEN 'a0000000-0000-0000-0000-000000000003'::uuid WHEN 2 THEN 'a0000000-0000-0000-0000-000000000004'::uuid END,
        seed.mileage_km,
        'a0000000-0000-0000-0000-000000000002'::uuid,
        now() - seed.hours_ago * interval '1 hour',
        now() - seed.hours_ago * interval '1 hour'
    FROM (VALUES
    (1, 2, 3, 'Check engine light on and rough idle', 'Received', NULL::int, 45950, 3),
    (2, 5, 6, 'Brake squeal and vibration when stopping', 'Received', 2, 143720, 8),
    (3, 3, 4, 'Air conditioning is not cooling', 'Diagnosed', 1, 83240, 20),
    (4, 6, 7, 'Oil leak under the engine', 'Diagnosed', 2, 97510, 26),
    (5, 7, 8, '60,000 km scheduled service', 'Approved', 1, 27110, 30),
    (6, 1, 2, 'Replace worn front tires and align', 'Approved', 2, 121310, 50),
    (7, 4, 5, 'Suspension noise over bumps', 'InProgress', 1, 59810, 72),
    (8, 8, 9, 'Transmission fluid flush and filter', 'InProgress', 2, 189620, 96),
    (9, 1, 1, 'Battery replacement and charging test', 'Completed', 1, 68410, 40),
    (10, 3, 10, 'Timing belt replacement', 'Completed', 2, 162420, 36),
    (11, 2, 3, 'Annual inspection and oil change', 'Delivered', 1, 45000, 144),
    (12, 5, 6, 'Replace rear brake pads and rotors', 'Delivered', 2, 143100, 528),
    (13, 6, 7, 'Coolant system flush', 'Delivered', 1, 96800, 1200),
    (14, 7, 8, 'Headlight restoration and wiper replacement', 'Delivered', 2, 26500, 2040),
    (15, 8, 9, 'Cosmetic dent repair quote', 'Cancelled', NULL::int, 189000, 120)
    ) AS seed (n, customer_n, vehicle_n, description, status, mechanic_n, mileage_km, hours_ago);

    INSERT INTO work_order_items (line_item_id, work_order_id, item_type, description, quantity, unit_price, line_total, added_by_user_id, created_at)
    SELECT
        md5('item-' || seed.order_n || '-' || seed.item_n)::uuid,
        ('d0000000-0000-0000-0000-' || lpad(seed.order_n::text, 12, '0'))::uuid,
        seed.item_type,
        seed.description,
        seed.quantity,
        seed.unit_price,
        round(seed.quantity * seed.unit_price, 2),
        COALESCE(work_orders.assigned_mechanic_id, 'a0000000-0000-0000-0000-000000000002'::uuid),
        work_orders.created_at + interval '1 hour'
    FROM (VALUES
    (3, 1, 'Service', 'A/C system diagnostic', 1, 89.00),
    (3, 2, 'Service', 'Refrigerant recharge', 1, 129.00),
    (3, 3, 'Part', 'Cabin air filter', 1, 24.50),
    (4, 1, 'Service', 'Leak inspection with UV dye', 1, 75.00),
    (4, 2, 'Part', 'Valve cover gasket set', 1, 58.90),
    (4, 3, 'Service', 'Gasket replacement labor (hours)', 2.5, 95.00),
    (5, 1, 'Service', '60,000 km scheduled service labor', 1, 180.00),
    (5, 2, 'Part', 'Synthetic oil 5W-30 (liters)', 5, 11.20),
    (5, 3, 'Part', 'Oil filter', 1, 12.90),
    (5, 4, 'Part', 'Engine air filter', 1, 21.40),
    (5, 5, 'Part', 'Spark plug', 4, 14.75),
    (6, 1, 'Part', 'All-season tire 215/55 R17', 2, 128.00),
    (6, 2, 'Service', 'Mount and balance (per tire)', 2, 22.00),
    (6, 3, 'Service', 'Four-wheel alignment', 1, 79.00),
    (7, 1, 'Service', 'Suspension inspection', 1, 60.00),
    (7, 2, 'Part', 'Front strut mount kit', 2, 64.50),
    (7, 3, 'Service', 'Strut mount replacement labor (hours)', 3, 95.00),
    (8, 1, 'Part', 'Transmission fluid ATF (liters)', 8, 14.20),
    (8, 2, 'Part', 'Transmission filter kit', 1, 46.00),
    (8, 3, 'Service', 'Transmission flush labor (hours)', 2, 95.00),
    (9, 1, 'Part', 'AGM battery 12V 70Ah', 1, 189.00),
    (9, 2, 'Service', 'Battery install and charging system test', 1, 45.00),
    (10, 1, 'Part', 'Timing belt kit with water pump', 1, 265.00),
    (10, 2, 'Service', 'Timing belt replacement labor (hours)', 4.5, 95.00),
    (10, 3, 'Part', 'Coolant (liters)', 4, 9.50),
    (11, 1, 'Service', 'Annual safety inspection', 1, 65.00),
    (11, 2, 'Part', 'Engine oil 5W-30 (liters)', 6, 11.20),
    (11, 3, 'Part', 'Oil filter', 1, 12.90),
    (12, 1, 'Part', 'Rear brake pad set', 1, 78.00),
    (12, 2, 'Part', 'Rear brake rotors (pair)', 1, 142.00),
    (12, 3, 'Service', 'Brake service labor (hours)', 2, 95.00),
    (13, 1, 'Service', 'Cooling system flush', 1, 110.00),
    (13, 2, 'Part', 'Coolant concentrate (liters)', 5, 9.50),
    (13, 3, 'Part', 'Thermostat', 1, 29.90),
    (14, 1, 'Service', 'Headlight restoration (per lens)', 2, 40.00),
    (14, 2, 'Part', 'Wiper blade set', 1, 31.00)
    ) AS seed (order_n, item_n, item_type, description, quantity, unit_price)
    JOIN work_orders ON work_orders.work_order_id = ('d0000000-0000-0000-0000-' || lpad(seed.order_n::text, 12, '0'))::uuid;

    INSERT INTO work_order_history (history_id, work_order_id, from_status, to_status, changed_by_user_id, changed_at, correlation_id, note)
    SELECT
        md5('history-' || work_orders.order_number || '-' || step.name)::uuid,
        work_orders.work_order_id,
        previous_step.name,
        step.name,
        CASE WHEN step.name IN ('Diagnosed', 'InProgress', 'Completed')
            THEN COALESCE(work_orders.assigned_mechanic_id, 'a0000000-0000-0000-0000-000000000002'::uuid)
            ELSE 'a0000000-0000-0000-0000-000000000002'::uuid
        END,
        work_orders.created_at + step.position * (3 + work_orders.order_number % 4) * interval '1 hour',
        'seed-' || work_orders.order_number || '-' || step.position,
        CASE step.name WHEN 'Received' THEN 'Vehicle checked in at the front desk.' WHEN 'Diagnosed' THEN 'Diagnosis finished and quote prepared.' WHEN 'Approved' THEN 'Customer approved the quote.' WHEN 'Delivered' THEN 'Vehicle handed over to the customer.' END
    FROM work_orders
    JOIN (VALUES (0, 'Received'), (1, 'Diagnosed'), (2, 'Approved'), (3, 'InProgress'), (4, 'Completed'), (5, 'Delivered')) AS current_step (position, name)
        ON current_step.name = CASE work_orders.status WHEN 'Cancelled' THEN 'Received' ELSE work_orders.status END
    JOIN (VALUES (0, 'Received'), (1, 'Diagnosed'), (2, 'Approved'), (3, 'InProgress'), (4, 'Completed'), (5, 'Delivered')) AS step (position, name)
        ON step.position <= current_step.position
    LEFT JOIN (VALUES (0, 'Received'), (1, 'Diagnosed'), (2, 'Approved'), (3, 'InProgress'), (4, 'Completed'), (5, 'Delivered')) AS previous_step (position, name)
        ON previous_step.position = step.position - 1
    WHERE work_orders.work_order_id::text LIKE 'd0000000-%';

    INSERT INTO work_order_history (history_id, work_order_id, from_status, to_status, changed_by_user_id, changed_at, correlation_id, note)
    SELECT
        md5('history-' || work_orders.order_number || '-Cancelled')::uuid,
        work_orders.work_order_id,
        'Received',
        'Cancelled',
        'a0000000-0000-0000-0000-000000000002'::uuid,
        work_orders.created_at + interval '4 hours',
        'seed-' || work_orders.order_number || '-cancel',
        'Customer declined the quote.'
    FROM work_orders
    WHERE work_orders.status = 'Cancelled' AND work_orders.work_order_id::text LIKE 'd0000000-%';

    UPDATE work_orders
    SET total_amount = COALESCE((SELECT sum(line_total) FROM work_order_items WHERE work_order_items.work_order_id = work_orders.work_order_id), 0),
        updated_at = COALESCE((SELECT max(changed_at) FROM work_order_history WHERE work_order_history.work_order_id = work_orders.work_order_id), work_orders.created_at),
        completed_at = (SELECT changed_at FROM work_order_history WHERE work_order_history.work_order_id = work_orders.work_order_id AND to_status = 'Completed')
    WHERE work_order_id::text LIKE 'd0000000-%';

    PERFORM setval('work_order_number_seq', GREATEST((SELECT max(order_number) FROM work_orders), 1001));
END
$$;
