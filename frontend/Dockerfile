# UI image: build the static site with Node, serve it with nginx (non-root variant).
# Build context: the frontend folder (docker compose does this for you).

FROM node:26-alpine AS build
WORKDIR /app

# Dependencies first, so "npm ci" is cached until package-lock.json changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.31-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Outside conf.d on purpose: files there are loaded at http level, this one is included per location.
COPY security-headers.conf /etc/nginx/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
