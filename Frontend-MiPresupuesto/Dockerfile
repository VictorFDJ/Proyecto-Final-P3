FROM node:22-alpine AS build
WORKDIR /app

COPY Frontend-MiPresupuesto/package.json Frontend-MiPresupuesto/package-lock.json ./
RUN npm ci
COPY Frontend-MiPresupuesto/ ./

ARG VITE_API_URL=/api
ARG VITE_PASSWORD_RESET_ENABLED=true
ENV VITE_API_URL=$VITE_API_URL
ENV VITE_PASSWORD_RESET_ENABLED=$VITE_PASSWORD_RESET_ENABLED
RUN npm run build

FROM nginx:alpine AS final
COPY Frontend-MiPresupuesto/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
