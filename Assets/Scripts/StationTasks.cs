using UnityEngine;

namespace KuleNobeti
{
    /// <summary>Optional station systems. Neither system changes the archive evacuation rule.</summary>
    public sealed class StationTasks : MonoBehaviour
    {
        public static readonly Vector3 VentilationPosition = new Vector3(-5.8f, 0, 6f);
        public static readonly Vector3 RadioPosition = new Vector3(5.8f, 0, 9.5f);
        private const float InteractionRange = 1.85f;
        private PlayerMotor player;
        private Door firstGate;

        public bool VentilationOn { get; private set; }
        public bool RadioOn { get; private set; }
        public int CompletedCount => (VentilationOn ? 1 : 0) + (RadioOn ? 1 : 0);

        private bool HasAccess => player != null && firstGate != null && firstGate.IsOpen &&
            player.transform.position.z > .6f;

        public bool InHazard
        {
            get
            {
                if (!HasAccess || VentilationOn) return false;
                Vector3 position = player.transform.position;
                return position.x > -7.4f && position.x < -4.2f &&
                    position.z >= 5f && position.z <= 10.6f;
            }
        }

        public string InteractionHint
        {
            get
            {
                if (!CanInteract) return "";
                switch (NearbyPanel())
                {
                    case 1:
                        return VentilationOn ? "HAVALANDIRMA DEVREDE · Yan koridor güvenli" :
                            "F  HAVALANDIRMAYI AÇ · Yan koridordaki sızıntıyı temizle";
                    case 2:
                        return RadioOn ? "TELSİZ DEVREDE · Çıkış yönü aydınlatıldı" :
                            "F  TELSİZİ AÇ · Çıkış yönü ışıklarını etkinleştir";
                    default:
                        return "";
                }
            }
        }

        private bool CanInteract => HasAccess && !player.HasFocusForDialogue && !player.Finished;

        public void Configure(PlayerMotor motor, Door gate)
        {
            player = motor;
            firstGate = gate;
            VentilationOn = false;
            RadioOn = false;
            player.SetStationTasks(this);
        }

        /// <returns>True only when a nearby panel changes from off to on.</returns>
        public bool TryInteract()
        {
            if (!CanInteract) return false;
            switch (NearbyPanel())
            {
                case 1:
                    if (VentilationOn) return false;
                    VentilationOn = true;
                    return true;
                case 2:
                    if (RadioOn) return false;
                    RadioOn = true;
                    return true;
                default:
                    return false;
            }
        }

        private int NearbyPanel()
        {
            Vector3 position = player.transform.position;
            position.y = 0;
            float ventDistance = (position - VentilationPosition).sqrMagnitude;
            float radioDistance = (position - RadioPosition).sqrMagnitude;
            float rangeSquared = InteractionRange * InteractionRange;
            if (ventDistance <= rangeSquared && ventDistance <= radioDistance) return 1;
            return radioDistance <= rangeSquared ? 2 : 0;
        }
    }
}
