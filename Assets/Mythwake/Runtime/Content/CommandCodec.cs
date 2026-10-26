using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mythwake.Core.Meta;
using Newtonsoft.Json;

namespace Mythwake.Content
{
    /// <summary>
    /// Wire format of player commands, shared by the client and the rules service: a type name plus the command's
    /// fields as JSON. Every <see cref="ICommand{TResult}"/> in Mythwake.Core is registered by its class name.
    /// </summary>
    public static class CommandCodec
    {
        private static readonly Dictionary<string, Type> Types;
        private static readonly Dictionary<Type, MethodInfo> ApplyMethods = new Dictionary<Type, MethodInfo>();

        static CommandCodec()
        {
            Types = typeof(ICommand<>).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && CommandInterface(t) != null)
                .ToDictionary(t => t.Name);
        }

        public static IReadOnlyCollection<string> Names => Types.Keys;

        private static Type CommandInterface(Type t) =>
            t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

        public static string NameOf(object command) => command.GetType().Name;

        public static string Encode(object command) => JsonConvert.SerializeObject(command, SaveJson.Settings);

        public static object Decode(string type, string json)
        {
            if (type == null || !Types.TryGetValue(type, out var t))
                throw new ContentException($"unknown command '{type}'");
            return JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "{}" : json, t, SaveJson.Settings)
                   ?? throw new ContentException($"empty command '{type}'");
        }

        /// <summary>Runs any command and returns its result as object. Throws <see cref="RuleViolation"/> like the command.</summary>
        public static object Run(object command, CommandContext ctx)
        {
            var type = command.GetType();
            if (!ApplyMethods.TryGetValue(type, out var apply))
            {
                apply = CommandInterface(type)?.GetMethod("Apply") ?? throw new ContentException($"{type.Name} is not a command");
                ApplyMethods[type] = apply;
            }
            try
            {
                return apply.Invoke(command, new object[] { ctx });
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }
    }
}
