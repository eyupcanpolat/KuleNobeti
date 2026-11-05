using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuleNobeti
{
    // A removable visual skin. The original solids remain the only physics and navigation geometry.
    public sealed class ServiceRoomVisual : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Dictionary<string, Mesh> rounds = new Dictionary<string, Mesh>();
        private Material concreteFloor, concreteWall, paintedMetal, darkMetal, safetyYellow, bareSteel;
        private Transform visuals;
        private Door gate;
        private Renderer indicator;
        private Light indicatorLight;
        private bool wasOpen;
        private MaterialPropertyBlock state;

        public void Configure(Transform stationRoot, Material fallback, Door firstGate, Transform lever)
        {
            state = new MaterialPropertyBlock();
            gate = firstGate;
            concreteFloor = Surface("FloorConcrete", fallback);
            concreteWall = Surface("WallConcrete", fallback);
            paintedMetal = Surface("PaintedMetal", fallback);
            darkMetal = Surface("DarkMetal", fallback);
            safetyYellow = Surface("SafetyYellow", fallback);
            bareSteel = Surface("BareSteel", fallback);
            visuals = new GameObject("01 Servis odası — yalnız görsel yüzeyler").transform;
            visuals.SetParent(stationRoot, false);
            BuildRoom();
            BuildGateSkin(firstGate.transform);
            BuildElectricalCabinet(lever);
            BuildWorkLights();
            RefreshIndicator();
        }

        private static Material Surface(string name, Material fallback)
            => Resources.Load<Material>("ServiceRoom/" + name) ?? fallback;

        private void BuildRoom()
        {
            // Three metres per texture repeat, matching the scanned concrete surfaces.
            Horizontal("Servis zemini / 3 m beton", -7.59f, 7.59f, -13.59f, -.40f, .007f, concreteFloor, 3);
            Horizontal("K-07 geçit zemini / 3 m beton", -7.59f, -4.76f, -.40f, 4.72f, .007f, concreteFloor, 3);

            WallX("Doğu duvarı", 7.594f, -13.59f, -.40f, Vector3.left);
            WallX("Batı duvarı", -7.594f, -13.59f, 4.72f, Vector3.right);
            WallZ("Güney duvarı", -13.594f, -7.59f, 7.59f, 2.5f, Vector3.forward);
            // Both faces of the existing tunnel separator keep their original silhouette and clearance.
            WallX("Geçit doğu yüzü", -4.244f, -4.724f, 4.724f, Vector3.right);
            WallX("Geçit batı yüzü", -4.756f, -4.724f, 4.724f, Vector3.left);
            WallZ("Kapı sol beton kanadı", -.386f, -4.24f, -1.35f, 2.3f, Vector3.back);
            WallZ("Kapı sağ beton kanadı", -.386f, 1.35f, 7.59f, 2.3f, Vector3.back);
            Horizontal("Doğu duvar üstü", 7.60f, 8.10f, -13.60f, -.40f, 2.506f, concreteWall, 3);
            Horizontal("Batı duvar üstü", -8.10f, -7.60f, -13.60f, 4.72f, 2.506f, concreteWall, 3);
            Horizontal("Güney duvar üstü", -8.10f, 8.10f, -14.10f, -13.60f, 2.506f, concreteWall, 3);
            Horizontal("Geçit duvar üstü", -4.75f, -4.25f, -4.724f, 4.724f, 2.506f, concreteWall, 3);
            Horizontal("Kapı sol duvar üstü", -4.50f, -1.35f, -.38f, .38f, 2.306f, concreteWall, 3);
            Horizontal("Kapı sağ duvar üstü", 1.35f, 7.80f, -.38f, .38f, 2.306f, concreteWall, 3);

            // A few real construction joints, rather than a repeated prop grid.
            foreach (float z in new[] { -10.1f, -5.9f })
            {
                Box("Doğu beton kalıp derzi", new Vector3(7.581f, 1.77f, z), new Vector3(.016f, 1.46f, .018f), darkMetal);
                Box("Doğu sac birleşim derzi", new Vector3(7.574f, .54f, z), new Vector3(.018f, .87f, .012f), darkMetal);
                foreach (float y in new[] { .24f, .82f })
                    Cylinder("Duvar sacı sabitleme başı", new Vector3(7.557f, y, z + .07f), .020f, .012f,
                        Quaternion.Euler(0, 0, 90), bareSteel);
            }
            foreach (float x in new[] { -3.7f, 3.7f })
                Box("Güney beton kalıp derzi", new Vector3(x, 1.77f, -13.580f), new Vector3(.018f, 1.46f, .015f), darkMetal);

            // One low service line stays on the wall and clear of the walking lane.
            Pipe("Doğu duvarı servis borusu", new Vector3(7.41f, .30f, -12.5f), new Vector3(7.41f, .30f, -5.0f), .055f, bareSteel);
            foreach (float z in new[] { -11.7f, -8.7f, -5.7f })
            {
                Cylinder("Boru kelepçe bandı", new Vector3(7.41f, .30f, z), .071f, .032f,
                    Quaternion.Euler(90, 0, 0), darkMetal);
                Box("Boru kelepçe duvar ayağı", new Vector3(7.505f, .30f, z), new Vector3(.11f, .13f, .07f), darkMetal);
            }

            // Replace the visual language of the existing storage solid without changing that solid.
            BevelBox("Servis ekipman kasası", new Vector3(-3.2f, .55f, -3.9f), new Vector3(1.17f, 1.10f, 1.32f), .06f, darkMetal);
            Box("Kasa üst servis kapağı", new Vector3(-3.2f, 1.112f, -3.9f), new Vector3(.94f, .026f, 1.06f), paintedMetal);
            foreach (float x in new[] { -3.59f, -2.81f })
            {
                Pipe("Kasa gömülü tutamak", new Vector3(x, 1.15f, -4.06f), new Vector3(x, 1.15f, -3.74f), .028f, bareSteel);
                foreach (float z in new[] { -4.08f, -3.72f })
                    Cylinder("Kasa kapak vidası", new Vector3(x, 1.132f, z), .025f, .018f, Quaternion.identity, bareSteel);
            }
            var originalCrate = visuals.parent.Find("Depo sandığı A");
            if (originalCrate != null)
            {
                var originalRenderer = originalCrate.GetComponent<Renderer>();
                if (originalRenderer != null) originalRenderer.enabled = false;
            }
        }

        private void WallX(string name, float x, float z0, float z1, Vector3 normal)
        {
            Quad(name + " / beton 3 m", new Vector3(x, 1, z0), new Vector3(x, 1, z1),
                new Vector3(x, 2.5f, z1), new Vector3(x, 2.5f, z0), normal, concreteWall, 3);
            float face = x + normal.x * .004f;
            Quad(name + " / boyalı alt sac 1 m", new Vector3(face, .11f, z0), new Vector3(face, .11f, z1),
                new Vector3(face, 1, z1), new Vector3(face, 1, z0), normal, paintedMetal, 1);
            Box(name + " / çelik süpürgelik", new Vector3(face + normal.x * .015f, .055f, (z0 + z1) * .5f),
                new Vector3(.035f, .11f, z1 - z0), darkMetal);
            Box(name + " / sac üst kıvrımı", new Vector3(face + normal.x * .008f, 1.01f, (z0 + z1) * .5f),
                new Vector3(.03f, .035f, z1 - z0), bareSteel);
        }

        private void WallZ(string name, float z, float x0, float x1, float height, Vector3 normal)
        {
            Quad(name + " / beton 3 m", new Vector3(x0, 1, z), new Vector3(x1, 1, z),
                new Vector3(x1, height, z), new Vector3(x0, height, z), normal, concreteWall, 3);
            float face = z + normal.z * .004f;
            Quad(name + " / boyalı alt sac 1 m", new Vector3(x0, .11f, face), new Vector3(x1, .11f, face),
                new Vector3(x1, 1, face), new Vector3(x0, 1, face), normal, paintedMetal, 1);
            Box(name + " / çelik süpürgelik", new Vector3((x0 + x1) * .5f, .055f, face + normal.z * .015f),
                new Vector3(x1 - x0, .11f, .035f), darkMetal);
            Box(name + " / sac üst kıvrımı", new Vector3((x0 + x1) * .5f, 1.01f, face + normal.z * .008f),
                new Vector3(x1 - x0, .035f, .03f), bareSteel);
        }

        private void BuildGateSkin(Transform movingGate)
        {
            // These coordinates are relative to the moving gate, including its original 1.12 m origin.
            Quad("01 kapı / dokulu sac yüz", new Vector3(-1.195f, -.51f, -.426f), new Vector3(1.195f, -.51f, -.426f),
                new Vector3(1.195f, .91f, -.426f), new Vector3(-1.195f, .91f, -.426f), Vector3.back, paintedMetal, 1, movingGate);
            Quad("01 kapı / çelik üst yüz", new Vector3(-1.37f, 1.126f, -.34f), new Vector3(1.37f, 1.126f, -.34f),
                new Vector3(1.37f, 1.126f, .34f), new Vector3(-1.37f, 1.126f, .34f), Vector3.up, darkMetal, 1, movingGate);
        }

        private void BuildElectricalCabinet(Transform lever)
        {
            Vector3 centre = lever.position + new Vector3(0, .20f, 0);
            BevelBox("01 kapı elektrik panosu / pahlı gövde", centre, new Vector3(.82f, 1.50f, .50f), .035f, darkMetal);
            // North is the robot approach side; the elevated lid and control also read from the south camera.
            var northCover = BevelBox("Pano contalı ön kapak", centre + new Vector3(0, .11f, .262f),
                new Vector3(.70f, 1.16f, .032f), .022f, paintedMetal);
            var southCover = BevelBox("Pano arka bakım kapağı", centre + new Vector3(0, .08f, -.262f),
                new Vector3(.70f, 1.13f, .032f), .022f, paintedMetal);
            var coverFinish = new MaterialPropertyBlock();
            coverFinish.SetColor("_Color", new Color(.98f, .95f, .83f));
            coverFinish.SetFloat("_AlbedoGain", 3.0f);
            northCover.GetComponent<Renderer>().SetPropertyBlock(coverFinish);
            southCover.GetComponent<Renderer>().SetPropertyBlock(coverFinish);
            foreach (float x in new[] { -.287f, .287f })
                foreach (float y in new[] { -.41f, .60f })
                {
                    Cylinder("Pano kapak vidası", centre + new Vector3(x, y, .288f), .022f, .014f,
                        Quaternion.Euler(90, 0, 0), bareSteel);
                    Cylinder("Pano bakım kapağı vidası", centre + new Vector3(x, y, -.288f), .022f, .014f,
                        Quaternion.Euler(90, 0, 0), bareSteel);
                }
            foreach (float y in new[] { -.22f, .40f })
            {
                Cylinder("Pano menteşe", centre + new Vector3(-.365f, y, .256f), .032f, .15f,
                    Quaternion.identity, bareSteel);
                Cylinder("Pano bakım kapağı menteşesi", centre + new Vector3(-.365f, y, -.256f), .032f, .15f,
                    Quaternion.identity, bareSteel);
            }
            BevelBox("Pano sigorta inceleme çerçevesi", centre + new Vector3(0, .27f, -.295f),
                new Vector3(.44f, .33f, .025f), .018f, bareSteel);
            BevelBox("Pano girintili sigorta bölmesi", centre + new Vector3(0, .27f, -.312f),
                new Vector3(.385f, .275f, .014f), .012f, darkMetal);
            for (int i = 0; i < 3; i++)
            {
                Vector3 fuse = centre + new Vector3(-.105f + i * .105f, .27f, -.327f);
                var body = BevelBox("Pano seramik sigorta yuvası", fuse, new Vector3(.075f, .19f, .025f), .007f, paintedMetal);
                body.GetComponent<Renderer>().SetPropertyBlock(coverFinish);
                Box("Sigorta ayırıcı tırnağı", fuse + new Vector3(0, .015f, -.024f),
                    new Vector3(.042f, .047f, .027f), darkMetal);
            }
            for (int i = 0; i < 3; i++)
            {
                Box("Pano kuzey havalandırma yarığı", centre + new Vector3(0, -.25f + i * .065f, .284f),
                    new Vector3(.39f, .024f, .012f), darkMetal);
                Box("Pano güney havalandırma yarığı", centre + new Vector3(0, -.25f + i * .065f, -.284f),
                    new Vector3(.39f, .024f, .012f), darkMetal);
            }
            Cylinder("Döner ayırıcı arka halka", centre + new Vector3(.16f, .28f, .299f), .113f, .032f,
                Quaternion.Euler(90, 0, 0), safetyYellow);
            var knob = Box("Döner ayırıcı tutamağı", centre + new Vector3(.16f, .28f, .33f),
                new Vector3(.055f, .17f, .056f), darkMetal);
            knob.transform.localRotation = Quaternion.Euler(0, 0, -25);
            Box("Şalter üst kontrol kaidesi", centre + new Vector3(0, .766f, 0), new Vector3(.59f, .04f, .34f), bareSteel);
            Cylinder("Şalter kolu mili", centre + new Vector3(0, .83f, -.015f), .042f, .15f, Quaternion.identity, bareSteel);
            Pipe("Sarı şalter tutamağı", centre + new Vector3(-.20f, .92f, -.015f),
                centre + new Vector3(.20f, .92f, -.015f), .055f, safetyYellow);
            Cylinder("Pano durum lambası çerçevesi", centre + new Vector3(.25f, .797f, -.11f), .065f, .035f, Quaternion.identity, darkMetal);
            indicator = Cylinder("Pano kapı durum lambası", centre + new Vector3(.25f, .821f, -.11f), .044f, .018f,
                Quaternion.identity, paintedMetal).GetComponent<Renderer>();
            indicatorLight = new GameObject("Pano durum ışığı").AddComponent<Light>();
            indicatorLight.transform.SetParent(visuals, false);
            indicatorLight.transform.position = centre + new Vector3(.25f, 1.02f, -.11f);
            indicatorLight.type = LightType.Point;
            indicatorLight.range = .55f;
            indicatorLight.intensity = .18f;
            indicatorLight.shadows = LightShadows.None;

            foreach (float x in new[] { -.22f, .22f })
            {
                Cylinder("Pano kablo rakoru", centre + new Vector3(x, -.66f, -.30f), .052f, .10f,
                    Quaternion.Euler(90, 0, 0), bareSteel);
                Pipe("Pano esnek bağlantı", centre + new Vector3(x, -.66f, -.35f),
                    new Vector3(centre.x + x, .075f, 2.02f), .035f, darkMetal);
            }
            Pipe("Kapı şalteri zemindeki koruma borusu", new Vector3(centre.x, .064f, 2.02f),
                new Vector3(centre.x, .064f, .70f), .040f, darkMetal);
            Sign("KAPI ŞALTERİ", centre + new Vector3(0, 1.16f, -.035f), 1.45f, .29f, .055f);
            BuildCabinetWorkLight(centre);

            // Only the old visual disappears. The exact collider and sensor target are retained.
            var original = lever.GetComponent<Renderer>();
            if (original != null) original.enabled = false;
            foreach (Renderer child in lever.GetComponentsInChildren<Renderer>())
                if (child != original) child.enabled = false;
        }

        private void BuildCabinetWorkLight(Vector3 cabinetCentre)
        {
            var position = new Vector3(-4.055f, 2.20f, 2.05f);
            Box("Pano çalışma lambası duvar plakası", new Vector3(-4.222f, 2.20f, 2.05f),
                new Vector3(.034f, .23f, .19f), darkMetal);
            Pipe("Pano lambası kısa montaj kolu", new Vector3(-4.20f, 2.20f, 2.05f),
                position, .028f, bareSteel);
            BevelBox("Pano küçük çalışma armatürü", position, new Vector3(.12f, .12f, .40f), .020f, darkMetal);
            var lens = Box("Pano armatürü mat dağıtıcı", position + new Vector3(.03f, -.069f, 0),
                new Vector3(.095f, .020f, .31f), paintedMetal);
            var glow = new MaterialPropertyBlock();
            glow.SetColor("_Color", new Color(.92f, .88f, .76f));
            glow.SetColor("_EmissionColor", new Color(.36f, .32f, .24f));
            lens.GetComponent<Renderer>().SetPropertyBlock(glow);
            var light = new GameObject("Pano yerel çalışma ışığı").AddComponent<Light>();
            light.transform.SetParent(visuals, false);
            light.transform.localPosition = position + new Vector3(.055f, -.10f, -.07f);
            light.transform.rotation = Quaternion.LookRotation(cabinetCentre + new Vector3(0, .25f, -.32f) - light.transform.position);
            light.type = LightType.Spot;
            light.color = new Color(1f, .94f, .83f);
            light.range = 3.8f;
            light.intensity = 1.1f;
            light.spotAngle = 75;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        private void BuildWorkLights()
        {
            WorkLight(new Vector3(-7.28f, 2.38f, -10.4f), new Vector3(-3.1f, 0, -9.2f));
            WorkLight(new Vector3(7.28f, 2.38f, -3.7f), new Vector3(3.3f, .55f, -3.1f));
        }

        private void WorkLight(Vector3 position, Vector3 target)
        {
            var fixture = BevelBox("Servis çalışma armatürü", position, new Vector3(1.25f, .105f, .23f), .04f, darkMetal);
            fixture.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var tube = Box("Armatür mat ışık dağıtıcı", position + Vector3.down * .064f, new Vector3(1.10f, .025f, .17f), paintedMetal);
            tube.transform.localRotation = Quaternion.Euler(0, 90, 0);
            float wallX = Mathf.Sign(position.x) * 7.575f;
            Box("Armatür duvar bağlantı plakası", new Vector3(wallX, position.y - .08f, position.z),
                new Vector3(.035f, .24f, .22f), darkMetal);
            Pipe("Armatür montaj kolu", new Vector3(wallX, position.y - .03f, position.z),
                position + Vector3.down * .03f, .035f, bareSteel);
            var glow = new MaterialPropertyBlock();
            glow.SetColor("_Color", new Color(.83f, .81f, .69f));
            glow.SetColor("_EmissionColor", new Color(.53f, .49f, .38f));
            tube.GetComponent<Renderer>().SetPropertyBlock(glow);
            var light = new GameObject("Servis yüzey çalışma ışığı").AddComponent<Light>();
            light.transform.SetParent(visuals, false);
            light.transform.position = position + Vector3.down * .10f;
            light.transform.rotation = Quaternion.LookRotation(target - light.transform.position);
            light.type = LightType.Spot;
            light.color = new Color(1f, .94f, .81f);
            light.intensity = 2.25f;
            light.range = 12;
            light.spotAngle = 105;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        private void Update()
        {
            if (gate != null && gate.IsOpen != wasOpen) RefreshIndicator();
        }

        private void RefreshIndicator()
        {
            wasOpen = gate != null && gate.IsOpen;
            Color color = wasOpen ? new Color(.20f, .66f, .30f) : new Color(.88f, .43f, .10f);
            state.Clear();
            state.SetColor("_Color", color);
            state.SetColor("_EmissionColor", color * .25f);
            if (indicator != null) indicator.SetPropertyBlock(state);
            if (indicatorLight != null) indicatorLight.color = color;
        }

        private void Horizontal(string name, float x0, float x1, float z0, float z1, float y, Material material, float metres)
            => Quad(name, new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1),
                new Vector3(x0, y, z1), Vector3.up, material, metres);

        private void Quad(string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
            Material material, float metres, Transform parent = null)
        {
            var vertices = new[] { a, b, c, d };
            var uv = new Vector2[4];
            for (int i = 0; i < vertices.Length; i++) uv[i] = MetreUv(vertices[i], normal) / metres;
            bool front = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0;
            Mesh mesh = new Mesh { name = name, vertices = vertices, uv = uv,
                triangles = front ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 } };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            MeshObject(name, mesh, material, parent);
        }

        private static Vector2 MetreUv(Vector3 point, Vector3 normal)
        {
            if (Mathf.Abs(normal.y) > .5f) return new Vector2(point.x, point.z);
            if (Mathf.Abs(normal.x) > .5f) return new Vector2(point.z, point.y);
            return new Vector2(point.x, point.y);
        }

        private GameObject Box(string name, Vector3 centre, Vector3 size, Material material)
            => BevelBox(name, centre, size, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .08f, material);

        private GameObject BevelBox(string name, Vector3 centre, Vector3 size, float bevel, Material material)
        {
            // An eight-sided extrusion gives service metal a small manufactured edge highlight.
            float w = size.x * .5f, h = size.y * .5f, depth = size.z * .5f;
            float b = Mathf.Min(bevel, Mathf.Min(w, h) * .7f);
            Vector2[] outline = {
                new Vector2(-w + b, -h), new Vector2(w - b, -h), new Vector2(w, -h + b), new Vector2(w, h - b),
                new Vector2(w - b, h), new Vector2(-w + b, h), new Vector2(-w, h - b), new Vector2(-w, -h + b)
            };
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                int start = vertices.Count;
                float z = side == 0 ? depth : -depth;
                Vector3 normal = side == 0 ? Vector3.forward : Vector3.back;
                for (int i = 0; i < 8; i++)
                {
                    var v = new Vector3(outline[i].x, outline[i].y, z);
                    vertices.Add(v);
                    uv.Add(MetreUv(centre + v, normal));
                }
                for (int i = 1; i < 7; i++)
                {
                    triangles.Add(start);
                    triangles.Add(start + (side == 0 ? i : i + 1));
                    triangles.Add(start + (side == 0 ? i + 1 : i));
                }
            }
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8, start = vertices.Count;
                Vector3 a = new Vector3(outline[i].x, outline[i].y, -depth);
                Vector3 b1 = new Vector3(outline[j].x, outline[j].y, -depth);
                Vector3 c = new Vector3(outline[j].x, outline[j].y, depth);
                Vector3 d = new Vector3(outline[i].x, outline[i].y, depth);
                Vector3 normal = Vector3.Cross(b1 - a, c - a).normalized;
                vertices.AddRange(new[] { a, b1, c, d });
                foreach (Vector3 v in new[] { a, b1, c, d }) uv.Add(MetreUv(centre + v, normal));
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            var item = MeshObject(name, mesh, material);
            item.transform.localPosition = centre;
            return item;
        }

        private GameObject Cylinder(string name, Vector3 centre, float radius, float length, Quaternion rotation, Material material)
        {
            // Meshes contain actual dimensions; no default primitive collider is ever introduced.
            string key = radius.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" +
                length.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            if (!rounds.TryGetValue(key, out Mesh mesh))
            {
                const int segments = 16;
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uv = new List<Vector2>();
                var triangles = new List<int>();
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    var normal = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    vertices.Add(normal * radius + Vector3.down * length * .5f);
                    vertices.Add(normal * radius + Vector3.up * length * .5f);
                    normals.Add(normal); normals.Add(normal);
                    uv.Add(new Vector2(i * Mathf.PI * 2 * radius / segments, 0));
                    uv.Add(new Vector2(i * Mathf.PI * 2 * radius / segments, length));
                    if (i < segments)
                    {
                        int k = i * 2;
                        triangles.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 });
                    }
                }
                for (int side = 0; side < 2; side++)
                {
                    int start = vertices.Count;
                    float sign = side == 0 ? -1 : 1;
                    vertices.Add(Vector3.up * sign * length * .5f);
                    normals.Add(Vector3.up * sign); uv.Add(Vector2.zero);
                    for (int i = 0; i <= segments; i++)
                    {
                        float angle = i * Mathf.PI * 2 / segments;
                        var v = new Vector3(Mathf.Cos(angle) * radius, sign * length * .5f, Mathf.Sin(angle) * radius);
                        vertices.Add(v); normals.Add(Vector3.up * sign); uv.Add(new Vector2(v.x, v.z));
                        if (i < segments)
                        {
                            triangles.Add(start);
                            triangles.Add(start + i + (side == 0 ? 1 : 2));
                            triangles.Add(start + i + (side == 0 ? 2 : 1));
                        }
                    }
                }
                mesh = new Mesh { name = "Servis silindiri " + key };
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
                mesh.RecalculateTangents(); mesh.RecalculateBounds();
                rounds.Add(key, mesh); meshes.Add(mesh);
            }
            var item = MeshObject(name, mesh, material);
            item.transform.localPosition = centre;
            item.transform.localRotation = rotation;
            return item;
        }

        private void Pipe(string name, Vector3 from, Vector3 to, float radius, Material material)
            => Cylinder(name, (from + to) * .5f, radius, Vector3.Distance(from, to),
                Quaternion.FromToRotation(Vector3.up, to - from), material);

        private GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent = null)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(parent != null ? parent : visuals, false);
            if (parent != null)
            {
                // Solid primitives carry dimensions in their scale. Our meshes already carry metres.
                Vector3 reference = visuals.lossyScale, inherited = parent.lossyScale;
                item.transform.localScale = new Vector3(reference.x / inherited.x,
                    reference.y / inherited.y, reference.z / inherited.z);
            }
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            return item;
        }

        private void Sign(string label, Vector3 position, float width, float height, float size)
        {
            var group = new GameObject(label).transform;
            group.SetParent(visuals, false);
            group.localPosition = position;
            group.localRotation = Quaternion.Euler(54, 0, 0);
            Quad(label + " plaka", new Vector3(-width * .5f, -height * .5f, 0), new Vector3(width * .5f, -height * .5f, 0),
                new Vector3(width * .5f, height * .5f, 0), new Vector3(-width * .5f, height * .5f, 0), Vector3.back,
                darkMetal, 1, group);
            var text = new GameObject(label).AddComponent<TextMesh>();
            text.transform.SetParent(group, false);
            text.transform.localPosition = new Vector3(0, 0, -.012f);
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = size;
            text.color = new Color(.84f, .82f, .66f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                text.font = font;
                text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            foreach (Renderer renderer in group.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
            meshes.Clear();
            rounds.Clear();
        }
    }
}
