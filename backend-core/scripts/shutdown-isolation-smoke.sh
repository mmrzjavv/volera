#!/usr/bin/env bash
# Smoke test against a domestically reachable API base (Compose or local).
# Does not block foreign networks itself — pair with host firewall / isolation lab.
# Usage: API_BASE=http://localhost:5002 ./shutdown-isolation-smoke.sh
set -euo pipefail

API_BASE="${API_BASE:-http://localhost:5002}"
API="${API_BASE%/}/api/v1"

echo "== health =="
curl -fsS "${API_BASE%/}/health" | tee /tmp/health.out
grep -qi "OK\|Healthy\|ok" /tmp/health.out || echo "(health body printed; confirm manually)"

echo "== auth login (set SMOKE_USER / SMOKE_PASSWORD) =="
if [[ -z "${SMOKE_USER:-}" || -z "${SMOKE_PASSWORD:-}" ]]; then
  echo "Skip login: set SMOKE_USER and SMOKE_PASSWORD to exercise send/sync."
  exit 0
fi

LOGIN=$(curl -fsS -X POST "${API}/Auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${SMOKE_USER}\",\"password\":\"${SMOKE_PASSWORD}\"}")
TOKEN=$(echo "$LOGIN" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [[ -z "$TOKEN" ]]; then
  echo "Login failed or token missing"
  exit 1
fi

CLIENT_ID=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || uuidgen || python -c 'import uuid; print(uuid.uuid4())')
PEER="${SMOKE_PEER_USER_ID:?Set SMOKE_PEER_USER_ID to a peer user GUID}"

echo "== send message clientMessageId=$CLIENT_ID =="
SEND1=$(curl -fsS -X POST "${API}/Message" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"receiverId\":\"${PEER}\",\"content\":\"isolation-smoke\",\"clientMessageId\":\"${CLIENT_ID}\"}")
ID1=$(echo "$SEND1" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')

echo "== idempotent resend =="
SEND2=$(curl -fsS -X POST "${API}/Message" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"receiverId\":\"${PEER}\",\"content\":\"isolation-smoke\",\"clientMessageId\":\"${CLIENT_ID}\"}")
ID2=$(echo "$SEND2" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')

if [[ "$ID1" != "$ID2" ]]; then
  echo "FAIL: idempotent send returned different ids: $ID1 vs $ID2"
  exit 1
fi
echo "OK same id $ID1"

echo "== sync =="
curl -fsS "${API}/Message/sync?peerUserId=${PEER}&limit=10" \
  -H "Authorization: Bearer ${TOKEN}" | head -c 500
echo
echo "Smoke completed."
