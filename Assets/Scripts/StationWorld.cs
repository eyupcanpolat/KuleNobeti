using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KuleNobeti
{
    public sealed class StationWorld
    {
        public PlayerMotor Player { get; private set; }
        public CompanionAgent Companion { get; private set; }
        public Door PhysicalGate { get; private set; }
        public Door ArchiveGate { get; private set; }
        public Transform Guard { get; private set; }
        public StationTasks Tasks { get; private set; }

        public static readonly Vector3 SwitchApproach = new Vector3(-2.65f, 0, 3.72f);
        public static readonly Vector3[] TestStarts = {
            new Vector3(-1.7f, 0, -8.7f), new Vector3(3.9f, 0, -10.8f), new Vector3(-5.9f, 0, -6.4f)
        };

        private static readonly Color Floor = new Color(.105f, .12f, .125f);
        private static readonly Color Walls = new Color(.18f, .205f, .21f);
        private static readonly Color Orange = new Color(.76f, .57f, .19f);
        private static readonly Color Mint = new Color(.70f, .68f, .54f);
        private readonly Material material;
        private readonly Transform root;
        private readonly List<NavMeshBuildSource> geometry = new List<NavMeshBuildSource>();
        private NavMeshData data;
        private NavMeshDataInstance navMesh;

        public StationWorld(Transform root, Material material)
        {
            this.root = root;
            this.material = material;
        }

        public void Build()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.26f, .29f, .30f);
            var light = new GameObject("Tavan ışığı").AddComponent<Light>();
            light.transform.SetParent(root);
            light.type = LightType.Directional;
            light.color = new Color(.90f, .93f, .94f);
            light.intensity = .95f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(56, -38, 0);

            Solid("Zemin", 0, -.3f, 2, 16, .6f, 32, Floor);
            Solid("Batı dış duvarı", -7.85f, 1.25f, 2, .5f, 2.5f, 32, Walls);
            Solid("Doğu dış duvarı", 7.85f, 1.25f, 2, .5f, 2.5f, 32, Walls);
            Solid("Güney dış duvarı", 0, 1.25f, -13.85f, 16, 2.5f, .5f, Walls);
            Solid("Kuzey dış duvarı", 0, 1.25f, 17.85f, 16, 2.5f, .5f, Walls);
            Solid("Servis tüneli duvarı", -4.5f, 1.25f, 0, .50f, 2.5f, 9.45f, Walls);
            // Robot yüksekliği .72 m: .80 m alt açıklığı olan bu fiziksel kirişlerden geçebilir.
            // Bake varsayılan insan yüksekliğiyle çalıştığı için iki kiriş kaynaklara eklenmez.
            Solid("Güney alçak tünel girişi", -6.18f, 1.51f, -4.7f, 3.52f, 1.42f, .50f, Orange, false);
            Solid("Kuzey alçak tünel girişi", -6.18f, 1.51f, 4.7f, 3.52f, 1.42f, .50f, Orange, false);

            Solid("Fiziksel bariyer / sol", -2.925f, 1.15f, 0, 3.15f, 2.3f, .76f, Walls);
            Solid("Fiziksel bariyer / sağ", 4.575f, 1.15f, 0, 6.45f, 2.3f, .76f, Walls);
            PhysicalGate = Solid("Şalter kapısı", 0, 1.12f, 0, 2.74f, 2.24f, .68f, Walls)
                .AddComponent<Door>();
            Solid("Arşiv duvarı / sol", -4.69f, 1.24f, 12, 6.62f, 2.48f, .85f, Walls);
            Solid("Arşiv duvarı / sağ", 4.69f, 1.24f, 12, 6.62f, 2.48f, .85f, Walls);
            ArchiveGate = Solid("Arşiv kapısı", 0, 1.15f, 12, 2.78f, 2.3f, .73f, Mint)
                .AddComponent<Door>();
            Solid("Depo sandığı A", -3.2f, .54f, -3.9f, 1.15f, 1.08f, 1.3f, Walls);
            Solid("Depo sandığı B", 3.0f, .54f, 5.4f, 1.4f, 1.08f, 1.6f, Walls);
            Solid("Depo sandığı C", 5.7f, .54f, 7.4f, 1.6f, 1.08f, 1.6f, Walls);

            var lever = Solid("Şalter sensör hedefi", -2.65f, .55f, 2.6f, .64f, 1.1f, .42f, Mint);
            Visual("Şalter lambası", PrimitiveType.Sphere, new Vector3(0, .18f, .57f),
                Vector3.one * .24f, Orange, lever.transform);
            // Gövde şaltere yapılan Raycast'i karşılar, lamba yalnızca görseldir.

            CreatePlayer();
            Guard = CreateGuard();
            CreateNavigation();
            CreateCompanion(lever.transform);
            Tasks = root.gameObject.AddComponent<StationTasks>();
            Tasks.Configure(Player, PhysicalGate);
            root.gameObject.AddComponent<StationDecor>().Configure(root, material, PhysicalGate, ArchiveGate, Tasks);
            root.gameObject.AddComponent<ServiceRoomVisual>().Configure(root, material, PhysicalGate, lever.transform);
            root.gameObject.AddComponent<StationCharacterVisuals>().Configure(Player.transform, Guard, Companion.transform, material);
            root.gameObject.AddComponent<StationEquipmentVisuals>().Configure(root, material);
        }

        public void ReleaseNavigation()
        {
            if (navMesh.valid) navMesh.Remove();
            if (data != null) Object.Destroy(data);
        }

        private void CreateNavigation()
        {
            if (NavMesh.GetSettingsCount() == 0)
            {
                Debug.LogError("NavMesh ayarı yok. Project Settings > Navigation altında varsayılan agent ayarını kontrol et.");
                return;
            }
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = .40f;
            settings.agentHeight = .72f;
            settings.agentClimb = .18f;
            data = NavMeshBuilder.BuildNavMeshData(settings, geometry,
                new Bounds(new Vector3(0, 1, 2), new Vector3(22, 10, 40)),
                Vector3.zero, Quaternion.identity);
            if (data == null)
                Debug.LogError("Sahne geometrisinden NavMesh üretilemedi.");
            else
                navMesh = NavMesh.AddNavMeshData(data);
        }

        private void CreatePlayer()
        {
            var hero = new GameObject("Oyuncu");
            hero.transform.SetParent(root);
            hero.transform.position = new Vector3(.4f, .08f, -10.1f);
            var controller = hero.AddComponent<CharacterController>();
            controller.height = 1.76f;
            controller.radius = .33f;
            controller.center = new Vector3(0, .90f, 0);
            controller.stepOffset = .22f;
            Visual("Tulum", PrimitiveType.Capsule, new Vector3(0, .78f, 0),
                new Vector3(.62f, .65f, .62f), new Color(.70f, .65f, .48f), hero.transform);
            Visual("Kask", PrimitiveType.Sphere, new Vector3(0, 1.46f, .035f), Vector3.one * .45f,
                new Color(.91f, .82f, .54f), hero.transform);
            Visual("Vizör", PrimitiveType.Cube, new Vector3(0, 1.48f, .22f),
                new Vector3(.28f, .115f, .095f), Walls, hero.transform);
            Visual("Göğüs reflektörü", PrimitiveType.Cube, new Vector3(0, 1.02f, .28f),
                new Vector3(.48f, .09f, .045f), new Color(.94f, .88f, .65f), hero.transform);
            Visual("Bakım çantası", PrimitiveType.Cube, new Vector3(0, .95f, -.29f),
                new Vector3(.38f, .48f, .18f), new Color(.23f, .27f, .26f), hero.transform);
            Player = hero.AddComponent<PlayerMotor>();
        }

        private Transform CreateGuard()
        {
            var guard = new GameObject("Nöbetçi Sera").transform;
            guard.SetParent(root);
            guard.position = new Vector3(1.65f, 0, 9.2f);
            Visual("Uzun nöbetçi paltosu", PrimitiveType.Cube, new Vector3(0, .88f, 0),
                new Vector3(.76f, 1.20f, .46f), new Color(.18f, .235f, .22f), guard);
            Visual("Omuzluk", PrimitiveType.Cube, new Vector3(0, 1.34f, 0),
                new Vector3(.97f, .18f, .50f), new Color(.32f, .35f, .31f), guard);
            Visual("Baş", PrimitiveType.Sphere, new Vector3(0, 1.64f, 0),
                new Vector3(.37f, .43f, .37f), new Color(.67f, .57f, .43f), guard);
            Visual("Siperli nöbetçi şapkası", PrimitiveType.Cube, new Vector3(0, 1.84f, .04f),
                new Vector3(.54f, .13f, .58f), new Color(.16f, .20f, .18f), guard);
            Visual("Nöbetçi rozeti", PrimitiveType.Cube, new Vector3(-.20f, 1.24f, .25f),
                new Vector3(.12f, .15f, .035f), Orange, guard);
            Visual("Sol çizme", PrimitiveType.Cube, new Vector3(-.22f, .19f, .06f),
                new Vector3(.27f, .34f, .40f), Walls, guard);
            Visual("Sağ çizme", PrimitiveType.Cube, new Vector3(.22f, .19f, .06f),
                new Vector3(.27f, .34f, .40f), Walls, guard);
            WorldSign("NÖBETÇİ SERA", guard.position + new Vector3(0, 2.15f, -.6f), new Color(.79f, .76f, .61f));
            return guard;
        }

        private void CreateCompanion(Transform lever)
        {
            var robot = new GameObject("Refakatçi robot K-07");
            robot.transform.SetParent(root);
            var start = TestStarts[0];
            if (NavMesh.SamplePosition(start, out NavMeshHit hit, 2f, NavMesh.AllAreas)) start = hit.position;
            robot.transform.position = start;
            var agent = robot.AddComponent<NavMeshAgent>();
            agent.radius = .31f;
            agent.height = .70f;
            agent.speed = 4.1f;
            agent.angularSpeed = 630f;
            agent.acceleration = 16f;
            agent.stoppingDistance = .12f;
            agent.autoBraking = true;
            Visual("Robot gövdesi", PrimitiveType.Cube, new Vector3(0, .34f, 0),
                new Vector3(.64f, .52f, .61f), new Color(.78f, .75f, .61f), robot.transform);
            Visual("Robot gözü", PrimitiveType.Cube, new Vector3(0, .47f, .33f),
                new Vector3(.40f, .11f, .08f), Walls, robot.transform);
            Visual("Robot durum ışığı", PrimitiveType.Sphere, new Vector3(0, .65f, 0),
                Vector3.one * .13f, new Color(.33f, .70f, .38f), robot.transform);
            Visual("Sol palet", PrimitiveType.Cube, new Vector3(-.36f, .18f, 0),
                new Vector3(.16f, .22f, .70f), Walls, robot.transform);
            Visual("Sağ palet", PrimitiveType.Cube, new Vector3(.36f, .18f, 0),
                new Vector3(.16f, .22f, .70f), Walls, robot.transform);
            Visual("Sarı tanıma şeridi", PrimitiveType.Cube, new Vector3(0, .61f, 0),
                new Vector3(.50f, .03f, .12f), Orange, robot.transform);
            Companion = robot.AddComponent<CompanionAgent>();
            Companion.Configure(Player.transform, lever, SwitchApproach, PhysicalGate);
        }

        private GameObject Solid(string name, float x, float y, float z, float sx, float sy, float sz,
            Color color, bool bake = true)
        {
            var box = Primitive(name, PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(sx, sy, sz), color, root);
            if (bake)
                geometry.Add(new NavMeshBuildSource {
                    shape = NavMeshBuildSourceShape.Box,
                    size = box.transform.lossyScale,
                    transform = Matrix4x4.TRS(box.transform.position, box.transform.rotation, Vector3.one),
                    area = 0
                });
            return box;
        }

        private GameObject Visual(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Transform parent)
        {
            var body = Primitive(name, type, pos, scale, color, parent);
            var collider = body.GetComponent<Collider>();
            collider.enabled = false;
            return body;
        }

        private GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Transform parent)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = pos;
            item.transform.localScale = scale;
            var renderer = item.GetComponent<MeshRenderer>();
            if (material != null) renderer.sharedMaterial = material;
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_Color", color);
            renderer.SetPropertyBlock(tint);
            return item;
        }

        private void WorldSign(string label, Vector3 pos, Color color)
        {
            var sign = new GameObject(label).AddComponent<TextMesh>();
            sign.transform.SetParent(root);
            sign.transform.position = pos;
            sign.transform.rotation = Quaternion.Euler(54f, 0, 0);
            sign.text = label;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                sign.font = font;
                sign.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            sign.fontSize = 42;
            sign.characterSize = .085f;
            sign.color = color;
        }
    }
}
