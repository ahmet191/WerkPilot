# ── Build stage ──────────────────────────────────────────────────────────
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
# Same-origin relative API paths; nginx proxies them to the api service.
ENV VITE_API_URL=/api
RUN npm run build

# ── Runtime stage (nginx) ────────────────────────────────────────────────
# Unprivileged nginx: runs as uid 101, listens on 8080 (a non-root user cannot
# bind :80). nginx.conf listens on 8080 to match; compose/ingress map to it.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080

# busybox wget ships in the alpine base — no extra package for the healthcheck.
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -q --spider http://localhost:8080/ || exit 1
