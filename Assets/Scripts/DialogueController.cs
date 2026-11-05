using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KuleNobeti
{
    public sealed class DialogueController : MonoBehaviour
    {
        private readonly List<ChatMessage> history = new List<ChatMessage>();
        private IDialogueClient client;
        private string prompt;
        private Door physicalGate, archiveGate;
        private Transform player, npc;
        private double started;

        public bool Busy { get; private set; }
        public string Response { get; private set; } = "";
        public string LastInput { get; private set; } = "";
        public bool HasError { get; private set; }
        public double Latency { get; private set; }
        public double WaitingTime => Busy ? Time.realtimeSinceStartupAsDouble - started : 0;
        public bool CanSpeak => physicalGate.IsOpen && player.position.z > .6f &&
            Vector3.Distance(player.position, npc.position) < 3.4f && !archiveGate.IsOpen;

        public void Configure(IDialogueClient service, string systemPrompt, Door firstDoor, Door finalDoor,
            Transform hero, Transform guard)
        {
            client = service;
            prompt = systemPrompt;
            physicalGate = firstDoor;
            archiveGate = finalDoor;
            player = hero;
            npc = guard;
        }

        public void Send(string line)
        {
            if (Busy || !CanSpeak || string.IsNullOrWhiteSpace(line)) return;
            line = line.Trim();
            if (line.Length > 320) return;
            LastInput = line;
            HasError = false;
            Response = "";
            Busy = true;
            started = Time.realtimeSinceStartupAsDouble;
            bool eligible = EvacuationPolicy.CanConsiderOpening(line);
            string rule = eligible
                ? "\nOyun denetimi: Bu tur open_gate seçilebilir; kararını oyuncunun son mesajına göre ver."
                : "\nOyun denetimi: Bu tur kapı kilitli kalır. action yalnızca none. Rol içinde kal ve tahliye bilgisine dön.";
            var messages = new List<ChatMessage> { new ChatMessage("system", prompt + rule) };
            messages.AddRange(history);
            messages.Add(new ChatMessage("user", line));
            StartCoroutine(SendRoutine(messages, line, eligible));
        }

        private IEnumerator SendRoutine(List<ChatMessage> messages, string line, bool eligible)
        {
            yield return client.Generate(messages, eligible, result => Apply(result, line, eligible));
        }

        private void Apply(DialogueResult result, string line, bool eligible)
        {
            Busy = false;
            Latency = Time.realtimeSinceStartupAsDouble - started;
            Response = result.Text;
            HasError = !result.Success;
            if (!result.Success) return;

            if (result.Action == NpcAction.OpenGate && !EvacuationPolicy.AllowsOpening(eligible, result.Action))
            {
                Debug.LogWarning($"Arşiv kapısı: modelin açma isteği oyun kuralı tarafından reddedildi; {Latency:0.00} sn.");
                return;
            }

            history.Add(new ChatMessage("user", line));
            history.Add(new ChatMessage("assistant", result.RawContent));
            if (history.Count > 6) history.RemoveRange(0, 2);

            if (EvacuationPolicy.AllowsOpening(eligible, result.Action))
            {
                if (CanSpeak) archiveGate.Open();
                else Response += " Onay alındı; kapı için nöbetçinin yanına dönüp yeniden konuş.";
            }
            Debug.Log($"Diyalog sonucu: {result.Action}; kural uygun: {eligible}; kapı açık: {archiveGate.IsOpen}; {Latency:0.00} sn");
        }
    }
}
