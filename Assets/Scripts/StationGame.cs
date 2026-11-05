using UnityEngine;
using UnityEngine.SceneManagement;

namespace KuleNobeti
{
    public sealed class StationGame : MonoBehaviour
    {
        [SerializeField] private Material worldMaterial;
        private StationWorld world;
        private DialogueController dialogue;
        private Camera view;

        public void SetMaterial(Material material) => worldMaterial = material;

        private void Start()
        {
            Application.targetFrameRate = 60;
            world = new StationWorld(transform, worldMaterial);
            world.Build();

            var prompt = Resources.Load<TextAsset>("WatchmanPrompt");
            var schema = Resources.Load<TextAsset>("DialogueSchema");
            var config = OllamaSettings.Load();
            dialogue = gameObject.AddComponent<DialogueController>();
            dialogue.Configure(new OllamaClient(config, schema.text), prompt.text,
                world.PhysicalGate, world.ArchiveGate, world.Player.transform, world.Guard);

            view = new GameObject("Oyun kamerası").AddComponent<Camera>();
            view.transform.SetParent(transform);
            view.transform.rotation = Quaternion.Euler(54, 0, 0);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.025f, .029f, .028f);
            view.fieldOfView = 54f;
            view.transform.position = CameraTarget();
            gameObject.AddComponent<StationHud>().Configure(world, dialogue, config.model);
        }

        private Vector3 CameraTarget() => new Vector3(world.Player.transform.position.x * .32f, 13.8f,
            world.Player.transform.position.z - 7.1f);

        private void LateUpdate()
        {
            if (world == null) return;
            view.transform.position = Vector3.Lerp(view.transform.position, CameraTarget(), 8f * Time.deltaTime);
            Vector3 position = world.Player.transform.position;
            if (world.ArchiveGate.IsOpen && position.z >= 14.6f && Mathf.Abs(position.x) < 2.05f)
                world.Player.Finished = true;
            if (Input.GetKeyDown(KeyCode.F8) || (world.Player.Finished && Input.GetKeyDown(KeyCode.R)))
                Restart();
        }

        // F8 and the editor flow check use the same complete scene reset.
        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        private void OnDestroy() => world?.ReleaseNavigation();
    }
}
