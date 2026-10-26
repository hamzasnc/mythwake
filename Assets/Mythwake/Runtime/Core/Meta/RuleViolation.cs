namespace Mythwake.Core.Meta
{
    /// <summary>
    /// Thrown by a command when the request breaks a rule (not enough gold, hero not owned, …).
    /// Commands validate everything before they mutate state, so a violation never leaves a half-applied change.
    /// </summary>
    public sealed class RuleViolation : System.Exception
    {
        /// <summary>Stable machine-readable code, e.g. "not_enough:gold". The UI maps it to localized text.</summary>
        public readonly string Code;

        public RuleViolation(string code, string detail = null) : base(detail == null ? code : code + ": " + detail)
        {
            Code = code;
        }
    }
}
