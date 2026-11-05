using UnityEngine;

namespace KuleNobeti
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        private CharacterController controller;
        private float verticalSpeed;
        private StationTasks stationTasks;
        public bool HasFocusForDialogue { get; set; }
        public bool Finished { get; set; }

        private void Awake() => controller = GetComponent<CharacterController>();

        internal void SetStationTasks(StationTasks tasks) => stationTasks = tasks;

        private void Update()
        {
            Vector3 direction = Vector3.zero;
            if (!HasFocusForDialogue && !Finished)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) direction.z += 1;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) direction.z -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direction.x += 1;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) direction.x -= 1;
            }

            direction = Vector3.ClampMagnitude(direction, 1);
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.LookRotation(direction);
            verticalSpeed = controller.isGrounded ? -2f : Mathf.Max(-25f, verticalSpeed - 22f * Time.deltaTime);
            float movementSpeed = speed * (stationTasks != null && stationTasks.InHazard ? .48f : 1f);
            controller.Move((direction * movementSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
    }
}
