# Build stage
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
# Build with same-origin API (nginx will proxy /api and hubs to backend)
ENV VITE_API_URL=
RUN npm run build

# Serve with nginx + TLS (WebRTC/getUserMedia requires a secure context on LAN IPs)
FROM nginx:alpine AS final
RUN apk add --no-cache openssl \
  && mkdir -p /etc/nginx/certs /etc/nginx/templates

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/templates/default.conf.template

# Write entrypoint with Unix LF (COPY from Windows can break shebang with CRLF)
RUN printf '%s\n' \
  '#!/bin/sh' \
  'set -eu' \
  'CERT_DIR=/etc/nginx/certs' \
  'CONF_SRC=/etc/nginx/templates/default.conf.template' \
  'CONF_DST=/etc/nginx/conf.d/default.conf' \
  'PUBLIC_HTTPS_PORT="${PUBLIC_HTTPS_PORT:-18262}"' \
  'TLS_EXTRA_IPS="${TLS_EXTRA_IPS:-192.168.1.4}"' \
  'TLS_EXTRA_DNS="${TLS_EXTRA_DNS:-localhost}"' \
  'mkdir -p "$CERT_DIR"' \
  'sed "s/\${PUBLIC_HTTPS_PORT}/${PUBLIC_HTTPS_PORT}/g" "$CONF_SRC" > "$CONF_DST"' \
  'SAN="DNS:localhost,IP:127.0.0.1"' \
  'OLD_IFS=$IFS' \
  'IFS=,' \
  'for d in $TLS_EXTRA_DNS; do' \
  '  d=$(echo "$d" | tr -d " ")' \
  '  [ -n "$d" ] && SAN="${SAN},DNS:${d}"' \
  'done' \
  'for ip in $TLS_EXTRA_IPS; do' \
  '  ip=$(echo "$ip" | tr -d " ")' \
  '  [ -n "$ip" ] && SAN="${SAN},IP:${ip}"' \
  'done' \
  'IFS=$OLD_IFS' \
  'if [ ! -f "$CERT_DIR/fullchain.pem" ] || [ ! -f "$CERT_DIR/privkey.pem" ]; then' \
  '  echo "[frontend] Generating self-signed TLS cert for WebRTC (SAN=${SAN})"' \
  '  openssl req -x509 -nodes -newkey rsa:2048 -days 825 \' \
  '    -keyout "$CERT_DIR/privkey.pem" \' \
  '    -out "$CERT_DIR/fullchain.pem" \' \
  '    -subj "/CN=localhost" \' \
  '    -addext "subjectAltName=${SAN}"' \
  'fi' \
  'echo "[frontend] HTTPS ready. Open https://<lan-ip>:${PUBLIC_HTTPS_PORT} and accept the cert once."' \
  'exec nginx -g "daemon off;"' \
  > /docker-entrypoint.sh \
  && chmod +x /docker-entrypoint.sh \
  && rm -f /etc/nginx/conf.d/default.conf

ENV PUBLIC_HTTPS_PORT=18262
ENV TLS_EXTRA_IPS=192.168.1.4
ENV TLS_EXTRA_DNS=localhost

EXPOSE 80 443
ENTRYPOINT ["/docker-entrypoint.sh"]
