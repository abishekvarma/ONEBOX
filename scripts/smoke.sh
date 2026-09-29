#!/usr/bin/env bash
set -euo pipefail
API="${API_URL:-http://localhost:5080}"
EMAIL="onebox-smoke-$(date +%s)@example.com"
PASS='OneboxSmoke!123'
NAME='ONEBOX Smoke'

echo '[1/6] API health'
curl -fsS "$API/health" | grep -q '"status":"ok"'

echo '[2/6] Register'
REG=$(curl -fsS -X POST "$API/api/auth/register" -H 'Content-Type: application/json' -d "{\"name\":\"$NAME\",\"email\":\"$EMAIL\",\"password\":\"$PASS\",\"phone\":\"9999999999\"}")
OTP=$(printf '%s' "$REG" | python3 -c 'import json,sys; print(json.load(sys.stdin)["developmentOtp"])")
VERIFY=$(curl -fsS -X POST "$API/api/auth/verify-otp" -H 'Content-Type: application/json' -d "{"email":"$EMAIL","otp":"$OTP"}")
TOKEN=$(printf '%s' "$VERIFY" | python3 -c 'import json,sys; print(json.load(sys.stdin)["token"])')
AUTH="Authorization: Bearer $TOKEN"

echo '[3/7] Capabilities endpoint'
CAP=$(curl -fsS "$API/api/capabilities" -H "$AUTH")
printf '%s' "$CAP" | python3 -c 'import json,sys; d=json.load(sys.stdin); assert "providers" in d and "payment" in d'

echo '[4/8] Provider connectors'
CONNECTORS=$(curl -fsS "$API/api/connectors" -H "$AUTH")
printf '%s' "$CONNECTORS" | python3 -c 'import json,sys; d=json.load(sys.stdin); assert any(x["id"]=="bookmyshow" for x in d), d'

 echo '[5/8] Create task (must await confirmation)'
TASK=$(curl -fsS -X POST "$API/api/tasks/agent" -H "$AUTH" -H 'Content-Type: application/json' -d '{"message":"Send a parcel from HSR Layout to Koramangala"}')
ID=$(printf '%s' "$TASK" | python3 -c 'import json,sys; d=json.load(sys.stdin); assert d["requiresConfirmation"] is True; print(d["taskId"])')

echo '[6/8] Confirm same task (sandbox mode required)'
DONE=$(curl -fsS -X POST "$API/api/tasks/$ID/confirm" -H "$AUTH")
printf '%s' "$DONE" | python3 -c 'import json,sys; d=json.load(sys.stdin); assert d["taskId"]==sys.argv[1] and d["status"]=="COMPLETED", d' "$ID"

echo '[7/8] Confirm again (idempotency)'
DONE2=$(curl -fsS -X POST "$API/api/tasks/$ID/confirm" -H "$AUTH")
printf '%s' "$DONE2" | python3 -c 'import json,sys; d=json.load(sys.stdin); assert d["taskId"]==sys.argv[1] and d["status"]=="COMPLETED", d' "$ID"

echo '[8/8] Task exists exactly once'
COUNT=$(curl -fsS "$API/api/tasks/$ID" -H "$AUTH" | python3 -c 'import json,sys; d=json.load(sys.stdin); print(d["id"])')
test "$COUNT" = "$ID"

echo 'ONEBOX smoke test PASSED'
