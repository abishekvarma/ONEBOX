FROM node:22-alpine AS build
WORKDIR /app
COPY package.json .
RUN npm install
COPY . .
ARG VITE_API_URL=http://localhost:5080
ARG VITE_GOOGLE_MAPS_API_KEY=
ARG VITE_ONEBOX_AGENT_URL=http://127.0.0.1:8787
ENV VITE_API_URL=$VITE_API_URL VITE_GOOGLE_MAPS_API_KEY=$VITE_GOOGLE_MAPS_API_KEY VITE_ONEBOX_AGENT_URL=$VITE_ONEBOX_AGENT_URL
RUN npm run build
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
