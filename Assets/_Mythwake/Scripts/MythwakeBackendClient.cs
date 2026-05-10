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
    private string cachedSessionToken;
    private string cachedPlayerId;
    private MythwakeServerClockDto lastServerClock;
    private DateTime lastServerClockUtc;
    private float lastServerClockRealtime;
    private bool hasServerClock;
    private const string SessionTokenCacheKey = "Mythwake.Backend.SessionToken";
    private const string PlayerIdCacheKey = "Mythwake.Backend.PlayerId";
    private const string DefinitionsJsonCacheKey = "Mythwake.Backend.Definitions.Json";
    private const string DefinitionsETagCacheKey = "Mythwake.Backend.Definitions.ETag";
    private const string DefinitionsContentHashCacheKey = "Mythwake.Backend.Definitions.ContentHash";

    public string BaseUrl
    {
        get => string.IsNullOrWhiteSpace(baseUrl) ? DefaultBackendBaseUrl : baseUrl.TrimEnd('/');
        set => baseUrl = string.IsNullOrWhiteSpace(value) ? DefaultBackendBaseUrl : value.TrimEnd('/');
    }

    public string SessionToken
    {
        get
        {
            if (cachedSessionToken == null)
            {
                cachedSessionToken = PlayerPrefs.GetString(SessionTokenCacheKey, string.Empty);
            }

            return cachedSessionToken;
        }
    }

    public string PlayerId
    {
        get
        {
            if (cachedPlayerId == null)
            {
                cachedPlayerId = PlayerPrefs.GetString(PlayerIdCacheKey, string.Empty);
            }

            return cachedPlayerId;
        }
    }

    public bool HasSession => !string.IsNullOrWhiteSpace(SessionToken);
    public bool HasServerClock => hasServerClock;

    public void ClearSession()
    {
        cachedSessionToken = string.Empty;
        cachedPlayerId = string.Empty;
        PlayerPrefs.DeleteKey(SessionTokenCacheKey);
        PlayerPrefs.DeleteKey(PlayerIdCacheKey);
        PlayerPrefs.Save();
    }

    public IEnumerator GetHealth(Action<bool, string, MythwakeHealthDto> completed)
    {
        return SendJson(Get("/health"), completed);
    }

    public IEnumerator GetServerClock(Action<bool, string, MythwakeServerClockDto> completed)
    {
        return SendJson<MythwakeServerClockDto>(Get("/time"), (success, error, clock) =>
        {
            if (success)
            {
                StoreServerClock(clock);
            }

            completed?.Invoke(success, error, clock);
        });
    }

    public IEnumerator GuestAuth(Action<bool, string, MythwakeGuestAuthResponseDto> completed)
    {
        return SendJson<MythwakeGuestAuthResponseDto>(Post("/auth/guest"), (success, error, response) =>
        {
            if (success)
            {
                StoreSession(response);
            }

            completed?.Invoke(success, error, response);
        });
    }

    public IEnumerator GetPlayerSnapshot(Action<bool, string, MythwakePlayerSnapshotDto> completed)
    {
        return SendAuthenticatedJson(() => Get("/player/state"), completed);
    }

    public IEnumerator GetPlayerCoreState(Action<bool, string, MythwakePlayerStateDto> completed)
    {
        return SendAuthenticatedJson(() => Get("/player/core-state"), completed);
    }

    public IEnumerator FlushPlayerState(Action<bool, string> completed)
    {
        if (!HasSession)
        {
            completed?.Invoke(false, "No backend session.");
            yield break;
        }

        yield return SendAuthenticatedJson<BackendStatusResponseDto>(() => Post("/player/state/flush"), (success, error, _) =>
        {
            completed?.Invoke(success, error);
        });
    }

    public IEnumerator Logout(Action<bool, string> completed)
    {
        if (!HasSession)
        {
            ClearSession();
            completed?.Invoke(true, string.Empty);
            yield break;
        }

        var request = Post("/auth/logout");
        yield return SendJsonWithStatus<BackendStatusResponseDto>(request, (success, error, statusCode, _) =>
        {
            if (success || statusCode == httpStatusUnauthorized)
            {
                ClearSession();
            }

            completed?.Invoke(success || statusCode == httpStatusUnauthorized, success || statusCode == httpStatusUnauthorized ? string.Empty : error);
        });
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
        return SendAuthenticatedActionJson("campaign_fight", () => Post("/campaign/fight"), completed);
    }

    public IEnumerator RunDungeon(string dungeonId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"dungeon_run:{dungeonId}", () => Post($"/dungeons/{EscapePath(dungeonId)}/run"), completed);
    }

    public IEnumerator LevelHero(string heroId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"hero_level:{heroId}", () => Post($"/heroes/{EscapePath(heroId)}/level-up"), completed);
    }

    public IEnumerator AscendHero(string heroId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"hero_ascend:{heroId}", () => Post($"/heroes/{EscapePath(heroId)}/ascend"), completed);
    }

    public IEnumerator LevelEquipment(string equipmentId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"equipment_level:{equipmentId}", () => Post($"/equipment/{EscapePath(equipmentId)}/level-up"), completed);
    }

    public IEnumerator EquipAccessory(string accessoryId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"accessory_equip:{accessoryId}", () => PostAccessory("/gear/accessories/equip", accessoryId), completed);
    }

    public IEnumerator LevelAccessory(string accessoryId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"accessory_level:{accessoryId}", () => PostAccessory("/gear/accessories/level-up", accessoryId), completed);
    }

    public IEnumerator FuseAccessory(string accessoryId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"accessory_fuse:{accessoryId}", () => PostAccessory("/gear/accessories/fuse", accessoryId), completed);
    }

    public IEnumerator PullSummon(string bannerId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"summon_pull:{bannerId}", () => Post($"/summons/{EscapePath(bannerId)}/pull"), completed);
    }

    public IEnumerator ClaimDailyMission(string missionId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"daily_mission_claim:{missionId}", () => Post($"/missions/{EscapePath(missionId)}/claim"), completed);
    }

    public IEnumerator ClaimBattlePassReward(string rewardId, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedActionJson($"battle_pass_claim:{rewardId}", () => Post($"/battle-pass/{EscapePath(rewardId)}/claim"), completed);
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
        return SendJsonWithStatus<T>(request, (success, error, statusCode, data) => completed?.Invoke(success, error, data));
    }

    private IEnumerator SendJsonWithStatus<T>(UnityWebRequest request, Action<bool, string, long, T> completed)
    {
        request.timeout = Mathf.Max(1, requestTimeoutSeconds);

        using (request)
        {
            yield return request.SendWebRequest();

            var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(false, BuildErrorMessage(request, body), request.responseCode, default(T));
                yield break;
            }

            try
            {
                var data = JsonUtility.FromJson<T>(body);
                completed?.Invoke(true, string.Empty, request.responseCode, data);
            }
            catch (ArgumentException exception)
            {
                completed?.Invoke(false, $"Invalid backend JSON: {exception.Message}", request.responseCode, default(T));
            }
        }
    }

    private IEnumerator SendAuthenticatedJson<T>(Func<UnityWebRequest> createRequest, Action<bool, string, T> completed)
    {
        var loginSuccess = true;
        var loginError = string.Empty;
        if (!HasSession)
        {
            loginSuccess = false;
            yield return GuestAuth((success, error, _) =>
            {
                loginSuccess = success;
                loginError = error;
            });
        }

        if (!loginSuccess)
        {
            completed?.Invoke(false, $"Backend login failed: {loginError}", default(T));
            yield break;
        }

        var requestSuccess = false;
        var requestError = string.Empty;
        var responseCode = 0L;
        var responseData = default(T);

        yield return SendJsonWithStatus<T>(createRequest(), (success, error, statusCode, data) =>
        {
            requestSuccess = success;
            requestError = error;
            responseCode = statusCode;
            responseData = data;
        });

        if (requestSuccess || responseCode != httpStatusUnauthorized)
        {
            completed?.Invoke(requestSuccess, requestError, responseData);
            yield break;
        }

        ClearSession();
        loginSuccess = false;
        loginError = string.Empty;
        yield return GuestAuth((success, error, _) =>
        {
            loginSuccess = success;
            loginError = error;
        });

        if (!loginSuccess)
        {
            completed?.Invoke(false, $"Backend session expired and re-login failed: {loginError}", default(T));
            yield break;
        }

        yield return SendJsonWithStatus<T>(createRequest(), (success, error, statusCode, data) =>
        {
            completed?.Invoke(success, error, data);
        });
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

    private IEnumerator SendAuthenticatedActionJson(string actionKey, Func<UnityWebRequest> createRequest, Action<bool, string, MythwakeActionResultDto> completed)
    {
        return SendAuthenticatedJson<MythwakeActionResultDto>(() =>
        {
            var request = createRequest();
            request.SetRequestHeader("Idempotency-Key", GetOrCreatePendingActionKey(actionKey));
            return request;
        }, (success, error, result) =>
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
        if (HasSession)
        {
            request.SetRequestHeader("Authorization", $"Bearer {SessionToken}");
        }
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
    private const long httpStatusUnauthorized = 401;

    private void StoreSession(MythwakeGuestAuthResponseDto response)
    {
        cachedSessionToken = response.sessionToken ?? string.Empty;
        cachedPlayerId = response.playerId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cachedSessionToken))
        {
            ClearSession();
            return;
        }

        PlayerPrefs.SetString(SessionTokenCacheKey, cachedSessionToken);
        if (!string.IsNullOrWhiteSpace(cachedPlayerId))
        {
            PlayerPrefs.SetString(PlayerIdCacheKey, cachedPlayerId);
        }
        PlayerPrefs.Save();
    }

    public bool TryGetApproximateServerUtc(out DateTime serverUtc)
    {
        if (!hasServerClock)
        {
            serverUtc = DateTime.MinValue;
            return false;
        }

        var elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - lastServerClockRealtime);
        serverUtc = lastServerClockUtc.AddSeconds(elapsed);
        return true;
    }

    public bool TryGetLastServerClock(out MythwakeServerClockDto clock)
    {
        clock = lastServerClock;
        return hasServerClock;
    }

    private void StoreServerClock(MythwakeServerClockDto clock)
    {
        if (!TryParseUtc(clock.serverTimeUtc, out var serverUtc))
        {
            hasServerClock = false;
            return;
        }

        lastServerClock = clock;
        lastServerClockUtc = serverUtc;
        lastServerClockRealtime = Time.realtimeSinceStartup;
        hasServerClock = true;
    }

    private static bool TryParseUtc(string value, out DateTime utc)
    {
        if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out utc))
        {
            utc = utc.ToUniversalTime();
            return true;
        }

        utc = DateTime.MinValue;
        return false;
    }

    [Serializable]
    private struct AccessoryRequestDto
    {
        public string accessoryId;
    }

    [Serializable]
    private struct BackendStatusResponseDto
    {
        public string status;
        public string playerId;
    }
}
