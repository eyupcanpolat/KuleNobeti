using UnityEngine;

namespace KuleNobeti
{
    public sealed class StationHud : MonoBehaviour
    {
        private static readonly Color Mint = new Color(.36f, 1f, .83f);
        private static readonly Color Amber = new Color(1f, .75f, .36f);
        private StationWorld world;
        private DialogueController dialogue;
        private string model, input = "", notice = "";
        private float noticeUntil;
        private bool chatOpen, focus, diagnostics;
        private GUIStyle title, text, centered, inputStyle;

        public void Configure(StationWorld station, DialogueController npc, string modelName)
        {
            world = station; dialogue = npc; model = modelName;
        }

        private void Update()
        {
            if (world == null || world.Player.Finished) return;
            if (Input.GetKeyDown(KeyCode.Escape)) CloseChat();
            if (chatOpen) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (dialogue.CanSpeak)
                {
                    chatOpen = true;
                    focus = true;
                    world.Player.HasFocusForDialogue = true;
                }
                else
                    ShowNotice("Nöbetçiyle konuşmak için yanına git.");
            }
            if (Input.GetKeyDown(KeyCode.Q) && !world.PhysicalGate.IsOpen &&
                world.Player.transform.position.z < .6f)
                world.Companion.Order();

            if (Input.GetKeyDown(KeyCode.F4)) diagnostics = !diagnostics;
            for (int i = 0; i < 3; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.F1 + i)) &&
                    world.Companion.Relocate(StationWorld.TestStarts[i]))
                    ShowNotice("Ajan test başlangıcı: " + (i + 1) + " — şimdi Q");
        }

        private void CloseChat()
        {
            chatOpen = false;
            world.Player.HasFocusForDialogue = false;
        }

        private void ShowNotice(string message)
        {
            notice = message;
            noticeUntil = Time.unscaledTime + 4;
        }

        private void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = false };
            centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 17 };
        }

        private static void Panel(Rect rect, float alpha = .94f)
        {
            Color previous = GUI.color;
            GUI.color = new Color(.045f, .075f, .12f, alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Label(Rect rect, string message, GUIStyle style, Color color)
        {
            var previous = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(rect, message, style);
            GUI.contentColor = previous;
        }

        private void OnGUI()
        {
            if (world == null) return;
            Styles();
            float scale = Mathf.Min(Screen.width / 1180f, Screen.height / 700f);
            float width = Screen.width / scale, height = Screen.height / scale;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            DrawStatus(width, height);
            if (chatOpen) DrawChat(width, height);
            if (world.Player.Finished) DrawFinish(width, height);
            GUI.matrix = old;
        }

        private void DrawStatus(float width, float height)
        {
            Panel(new Rect(22, 22, 420, 124));
            Label(new Rect(38, 31, 380, 30), "KULE NÖBETİ  /  07", title, Mint);
            string objective = !world.PhysicalGate.IsOpen ? "01   Q: K-07'ye şalter emri ver." :
                !world.ArchiveGate.IsOpen ? "02   Arşiv nöbetçisine yaklaş, E ile konuş." :
                "03   Açık kapıdan yeşil çıkış alanına ilerle.";
            Label(new Rect(39, 70, 387, 28), objective, text, Color.white);
            var companion = world.Companion;
            Label(new Rect(39, 104, 387, 36), $"{companion.Status}   ·   {companion.Elapsed:0.00} sn",
                text, Amber);

            if (diagnostics)
            {
                Panel(new Rect(width - 329, 22, 307, 104));
                var sensor = companion.Sensor;
                string obstacle = float.IsInfinity(sensor.ObstacleDistance) ? "yok" : $"{sensor.ObstacleDistance:0.00} m";
                Label(new Rect(width - 315, 32, 283, 84),
                    $"Durum: {companion.State}\nEngel: {obstacle}  |  Açı: {sensor.TargetAngle:0}°\nŞalter: {sensor.TargetDistance:0.00} m",
                    text, Mint);
            }

            string help = Time.unscaledTime < noticeUntil ? notice :
                dialogue.Busy && !chatOpen ? $"Nöbetçi düşünüyor… {dialogue.WaitingTime:0.0} sn · E: konuşmaya dön" :
                "WASD / oklar: yürü    •    F1–F3: ajan test noktaları    •    F4: sensör    •    F8: sıfırla";
            Panel(new Rect(22, height - 54, width - 44, 35), .87f);
            Label(new Rect(30, height - 53, width - 60, 32), help, centered, Color.white);
        }

        private void DrawChat(float width, float height)
        {
            const float w = 648, h = 310;
            float x = (width - w) / 2, y = height - h - 67;
            Panel(new Rect(x, y, w, h));
            Label(new Rect(x + 22, y + 13, 470, 35), "NÖBETÇİ SERA  /  ARŞİV", title, Mint);
            Label(new Rect(x + 23, y + 56, w - 46, 42), "Telsiz notu: Tahliye gerekçeni ve insanları güvene nasıl alacağını anlat.", text, Amber);
            if (!string.IsNullOrEmpty(dialogue.LastInput))
                Label(new Rect(x + 23, y + 100, w - 46, 44), "Sen: " + dialogue.LastInput, text, Color.white);

            string answer = dialogue.Busy ? $"Düşünüyor… {dialogue.WaitingTime:0.0} sn" :
                !string.IsNullOrEmpty(dialogue.Response)
                    ? (dialogue.HasError ? "Bağlantı: " : "Sera: ") + dialogue.Response
                    : "Nöbetçiyle kendi sözlerinle konuş.";
            if (world.ArchiveGate.IsOpen) answer += "\nKapı açık. Esc ile dön ve çıkışa ilerle.";
            Label(new Rect(x + 23, y + 147, w - 46, 75), answer, text, dialogue.HasError ? Amber : Mint);
            Label(new Rect(x + 23, y + 277, w - 46, 28),
                $"Yerel model: {model}  ·  Esc: kapat" +
                (dialogue.Latency > 0 && !dialogue.Busy ? $"  ·  Son istek: {dialogue.Latency:0.00} sn" : ""),
                text, Color.white);

            Event current = Event.current;
            bool enter = current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter);
            if (enter) current.Use();

            GUI.SetNextControlName("npc_input");
            input = GUI.TextField(new Rect(x + 23, y + 229, w - 164, 35), input, 320, inputStyle);
            if (focus)
            {
                GUI.FocusControl("npc_input");
                focus = false;
            }

            bool previous = GUI.enabled;
            GUI.enabled = !dialogue.Busy && !world.ArchiveGate.IsOpen && !string.IsNullOrWhiteSpace(input);
            bool click = GUI.Button(new Rect(x + w - 127, y + 229, 104, 35), "Gönder");
            if (GUI.enabled && (click || enter))
            {
                dialogue.Send(input);
                if (dialogue.Busy) input = "";
                focus = true;
            }
            GUI.enabled = previous;
        }

        private void DrawFinish(float width, float height)
        {
            float x = (width - 620) / 2, y = (height - 180) / 2;
            Panel(new Rect(x, y, 620, 180));
            Label(new Rect(x + 38, y + 30, 544, 42), "TAHLİYE TAMAMLANDI", title, Mint);
            string reply = dialogue.HasError || string.IsNullOrEmpty(dialogue.Response)
                ? "Kule güvenli.  •  R: yeniden başlat"
                : "Sera: " + dialogue.Response + "\nR: yeniden başlat";
            Label(new Rect(x + 38, y + 79, 544, 88), reply, text, Color.white);
        }
    }
}
