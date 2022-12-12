using Unity.Netcode;

namespace Game
{
    public class ButtonDoor : NetworkBehaviour
    {
        public delegate void ButtonPressed(ButtonDoor buttonDoor);
        public event ButtonPressed OnButtonPressed;

        public void Activate()
        {
            if (IsServer)
            {
                OnButtonPressed?.Invoke(this);
            }
        }
    }
}