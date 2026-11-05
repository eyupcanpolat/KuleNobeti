using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace KuleNobeti
{
    [Serializable]
    public sealed class OllamaSettings
    {
        public string baseUrl = "http://127.0.0.1:11434";
        public string model = "qwen2.5:3b";
        public int timeoutSeconds = 45;

        public static OllamaSettings Load()
        {
            var result = new OllamaSettings();
            string file = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ollama.local.json");
            try
            {
                if (File.Exists(file))
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(file), result);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Ollama ayarı okunamadı: " + e.Message);
                return new OllamaSettings();
            }
            result.timeoutSeconds = Mathf.Clamp(result.timeoutSeconds, 5, 180);
            return result;
        }

        public bool TryEndpoint(out string url)
        {
            url = null;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https") ||
                uri.AbsolutePath != "/" || uri.UserInfo.Length != 0 ||
                uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                string.IsNullOrWhiteSpace(model) ||
                model.EndsWith("-cloud", StringComparison.OrdinalIgnoreCase) ||
                model.EndsWith(":cloud", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!uri.IsLoopback)
            {
                if (!IPAddress.TryParse(uri.DnsSafeHost, out IPAddress address)) return false;
                byte[] b = address.GetAddressBytes();
                if (b.Length != 4 ||
                    !(b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                      (b[0] == 192 && b[1] == 168)))
                    return false;
            }
            url = uri.GetLeftPart(UriPartial.Authority) + "/api/chat";
            return true;
        }
    }

    public sealed class OllamaClient : IDialogueClient
    {
        private readonly OllamaSettings settings;
        private readonly JObject schema;

        public OllamaClient(OllamaSettings settings, string schemaText)
        {
            this.settings = settings;
            schema = JObject.Parse(schemaText);
        }

        public IEnumerator Generate(IReadOnlyList<ChatMessage> messages, Action<DialogueResult> finished)
        {
            if (!settings.TryEndpoint(out string endpoint))
            {
                finished(DialogueResult.Error("Ollama ayarı geçersiz. Yerel adresi ve model adını kontrol et."));
                yield break;
            }

            var body = new JObject
            {
                ["model"] = settings.model,
                ["messages"] = JArray.FromObject(messages),
                ["stream"] = false,
                ["format"] = schema.DeepClone(),
                ["options"] = new JObject { ["temperature"] = 0, ["num_predict"] = 180, ["num_ctx"] = 2048 }
            };
            byte[] bytes = Encoding.UTF8.GetBytes(body.ToString(Formatting.None));
            using (var request = new UnityWebRequest(endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = settings.timeoutSeconds;
                request.redirectLimit = 0;

                double start = Time.realtimeSinceStartupAsDouble;
                UnityWebRequestAsyncOperation pending = null;
                string startupError = null;
                try { pending = request.SendWebRequest(); }
                catch (Exception e) { startupError = e.Message; }
                if (startupError != null)
                {
                    Debug.LogWarning("Ollama isteği başlatılamadı: " + startupError);
                    finished(DialogueResult.Error("Ollama bağlantısı başlatılamadı. Ayarları kontrol et."));
                    yield break;
                }

                while (!pending.isDone)
                {
                    if (Time.realtimeSinceStartupAsDouble - start > settings.timeoutSeconds)
                    {
                        request.Abort();
                        finished(DialogueResult.Error("Ollama zamanında yanıt vermedi. Yeniden deneyebilirsin."));
                        yield break;
                    }
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Ollama: HTTP {request.responseCode} ({request.error})");
                    if (request.responseCode == 404)
                        finished(DialogueResult.Error($"Model bulunamadı: {settings.model}. Önce modeli indir."));
                    else if (Time.realtimeSinceStartupAsDouble - start >= settings.timeoutSeconds - .25 ||
                             (request.error ?? "").IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
                        finished(DialogueResult.Error("Ollama zamanında yanıt vermedi. Yeniden deneyebilirsin."));
                    else
                        finished(DialogueResult.Error("Ollama'ya erişilemedi veya sunucu hata verdi. Sunucuyu kontrol et."));
                    yield break;
                }
                finished(DialogueProtocol.Parse(request.downloadHandler.text));
            }
        }
    }
}
