# UI image: build the static site with Node, serve it with nginx (non-root variant).
# Build context: the frontend folder (docker compose does this for you).

FROM node:24-alpine AS build
WORKDIR /app

# Dependencies first, so "npm ci" is cached until package-lock.json changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.28-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
