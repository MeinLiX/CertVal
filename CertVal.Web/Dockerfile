FROM node:22-alpine AS builder
WORKDIR /app

ARG PUBLIC_API_BASE_URL
ENV PUBLIC_API_BASE_URL=${PUBLIC_API_BASE_URL}
ENV VITE_API_BASE_URL=${PUBLIC_API_BASE_URL}

COPY CertVal.Web/package*.json ./

RUN npm ci
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