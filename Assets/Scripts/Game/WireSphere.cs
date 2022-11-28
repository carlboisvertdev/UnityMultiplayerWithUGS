using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class WireSphere : MonoBehaviour
    {
        [SerializeField] private Color _color;
        [SerializeField] private float _size;
        private void OnDrawGizmos()
        {
            Gizmos.color = _color;
            Gizmos.DrawWireSphere(transform.position, _size);
        }
    }
}

