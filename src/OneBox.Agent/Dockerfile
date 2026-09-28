FROM node:22-bookworm
WORKDIR /app
COPY package.json ./
RUN npm install --omit=dev && npx playwright install --with-deps chromium
COPY server.mjs ./
EXPOSE 8787
CMD ["node","server.mjs"]
