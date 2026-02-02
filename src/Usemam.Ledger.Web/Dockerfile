FROM node:20-alpine AS build
WORKDIR /app
COPY src/Usemam.Ledger.Web/package*.json ./
RUN npm ci
COPY src/Usemam.Ledger.Web/ .
ARG VITE_API_URL=http://localhost:5000
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY src/Usemam.Ledger.Web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
