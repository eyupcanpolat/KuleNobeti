using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KuleNobeti
{
    [Serializable]
    public sealed class ChatMessage
    {
        public string role;
        public string content;
        public ChatMessage(string role, string content) { this.role = role; this.content = content; }
    }

    public enum NpcAction { None, OpenGate }

    public sealed class DialogueResult
    {
        public bool Success { get; }
        public string Text { get; }
        public NpcAction Action { get; }
        public string RawContent { get; }

        private DialogueResult(bool success, string text, NpcAction action, string raw)
        {
            Success = success; Text = text; Action = action; RawContent = raw;
        }

        public static DialogueResult Error(string text) => new DialogueResult(false, text, NpcAction.None, null);
        public static DialogueResult Accept(string text, NpcAction action, string raw)
            => new DialogueResult(true, text, action, raw);
    }

    public interface IDialogueClient
    {
        IEnumerator Generate(IReadOnlyList<ChatMessage> messages, bool canConsiderOpening, Action<DialogueResult> finished);
    }

    public static class DialogueProtocol
    {
        public static DialogueResult Parse(string payload)
        {
            const string invalid = "Modelin yanıtı beklenen biçimde değil. Kapı kilitli; yeniden deneyebilirsin.";
            if (string.IsNullOrWhiteSpace(payload) || payload.Length > 65536) return DialogueResult.Error(invalid);
            try
            {
                var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
                var outer = JObject.Parse(payload, settings);
                if (outer["done"]?.Type != JTokenType.Boolean || !(bool)outer["done"] ||
                    !(outer["message"] is JObject message) || message["content"]?.Type != JTokenType.String ||
                    (string)outer["done_reason"] == "length")
                    return DialogueResult.Error(invalid);
                string raw = (string)message["content"];
                if (string.IsNullOrWhiteSpace(raw) || raw.Length > 4096) return DialogueResult.Error(invalid);
                var inner = JObject.Parse(raw, settings);
                if (inner.Count != 2 || inner["reply"]?.Type != JTokenType.String ||
                    inner["action"]?.Type != JTokenType.String)
                    return DialogueResult.Error(invalid);
                string reply = ((string)inner["reply"]).Trim();
                string action = (string)inner["action"];
                if (reply.Length == 0 || reply.Length > 450 || (action != "none" && action != "open_gate"))
                    return DialogueResult.Error(invalid);
                return DialogueResult.Accept(reply, action == "open_gate" ? NpcAction.OpenGate : NpcAction.None, raw);
            }
            catch (JsonException) { return DialogueResult.Error(invalid); }
            catch (ArgumentException) { return DialogueResult.Error(invalid); }
            catch (InvalidCastException) { return DialogueResult.Error(invalid); }
        }
    }
}
