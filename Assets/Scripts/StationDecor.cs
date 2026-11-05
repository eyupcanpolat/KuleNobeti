using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuleNobeti
{
    // Presentation only: no collider, navigation source, trigger or sensor target is created here.
    public sealed class StationDecor : MonoBehaviour
    {
        private static readonly Color Metal = new Color(.115f, .14f, .135f);
        private static readonly Color Cream = new Color(.68f, .66f, .54f);
        private static readonly Color Paper = new Color(.80f, .77f, .64f);
        private static readonly Color Yellow = new Color(.77f, .58f, .20f);
        private static readonly Color Red = new Color(.70f, .22f, .13f);
        private static readonly Color Green = new Color(.28f, .66f, .37f);
        private static readonly Color DarkGreen = new Color(.13f, .23f, .17f);
        private static readonly Color Steel = new Color(.40f, .43f, .40f);
        private readonly List<Renderer> exitLights = new List<Renderer>();
        private readonly List<Light> exitBeacons = new List<Light>();
        private MaterialPropertyBlock tint;
        private Material surface;
        private Transform decor;
        private StationTasks tasks;
        private Door serviceGate, archiveGate;
        private Renderer serviceLock, archiveLock, ventLamp, radioLamp;
        private TextMesh serviceStatus, archiveStatus, ventStatus, radioStatus;
        private GameObject hazard;
        private Transform fan, pressureNeedle;
        private bool lastVent, lastRadio, lastService, lastArchive, hasState;

        public void Configure(Transform parent, Material material, Door first, Door archive, StationTasks taskState)
        {
            tint = new MaterialPropertyBlock();
            surface = material;
            serviceGate = first;
            archiveGate = archive;
            tasks = taskState;
            decor = new GameObject("Tesis görsel donanımı — çarpışmasız").transform;
            decor.SetParent(parent, false);
            BuildServiceCorridor();
            BuildDoor(serviceGate, 0, "01  SERVİS", out serviceLock, out serviceStatus);
            BuildDoor(archiveGate, 12, "02  ARŞİV", out archiveLock, out archiveStatus);
            BuildVentilation();
            BuildRadio();
            BuildArchive();
            BuildExit();
            Refresh();
        }

        private void BuildServiceCorridor()
        {
            // A sparse transport lane leads to the first door; side details have distinct jobs.
            for (float z = -12; z < -.8f; z += 2.1f)
                Box("Soluk rota boyası", new Vector3(0, .018f, z), new Vector3(.09f, .025f, .72f), Cream);
            Hatch(new Vector3(3.65f, .035f, -9.3f), 1.9f, 2.1f);
            Hatch(new Vector3(-1.7f, .035f, -5.7f), 1.30f, 1.55f);
            for (int i = 0; i < 14; i++)
            {
                float z = -11.5f + i * .62f;
                for (int track = 0; track < 2; track++)
                {
                    var tread = Box("Servis arabası tekerlek izi", new Vector3(3.1f + track * 1.05f, .014f, z),
                        new Vector3(.20f, .018f, .29f), new Color(.15f, .165f, .155f));
                    tread.transform.localRotation = Quaternion.Euler(0, -18, 0);
                }
            }
            FloorText("SERVİS / 01", new Vector3(.10f, .04f, -3.0f), .083f, Cream);
            Box("Tünel batı güvenlik çizgisi", new Vector3(-7.30f, .027f, 0), new Vector3(.09f, .03f, 9.5f), Yellow);
            Box("Tünel doğu güvenlik çizgisi", new Vector3(-5.07f, .027f, 0), new Vector3(.09f, .03f, 9.5f), Yellow);
            foreach (float z in new[] { -4.7f, 4.7f })
            {
                Box("Alçak geçit siyah koruyucu", new Vector3(-6.18f, 1.50f, z - .263f),
                    new Vector3(3.40f, 1.22f, .035f), Metal);
                for (int i = 0; i < 10; i++)
                {
                    var stripe = Box("Aşınmış tünel güvenlik şeridi", new Vector3(-7.68f + i * .33f, 1.51f, z - .29f),
                        new Vector3(.14f, .91f, .027f), Yellow);
                    stripe.transform.localRotation = Quaternion.Euler(0, 0, -21);
                }
                Box("Alçak geçit üst güvenlik şeridi", new Vector3(-6.18f, 2.233f, z),
                    new Vector3(3.45f, .035f, .40f), Yellow);
                Box("Üst güvenlik boyasında aşınma", new Vector3(-6.85f, 2.255f, z - .04f),
                    new Vector3(.19f, .012f, .13f), Steel);
                Box("Üst güvenlik boyasında küçük aşınma", new Vector3(-5.46f, 2.255f, z + .08f),
                    new Vector3(.11f, .012f, .07f), Steel);
            }
            Sign("K-07 SERVİS GEÇİDİ", new Vector3(-6.18f, 2.48f, -4.82f), 3.2f, .45f, Paper, .062f);
            FloorText("YALNIZ K-07", new Vector3(-6.18f, .043f, -6.15f), .066f, Yellow);
            Arrow(new Vector3(-6.18f, .036f, -7.45f), Yellow, .65f);

            // Switch body remains the original raycast target. These parts cannot intercept it.
            Box("Şalter taban plakası", new Vector3(-2.65f, .022f, 2.60f), new Vector3(1.15f, .025f, 1.02f), Metal);
            Box("Şalter kolu", new Vector3(-2.65f, 1.19f, 2.58f), new Vector3(.10f, .39f, .10f), Steel);
            Box("Şalter tutamağı", new Vector3(-2.65f, 1.36f, 2.58f), new Vector3(.42f, .13f, .16f), Yellow);
            Sign("KAPI ŞALTERİ", new Vector3(-2.65f, 1.92f, 2.56f), 1.80f, .38f, Paper, .062f);
            Cable("Şalter kapı kablosu", new Vector3(-2.65f, .06f, 2.2f), new Vector3(-2.65f, .06f, .7f), .065f, Metal);

            // Wall seams and two useful service fixtures, rather than a repeated prop grid.
            for (int i = 0; i < 4; i++)
                Box("Krem duvar servis paneli", new Vector3(7.575f, 1.35f, -10.5f + i * 2.6f),
                    new Vector3(.035f, 1.5f, 2.10f), Cream);
            Sign("KULE NÖBETİ\nGECE VARDİYASI", new Vector3(4.9f, 2.28f, -12.9f), 3.4f, .86f, Paper, .084f);
            Box("Yangın dolabı", new Vector3(7.25f, .90f, -3.6f), new Vector3(.48f, 1.35f, .85f), Red);
            Box("Yangın dolabı kapağı", new Vector3(6.99f, .90f, -3.6f), new Vector3(.035f, 1.11f, .64f), Cream);
            Alarm(new Vector3(6.9f, 2.55f, -.1f));
            Alarm(new Vector3(-3.75f, 2.55f, 11.9f));
        }

        private void Hatch(Vector3 position, float width, float length)
        {
            Box("Bakım kapağı çerçevesi", position, new Vector3(width + .15f, .04f, length + .15f), Metal);
            Box("Bakım kapağı", position + Vector3.up * .024f, new Vector3(width, .035f, length), Steel);
            for (int i = 0; i < 4; i++)
                Box("Kapak yivi", position + new Vector3(0, .045f, -length * .32f + i * length * .21f),
                    new Vector3(width * .8f, .012f, .032f), Metal);
            Box("Gömülü bakım tutamağı", position + new Vector3(0, .055f, length * .35f),
                new Vector3(.32f, .018f, .105f), Metal);
        }

        private void BuildDoor(Door gate, float z, string label, out Renderer lamp, out TextMesh status)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Kapı taşıyıcı ray", new Vector3(side * 1.58f, 1.75f, z - .12f), new Vector3(.22f, 3.5f, .88f), Metal);
                Box("Ray metal kılavuzu", new Vector3(side * 1.58f, 1.73f, z - .58f), new Vector3(.07f, 3.34f, .045f), Steel);
                for (int i = 0; i < 5; i++)
                    Box("Ray bağlantısı", new Vector3(side * 1.58f, .25f + .64f * i, z - .62f),
                        new Vector3(.15f, .095f, .05f), Cream);
            }
            Box("Kapı üst motor kutusu", new Vector3(0, 3.42f, z), new Vector3(3.52f, .36f, .97f), Metal);
            var face = Box("Kapı kirli krem iç paneli", new Vector3(0, 1.32f, z - .39f),
                new Vector3(2.39f, 1.42f, .06f), Cream);
            face.transform.SetParent(gate.transform, true);
            var seam = Box("Kapı panel birleşimi", new Vector3(0, 1.34f, z - .434f), new Vector3(.055f, 1.35f, .025f), Metal);
            seam.transform.SetParent(gate.transform, true);
            var sill = Box("Kapı alt uyarı boyası", new Vector3(0, .26f, z - .40f), new Vector3(2.58f, .30f, .055f), Yellow);
            sill.transform.SetParent(gate.transform, true);
            for (int i = 0; i < 7; i++)
            {
                var stripe = Box("Kapı eşik çizgisi", new Vector3(-1.10f + i * .37f, .26f, z - .44f),
                    new Vector3(.12f, .29f, .028f), Metal);
                stripe.transform.localRotation = Quaternion.Euler(0, 0, -30);
                stripe.transform.SetParent(gate.transform, true);
            }
            var chip = Box("Kapı sarı boyasında aşınma", new Vector3(-.74f, .32f, z - .475f),
                new Vector3(.17f, .045f, .018f), Steel);
            chip.transform.SetParent(gate.transform, true);
            var smallChip = Box("Kapı sarı boyasında küçük aşınma", new Vector3(.68f, .18f, z - .475f),
                new Vector3(.12f, .04f, .018f), Steel);
            smallChip.transform.SetParent(gate.transform, true);
            Sign(label, new Vector3(0, 3.08f, z - .72f), 2.84f, .50f, Paper, .095f);
            lamp = Box("Kapı kilit göstergesi", new Vector3(2.08f, 1.95f, z - .53f), new Vector3(.33f, .33f, .12f), Yellow).GetComponent<Renderer>();
            status = Sign("KİLİTLİ", new Vector3(2.1f, 1.50f, z - .60f), 1.1f, .33f, Yellow, .058f);
            Arrow(new Vector3(0, .044f, z - 1.25f), Cream, .60f);
        }

        private void BuildVentilation()
        {
            Vector3 p = StationTasks.VentilationPosition;
            Console(p, "HAVALANDIRMA", out ventLamp, out ventStatus);
            Box("Havalandırma ana gövde", new Vector3(-7.05f, 1.02f, 7.15f), new Vector3(.82f, 1.8f, 1.70f), Steel);
            Box("Fan koyu açıklık", new Vector3(-7.05f, 1.11f, 6.28f), new Vector3(.72f, 1.29f, .065f), Metal);
            fan = Empty("Havalandırma pervanesi", new Vector3(-7.05f, 1.12f, 6.21f));
            for (int i = 0; i < 4; i++)
            {
                var blade = Box("Fan kanadı", new Vector3(.11f, .23f, 0), new Vector3(.24f, .48f, .04f), Cream, fan);
                blade.transform.localPosition = Quaternion.Euler(0, 0, i * 90) * blade.transform.localPosition;
                blade.transform.localRotation = Quaternion.Euler(0, 0, i * 90 - 22);
            }
            Ball("Fan göbeği", new Vector3(-7.05f, 1.12f, 6.15f), .19f, Steel);
            Cable("Havalandırma dikey boru", new Vector3(-7.12f, .4f, 8.0f), new Vector3(-7.12f, 2.45f, 8.0f), .22f, Steel);
            Cable("Havalandırma üst dirseği", new Vector3(-7.12f, 2.45f, 8.0f), new Vector3(-5.84f, 2.45f, 8.0f), .22f, Steel);
            Cable("Havalandırma basınç hattı", new Vector3(-5.84f, 2.45f, 8.0f), new Vector3(-5.84f, 1.5f, 8.0f), .22f, Steel);
            Box("Basınç borusu sarı bandı", new Vector3(-7.12f, 1.92f, 8.0f), new Vector3(.26f, .15f, .26f), Yellow);
            var gauge = Shape("Basınç göstergesi", PrimitiveType.Cylinder, new Vector3(-5.84f, 1.62f, 7.82f), new Vector3(.55f, .065f, .55f), Paper);
            gauge.transform.localRotation = Quaternion.Euler(90, 0, 0);
            pressureNeedle = Empty("Basınç ibresi", new Vector3(-5.84f, 1.62f, 7.745f));
            Box("Kırmızı basınç ibresi", new Vector3(0, .10f, 0), new Vector3(.035f, .23f, .025f), Red, pressureNeedle);
            Sign("BASINÇ", new Vector3(-5.84f, 2.10f, 7.70f), 1.15f, .31f, Cream, .059f);
            Box("Yan koridor sol sınırı", new Vector3(-7.37f, .032f, 7.8f), new Vector3(.075f, .03f, 5.6f), Yellow);
            Box("Yan koridor sağ sınırı", new Vector3(-4.22f, .032f, 7.8f), new Vector3(.075f, .03f, 5.6f), Yellow);
            FloorText("YAN KORİDOR", new Vector3(-5.8f, .05f, 10.35f), .069f, Cream);
            hazard = new GameObject("Havalandırılmamış tehlikeli yan koridor");
            hazard.transform.SetParent(decor, false);
            for (int i = 0; i < 5; i++)
            {
                var band = Box("Tehlike bölgesi zemin taraması", new Vector3(-5.80f, .023f, 5.15f + i * 1.05f),
                    new Vector3(2.65f, .018f, .14f), new Color(.46f, .25f, .13f));
                band.transform.SetParent(hazard.transform, true);
            }
            var warning = Sign("DUMAN / YAVAŞ", new Vector3(-5.80f, .42f, 5.05f), 2.2f, .38f, Yellow, .07f);
            warning.transform.parent.SetParent(hazard.transform, true);
        }

        private void BuildRadio()
        {
            Vector3 p = StationTasks.RadioPosition;
            Console(p, "TAHLİYE TELSİZİ", out radioLamp, out radioStatus);
            Box("Telsiz alıcı gövdesi", new Vector3(6.75f, 1.1f, 10.15f), new Vector3(1.05f, 1.6f, .73f), Cream);
            Box("Telsiz hoparlörü", new Vector3(6.75f, 1.27f, 9.76f), new Vector3(.76f, .65f, .055f), Metal);
            for (int i = 0; i < 5; i++)
                Box("Hoparlör ızgarası", new Vector3(6.75f, 1.03f + i * .12f, 9.718f), new Vector3(.62f, .035f, .03f), Steel);
            Cable("Telsiz anteni", new Vector3(6.76f, 1.9f, 10.15f), new Vector3(6.76f, 3.35f, 10.15f), .045f, Steel);
            Cable("Anten yatay dipol", new Vector3(6.25f, 2.85f, 10.15f), new Vector3(7.27f, 2.85f, 10.15f), .04f, Steel);
            Cable("Telsiz kablo hattı dikey", new Vector3(7.4f, .10f, 9.5f), new Vector3(7.4f, 2.4f, 9.5f), .06f, Metal);
            Cable("Telsiz kablo hattı kuzey", new Vector3(7.4f, 2.4f, 9.5f), new Vector3(7.4f, 2.4f, 16.2f), .06f, Metal);
            Cable("Telsiz kontrol kablosu", new Vector3(5.8f, .07f, 9.5f), new Vector3(7.4f, .07f, 9.5f), .06f, Metal);
        }

        private void Console(Vector3 position, string label, out Renderer lamp, out TextMesh status)
        {
            Box(label + " ayak", position + new Vector3(0, .38f, 0), new Vector3(.30f, .76f, .36f), Metal);
            Box(label + " taban", position + new Vector3(0, .04f, 0), new Vector3(.86f, .08f, .67f), Metal);
            var panel = Box(label + " krem konsol", position + new Vector3(0, .98f, 0), new Vector3(1.03f, .31f, .75f), Cream);
            panel.transform.localRotation = Quaternion.Euler(20, 0, 0);
            Box(label + " ekran", position + new Vector3(-.13f, 1.18f, -.035f), new Vector3(.43f, .035f, .34f), Metal);
            lamp = Ball(label + " durum lambası", position + new Vector3(.30f, 1.20f, -.04f), .16f, Yellow).GetComponent<Renderer>();
            Sign(label, position + new Vector3(0, 1.66f, .13f), 2.45f, .41f, Paper, .069f);
            status = Sign("F  /  AÇ", position + new Vector3(0, .60f, -.39f), 1.0f, .33f, Yellow, .065f);
            // An open interaction bracket reads from above without suggesting a solid barrier.
            Box("Panel etkileşim çizgisi", position + new Vector3(0, .035f, -.83f), new Vector3(1.22f, .028f, .07f), Yellow);
            foreach (int side in new[] { -1, 1 })
                Box("Panel etkileşim köşesi", position + new Vector3(side * .58f, .035f, -.65f), new Vector3(.07f, .028f, .40f), Yellow);
        }

        private void BuildArchive()
        {
            Shelf(new Vector3(-5.75f, 0, 14.65f), false);
            Shelf(new Vector3(5.75f, 0, 14.65f), true);
            // Files visible before the archive door give the checkpoint a separate silhouette.
            Box("Arşiv kontrol masası", new Vector3(3.13f, .72f, 10.46f), new Vector3(1.7f, .12f, .78f), Cream);
            foreach (int side in new[] { -1, 1 })
                Box("Masa ayağı", new Vector3(3.13f + side * .65f, .35f, 10.46f), new Vector3(.10f, .70f, .60f), Metal);
            for (int i = 0; i < 3; i++)
                Box("Nöbet kayıt dosyası", new Vector3(2.72f, .81f + i * .045f, 10.42f), new Vector3(.57f, .035f, .38f), i == 2 ? Yellow : Paper);
        }

        private void Shelf(Vector3 position, bool reverse)
        {
            for (int side = -1; side <= 1; side += 2)
                Box("Arşiv raf dikmesi", position + new Vector3(side * 1.18f, 1.10f, .25f), new Vector3(.10f, 2.2f, 1.1f), Metal);
            for (int level = 0; level < 3; level++)
            {
                float y = .20f + level * .67f;
                Box("Arşiv raf tablası", position + new Vector3(0, y, .25f), new Vector3(2.42f, .07f, 1.1f), Steel);
                for (int file = 0; file < 4; file++)
                {
                    var pos = position + new Vector3(-.86f + file * .57f, y + .24f, .25f);
                    Box("Arşiv dosya kutusu", pos, new Vector3(.47f, .43f, .78f), (file + level) % 3 == 0 ? Cream : new Color(.43f, .39f, .29f));
                    Box("Dosya kutusu etiketi", pos + new Vector3(0, .05f, -.40f), new Vector3(.25f, .12f, .018f), Paper);
                    Box("Dosya kutusu tutamağı", pos + new Vector3(0, -.08f, -.416f), new Vector3(.12f, .035f, .015f), Metal);
                }
            }
            Sign(reverse ? "KAYITLAR / B" : "KAYITLAR / A", position + new Vector3(0, 2.40f, -.23f), 2.4f, .36f, Paper, .075f);
        }

        private void BuildExit()
        {
            Box("Tahliye alanı mat zemini", new Vector3(0, .021f, 15.6f), new Vector3(3.9f, .025f, 2.35f), DarkGreen);
            foreach (int side in new[] { -1, 1 })
                Box("Tahliye alanı sınırı", new Vector3(side * 1.95f, .042f, 15.6f), new Vector3(.08f, .025f, 2.35f), Green);
            Arrow(new Vector3(0, .055f, 14.95f), Green, .9f);
            Sign("ÇIKIŞ  ↑", new Vector3(0, 2.76f, 17.35f), 2.6f, .68f, Paper, .125f, DarkGreen);
            // The radio activates the lamps that continue north, the actual finish direction.
            foreach (float z in new[] { 6.8f, 9.5f, 13.1f, 16.0f })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    Box("Tahliye ışığı muhafazası", new Vector3(side * 3.85f, .12f, z), new Vector3(.54f, .20f, .31f), Metal);
                    exitLights.Add(Box("Telsiz kontrollü yön lambası", new Vector3(side * 3.85f, .24f, z),
                        new Vector3(.42f, .045f, .20f), DarkGreen).GetComponent<Renderer>());
                    if (z == 9.5f || z == 13.1f)
                    {
                        var beacon = Empty("Telsiz kontrollü rota aydınlatması", new Vector3(side * 3.85f, .70f, z)).gameObject.AddComponent<Light>();
                        beacon.type = LightType.Point;
                        beacon.color = Green;
                        beacon.range = 2.25f;
                        beacon.intensity = .70f;
                        beacon.shadows = LightShadows.None;
                        exitBeacons.Add(beacon);
                    }
                }
            }
            Sign("ÇIKIŞ ↑", new Vector3(-2.65f, 2.15f, 13.0f), 1.5f, .42f, Green, .079f);
        }

        private void Alarm(Vector3 position)
        {
            Box("Alarm koruyucu", position, new Vector3(.55f, .24f, .48f), Metal);
            Ball("Vardiya alarm lambası", position + Vector3.up * .20f, .28f, Red);
            var alarmLight = Empty("Vardiya alarm aydınlatması", position + new Vector3(0, .20f, -.45f))
                .gameObject.AddComponent<Light>();
            alarmLight.type = LightType.Point;
            alarmLight.color = new Color(.90f, .27f, .13f);
            alarmLight.intensity = .70f;
            alarmLight.range = 3f;
            alarmLight.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (tasks == null) return;
            if (tasks.VentilationOn && fan != null) fan.Rotate(0, 0, 160f * Time.deltaTime, Space.Self);
            if (!hasState || lastVent != tasks.VentilationOn || lastRadio != tasks.RadioOn ||
                lastService != serviceGate.IsOpen || lastArchive != archiveGate.IsOpen) Refresh();
        }

        private void Refresh()
        {
            lastVent = tasks.VentilationOn;
            lastRadio = tasks.RadioOn;
            lastService = serviceGate.IsOpen;
            lastArchive = archiveGate.IsOpen;
            hasState = true;
            Paint(ventLamp, lastVent ? Green : Yellow);
            Paint(radioLamp, lastRadio ? Green : Yellow);
            Paint(serviceLock, lastService ? Green : Yellow);
            Paint(archiveLock, lastArchive ? Green : Yellow);
            ventStatus.text = lastVent ? "ÇALIŞIYOR" : "F  /  AÇ";
            radioStatus.text = lastRadio ? "HAT AÇIK" : "F  /  AÇ";
            serviceStatus.text = lastService ? "AÇIK ↑" : "KİLİTLİ";
            archiveStatus.text = lastArchive ? "AÇIK ↑" : "KİLİTLİ";
            ventStatus.color = lastVent ? Green : Yellow;
            radioStatus.color = lastRadio ? Green : Yellow;
            serviceStatus.color = lastService ? Green : Yellow;
            archiveStatus.color = lastArchive ? Green : Yellow;
            hazard.SetActive(!lastVent);
            pressureNeedle.localRotation = Quaternion.Euler(0, 0, lastVent ? -55 : 55);
            foreach (var lamp in exitLights) Paint(lamp, lastRadio ? Green : DarkGreen);
            foreach (var light in exitBeacons) light.enabled = lastRadio;
        }

        private Transform Empty(string label, Vector3 position)
        {
            var item = new GameObject(label).transform;
            item.SetParent(decor, false);
            item.localPosition = position;
            return item;
        }

        private GameObject Box(string label, Vector3 position, Vector3 scale, Color color, Transform parent = null)
            => Shape(label, PrimitiveType.Cube, position, scale, color, parent);

        private GameObject Ball(string label, Vector3 position, float size, Color color)
            => Shape(label, PrimitiveType.Sphere, position, Vector3.one * size, color);

        private GameObject Shape(string label, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent = null)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = label;
            item.transform.SetParent(parent != null ? parent : decor, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Collider>().enabled = false;
            var mesh = item.GetComponent<MeshRenderer>();
            if (surface != null) mesh.sharedMaterial = surface;
            if (scale.y < .09f) mesh.shadowCastingMode = ShadowCastingMode.Off;
            Paint(mesh, color);
            return item;
        }

        private void Paint(Renderer renderer, Color color)
        {
            tint.Clear();
            tint.SetColor("_Color", color);
            tint.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(tint);
        }

        private void Cable(string label, Vector3 from, Vector3 to, float radius, Color color)
        {
            var rod = Shape(label, PrimitiveType.Cylinder, (from + to) * .5f,
                new Vector3(radius, Vector3.Distance(from, to) * .5f, radius), color);
            rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }

        private void Arrow(Vector3 position, Color color, float size)
        {
            Box("Kuzeye yön oku gövdesi", position, new Vector3(.13f * size, .025f, .70f * size), color);
            foreach (int side in new[] { -1, 1 })
            {
                var wing = Box("Kuzeye yön oku ucu", position + new Vector3(side * .13f * size, 0, .25f * size),
                    new Vector3(.12f * size, .025f, .43f * size), color);
                wing.transform.localRotation = Quaternion.Euler(0, side * -42f, 0);
            }
        }

        private TextMesh Sign(string label, Vector3 position, float width, float height, Color color,
            float typeSize, Color? backing = null)
        {
            var group = Empty(label + " tabelası", position);
            group.localRotation = Quaternion.Euler(54, 0, 0);
            Box("Tabela plakası", Vector3.zero, new Vector3(width, height, .042f), backing ?? Metal, group);
            return Text(label, group, new Vector3(0, 0, -.032f), typeSize, color);
        }

        private void FloorText(string label, Vector3 position, float size, Color color)
        {
            var group = Empty(label + " zemin yazısı", position);
            group.localRotation = Quaternion.Euler(90, 0, 0);
            Text(label, group, Vector3.zero, size, color);
        }

        private static TextMesh Text(string label, Transform parent, Vector3 localPosition, float size, Color color)
        {
            var text = new GameObject(label).AddComponent<TextMesh>();
            text.transform.SetParent(parent, false);
            text.transform.localPosition = localPosition;
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = size;
            text.color = color;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                text.font = font;
                text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            return text;
        }
    }
}
