#!/bin/sh
set -eu

CERT_DIR=/etc/nginx/certs
CONF_SRC=/etc/nginx/templates/default.conf.template
CONF_DST=/etc/nginx/conf.d/default.conf
PUBLIC_HTTPS_PORT="${PUBLIC_HTTPS_PORT:-18262}"
TLS_EXTRA_IPS="${TLS_EXTRA_IPS:-192.168.1.4}"
TLS_EXTRA_DNS="${TLS_EXTRA_DNS:-localhost}"

mkdir -p "$CERT_DIR"

# Substitute redirect port into nginx config (HTTP → HTTPS on the published port).
sed "s/\${PUBLIC_HTTPS_PORT}/${PUBLIC_HTTPS_PORT}/g" "$CONF_SRC" > "$CONF_DST"

# Build SAN list: localhost + extra DNS + extra IPs (comma-separated).
SAN="DNS:localhost,IP:127.0.0.1"
OLD_IFS=$IFS
IFS=,
for d in $TLS_EXTRA_DNS; do
  d=$(echo "$d" | tr -d ' ')
  [ -n "$d" ] && SAN="${SAN},DNS:${d}"
done
for ip in $TLS_EXTRA_IPS; do
  ip=$(echo "$ip" | tr -d ' ')
  [ -n "$ip" ] && SAN="${SAN},IP:${ip}"
done
IFS=$OLD_IFS

NEED_CERT=1
if [ -f "$CERT_DIR/fullchain.pem" ] && [ -f "$CERT_DIR/privkey.pem" ]; then
  NEED_CERT=0
fi

if [ "$NEED_CERT" = "1" ]; then
  echo "[frontend] Generating self-signed TLS cert for WebRTC (SAN=${SAN})"
  openssl req -x509 -nodes -newkey rsa:2048 -days 825 \
    -keyout "$CERT_DIR/privkey.pem" \
    -out "$CERT_DIR/fullchain.pem" \
    -subj "/CN=localhost" \
    -addext "subjectAltName=${SAN}"
fi

echo "[frontend] HTTPS ready on :443 (host port ${PUBLIC_HTTPS_PORT}). Open https://<lan-ip>:${PUBLIC_HTTPS_PORT} and accept the certificate warning once — required for microphone/camera."
exec nginx -g 'daemon off;'
