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
            var messages = new List<ChatMessage> { new ChatMessage("system", prompt) };
            messages.AddRange(history);
            messages.Add(new ChatMessage("user", line));
            StartCoroutine(SendRoutine(messages, line));
        }

        private IEnumerator SendRoutine(List<ChatMessage> messages, string line)
        {
            yield return client.Generate(messages, result => Apply(result, line));
        }

        private void Apply(DialogueResult result, string line)
        {
            Busy = false;
            Latency = Time.realtimeSinceStartupAsDouble - started;
            Response = result.Text;
            HasError = !result.Success;
            if (!result.Success) return;

            history.Add(new ChatMessage("user", line));
            history.Add(new ChatMessage("assistant", result.RawContent));
            if (history.Count > 6) history.RemoveRange(0, 2);

            if (result.Action == NpcAction.OpenGate)
            {
                if (CanSpeak) archiveGate.Open();
                else Response += " Onay alındı; kapı için nöbetçinin yanına dönüp yeniden konuş.";
            }
            Debug.Log($"Diyalog sonucu: {result.Action}; {Latency:0.00} sn");
        }
    }
}
