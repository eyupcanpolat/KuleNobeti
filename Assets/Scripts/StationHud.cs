using UnityEngine;

namespace KuleNobeti
{
    public sealed class StationHud : MonoBehaviour
    {
        private static readonly Color Cream = new Color(.88f, .86f, .76f);
        private static readonly Color Muted = new Color(.61f, .63f, .59f);
        private static readonly Color Yellow = new Color(.87f, .70f, .29f);
        private static readonly Color Green = new Color(.49f, .74f, .49f);
        private static readonly Color Amber = new Color(.92f, .56f, .24f);
        private static readonly Color Charcoal = new Color(.085f, .095f, .09f);
        private StationWorld world;
        private DialogueController dialogue;
        private string model, input = "", notice = "", displayedReply = "", displayedInput = "";
        private float noticeUntil;
        private bool chatOpen, focus, diagnostics;
        private Vector2 chatScroll, playerMessageScroll;
        private GUIStyle title, chatTitle, text, small, heading, centered, centeredSmall, inputStyle, button;

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
                    return;
                }
                ShowNotice("Nöbetçi Sera ile konuşmak için arşiv kapısına yaklaş.");
            }
            if (Input.GetKeyDown(KeyCode.Q) && !world.PhysicalGate.IsOpen &&
                world.Player.transform.position.z < .6f)
                world.Companion.Order();

            if (Input.GetKeyDown(KeyCode.F) && world.Tasks != null)
            {
                bool ventilationWasOn = world.Tasks.VentilationOn;
                if (world.Tasks.TryInteract())
                    ShowNotice(!ventilationWasOn && world.Tasks.VentilationOn
                        ? "Havalandırma devrede. Yan koridor artık güvenli; yavaşlama kalktı."
                        : "Telsiz devrede. Çıkış yönündeki kılavuz ışıkları yandı.");
            }

            if (Input.GetKeyDown(KeyCode.F4)) diagnostics = !diagnostics;
            for (int i = 0; i < 3; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.F1 + i)) &&
                    world.Companion.Relocate(StationWorld.TestStarts[i]))
                    ShowNotice("K-07 test başlangıcı: " + (i + 1) + "  ·  Q: şalter emri");
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
            title = new GUIStyle(GUI.skin.label)
                { fontSize = 23, fontStyle = FontStyle.Bold, richText = false };
            chatTitle = new GUIStyle(title) { fontSize = 18 };
            text = new GUIStyle(GUI.skin.label)
                { fontSize = 16, wordWrap = true, richText = false };
            small = new GUIStyle(text) { fontSize = 13 };
            heading = new GUIStyle(small) { fontStyle = FontStyle.Bold };
            centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            centeredSmall = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            inputStyle = new GUIStyle(GUI.skin.textField)
                { fontSize = 17, padding = new RectOffset(9, 9, 7, 7) };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Panel(Rect rect, Color accent)
        {
            Fill(rect, new Color(Charcoal.r, Charcoal.g, Charcoal.b, .96f));
            Fill(new Rect(rect.x, rect.y, 3, rect.height), accent);
            Fill(new Rect(rect.x + 3, rect.y, rect.width - 3, 1), new Color(.36f, .38f, .32f));
        }

        private static void Label(Rect rect, string message, GUIStyle style, Color color)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(rect, message, style);
            GUI.contentColor = previous;
        }

        private void OnGUI()
        {
            if (world == null) return;
            Styles();
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 1180f, Screen.height / 700f));
            float width = Screen.width / scale, height = Screen.height / scale;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            DrawStatus(width, height);
            if (chatOpen && !world.Player.Finished) DrawChat(width, height);
            if (world.Player.Finished) DrawFinish(width, height);
            GUI.matrix = old;
        }

        private void DrawStatus(float width, float height)
        {
            Panel(new Rect(20, 20, 398, 140), Yellow);
            Label(new Rect(37, 27, 361, 32), "KULE NÖBETİ", title, Cream);
            Label(new Rect(38, 59, 360, 19), "TESİS 07   /   GECE VARDİYASI   /   ALARM", heading, Yellow);
            string objective = !world.PhysicalGate.IsOpen ? "01  Q: K-07'yi servis geçidindeki şaltere gönder." :
                !world.ArchiveGate.IsOpen ? "02  Arşiv nöbetçisi Sera'ya yaklaş. E ile konuş." :
                "03  Arşivden geç. Yeşil işaretleri izleyerek çıkışa ulaş.";
            Label(new Rect(38, 83, 361, 43), objective, text, Cream);
            var companion = world.Companion;
            Label(new Rect(38, 130, 361, 22), "K-07  /  " + companion.Status, small,
                world.PhysicalGate.IsOpen ? Green : Muted);

            DrawTasks(new Rect(width - 310, 20, 290, 140), false);
            if (diagnostics)
            {
                Panel(new Rect(width - 310, 171, 290, 121), Muted);
                var sensor = companion.Sensor;
                string obstacle = float.IsInfinity(sensor.ObstacleDistance) ? "yok" :
                    $"{sensor.ObstacleDistance:0.00} m / {sensor.ObstacleAngle:+0;-0;0}°";
                Label(new Rect(width - 294, 181, 261, 105),
                    $"K-07 SENSÖRÜ  ·  {companion.Elapsed:0.00} sn\nDurum: {companion.State}\n" +
                    $"Konum: {sensor.Position.x:0.0}, {sensor.Position.z:0.0}\nEngel: {obstacle}\n" +
                    $"Şalter: {sensor.TargetDistance:0.00} m / {sensor.TargetAngle:0}°", small, Muted);
            }

            if (!chatOpen && !world.Player.Finished)
            {
                string hint = ContextHint();
                if (!string.IsNullOrEmpty(hint))
                {
                    float hintWidth = Mathf.Min(790, width - 80);
                    Rect rect = new Rect((width - hintWidth) / 2, height - 107, hintWidth, 51);
                    bool hazard = world.Tasks != null && world.Tasks.InHazard;
                    Panel(rect, hazard ? Amber : Yellow);
                    Label(new Rect(rect.x + 14, rect.y + 4, rect.width - 28, 44), hint, centered,
                        hazard ? Amber : Cream);
                }
            }

            if (!chatOpen)
            {
                Fill(new Rect(20, height - 43, width - 40, 27), new Color(Charcoal.r, Charcoal.g, Charcoal.b, .91f));
                Label(new Rect(29, height - 42, width - 58, 25),
                    "WASD / oklar: yürü     Q: K-07     E: konuş     F: panel     F1–F3: ajan testi     F4: sensör     F8: sıfırla",
                    centeredSmall, Muted);
            }
        }

        private string ContextHint()
        {
            if (Time.unscaledTime < noticeUntil) return notice;
            if (world.Tasks != null && !string.IsNullOrEmpty(world.Tasks.InteractionHint))
                return world.Tasks.InteractionHint;
            if (world.Tasks != null && world.Tasks.InHazard)
                return "BASINÇ KAÇAĞI  /  Hareket yavaşlıyor. Soldaki havalandırma paneline ulaş.";
            if (dialogue.CanSpeak)
                return dialogue.Busy ? $"Nöbetçi yanıt hazırlıyor… {dialogue.WaitingTime:0.0} sn  ·  E: konuşmaya dön"
                    : "E  /  Nöbetçi Sera ile konuş";
            if (!world.PhysicalGate.IsOpen && world.Player.transform.position.z < .6f)
                return "Q  /  K-07'ye şalter emri ver. Sarı servis geçidini izle.";
            return "";
        }

        private void DrawTasks(Rect rect, bool finished)
        {
            Panel(rect, finished ? Green : Muted);
            int count = world.Tasks != null ? world.Tasks.CompletedCount : 0;
            Label(new Rect(rect.x + 16, rect.y + 10, rect.width - 32, 22),
                $"İSTEĞE BAĞLI İŞLER   /   {count} / 2", heading, finished ? Green : Yellow);
            TaskRow(rect, 42, "Havalandırma", world.Tasks != null && world.Tasks.VentilationOn, finished);
            TaskRow(rect, 73, "Telsiz", world.Tasks != null && world.Tasks.RadioOn, finished);
            Label(new Rect(rect.x + 16, rect.y + 109, rect.width - 32, 23),
                finished ? "Tahliye için zorunlu değildi." : "Panellere yaklaş  ·  F: etkinleştir", small, Muted);
        }

        private void TaskRow(Rect rect, float offset, string label, bool complete, bool finished)
        {
            Label(new Rect(rect.x + 16, rect.y + offset, rect.width - 154, 25), label, text, Cream);
            Label(new Rect(rect.x + rect.width - 132, rect.y + offset + 2, 117, 24),
                complete ? "TAMAMLANDI" : finished ? "ATLANDI" : "BEKLİYOR", heading, complete ? Green : Muted);
        }

        private void DrawChat(float width, float height)
        {
            const float h = 234;
            float w = Mathf.Min(980, width - 40);
            float x = (width - w) / 2, y = height - h - 16;
            Panel(new Rect(x, y, w, h), Yellow);
            Label(new Rect(x + 16, y + 9, w - 270, 27), "NÖBETÇİ SERA  /  ARŞİV 02", chatTitle, Cream);
            Fill(new Rect(x + w - 242, y + 9, 226, 26), new Color(.045f, .055f, .05f));
            Label(new Rect(x + w - 233, y + 12, 210, 21),
                world.ArchiveGate.IsOpen ? "ARŞİV KAPISI: AÇIK" : "ARŞİV KAPISI: KİLİTLİ",
                heading, world.ArchiveGate.IsOpen ? Green : Yellow);
            string instruction = world.ArchiveGate.IsOpen
                ? "Geçiş onaylandı. Esc ile konuşmayı kapatıp arşivden ilerle."
                : "Acil durumu ve insanları nereye, nasıl tahliye edeceğini yaz.";
            Label(new Rect(x + 16, y + 37, w - 32, 21), instruction, small, Muted);

            string answer = dialogue.Busy ? $"Yanıt bekleniyor… {dialogue.WaitingTime:0.0} sn" :
                !string.IsNullOrEmpty(dialogue.Response)
                    ? (dialogue.HasError ? "Bağlantı: " : "") + dialogue.Response
                    : "Henüz yanıt yok.";
            float playerWidth = (w - 52) * .36f;
            float replyWidth = w - 52 - playerWidth;
            float replyX = x + 36 + playerWidth;
            Label(new Rect(x + 16, y + 60, playerWidth, 20), "SENİN MESAJIN", heading, Yellow);
            Label(new Rect(replyX, y + 60, replyWidth, 20), "SERA'NIN CEVABI", heading, Yellow);
            Fill(new Rect(x + 16, y + 81, playerWidth, 83), new Color(.11f, .12f, .11f));
            Fill(new Rect(replyX, y + 81, replyWidth, 83), new Color(.11f, .12f, .11f));
            if (displayedInput != dialogue.LastInput)
            {
                displayedInput = dialogue.LastInput;
                playerMessageScroll = Vector2.zero;
            }
            if (displayedReply != dialogue.Response)
            {
                displayedReply = dialogue.Response;
                chatScroll = Vector2.zero;
            }
            DrawChatMessage(new Rect(x + 22, y + 84, playerWidth - 12, 77),
                string.IsNullOrEmpty(dialogue.LastInput) ? "Henüz mesaj göndermedin." : dialogue.LastInput,
                ref playerMessageScroll, Cream);
            DrawChatMessage(new Rect(replyX + 6, y + 84, replyWidth - 12, 77), answer,
                ref chatScroll, dialogue.HasError || dialogue.Busy ? Amber : Cream);

            Event current = Event.current;
            bool enter = current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter);
            if (enter) current.Use();
            GUI.SetNextControlName("npc_input");
            input = GUI.TextField(new Rect(x + 16, y + 174, w - 152, 34), input, 320, inputStyle);
            if (focus)
            {
                GUI.FocusControl("npc_input");
                focus = false;
            }

            bool previous = GUI.enabled;
            GUI.enabled = !dialogue.Busy && !world.ArchiveGate.IsOpen && !string.IsNullOrWhiteSpace(input);
            bool click = GUI.Button(new Rect(x + w - 124, y + 174, 108, 34), "Gönder", button);
            if (GUI.enabled && (click || enter))
            {
                dialogue.Send(input);
                if (dialogue.Busy)
                {
                    input = "";
                    chatScroll.y = 0;
                }
                focus = true;
            }
            GUI.enabled = previous;
            Label(new Rect(x + 16, y + 212, 340, 19), "Enter: gönder     Esc: konuşmayı kapat", small, Cream);
            Label(new Rect(x + 370, y + 212, w - 386, 19),
                $"Yerel model: {model}" + (dialogue.Latency > 0 && !dialogue.Busy
                    ? $"   /   Yanıt süresi: {dialogue.Latency:0.00} sn" : ""), small, Muted);
        }

        private void DrawChatMessage(Rect viewport, string message, ref Vector2 scroll, Color color)
        {
            float contentWidth = viewport.width - 18;
            float contentHeight = Mathf.Max(viewport.height - 1,
                text.CalcHeight(new GUIContent(message), contentWidth) + 4);
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, contentWidth, contentHeight));
            Label(new Rect(0, 0, contentWidth, contentHeight), message, text, color);
            GUI.EndScrollView();
        }

        private void DrawFinish(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(.025f, .03f, .025f, .73f));
            const float w = 620, h = 324;
            float x = (width - w) / 2, y = (height - h) / 2;
            Panel(new Rect(x, y, w, h), Green);
            Label(new Rect(x + 30, y + 21, w - 60, 39), "TAHLİYE TAMAMLANDI", title, Cream);
            Label(new Rect(x + 31, y + 63, w - 62, 29), "Arşiv geçildi. Çıkışa güvenle ulaştın.", text, Green);
            DrawTasks(new Rect(x + 29, y + 104, w - 58, 140), true);
            Label(new Rect(x + 31, y + 266, w - 62, 35),
                "R: yeniden başlat     F8: tüm vardiyayı sıfırla", centered, Cream);
        }
    }
}
