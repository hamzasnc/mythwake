FROM golang:1.25-alpine AS build

WORKDIR /src
COPY backend/go.mod backend/go.sum ./
RUN go mod download

COPY backend/ ./
RUN CGO_ENABLED=0 GOOS=linux go build -trimpath -ldflags="-s -w" -o /out/mythwake-api ./cmd/api

FROM alpine:3.22

RUN adduser -D -H -u 10001 mythwake
USER mythwake

COPY --from=build /out/mythwake-api /usr/local/bin/mythwake-api

EXPOSE 8080
ENTRYPOINT ["mythwake-api"]
