using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Mythwake.Game
{
    /// <summary>Response of a server call: status code plus parsed JSON body (null on network failure).</summary>
    public sealed class ApiResponse
    {
        public long Status;
        public JObject Body;
        public string NetworkError;

        public bool Ok => Status >= 200 && Status < 300 && Body != null;
        public string Error => Body?.Value<string>("error") ?? NetworkError ?? $"http_{Status}";
    }

    /// <summary>Thin JSON-over-HTTP client for the Mythwake API (docs/SERVER.md). Coroutine based (UnityWebRequest).</summary>
    public sealed class ServerApi
    {
        private const string DeviceIdKey = "mythwake.deviceId";
        private const string DeviceSecretKey = "mythwake.deviceSecret";

        public string BaseUrl { get; }
        public string Token { get; private set; }
        public string AccountId { get; private set; }

        public ServerApi(string baseUrl) => BaseUrl = baseUrl.TrimEnd('/');

        public IEnumerator Send(string method, string path, JToken body, Action<ApiResponse> done, int timeoutSeconds = 15)
        {
            using (var req = new UnityWebRequest(BaseUrl + path, method))
            {
                if (body != null)
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None)));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(Token))
                    req.SetRequestHeader("Authorization", "Bearer " + Token);
                req.timeout = timeoutSeconds;
                yield return req.SendWebRequest();

                var response = new ApiResponse { Status = req.responseCode };
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.DataProcessingError)
                    response.NetworkError = req.error ?? "network";
                var text = req.downloadHandler?.text;
                if (!string.IsNullOrEmpty(text))
                {
                    try
                    {
                        response.Body = JObject.Parse(text);
                    }
                    catch (Exception)
                    {
                        response.NetworkError ??= "bad_response";
                    }
                }
                done(response);
            }
        }

        /// <summary>Device login. The device id and secret are created once and kept in PlayerPrefs.</summary>
        public IEnumerator Login(Action<ApiResponse> done)
        {
            var id = PlayerPrefs.GetString(DeviceIdKey, "");
            var secret = PlayerPrefs.GetString(DeviceSecretKey, "");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(secret))
            {
                id = "dev-" + Guid.NewGuid().ToString("N");
                secret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(DeviceIdKey, id);
                PlayerPrefs.SetString(DeviceSecretKey, secret);
                PlayerPrefs.Save();
            }
            ApiResponse result = null;
            yield return Send("POST", "/v1/auth/device", new JObject { ["deviceId"] = id, ["deviceSecret"] = secret }, r => result = r);
            if (result.Ok)
            {
                Token = result.Body.Value<string>("token");
                AccountId = result.Body.Value<string>("accountId");
            }
            done(result);
        }
    }
}
