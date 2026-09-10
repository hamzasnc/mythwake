using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Builds/disposes requests only: no SendWebRequest, network, profile or account writes.</summary>
public static class KaelBackendRetryValidation
{
    [MenuItem("Mythwake/Tests/Frozen Combat Retry Envelope")]
    public static void Run()
    {
        var owner = new GameObject("Frozen retry validation");
        owner.SetActive(false);
        try
        {
            var client = owner.AddComponent<MythwakeBackendClient>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(MythwakeBackendClient);
            type.GetField("cachedPlayerId", flags).SetValue(client, "isolated-request-fixture");
            type.GetField("cachedSessionToken", flags).SetValue(client, "fixture-not-a-session");
            type.GetField("cachedStateRevision", flags).SetValue(client, 11L);
            var post = type.GetMethod("PostJson", flags);
            var pending = type.GetMethod("CreatePendingActionRequest", flags);
            const string body = "{\"heroIds\":[\"hero_kael\",\"hero_elowen\"]}";
            Func<UnityWebRequest> original = () => (UnityWebRequest)post.Invoke(client, new object[] { "/campaign/fight", body });
            string key, endpoint;
            using (var first = (UnityWebRequest)pending.Invoke(client, new object[] { "campaign_fight", original }))
            {
                key = first.GetRequestHeader("Idempotency-Key"); endpoint = first.url;
                Assert(!string.IsNullOrEmpty(key), "First action needs an idempotency key.");
                Assert(first.GetRequestHeader("X-Player-State-Revision") == "11", "First action snapshots revision.");
            }
            type.GetField("cachedStateRevision", flags).SetValue(client, 99L);
            var changedFactoryCalls = 0;
            Func<UnityWebRequest> changed = () => { changedFactoryCalls++; return (UnityWebRequest)post.Invoke(client,
                new object[] { "/campaign/fight?changed=1", "{\"heroIds\":[\"hero_borin\"]}" }); };
            using (var retry = (UnityWebRequest)pending.Invoke(client, new object[] { "campaign_fight", changed }))
            {
                Assert(changedFactoryCalls == 0, "An unresolved retry must not rebuild from the changed formation.");
                Assert(retry.GetRequestHeader("Idempotency-Key") == key, "Retry keeps the same idempotency key.");
                Assert(retry.GetRequestHeader("X-Player-State-Revision") == "11", "Retry retains the original revision after refresh.");
                Assert(retry.url == endpoint && retry.method == UnityWebRequest.kHttpVerbPOST, "Retry retains endpoint and verb.");
                Assert(Encoding.UTF8.GetString(retry.uploadHandler.data) == body, "Retry keeps byte-identical ordered hero IDs.");
            }
            type.GetField("cachedPlayerId", flags).SetValue(client, "different-isolated-request-fixture");
            using (var otherAccount = (UnityWebRequest)pending.Invoke(client, new object[] { "campaign_fight", changed }))
            {
                Assert(changedFactoryCalls == 1 && otherAccount.GetRequestHeader("Idempotency-Key") != key,
                    "A different account cannot inherit another account's pending action.");
            }
            Debug.Log("Frozen combat retry envelope: body, endpoint, method, key, revision and account boundary passed; no network requests sent.");
        }
        finally { UnityEngine.Object.DestroyImmediate(owner); }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
