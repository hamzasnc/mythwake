using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mythwake.Content;
using Mythwake.Core.Meta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mythwake.Rules
{
    /// <summary>
    /// Stateless rules service. The Go API sends a player state and commands; this process runs the same Mythwake.Core
    /// the client runs and returns the new state. It never stores anything. See docs/SERVER.md.
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddSimpleConsole(o => o.SingleLine = true);
            var app = builder.Build();

            var contentDir = Environment.GetEnvironmentVariable("MYTHWAKE_CONTENT")
                             ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Assets/Mythwake/Content"));
            var rules = RulesEngine.Load(contentDir);
            app.Logger.LogInformation("content {Dir} version {Version}, {Heroes} heroes, {Commands} commands",
                contentDir, rules.ContentVersion, rules.Content.Heroes.Count, CommandCodec.Names.Count);

            app.MapGet("/health", (Func<IResult>)(() => Results.Json(new { ok = true, contentVersion = rules.ContentVersion })));
            app.MapPost("/create", (Func<HttpRequest, Task<IResult>>)(async req => Reply(rules.Create(await Body(req)))));
            app.MapPost("/apply", (Func<HttpRequest, Task<IResult>>)(async req => Reply(rules.Apply(await Body(req)))));
            app.MapPost("/arena/team", (Func<HttpRequest, Task<IResult>>)(async req => Reply(rules.ArenaTeam(await Body(req)))));
            app.MapGet("/config/arena", (Func<IResult>)(() => Reply(rules.ArenaConfig())));
            app.MapPost("/arena/bot", (Func<HttpRequest, Task<IResult>>)(async req => Reply(rules.ArenaBot(await Body(req)))));
            app.MapPost("/arena/battle", (Func<HttpRequest, Task<IResult>>)(async req => Reply(rules.ArenaBattle(await Body(req)))));
            app.MapGet("/config/guild", (Func<IResult>)(() => Reply(rules.GuildConfig())));
            app.MapPost("/raid/battle", (Func<HttpRequest, Task<IResult>>)(async req => Reply(rules.RaidBattle(await Body(req)))));

            app.Run(Environment.GetEnvironmentVariable("MYTHWAKE_RULES_ADDR") ?? "http://127.0.0.1:5081");
        }

        private static async Task<JObject> Body(HttpRequest req)
        {
            using var reader = new StreamReader(req.Body);
            var text = await reader.ReadToEndAsync();
            return string.IsNullOrWhiteSpace(text) ? new JObject() : JObject.Parse(text);
        }

        private static IResult Reply(JObject body) =>
            Results.Content(body.ToString(Formatting.None), "application/json", null, body.Value<bool?>("ok") == false ? 422 : 200);
    }
}
