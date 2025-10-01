FROM node:22-alpine AS builder
WORKDIR /app

ARG PUBLIC_API_BASE_URL
ENV PUBLIC_API_BASE_URL=${PUBLIC_API_BASE_URL}
ENV VITE_API_BASE_URL=${PUBLIC_API_BASE_URL}

ARG PUBLIC_GOOGLE_CLIENT_ID
ENV PUBLIC_GOOGLE_CLIENT_ID=${PUBLIC_GOOGLE_CLIENT_ID}
ENV VITE_GOOGLE_CLIENT_ID=${PUBLIC_GOOGLE_CLIENT_ID}

COPY CertVal.Web/package*.json ./

RUN if [ -f package-lock.json ]; then \
			echo "Using npm ci with existing lockfile"; \
			npm ci; \
		else \
			echo "No package-lock.json found, using npm install"; \
			npm install --no-audit --no-fund; \
		fi
COPY . .
WORKDIR /app/CertVal.Web
RUN npm run build
WORKDIR /app
RUN npm prune --production

FROM node:22-alpine
WORKDIR /app
COPY --from=builder /app/CertVal.Web/build build/
COPY --from=builder /app/node_modules node_modules/
COPY --from=builder /app/package.json .
EXPOSE 3000
ENV NODE_ENV=production
CMD [ "node", "build" ]