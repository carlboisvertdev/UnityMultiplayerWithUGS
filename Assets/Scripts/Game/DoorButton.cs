using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class DoorButton : Door
    {
        [SerializeField] private List<ButtonDoor> _buttons;
        [SerializeField] private float _timeBetweenButtonPressed;
        
        private Dictionary<ButtonDoor, bool> _activeButtons = new Dictionary<ButtonDoor, bool>();
        
        private float _lastButtonPressed = 0;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            foreach (ButtonDoor buttonDoor in _buttons)
            {
                buttonDoor.OnButtonPressed += OnButtonPressed;
                _activeButtons.Add(buttonDoor, false);
            }
        }

        private void OnButtonPressed(ButtonDoor buttonDoor)
        {
            if (_buttons.Contains(buttonDoor))
            {
                _activeButtons[buttonDoor] = true;
                int _numberOfButtonPressed = CountActivatedButtons();
                
                if (_numberOfButtonPressed == 1)
                {
                    Debug.Log($"First button pressed");
                    _lastButtonPressed = Time.time;

                    if (_numberOfButtonPressed == _buttons.Count)
                    {
                        _animCtrl.SetTrigger("OpenDoor");
                    }
                }
                else
                {
                    if (_lastButtonPressed + _timeBetweenButtonPressed >= Time.time)
                    {
                        if (_numberOfButtonPressed == _buttons.Count)
                        {
                            _animCtrl.SetTrigger("OpenDoor");
                        }
                    }
                    else
                    {
                        ResetButtons();
                        _activeButtons[buttonDoor] = true;
                        _lastButtonPressed = Time.time;
                        Debug.Log($"Reset button");
                    }
                }
                Debug.Log($"Number of button pressed: {_numberOfButtonPressed}");
            }
        }
        
        private int CountActivatedButtons()
        {
            int _numberOfButtonPressed = 0;
            foreach (KeyValuePair<ButtonDoor, bool> button in _activeButtons)
            {
                _numberOfButtonPressed = button.Value ? _numberOfButtonPressed + 1 : _numberOfButtonPressed;
            }

            return _numberOfButtonPressed;
        }

        private void ResetButtons()
        {
            foreach (ButtonDoor button in _buttons)
            {
                _activeButtons[button] = false;
            }
        }
    }
}