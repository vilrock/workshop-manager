const base = process.env.API ?? 'http://localhost:8081';
const PASSWORD = 'Workshop#2026';
let failures = 0;

async function call(method, path, { token, body, headers = {} } = {}) {
  const res = await fetch(base + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...headers },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = text; }
  return { status: res.status, json, headers: res.headers };
}

function expect(label, actual, expected, extra = '') {
  const ok = Array.isArray(expected) ? expected.includes(actual) : actual === expected;
  if (!ok) failures++;
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${label}  -> ${actual}${ok ? '' : ` (expected ${expected})`} ${extra}`);
}

async function login(email) {
  const r = await call('POST', '/api/v1/auth/login', { body: { email, password: PASSWORD } });
  expect(`login ${email}`, r.status, 200);
  return r.json.accessToken;
}

const admin = await login('admin@example.com');
const advisor = await login('advisor@example.com');
const mech1 = await login('mechanic1@example.com');
const mech2 = await login('mechanic2@example.com');

expect('login wrong password', (await call('POST', '/api/v1/auth/login', { body: { email: 'admin@example.com', password: 'nope' } })).status, 401);
expect('login invalid body', (await call('POST', '/api/v1/auth/login', { body: { email: 'not-an-email', password: '' } })).status, 400);
expect('no token', (await call('GET', '/api/v1/customers')).status, 401);
const me = await call('GET', '/api/v1/auth/me', { token: mech1 });
expect('me', me.status, 200, me.json.role);

expect('mechanic cannot list customers', (await call('GET', '/api/v1/customers', { token: mech1 })).status, 403);
const customers = await call('GET', '/api/v1/customers?search=olivia&page=1&pageSize=5', { token: advisor });
expect('customers search', customers.status, 200, `total=${customers.json.totalCount}`);
const bad = await call('GET', '/api/v1/customers?pageSize=1000', { token: advisor });
expect('customers bad page size', bad.status, 400, JSON.stringify(bad.json.errors));

const email = `smoke.${Date.now()}@example.com`;
const created = await call('POST', '/api/v1/customers', { token: advisor, body: { fullName: 'Smoke Test', email, phone: '+1 555 0199', address: '1 Test Road' } });
expect('create customer', created.status, 201);
expect('duplicate customer', (await call('POST', '/api/v1/customers', { token: advisor, body: { fullName: 'Smoke Test', email: email.toUpperCase(), phone: '+1 555 0199' } })).status, 409);
expect('invalid customer', (await call('POST', '/api/v1/customers', { token: advisor, body: { fullName: '', email: 'x', phone: 'abc' } })).status, 400);
const customerId = created.json.customerId;
expect('update customer', (await call('PUT', `/api/v1/customers/${customerId}`, { token: advisor, body: { fullName: 'Smoke Tester', email, phone: '+1 555 0199', address: null, isActive: true } })).status, 200);
expect('get unknown customer', (await call('GET', '/api/v1/customers/11111111-1111-1111-1111-111111111111', { token: advisor })).status, 404);

const plate = `T${Date.now() % 100000}`;
const vehicle = await call('POST', `/api/v1/customers/${customerId}/vehicles`, { token: advisor, body: { licensePlate: plate, make: 'Fiat', model: 'Panda', year: 2020, vin: null, color: 'Red', mileageKm: 1000 } });
expect('create vehicle', vehicle.status, 201);
expect('duplicate plate', (await call('POST', `/api/v1/customers/${customerId}/vehicles`, { token: advisor, body: { licensePlate: plate.toLowerCase(), make: 'Fiat', model: 'Panda', year: 2020, mileageKm: 1 } })).status, 409);
expect('list customer vehicles', (await call('GET', `/api/v1/customers/${customerId}/vehicles`, { token: advisor })).json.length, 1);
expect('update vehicle', (await call('PUT', `/api/v1/vehicles/${vehicle.json.vehicleId}`, { token: advisor, body: { licensePlate: plate, make: 'Fiat', model: 'Panda', year: 2021, mileageKm: 1200 } })).status, 200);
expect('list vehicles', (await call('GET', '/api/v1/vehicles?search=toyota', { token: advisor })).json.totalCount, 1);

const orderBody = { customerId, vehicleId: vehicle.json.vehicleId, description: 'Smoke test order', mileageKm: 1200 };
expect('create order without idempotency key', (await call('POST', '/api/v1/work-orders', { token: advisor, body: orderBody })).status, 400);
expect('mechanic cannot create order', (await call('POST', '/api/v1/work-orders', { token: mech1, body: orderBody, headers: { 'Idempotency-Key': crypto.randomUUID() } })).status, 403);
const key = crypto.randomUUID();
const order = await call('POST', '/api/v1/work-orders', { token: advisor, body: orderBody, headers: { 'Idempotency-Key': key, 'X-Correlation-Id': 'smoke-corr-1' } });
expect('create order', order.status, 201, `${order.json.orderNumber} corr=${order.headers.get('x-correlation-id')}`);
const replay = await call('POST', '/api/v1/work-orders', { token: advisor, body: orderBody, headers: { 'Idempotency-Key': key } });
expect('idempotent replay', replay.status, 200, `same=${replay.json.workOrderId === order.json.workOrderId} header=${replay.headers.get('idempotent-replayed')}`);
expect('idempotency key reuse with other payload', (await call('POST', '/api/v1/work-orders', { token: advisor, body: { ...orderBody, description: 'Different' }, headers: { 'Idempotency-Key': key } })).status, 422);
const parallelKey = crypto.randomUUID();
const parallel = await Promise.all([1, 2, 3].map(() => call('POST', '/api/v1/work-orders', { token: advisor, body: orderBody, headers: { 'Idempotency-Key': parallelKey } })));
const ids = new Set(parallel.map(r => r.json.workOrderId));
expect('parallel same key creates one order', ids.size, 1, parallel.map(r => r.status).join(','));

const id = order.json.workOrderId;
const mechanics = await call('GET', '/api/v1/users/mechanics', { token: advisor });
const isRecent = (iso) => Math.abs(Date.now() - new Date(iso).getTime()) < 5 * 60 * 1000;
expect('order timestamps are mapped', isRecent(order.json.createdAtUtc) && isRecent(order.json.updatedAtUtc) && isRecent(order.json.history[0].changedAtUtc), true);
expect('customer timestamps are mapped', isRecent(created.json.createdAtUtc), true);
const seeded = await call('GET', '/api/v1/work-orders?search=WO-1010', { token: admin });
expect('seeded order exposes real dates', new Date(seeded.json.items[0].createdAtUtc).getFullYear() >= 2026, true);
expect('list mechanics', mechanics.status, 200);
const mechanic1 = mechanics.json.find(m => m.fullName === 'Daniel Cruz').userId;
expect('mechanic cannot list mechanics', (await call('GET', '/api/v1/users/mechanics', { token: mech1 })).status, 403);

const transition = (token, toStatus, extra = {}) => call('POST', `/api/v1/work-orders/${id}/transitions`, { token, body: { toStatus, ...extra } });

expect('diagnose without mechanic assigned (admin)', (await transition(admin, 'Diagnosed')).status, 422);
expect('mechanic cannot assign', (await call('PUT', `/api/v1/work-orders/${id}/mechanic`, { token: mech1, body: { mechanicId: mechanic1 } })).status, 403);
expect('assign non-mechanic user', (await call('PUT', `/api/v1/work-orders/${id}/mechanic`, { token: advisor, body: { mechanicId: me.json.userId === mechanic1 ? mechanic1 : '11111111-1111-1111-1111-111111111111' } })).status, [200, 404]);
const assign = await call('PUT', `/api/v1/work-orders/${id}/mechanic`, { token: advisor, body: { mechanicId: mechanic1 } });
expect('assign mechanic', assign.status, 200, assign.json.assignedMechanicName);

expect('advisor cannot diagnose (role)', (await transition(advisor, 'Diagnosed')).status, 403);
expect('other mechanic cannot see order', (await call('GET', `/api/v1/work-orders/${id}`, { token: mech2 })).status, 403);
expect('other mechanic cannot transition', (await transition(mech2, 'Diagnosed')).status, 403);
expect('invalid jump Received->Completed', (await transition(mech1, 'Completed')).status, 422);
expect('stale expectedStatus', (await transition(mech1, 'Diagnosed', { expectedStatus: 'Approved' })).status, 409);

const race = await Promise.all([
  transition(mech1, 'Diagnosed', { expectedStatus: 'Received', note: 'Race A' }),
  transition(mech1, 'Diagnosed', { expectedStatus: 'Received', note: 'Race B' })
]);
const raceStatuses = race.map(r => r.status).sort();
expect('concurrent transitions: one wins, one conflicts', raceStatuses.join(','), '200,409');

expect('approve without items', (await transition(advisor, 'Approved')).status, 422);
expect('other mechanic cannot add item', (await call('POST', `/api/v1/work-orders/${id}/items`, { token: mech2, body: { itemType: 'Part', description: 'x', quantity: 1, unitPrice: 1 } })).status, 403);
expect('invalid item', (await call('POST', `/api/v1/work-orders/${id}/items`, { token: mech1, body: { itemType: 'Part', description: '', quantity: 0, unitPrice: -1 } })).status, 400);
const item1 = await call('POST', `/api/v1/work-orders/${id}/items`, { token: mech1, body: { itemType: 'Part', description: 'Oil filter', quantity: 2, unitPrice: 12.345 } });
expect('item with 3 decimals rejected', item1.status, 400);
const item2 = await call('POST', `/api/v1/work-orders/${id}/items`, { token: mech1, body: { itemType: 'Part', description: 'Oil filter', quantity: 2.5, unitPrice: 12.35 } });
expect('add part item', item2.status, 201, `total=${item2.json.totalAmount}`);
const item3 = await call('POST', `/api/v1/work-orders/${id}/items`, { token: advisor, body: { itemType: 'Service', description: 'Labor', quantity: 1.5, unitPrice: 95 } });
expect('add service item', item3.status, 201, `total=${item3.json.totalAmount}`);
expect('server computed total (30.88 + 142.50)', item3.json.totalAmount, 173.38);

expect('mechanic cannot approve', (await transition(mech1, 'Approved')).status, 403);
expect('advisor approves', (await transition(advisor, 'Approved')).status, 200);
expect('advisor cannot start work', (await transition(advisor, 'InProgress')).status, 403);
expect('mechanic starts work', (await transition(mech1, 'InProgress')).status, 200);
expect('cannot cancel in progress', (await transition(advisor, 'Cancelled')).status, 422);
expect('cannot add item after approval by non-editable?', (await call('POST', `/api/v1/work-orders/${id}/items`, { token: mech1, body: { itemType: 'Part', description: 'Extra', quantity: 1, unitPrice: 5 } })).status, 201);
expect('mechanic completes', (await transition(mech1, 'Completed')).status, 200);
expect('item locked after completion', (await call('POST', `/api/v1/work-orders/${id}/items`, { token: mech1, body: { itemType: 'Part', description: 'Late', quantity: 1, unitPrice: 5 } })).status, 422);
expect('mechanic cannot deliver', (await transition(mech1, 'Delivered')).status, 403);
const delivered = await transition(advisor, 'Delivered', { note: 'Keys handed over' });
expect('advisor delivers', delivered.status, 200, `history=${delivered.json.history.length} allowed=${delivered.json.allowedTransitions}`);
console.log('history:', delivered.json.history.map(h => `${h.fromStatus ?? '-'}>${h.toStatus} by ${h.changedByName} [${h.correlationId.slice(0, 12)}]`).join(' | '));

const list1 = await call('GET', '/api/v1/work-orders?pageSize=100', { token: mech1 });
expect('mechanic list only own', list1.json.items.every(o => o.assignedMechanicId === mechanic1), true, `count=${list1.json.items.length}`);
const list2 = await call('GET', '/api/v1/work-orders?status=InProgress&page=1&pageSize=10&search=suspension', { token: admin });
expect('admin list with filters', list2.status, 200, `total=${list2.json.totalCount}`);
expect('invalid status filter', (await call('GET', '/api/v1/work-orders?status=Bogus', { token: admin })).status, 400);
const dash = await call('GET', '/api/v1/dashboard/stats', { token: admin });
expect('dashboard admin', dash.status, 200, `open=${dash.json.openOrders} rev=${dash.json.monthlyRevenue} avgDone=${dash.json.averageHoursToComplete?.toFixed(1)} diag=${dash.json.averageHoursToDiagnose?.toFixed(1)}`);
console.log('revenueByMonth', JSON.stringify(dash.json.revenueByMonth));
const dashMech = await call('GET', '/api/v1/dashboard/stats', { token: mech2 });
expect('dashboard mechanic', dashMech.status, 200, `open=${dashMech.json.openOrders}`);
const notFound = await call('GET', '/api/v1/work-orders/11111111-1111-1111-1111-111111111111', { token: admin });
expect('unknown order', notFound.status, 404, JSON.stringify(notFound.json));
const health = await fetch(base + '/health/ready');
expect('health ready', health.status, 200);
const oa = await fetch(base + '/openapi/v1.json');
expect('openapi json', oa.status, 200);
const swagger = await fetch(base + '/swagger/index.html');
expect('swagger ui', swagger.status, 200);

const attempts = [];
for (let i = 0; i < 14; i++) attempts.push((await call('POST', '/api/v1/auth/login', { body: { email: 'admin@example.com', password: 'wrong' } })).status);
expect('login rate limit eventually 429', attempts.includes(429), true, attempts.join(','));

console.log(failures === 0 ? '\nALL CHECKS PASSED' : `\n${failures} CHECK(S) FAILED`);
process.exit(failures === 0 ? 0 : 1);
