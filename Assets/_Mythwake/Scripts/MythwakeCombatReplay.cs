using System;
using System.Collections.Generic;

/// <summary>Consumes an immutable server result once. HP comes only from server snapshots.</summary>
public sealed class MythwakeCombatReplay
{
    private readonly MythwakeCombatEventDto[] records;
    private readonly List<int> order = new List<int>();
    private readonly HashSet<string> consumed = new HashSet<string>();
    private int cursor, lastTimeMs = -1;
    public int TeamHp { get; private set; }
    public int EnemyHp { get; private set; }
    public int DamageDealt { get; private set; }
    public int DamageTaken { get; private set; }
    public int LastEventTimeMs { get; private set; }
    public bool Finished => cursor >= order.Count;

    // A killing early contact is the action's effective end: later contacts are cancelled.
    public static bool IsFinalContact(MythwakeCombatEventDto record) => record.contactCount <= 0 ||
        record.contactIndex == record.contactCount - 1 || record.enemyHpRemaining <= 0;

    public static string RecordKey(MythwakeCombatEventDto record, int index)
    {
        if (string.IsNullOrEmpty(record.actionId)) return "record_" + index;
        var contact = !string.IsNullOrEmpty(record.contactId) ? record.contactId :
            record.contactCount > 0 && record.eventType != "action_start" ? record.actionId + ":contact:" + record.contactIndex : record.actionId;
        return contact + ":" + record.eventType;
    }

    public MythwakeCombatReplay(MythwakeCombatResultDto result)
    {
        records = result.events == null ? Array.Empty<MythwakeCombatEventDto>() : (MythwakeCombatEventDto[])result.events.Clone();
        TeamHp = Math.Max(0, result.teamMaxHp);
        EnemyHp = Math.Max(0, result.enemyMaxHp);
        for (var i = 0; i < records.Length; i++) order.Add(i);
        order.Sort((a, b) => { var compare = records[a].timeMs.CompareTo(records[b].timeMs); return compare == 0 ? a.CompareTo(b) : compare; });
        LastEventTimeMs = order.Count == 0 ? 0 : records[order[order.Count - 1]].timeMs;
    }

    public void AdvanceTo(int timeMs, Action<MythwakeCombatEventDto, int> present)
    {
        if (timeMs < lastTimeMs) return;
        lastTimeMs = timeMs;
        while (cursor < order.Count && records[order[cursor]].timeMs <= timeMs)
        {
            var index = order[cursor++];
            var record = records[index];
            var key = RecordKey(record, index);
            if (!consumed.Add(key)) continue;
            var heroDamage = record.eventType == "auto_attack" || record.eventType == "critical_attack" || record.eventType == "ultimate" || record.eventType == "execute";
            if (heroDamage)
            {
                // Legacy ultimate Amount can include healing; the enemy HP difference is damage.
                record.amount = Math.Max(0, EnemyHp - record.enemyHpRemaining);
                DamageDealt += record.amount;
            }
            if (record.eventType == "enemy_attack")
            {
                record.amount = Math.Max(0, TeamHp - record.teamHpRemaining);
                DamageTaken += record.amount;
            }
            TeamHp = Math.Max(0, record.teamHpRemaining);
            EnemyHp = Math.Max(0, record.enemyHpRemaining);
            present?.Invoke(record, index);
        }
    }
}
