using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class MythwakeBackendClient : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string DefaultBackendBaseUrl = "http://10.0.2.2:8080";
#else
    private const string DefaultBackendBaseUrl = "http://localhost:8080";
#endif

    [SerializeField] private string baseUrl = DefaultBackendBaseUrl;
    [SerializeField] private int requestTimeoutSeconds = 10;
    private readonly Dictionary<string, string> pendingActionKeys = new Dictionary<string, string>();
    private const string DefinitionsJsonCacheKey = "Mythwake.Backend.Definitions.Json";
    private const string DefinitionsETagCacheKey = "Mythwake.Backend.Definitions.ETag";
    private const string DefinitionsContentHashCacheKey = "Mythwake.Backend.Definitions.ContentHash";

    public string BaseUrl
    {
        get => string.IsNullOrWhiteSpace(baseUrl) ? DefaultBackendBaseUrl : baseUrl.TrimEnd('/');
        set => baseUrl = string.IsNullOrWhiteSpace(value) ? DefaultBackendBaseUrl : value.TrimEnd('/');
    }

    public IEnumerator GetHealth(Action<bool, string, MythwakeHealthDto> completed)
    {
        return SendJson(Get("/health"), completed);
    }

    public IEnumerator GuestAuth(Action<bool, string, MythwakeGuestAuthResponseDto> completed)
    {
        return SendJson(Post("/auth/guest"), completed);
    }

    public IEnumerator GetPlayerSnapshot(Action<bool, string, MythwakePlayerSnapshotDto> completed)
    {
        return SendJson(Get("/player/state"), completed);
    }

    public IEnumerator GetPlayerCoreState(Action<bool, string, MythwakePlayerStateDto> completed)
    {
        return SendJson(Get("/player/core-state"), completed);
    }

    public IEnumerator GetDefinitions(Action<bool, string, MythwakeDefinitionSnapshotDto, bool> completed)
    {
        var request = Get("/definitions");
        var cachedETag = PlayerPrefs.GetString(DefinitionsETagCacheKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(cachedETag))
        {
            request.SetRequestHeader("If-None-Match", cachedETag);
        }

        return SendDefinitionsJson(request, completed);
    }

    public IEnumerator FightCampaign(Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson("campaign_fight", Post("/campaign/fight"), completed);
    }

    public IEnumerator RunDungeon(string dungeonId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"dungeon_run:{dungeonId}", Post($"/dungeons/{EscapePath(dungeonId)}/run"), completed);
    }

    public IEnumerator LevelHero(string heroId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"hero_level:{heroId}", Post($"/heroes/{EscapePath(heroId)}/level-up"), completed);
    }

    public IEnumerator AscendHero(string heroId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"hero_ascend:{heroId}", Post($"/heroes/{EscapePath(heroId)}/ascend"), completed);
    }

    public IEnumerator LevelEquipment(string equipmentId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"equipment_level:{equipmentId}", Post($"/equipment/{EscapePath(equipmentId)}/level-up"), completed);
    }

    public IEnumerator EquipAccessory(string accessoryId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"accessory_equip:{accessoryId}", PostAccessory("/gear/accessories/equip", accessoryId), completed);
    }

    public IEnumerator LevelAccessory(string accessoryId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"accessory_level:{accessoryId}", PostAccessory("/gear/accessories/level-up", accessoryId), completed);
    }

    public IEnumerator FuseAccessory(string accessoryId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"accessory_fuse:{accessoryId}", PostAccessory("/gear/accessories/fuse", accessoryId), completed);
    }

    public IEnumerator PullSummon(string bannerId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"summon_pull:{bannerId}", Post($"/summons/{EscapePath(bannerId)}/pull"), completed);
    }

    public IEnumerator ClaimDailyMission(string missionId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"daily_mission_claim:{missionId}", Post($"/missions/{EscapePath(missionId)}/claim"), completed);
    }

    public IEnumerator ClaimBattlePassReward(string rewardId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendActionJson($"battle_pass_claim:{rewardId}", Post($"/battle-pass/{EscapePath(rewardId)}/claim"), completed);
    }

    private UnityWebRequest Get(string path)
    {
        var request = UnityWebRequest.Get(BuildUrl(path));
        ApplyCommonHeaders(request);
        return request;
    }

    private UnityWebRequest Post(string path)
    {
        return PostJson(path, "{}");
    }

    private UnityWebRequest PostAccessory(string path, string accessoryId)
    {
        var payload = JsonUtility.ToJson(new AccessoryRequestDto { accessoryId = accessoryId });
        return PostJson(path, payload);
    }

    private UnityWebRequest PostJson(string path, string payload)
    {
        var request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload ?? "{}")),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = Mathf.Max(1, requestTimeoutSeconds)
        };

        ApplyCommonHeaders(request);
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private IEnumerator SendJson<T>(UnityWebRequest request, Action<bool, string, T> completed)
    {
        request.timeout = Mathf.Max(1, requestTimeoutSeconds);

        using (request)
        {
            yield return request.SendWebRequest();

            var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(false, BuildErrorMessage(request, body), default(T));
                yield break;
            }

            try
            {
                var data = JsonUtility.FromJson<T>(body);
                completed?.Invoke(true, string.Empty, data);
            }
            catch (ArgumentException exception)
            {
                completed?.Invoke(false, $"Invalid backend JSON: {exception.Message}", default(T));
            }
        }
    }

    private IEnumerator SendDefinitionsJson(UnityWebRequest request, Action<bool, string, MythwakeDefinitionSnapshotDto, bool> completed)
    {
        request.timeout = Mathf.Max(1, requestTimeoutSeconds);

        using (request)
        {
            yield return request.SendWebRequest();

            var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.responseCode == httpStatusNotModified)
            {
                var cachedBody = PlayerPrefs.GetString(DefinitionsJsonCacheKey, string.Empty);
                if (string.IsNullOrWhiteSpace(cachedBody))
                {
                    completed?.Invoke(false, "Definitions unchanged but no local cache exists.", default(MythwakeDefinitionSnapshotDto), true);
                    yield break;
                }

                CompleteDefinitionsJson(cachedBody, completed, fromCache: true);
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(false, BuildErrorMessage(request, body), default(MythwakeDefinitionSnapshotDto), false);
                yield break;
            }

            if (!CompleteDefinitionsJson(body, completed, fromCache: false))
            {
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                var etag = request.GetResponseHeader("ETag");
                if (!string.IsNullOrWhiteSpace(etag))
                {
                    PlayerPrefs.SetString(DefinitionsETagCacheKey, etag);
                }

                PlayerPrefs.SetString(DefinitionsJsonCacheKey, body);
                PlayerPrefs.Save();
            }
        }
    }

    private static bool CompleteDefinitionsJson(string body, Action<bool, string, MythwakeDefinitionSnapshotDto, bool> completed, bool fromCache)
    {
        try
        {
            var data = JsonUtility.FromJson<MythwakeDefinitionSnapshotDto>(body);
            if (string.IsNullOrWhiteSpace(data.contentHash))
            {
                completed?.Invoke(false, "Definitions response did not include a content hash.", default(MythwakeDefinitionSnapshotDto), fromCache);
                return false;
            }

            PlayerPrefs.SetString(DefinitionsContentHashCacheKey, data.contentHash);
            completed?.Invoke(true, string.Empty, data, fromCache);
            return true;
        }
        catch (ArgumentException exception)
        {
            completed?.Invoke(false, $"Invalid definitions JSON: {exception.Message}", default(MythwakeDefinitionSnapshotDto), fromCache);
            return false;
        }
    }

    private IEnumerator SendActionJson(string actionKey, UnityWebRequest request, Action<bool, string, MythwakeActionResultDto> completed)
    {
        request.SetRequestHeader("Idempotency-Key", GetOrCreatePendingActionKey(actionKey));
        return SendJson<MythwakeActionResultDto>(request, (success, error, result) =>
        {
            if (success)
            {
                pendingActionKeys.Remove(actionKey);
            }

            completed?.Invoke(success, error, result);
        });
    }

    private void ApplyCommonHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("Accept", "application/json");
    }

    private string GetOrCreatePendingActionKey(string actionKey)
    {
        if (pendingActionKeys.TryGetValue(actionKey, out var idempotencyKey) && !string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return idempotencyKey;
        }

        idempotencyKey = Guid.NewGuid().ToString("N");
        pendingActionKeys[actionKey] = idempotencyKey;
        return idempotencyKey;
    }

    private string BuildUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BaseUrl;
        }

        return path[0] == '/' ? $"{BaseUrl}{path}" : $"{BaseUrl}/{path}";
    }

    private static string EscapePath(string value)
    {
        return Uri.EscapeDataString(value ?? string.Empty);
    }

    private static string BuildErrorMessage(UnityWebRequest request, string body)
    {
        var error = string.IsNullOrWhiteSpace(request.error) ? "Backend request failed" : request.error;
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"{request.responseCode}: {error}";
        }

        return $"{request.responseCode}: {error} - {body}";
    }

    private const long httpStatusNotModified = 304;

    [Serializable]
    private struct AccessoryRequestDto
    {
        public string accessoryId;
    }
}
