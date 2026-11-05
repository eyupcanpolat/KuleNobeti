using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuleNobeti
{
    // Clothing and equipment on the existing figures. This component owns no body,
    // collider, navigation or sensor geometry and never moves their original children.
    public sealed class StationCharacterVisuals : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private Transform playerVisuals, guardVisuals, robotVisuals;
        private Material palette, lampMaterial;
        private MaterialPropertyBlock tint;

        private static readonly Color WorkBlue = new Color(.22f, .32f, .35f);
        private static readonly Color Yellow = new Color(.91f, .72f, .27f);
        private static readonly Color Reflective = new Color(.93f, .91f, .66f);
        private static readonly Color Rubber = new Color(.075f, .095f, .10f);
        private static readonly Color Steel = new Color(.40f, .44f, .43f);
        private static readonly Color Uniform = new Color(.12f, .17f, .21f);
        private static readonly Color Card = new Color(.89f, .88f, .76f);

        public void Configure(Transform player, Transform guard, Transform robot, Material material)
        {
            palette = material;
            tint = new MaterialPropertyBlock();
            playerVisuals = Group(player, "Bakım teknisyeni / yalnız kıyafet ve ekipman");
            guardVisuals = Group(guard, "Sera / yalnız üniforma ve ekipman");
            robotVisuals = Group(robot, "K-07 / yalnız servis donanımı");
            DressTechnician(player);
            DressSera(guard);
            EquipRobot(robot);
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private void DressTechnician(Transform player)
        {
            Recolor(player, "Tulum", WorkBlue);
            Recolor(player, "Kask", Yellow);
            Recolor(player, "Vizör", new Color(.12f, .17f, .19f));
            // Replace only the old flat clothing patch with surface-following tape.
            // The existing torso, head and all original transforms remain intact.
            var oldPatch = player.Find("Göğüs reflektörü");
            if (oldPatch != null && oldPatch.TryGetComponent(out Renderer patch)) patch.enabled = false;
            Recolor(player, "Bakım çantası", new Color(.32f, .29f, .20f));

            // The original capsule is the complete body. These thin ribbons follow
            // its cylindrical section; no replacement torso or limbs are introduced.
            Ribbon("Tulum bel kemeri", playerVisuals, .70f, .052f, .313f, Rubber);
            Ribbon("Tulum çevresel reflektif şerit", playerVisuals, .955f, .064f, .317f, Reflective);
            foreach (float angle in new[] { -29f, 29f, 151f, 209f })
                CurvedStripe("Tulum omuz askısı / reflektif", playerVisuals, angle, 9.5f,
                    .82f, 1.095f, .319f, Reflective);
            Box("Kemer metal tokası", playerVisuals, new Vector3(0, .70f, .322f),
                new Vector3(.09f, .066f, .018f), Steel);

            // A shallow brim and moulded ribs turn the existing yellow sphere into
            // a recognisable hardhat without changing its position or original mesh.
            Cylinder("Baret güvenlik siperi", playerVisuals, new Vector3(0, 1.54f, .035f),
                .277f, .027f, Quaternion.identity, Yellow);
            foreach (float x in new[] { -.075f, 0f, .075f })
            {
                Vector3 previous = Vector3.zero;
                for (int i = 0; i <= 8; i++)
                {
                    float z = Mathf.Lerp(-.17f, .17f, i / 8f);
                    float y = 1.46f + Mathf.Sqrt(.232f * .232f - x * x - z * z);
                    var point = new Vector3(x, y, .035f + z);
                    if (i > 0) Pipe("Baret kabartma takviye", playerVisuals, previous, point, .008f, Yellow);
                    previous = point;
                }
            }
            Box("Baret ön tanıtma bandı", playerVisuals, new Vector3(0, 1.57f, .282f),
                new Vector3(.12f, .035f, .009f), Reflective);

            // Fittings wrap the existing small bag, rather than adding another pack.
            Box("Alet çantası üst deri kapağı", playerVisuals, new Vector3(0, 1.145f, -.390f),
                new Vector3(.39f, .085f, .036f), new Color(.41f, .36f, .23f));
            foreach (float x in new[] { -.125f, .125f })
            {
                Box("Çanta toka kayışı", playerVisuals, new Vector3(x, 1.01f, -.393f),
                    new Vector3(.032f, .21f, .015f), Rubber);
                Box("Çanta metal tokası", playerVisuals, new Vector3(x, 1.065f, -.408f),
                    new Vector3(.047f, .044f, .014f), Steel);
            }
            Pipe("Alet çantası taşıma sapı", playerVisuals, new Vector3(-.09f, 1.23f, -.30f),
                new Vector3(.09f, 1.23f, -.30f), .015f, Rubber);
            foreach (float x in new[] { -.09f, .09f })
                Pipe("Çanta sapı bağlantısı", playerVisuals, new Vector3(x, 1.175f, -.30f),
                    new Vector3(x, 1.23f, -.30f), .012f, Rubber);
            Box("Çanta reflektif yama", playerVisuals, new Vector3(0, .91f, -.390f),
                new Vector3(.21f, .045f, .015f), Reflective);
            // A screwdriver and spanner sit in the existing bag's side loops.
            Pipe("Tornavida sapı", playerVisuals, new Vector3(.211f, .96f, -.31f),
                new Vector3(.211f, 1.10f, -.31f), .023f, Yellow);
            Pipe("Tornavida çelik ucu", playerVisuals, new Vector3(.211f, 1.10f, -.31f),
                new Vector3(.211f, 1.20f, -.31f), .008f, Steel);
            Pipe("Çanta anahtar sapı", playerVisuals, new Vector3(-.213f, .92f, -.31f),
                new Vector3(-.213f, 1.15f, -.31f), .013f, Steel);
            Box("Çanta anahtar ağzı", playerVisuals, new Vector3(-.213f, 1.165f, -.31f),
                new Vector3(.052f, .048f, .018f), Steel);
        }

        private void DressSera(Transform guard)
        {
            Recolor(guard, "Uzun nöbetçi paltosu", Uniform);
            Recolor(guard, "Omuzluk", new Color(.20f, .25f, .28f));
            Recolor(guard, "Siperli nöbetçi şapkası", new Color(.09f, .12f, .15f));
            Recolor(guard, "Nöbetçi rozeti", new Color(.69f, .62f, .38f));
            Recolor(guard, "Sol çizme", Rubber);
            Recolor(guard, "Sağ çizme", Rubber);

            Box("Üniforma arka kemeri", guardVisuals, new Vector3(0, .79f, -.242f),
                new Vector3(.75f, .065f, .024f), Rubber);
            Box("Üniforma ön kemeri", guardVisuals, new Vector3(0, .79f, .242f),
                new Vector3(.75f, .065f, .024f), Rubber);
            foreach (float x in new[] { -.12f, .12f })
            {
                var collar = Box("Üniforma yaka kıvrımı", guardVisuals, new Vector3(x, 1.345f, .263f),
                    new Vector3(.12f, .25f, .023f), new Color(.25f, .30f, .32f));
                collar.transform.localRotation = Quaternion.Euler(0, 0, x < 0 ? 19 : -19);
            }
            foreach (float y in new[] { 1.13f, 1.00f, .88f })
                Cylinder("Üniforma düğmesi", guardVisuals, new Vector3(0, y, .247f), .014f, .012f,
                    Quaternion.Euler(90, 0, 0), Steel);

            // The shoulder set is deliberately on the camera-facing side, with a
            // short aerial and coiled lead that keep Sera distinct at play distance.
            Box("Sera omuz telsizi", guardVisuals, new Vector3(-.37f, 1.39f, -.294f),
                new Vector3(.15f, .235f, .105f), Rubber);
            Pipe("Omuz telsizi anteni", guardVisuals, new Vector3(-.415f, 1.50f, -.29f),
                new Vector3(-.43f, 1.73f, -.29f), .011f, Rubber);
            Box("Telsiz hoparlör ızgarası", guardVisuals, new Vector3(-.37f, 1.40f, -.352f),
                new Vector3(.104f, .085f, .012f), Steel);
            for (int i = 0; i < 3; i++)
                Box("Omuz telsizi ızgara yarığı", guardVisuals, new Vector3(-.37f, 1.375f + i * .022f, -.362f),
                    new Vector3(.081f, .006f, .008f), Rubber);
            Cylinder("Telsiz ses düğmesi", guardVisuals, new Vector3(-.33f, 1.52f, -.29f),
                .022f, .025f, Quaternion.identity, Rubber);
            Vector3 prior = new Vector3(-.37f, 1.27f, -.30f);
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                Vector3 next = new Vector3(-.37f + Mathf.Sin(t * Mathf.PI * 8) * .018f,
                    Mathf.Lerp(1.27f, .87f, t), -.264f);
                Pipe("Telsiz kıvrımlı omuz kablosu", guardVisuals, prior, next, .008f, Rubber);
                prior = next;
            }
            // One camera-facing ID, with text contained within the physical card.
            // A second font quad on the reverse would show mirrored through the figure.
            {
                const float side = -1;
                Box("Sera görünür yaka kartı", guardVisuals, new Vector3(.16f, 1.235f, side * .257f),
                    new Vector3(.17f, .215f, .018f), Card);
                Box("Yaka kartı klipsi", guardVisuals, new Vector3(.16f, 1.36f, side * .257f),
                    new Vector3(.043f, .051f, .020f), Steel);
                Box("Yaka kartı fotoğrafı", guardVisuals, new Vector3(.125f, 1.258f, side * .271f),
                    new Vector3(.047f, .061f, .006f), new Color(.25f, .36f, .39f));
                Label("SERA", guardVisuals, new Vector3(.17f, 1.18f, side * .279f),
                    Quaternion.identity, .024f, Uniform, .13f, .045f);
            }
            // A thin muted shoulder tab replaces high-visibility workwear on Sera.
            foreach (float x in new[] { -.36f, .36f })
                Box("Nöbetçi omuz şeridi", guardVisuals, new Vector3(x, 1.438f, 0),
                    new Vector3(.16f, .018f, .35f), new Color(.47f, .45f, .31f));
        }

        private void EquipRobot(Transform robot)
        {
            Recolor(robot, "Robot gövdesi", new Color(.67f, .67f, .55f));
            Recolor(robot, "Robot gözü", Rubber);
            Recolor(robot, "Sarı tanıma şeridi", Yellow);
            Recolor(robot, "Sol palet", Rubber);
            Recolor(robot, "Sağ palet", Rubber);

            // The original body, green status light, sensor origin and tracks keep
            // their exact transforms. All new hardware stays below 0.64 metres.
            Cylinder("K-07 çalışma farı gövdesi", robotVisuals, new Vector3(.22f, .485f, .365f),
                .086f, .10f, Quaternion.Euler(90, 0, 0), Rubber);
            Cylinder("K-07 çalışma farı çerçevesi", robotVisuals, new Vector3(.22f, .485f, .419f),
                .075f, .014f, Quaternion.Euler(90, 0, 0), Steel);
            var lens = Cylinder("K-07 çalışma farı camı", robotVisuals, new Vector3(.22f, .485f, .431f),
                .060f, .009f, Quaternion.Euler(90, 0, 0), new Color(.98f, .90f, .68f));
            if (palette != null)
            {
                lampMaterial = new Material(palette) { name = "K-07 yalnız çalışma farı camı" };
                lampMaterial.EnableKeyword("_EMISSION");
                lampMaterial.SetColor("_EmissionColor", new Color(.19f, .16f, .09f));
                lens.GetComponent<Renderer>().sharedMaterial = lampMaterial;
            }
            var lamp = new GameObject("K-07 sıcak çalışma farı").AddComponent<Light>();
            lamp.transform.SetParent(robotVisuals, false);
            lamp.transform.localPosition = new Vector3(.22f, .485f, .445f);
            lamp.transform.localRotation = Quaternion.LookRotation(new Vector3(0, -.18f, 1));
            lamp.type = LightType.Spot;
            lamp.color = new Color(1, .92f, .75f);
            lamp.intensity = 1.15f;
            lamp.range = 3.2f;
            lamp.spotAngle = 57;
            lamp.innerSpotAngle = 34;
            lamp.shadows = LightShadows.None;

            Box("K-07 servis etiketi plakası", robotVisuals, new Vector3(-.065f, .616f, -.13f),
                new Vector3(.31f, .015f, .18f), Rubber);
            Label("K-07", robotVisuals, new Vector3(-.065f, .627f, -.13f),
                Quaternion.Euler(90, 0, 0), .057f, Card, .26f, .10f);
            foreach (float x in new[] { -.38f, .38f })
                for (int i = 0; i < 6; i++)
                    Box("K-07 palet sırtı", robotVisuals, new Vector3(x, .298f, -.29f + i * .116f),
                        new Vector3(.12f, .024f, .044f), new Color(.17f, .19f, .19f));
        }

        private void Recolor(Transform root, string name, Color color)
        {
            var item = root.Find(name);
            if (item == null) return;
            var renderer = item.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.GetPropertyBlock(tint);
            tint.SetColor("_Color", color);
            tint.SetFloat("_Glossiness", .22f);
            renderer.SetPropertyBlock(tint);
            tint.Clear();
        }

        private GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Color color)
        {
            // Chamfer the face corners so equipment catches a small edge highlight.
            float w = size.x * .5f, h = size.y * .5f, d = size.z * .5f;
            float b = Mathf.Min(w, h) * .20f;
            Vector2[] outline = {
                new Vector2(-w + b, -h), new Vector2(w - b, -h), new Vector2(w, -h + b), new Vector2(w, h - b),
                new Vector2(w - b, h), new Vector2(-w + b, h), new Vector2(-w, h - b), new Vector2(-w, -h + b)
            };
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                int start = v.Count;
                for (int i = 0; i < 8; i++) v.Add(new Vector3(outline[i].x, outline[i].y, side == 0 ? d : -d));
                for (int i = 1; i < 7; i++)
                    t.AddRange(new[] { start, start + (side == 0 ? i : i + 1), start + (side == 0 ? i + 1 : i) });
            }
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8, start = v.Count;
                v.AddRange(new[] {
                    new Vector3(outline[i].x, outline[i].y, -d), new Vector3(outline[j].x, outline[j].y, -d),
                    new Vector3(outline[j].x, outline[j].y, d), new Vector3(outline[i].x, outline[i].y, d)
                });
                t.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            return MeshObject(name, parent, position, MakeMesh(name, v, t), color);
        }

        private GameObject Cylinder(string name, Transform parent, Vector3 position, float radius,
            float length, Quaternion rotation, Color color)
        {
            const int segments = 20;
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                v.Add(radial + Vector3.down * length * .5f);
                v.Add(radial + Vector3.up * length * .5f);
                if (i < segments)
                {
                    int k = i * 2;
                    t.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 });
                }
            }
            for (int side = 0; side < 2; side++)
            {
                int start = v.Count;
                float y = (side == 0 ? -.5f : .5f) * length;
                v.Add(new Vector3(0, y, 0));
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    v.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                    if (i < segments)
                        t.AddRange(new[] { start, start + i + (side == 0 ? 1 : 2), start + i + (side == 0 ? 2 : 1) });
                }
            }
            var item = MeshObject(name, parent, position, MakeMesh(name, v, t), color);
            item.transform.localRotation = rotation;
            return item;
        }

        private void Pipe(string name, Transform parent, Vector3 start, Vector3 end, float radius, Color color)
            => Cylinder(name, parent, (start + end) * .5f, radius, Vector3.Distance(start, end),
                Quaternion.FromToRotation(Vector3.up, end - start), color);

        private void Ribbon(string name, Transform parent, float y, float height, float radius, Color color)
            => CurvedStripe(name, parent, 180, 180, y - height * .5f, y + height * .5f, radius, color);

        private void CurvedStripe(string name, Transform parent, float angle, float halfWidth,
            float bottom, float top, float radius, Color color)
        {
            int segments = Mathf.Max(3, Mathf.CeilToInt(halfWidth / 8));
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(angle - halfWidth, angle + halfWidth, i / (float)segments) * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius);
                v.Add(offset + Vector3.up * bottom);
                v.Add(offset + Vector3.up * top);
                if (i < segments)
                {
                    int k = i * 2;
                    t.AddRange(new[] { k, k + 2, k + 3, k, k + 3, k + 1 });
                }
            }
            MeshObject(name, parent, Vector3.zero, MakeMesh(name, v, t), color);
        }

        private Mesh MakeMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            return mesh;
        }

        private GameObject MeshObject(string name, Transform parent, Vector3 position, Mesh mesh, Color color)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = palette;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            tint.SetColor("_Color", color);
            tint.SetFloat("_Glossiness", .24f);
            renderer.SetPropertyBlock(tint);
            tint.Clear();
            return item;
        }

        private static void Label(string value, Transform parent, Vector3 position, Quaternion rotation,
            float size, Color color, float maxWidth, float maxHeight)
        {
            var label = new GameObject(value + " / ekipman etiketi").AddComponent<TextMesh>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = rotation;
            label.text = value;
            label.fontSize = 40;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                label.font = font;
                label.GetComponent<Renderer>().sharedMaterial = font.material;
            }
            var renderer = label.GetComponent<Renderer>();
            // TextMesh characterSize depends on the font glyph metrics. Fit the actual
            // generated local bounds rather than assuming a nominal text size in metres.
            Vector3 measured = renderer.localBounds.size;
            float fit = Mathf.Min(1, maxWidth / Mathf.Max(.0001f, measured.x),
                maxHeight / Mathf.Max(.0001f, measured.y));
            label.characterSize *= fit;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void OnDestroy()
        {
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
            if (lampMaterial != null) Destroy(lampMaterial);
        }
    }
}
