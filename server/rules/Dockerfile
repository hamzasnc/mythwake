# Build context: repository root (the rules service compiles the shared Core sources from Assets/).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Assets/Mythwake/Runtime/Core Assets/Mythwake/Runtime/Core
COPY Assets/Mythwake/Runtime/Content Assets/Mythwake/Runtime/Content
COPY server/rules server/rules
RUN dotnet publish server/rules -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
COPY Assets/Mythwake/Content/Data /content/Data
COPY Assets/Mythwake/Content/Heroes /content/Heroes
COPY Assets/Mythwake/Content/Monsters /content/Monsters
ENV MYTHWAKE_CONTENT=/content MYTHWAKE_RULES_ADDR=http://0.0.0.0:5081
EXPOSE 5081
ENTRYPOINT ["dotnet", "Mythwake.Rules.dll"]
