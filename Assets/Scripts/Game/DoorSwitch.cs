using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class DoorSwitch : Door
    {
        [SerializeField] private List<Switch> _switches;
        
        private Dictionary<Switch, bool> _activeSwitches = new Dictionary<Switch, bool>();
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                foreach (Switch doorSwitch in _switches)
                {
                    doorSwitch.OnSwitchChanged += OnSwitchChanged;
                    _activeSwitches.Add(doorSwitch, false);
                }
            }
        }
        
        private void OnSwitchChanged(Switch doorswitch, bool isactive)
        {
            _activeSwitches[doorswitch] = isactive;

            foreach (var doorSwitch in _switches)
            {
                if (!_activeSwitches[doorSwitch])
                {
                    return;
                }
            }
            
            Debug.Log("Open the door");
            _animCtrl.SetTrigger("OpenDoor");
        }
    }
}