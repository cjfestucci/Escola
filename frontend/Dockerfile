# Site (Angular) servido pelo nginx, que também repassa /api e /uploads pra API — assim o navegador fala com um endereço só
# (o app em produção chama '/api', ver src/environments/environment.ts). Contexto de build: a pasta frontend/.
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/escola-frontend/browser /usr/share/nginx/html
# Endereço da API dentro da rede do compose (o template do nginx troca ${API_UPSTREAM} ao subir)
ENV API_UPSTREAM=http://api:8080
EXPOSE 80
