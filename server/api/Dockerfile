FROM golang:1.26 AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build -o /out/mythwake-api ./cmd/mythwake-api

FROM gcr.io/distroless/static-debian12
COPY --from=build /out/mythwake-api /mythwake-api
EXPOSE 8080
ENTRYPOINT ["/mythwake-api"]
