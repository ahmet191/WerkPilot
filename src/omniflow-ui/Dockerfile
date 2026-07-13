# ── Build stage ──────────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
# Same-origin relative API paths; nginx proxies them to the api service.
ENV VITE_API_URL=/api
RUN npm run build

# ── Runtime stage (nginx) ────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
