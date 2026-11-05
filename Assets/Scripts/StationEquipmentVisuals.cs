using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuleNobeti
{
    // Wall-side set dressing only. No physics components, navigation geometry or gameplay state.
    public sealed class StationEquipmentVisuals : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Dictionary<string, Mesh> cylinders = new Dictionary<string, Mesh>();
        private Transform visuals;
        private Material painted, dark, steel, yellow, palette;
        private MaterialPropertyBlock colour;

        public void Configure(Transform parent, Material material)
        {
            palette = material;
            painted = Resources.Load<Material>("ServiceRoom/PaintedMetal") ?? material;
            dark = Resources.Load<Material>("ServiceRoom/DarkMetal") ?? material;
            steel = Resources.Load<Material>("ServiceRoom/BareSteel") ?? material;
            yellow = Resources.Load<Material>("ServiceRoom/SafetyYellow") ?? material;
            colour = new MaterialPropertyBlock();
            visuals = Group("Tesis ekipmanları — yalnız görsel", Vector3.zero, parent);
            BuildMaintenance();
            BuildValveLine();
            BuildRadioEquipment();
            BuildArchiveCases();
        }

        private void BuildMaintenance()
        {
            // East-wall clusters keep the central walking lane, floor hatch and robot starts clear.
            var bench = Group("Bakım tezgâhı", new Vector3(6.24f, 0, -8.55f), visuals);
            Box("Çelik çalışma tablası", new Vector3(0, .94f, 0), new Vector3(2.30f, .09f, .84f), steel, bench);
            Box("Tezgâh ön kenarı", new Vector3(0, .885f, -.42f), new Vector3(2.34f, .13f, .055f), yellow, bench);
            foreach (float x in new[] { -.99f, .99f })
            {
                Box("Tezgâh ayak dikmesi", new Vector3(x, .45f, .28f), new Vector3(.10f, .90f, .10f), dark, bench);
                Box("Tezgâh ön ayak", new Vector3(x, .45f, -.29f), new Vector3(.10f, .90f, .10f), dark, bench);
                Box("Tezgâh ayak pabucu", new Vector3(x, .045f, 0), new Vector3(.19f, .07f, .78f), dark, bench);
            }
            Box("Tezgâh alt rafı", new Vector3(0, .20f, .015f), new Vector3(2.08f, .045f, .68f), painted, bench);
            Box("Takım çekmece gövdesi", new Vector3(-.67f, .59f, .015f), new Vector3(.63f, .60f, .66f), dark, bench);
            for (int i = 0; i < 3; i++)
            {
                float y = .395f + i * .19f;
                Box("Takım çekmecesi", new Vector3(-.67f, y, -.331f), new Vector3(.58f, .17f, .035f), painted, bench);
                Pipe("Çekmece tutamağı", new Vector3(-.82f, y + .015f, -.371f),
                    new Vector3(-.52f, y + .015f, -.371f), .017f, steel, bench);
            }
            Box("Takım askı panosu çerçevesi", new Vector3(0, 1.43f, .335f), new Vector3(2.24f, .83f, .065f), dark, bench);
            Box("Takım askı panosu", new Vector3(0, 1.43f, .292f), new Vector3(2.08f, .68f, .025f), painted, bench);
            // Four distinct tools, rather than repeated marks masquerading as equipment.
            Wrench(new Vector3(-.75f, 1.40f, .245f), bench);
            Pipe("Uzun tornavida mili", new Vector3(-.24f, 1.31f, .239f), new Vector3(-.24f, 1.62f, .239f), .015f, steel, bench);
            Box("Tornavida yalıtkan sapı", new Vector3(-.24f, 1.22f, .239f), new Vector3(.075f, .20f, .065f), yellow, bench);
            Pipe("Çekiç sapı", new Vector3(.28f, 1.19f, .239f), new Vector3(.28f, 1.54f, .239f), .024f, dark, bench);
            Box("Çekiç başı", new Vector3(.28f, 1.59f, .239f), new Vector3(.29f, .105f, .095f), steel, bench);
            Pipe("Pense sol sapı", new Vector3(.69f, 1.18f, .23f), new Vector3(.78f, 1.43f, .23f), .025f, yellow, bench);
            Pipe("Pense sağ sapı", new Vector3(.93f, 1.18f, .23f), new Vector3(.82f, 1.43f, .23f), .025f, yellow, bench);
            Pipe("Pense sol çenesi", new Vector3(.78f, 1.43f, .23f), new Vector3(.86f, 1.59f, .23f), .017f, steel, bench);
            Pipe("Pense sağ çenesi", new Vector3(.82f, 1.43f, .23f), new Vector3(.74f, 1.59f, .23f), .017f, steel, bench);
            for (int i = 0; i < 4; i++)
                Cylinder("Takım askısı", new Vector3(-.75f + i * .52f, 1.68f, .225f), .021f, .08f,
                    Quaternion.Euler(90, 0, 0), steel, bench);
            // Bench vise and a single service part make the work surface read as a workstation.
            Box("Mengene tabanı", new Vector3(.67f, 1.015f, -.17f), new Vector3(.34f, .07f, .34f), dark, bench);
            Box("Mengene sabit çenesi", new Vector3(.67f, 1.13f, -.04f), new Vector3(.29f, .18f, .08f), painted, bench);
            Box("Mengene hareketli çenesi", new Vector3(.67f, 1.13f, -.23f), new Vector3(.29f, .18f, .08f), painted, bench);
            Pipe("Mengene vida mili", new Vector3(.67f, 1.065f, -.40f), new Vector3(.67f, 1.065f, -.08f), .025f, steel, bench);
            Pipe("Mengene çevirme kolu", new Vector3(.54f, 1.065f, -.41f), new Vector3(.80f, 1.065f, -.41f), .014f, steel, bench);
            Box("Parça çalışma altlığı", new Vector3(-.23f, .993f, -.035f), new Vector3(.48f, .018f, .39f), dark, bench);
            Cylinder("Sökülmüş filtre parçası", new Vector3(-.23f, 1.07f, -.035f), .105f, .14f, Quaternion.identity, steel, bench);
            Ring("Filtre üst contası", new Vector3(-.23f, 1.145f, -.035f), .074f, .012f, Quaternion.Euler(90, 0, 0), dark, bench);

            var cabinet = Group("Duvar kenarı alet dolabı", new Vector3(7.06f, 0, -5.55f), visuals, 90);
            Box("Dolap kaidesi", new Vector3(0, .08f, 0), new Vector3(1.29f, .14f, .73f), dark, cabinet);
            Box("Dolap metal gövdesi", new Vector3(0, .97f, 0), new Vector3(1.28f, 1.68f, .73f), dark, cabinet);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("Alet dolabı kapağı", new Vector3(side * .312f, .97f, -.377f), new Vector3(.60f, 1.57f, .035f), painted, cabinet);
                Pipe("Alet dolabı kapak tutamağı", new Vector3(side * .095f, .80f, -.421f),
                    new Vector3(side * .095f, 1.09f, -.421f), .019f, steel, cabinet);
                for (int i = 0; i < 3; i++)
                    Box("Dolap hava yarığı", new Vector3(side * .312f, .34f + i * .075f, -.400f),
                        new Vector3(.32f, .025f, .012f), dark, cabinet);
                foreach (float y in new[] { .43f, 1.50f })
                    Box("Dolap kapak menteşesi", new Vector3(side * .59f, y, -.407f), new Vector3(.055f, .12f, .035f), steel, cabinet);
            }
            Box("Dolap küçük bakım etiketi", new Vector3(-.31f, 1.49f, -.401f), new Vector3(.26f, .13f, .013f), yellow, cabinet);
            // South-facing end and top are visible from the existing elevated camera.
            Box("Dolap güney yan servis kapağı", new Vector3(.650f, .99f, 0), new Vector3(.025f, 1.32f, .56f), painted, cabinet);
            for (int i = 0; i < 4; i++)
                Box("Dolap yan havalandırma yarığı", new Vector3(.667f, .60f + i * .085f, 0), new Vector3(.012f, .035f, .35f), dark, cabinet);
            foreach (float z in new[] { -.23f, .23f })
                Cylinder("Dolap yan kapak vidası", new Vector3(.671f, 1.48f, z), .020f, .016f, Quaternion.Euler(0, 0, 90), steel, cabinet);
            Box("Dolap üst kapak şeridi", new Vector3(0, 1.82f, 0), new Vector3(1.18f, .025f, .63f), painted, cabinet);
        }

        private void Wrench(Vector3 centre, Transform parent)
        {
            Box("Anahtar sapı", centre + Vector3.down * .045f, new Vector3(.065f, .36f, .035f), steel, parent);
            Ring("Anahtar halka ağzı", centre + Vector3.up * .16f, .062f, .018f, Quaternion.identity, steel, parent);
            Box("Anahtar açık ağız sol çene", centre + new Vector3(-.048f, -.23f, 0), new Vector3(.03f, .11f, .035f), steel, parent);
            Box("Anahtar açık ağız sağ çene", centre + new Vector3(.048f, -.23f, 0), new Vector3(.03f, .11f, .035f), steel, parent);
            Box("Anahtar çene köprüsü", centre + Vector3.down * .185f, new Vector3(.12f, .035f, .035f), steel, parent);
        }

        private void BuildValveLine()
        {
            var line = Group("Havalandırma izolasyon borusu ve vana", new Vector3(-7.12f, 0, 8.90f), visuals);
            Pipe("İzolasyon besleme borusu", new Vector3(0, 1.38f, -.90f), new Vector3(0, 1.38f, .72f), .095f, steel, line);
            Pipe("Duvar dönüş borusu", new Vector3(0, 1.38f, .72f), new Vector3(0, 2.20f, .72f), .095f, steel, line);
            Pipe("Duvar boru bağlantısı", new Vector3(0, 2.20f, .72f), new Vector3(-.40f, 2.20f, .72f), .095f, steel, line);
            foreach (float z in new[] { -.60f, .43f })
            {
                Cylinder("Boru bağlantı flanşı", new Vector3(0, 1.38f, z), .155f, .065f, Quaternion.Euler(90, 0, 0), dark, line);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * .5f + Mathf.PI * .25f;
                    Cylinder("Flanş cıvatası", new Vector3(Mathf.Cos(a) * .12f, 1.38f + Mathf.Sin(a) * .12f, z - .043f),
                        .023f, .035f, Quaternion.Euler(90, 0, 0), steel, line);
                }
            }
            Box("Vana döküm gövdesi", new Vector3(0, 1.38f, -.08f), new Vector3(.25f, .28f, .27f), painted, line);
            Pipe("Vana mili", new Vector3(0, 1.38f, -.09f), new Vector3(.29f, 1.70f, -.12f), .035f, steel, line);
            Vector3 wheel = new Vector3(.29f, 1.72f, -.15f);
            Ring("Sarı izolasyon vana çarkı", wheel, .235f, .035f, Quaternion.identity, yellow, line);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2 / 3;
                Pipe("Vana çarkı kolu", wheel, wheel + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .225f, .023f, yellow, line);
            }
            Cylinder("Vana çarkı göbeği", wheel, .065f, .065f, Quaternion.Euler(90, 0, 0), dark, line);
            foreach (float y in new[] { 1.78f, 2.10f })
            {
                Pipe("Boru duvar askısı", new Vector3(-.39f, y, .72f), new Vector3(0, y, .72f), .023f, dark, line);
                Ring("Dikey boru kelepçesi", new Vector3(0, y, .72f), .106f, .014f, Quaternion.Euler(90, 0, 0), dark, line);
            }
        }

        private void BuildRadioEquipment()
        {
            var reel = Group("Telsiz servis kablo makarası", new Vector3(7.02f, 0, 8.0f), visuals, 90);
            // Reel axle runs along the wall; two flanges and wound cable remain distinct from the console.
            Box("Makara taşıma tabanı", new Vector3(0, .065f, 0), new Vector3(.72f, .10f, .66f), dark, reel);
            foreach (float x in new[] { -.25f, .25f })
                Pipe("Makara çerçeve kolu", new Vector3(x, .09f, -.24f), new Vector3(x, .43f, .035f), .035f, painted, reel);
            Cylinder("Makara metal mili", new Vector3(0, .44f, .035f), .06f, .67f, Quaternion.Euler(0, 0, 90), steel, reel);
            Cylinder("Kablo sargı gövdesi", new Vector3(0, .44f, .035f), .255f, .43f, Quaternion.Euler(0, 0, 90), dark, reel);
            foreach (float x in new[] { -.255f, .255f })
            {
                Cylinder("Kablo makarası yan diski", new Vector3(x, .44f, .035f), .345f, .045f, Quaternion.Euler(0, 0, 90), painted, reel);
                Ring("Makara disk takviye çemberi", new Vector3(x + Mathf.Sign(x) * .027f, .44f, .035f), .255f, .015f,
                    Quaternion.Euler(0, 90, 0), steel, reel);
            }
            for (int i = 0; i < 8; i++)
                Ring("Sargılı servis kablosu", new Vector3(-.20f + i * .057f, .44f, .035f), .263f, .022f,
                    Quaternion.Euler(0, 90, 0), dark, reel);
            Pipe("Makara taşıma kulpu sol", new Vector3(-.25f, .40f, .035f), new Vector3(-.25f, .88f, .035f), .026f, dark, reel);
            Pipe("Makara taşıma kulpu sağ", new Vector3(.25f, .40f, .035f), new Vector3(.25f, .88f, .035f), .026f, dark, reel);
            Pipe("Makara taşıma tutamağı", new Vector3(-.25f, .88f, .035f), new Vector3(.25f, .88f, .035f), .035f, dark, reel);
            Pipe("Makara kablo çıkışı", new Vector3(7.12f, .07f, 8.26f), new Vector3(7.37f, .07f, 8.45f), .023f, dark, visuals);
            Pipe("Telsize takılı servis kablosu", new Vector3(7.37f, .07f, 8.45f), new Vector3(7.37f, .35f, 10.15f), .023f, dark, visuals);

            var bag = Group("Telsiz yanı acil durum çantası", new Vector3(7.12f, .08f, 8.85f), visuals, 90);
            var red = new Color(.58f, .22f, .13f);
            Box("Acil çanta gövdesi", new Vector3(0, .24f, 0), new Vector3(.65f, .43f, .43f), palette, bag, red);
            Box("Acil çanta üst kapağı", new Vector3(0, .455f, 0), new Vector3(.67f, .065f, .46f), palette, bag, red);
            foreach (float x in new[] { -.21f, .21f })
            {
                Box("Acil çanta bağlama kayışı", new Vector3(x, .25f, -.229f), new Vector3(.045f, .38f, .022f), dark, bag);
                Box("Acil çanta kayış tokası", new Vector3(x, .34f, -.249f), new Vector3(.07f, .075f, .018f), steel, bag);
                Pipe("Çanta tutamak bağlantısı", new Vector3(x * .48f, .47f, 0), new Vector3(x * .48f, .57f, 0), .020f, dark, bag);
            }
            Pipe("Acil çanta taşıma kulpu", new Vector3(-.10f, .57f, 0), new Vector3(.10f, .57f, 0), .026f, dark, bag);
            Box("Acil çanta yatay sağlık işareti", new Vector3(0, .27f, -.234f), new Vector3(.21f, .055f, .012f), palette, bag,
                new Color(.86f, .84f, .72f));
            Box("Acil çanta dikey sağlık işareti", new Vector3(0, .27f, -.239f), new Vector3(.055f, .21f, .012f), palette, bag,
                new Color(.86f, .84f, .72f));
            Box("Çanta üst yatay sağlık işareti", new Vector3(0, .492f, .125f), new Vector3(.17f, .010f, .045f), palette, bag,
                new Color(.86f, .84f, .72f));
            Box("Çanta üst dikey sağlık işareti", new Vector3(0, .494f, .125f), new Vector3(.045f, .010f, .17f), palette, bag,
                new Color(.86f, .84f, .72f));
        }

        private void BuildArchiveCases()
        {
            var group = Group("Arşiv dosya taşıma kasaları", new Vector3(-3.70f, 0, 16.95f), visuals);
            ArchiveCase(new Vector3(-.52f, 0, 0), group, false);
            ArchiveCase(new Vector3(.47f, 0, 0), group, true);
            ArchiveCase(new Vector3(-.52f, .56f, 0), group, true);
        }

        private void ArchiveCase(Vector3 p, Transform parent, bool cream)
        {
            Material shell = cream ? painted : dark;
            Box("Dosya taşıma kasası", p + new Vector3(0, .255f, 0), new Vector3(.88f, .49f, .66f), shell, parent);
            Box("Dosya kasası kapak kenarı", p + new Vector3(0, .515f, 0), new Vector3(.92f, .055f, .70f), steel, parent);
            foreach (float x in new[] { -.32f, .32f })
            {
                Box("Kasada köşe koruyucusu", p + new Vector3(x, .255f, -.337f), new Vector3(.075f, .43f, .028f), steel, parent);
                Box("Kasada kapak mandalı", p + new Vector3(x, .465f, -.365f), new Vector3(.055f, .10f, .025f), dark, parent);
            }
            Box("Dosya kasası iç tutamak", p + new Vector3(0, .31f, -.341f), new Vector3(.25f, .065f, .025f), dark, parent);
            Box("Dosya kasası kayıt etiketi", p + new Vector3(0, .16f, -.352f), new Vector3(.27f, .105f, .012f), palette, parent,
                new Color(.78f, .75f, .62f));
            Box("Kayıt etiketi ayraç çizgisi", p + new Vector3(-.065f, .16f, -.362f), new Vector3(.025f, .07f, .008f), dark, parent);
        }

        private Transform Group(string name, Vector3 position, Transform parent, float yaw = 0)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            group.localRotation = Quaternion.Euler(0, yaw, 0);
            return group;
        }

        private GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent, Color? tint = null)
        {
            // A small eight-sided manufactured edge keeps case/bench silhouettes from razor-sharp cubes.
            float w = size.x * .5f, h = size.y * .5f, d = size.z * .5f;
            float bevel = Mathf.Min(.025f, Mathf.Min(w, h) * .20f);
            Vector2[] outline = {
                new Vector2(-w + bevel, -h), new Vector2(w - bevel, -h), new Vector2(w, -h + bevel), new Vector2(w, h - bevel),
                new Vector2(w - bevel, h), new Vector2(-w + bevel, h), new Vector2(-w, h - bevel), new Vector2(-w, -h + bevel)
            };
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                int start = v.Count;
                for (int i = 0; i < 8; i++)
                {
                    v.Add(new Vector3(outline[i].x, outline[i].y, side == 0 ? d : -d));
                    uv.Add(outline[i]);
                }
                for (int i = 1; i < 7; i++)
                    tri.AddRange(new[] { start, start + (side == 0 ? i : i + 1), start + (side == 0 ? i + 1 : i) });
            }
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8, start = v.Count;
                v.AddRange(new[] { new Vector3(outline[i].x, outline[i].y, -d), new Vector3(outline[next].x, outline[next].y, -d),
                    new Vector3(outline[next].x, outline[next].y, d), new Vector3(outline[i].x, outline[i].y, d) });
                float length = Vector2.Distance(outline[i], outline[next]);
                uv.AddRange(new[] { Vector2.zero, new Vector2(length, 0), new Vector2(length, size.z), new Vector2(0, size.z) });
                tri.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); meshes.Add(mesh);
            return MeshObject(name, position, Quaternion.identity, mesh, material, parent, tint);
        }

        private void Pipe(string name, Vector3 from, Vector3 to, float radius, Material material, Transform parent)
            => Cylinder(name, (from + to) * .5f, radius, Vector3.Distance(from, to),
                Quaternion.FromToRotation(Vector3.up, to - from), material, parent);

        private GameObject Cylinder(string name, Vector3 position, float radius, float length, Quaternion rotation, Material material, Transform parent)
        {
            string key = radius.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" +
                length.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            if (!cylinders.TryGetValue(key, out Mesh mesh))
            {
                const int n = 16;
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var normals = new List<Vector3>(); var tri = new List<int>();
                for (int i = 0; i <= n; i++)
                {
                    float angle = i * Mathf.PI * 2 / n;
                    var normal = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    v.Add(normal * radius + Vector3.down * length * .5f); v.Add(normal * radius + Vector3.up * length * .5f);
                    normals.Add(normal); normals.Add(normal);
                    uv.Add(new Vector2(i * Mathf.PI * 2 * radius / n, 0)); uv.Add(new Vector2(i * Mathf.PI * 2 * radius / n, length));
                    if (i < n) { int k = i * 2; tri.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 }); }
                }
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1 : 1;
                    int start = v.Count;
                    v.Add(Vector3.up * sign * length * .5f); normals.Add(Vector3.up * sign); uv.Add(Vector2.zero);
                    for (int i = 0; i <= n; i++)
                    {
                        float angle = i * Mathf.PI * 2 / n;
                        var point = new Vector3(Mathf.Cos(angle) * radius, sign * length * .5f, Mathf.Sin(angle) * radius);
                        v.Add(point); normals.Add(Vector3.up * sign); uv.Add(new Vector2(point.x, point.z));
                        if (i < n) tri.AddRange(new[] { start, start + i + (side == 0 ? 1 : 2), start + i + (side == 0 ? 2 : 1) });
                    }
                }
                mesh = new Mesh { name = "Ekipman silindiri " + key };
                mesh.SetVertices(v); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
                mesh.RecalculateTangents(); mesh.RecalculateBounds(); meshes.Add(mesh); cylinders.Add(key, mesh);
            }
            return MeshObject(name, position, rotation, mesh, material, parent);
        }

        private void Ring(string name, Vector3 position, float radius, float tube, Quaternion rotation, Material material, Transform parent)
        {
            // XY-plane torus; smooth tubing gives wheels, wound cables and seals their functional profiles.
            const int n = 24, m = 6;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var normals = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                for (int j = 0; j <= m; j++)
                {
                    float b = j * Mathf.PI * 2 / m;
                    Vector3 normal = radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    v.Add(radial * radius + normal * tube); normals.Add(normal);
                    uv.Add(new Vector2(i * Mathf.PI * 2 * radius / n, j * Mathf.PI * 2 * tube / m));
                    if (i < n && j < m)
                    {
                        int k = i * (m + 1) + j;
                        tri.AddRange(new[] { k, k + m + 2, k + 1, k, k + m + 1, k + m + 2 });
                    }
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateTangents(); mesh.RecalculateBounds(); meshes.Add(mesh);
            MeshObject(name, position, rotation, mesh, material, parent);
        }

        private GameObject MeshObject(string name, Vector3 position, Quaternion rotation, Mesh mesh, Material material,
            Transform parent, Color? tint = null)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position; item.transform.localRotation = rotation;
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (tint.HasValue)
            {
                colour.Clear(); colour.SetColor("_Color", tint.Value); renderer.SetPropertyBlock(colour);
            }
            return item;
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
            meshes.Clear(); cylinders.Clear();
        }
    }
}
