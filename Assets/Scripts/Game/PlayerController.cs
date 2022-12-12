using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Game;
using GameFramework.Network.Movement;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Vector2 _minMaxRotationX;
    [SerializeField] private Transform _camTransform;
    [SerializeField] private NetworkMovementComponent _playerMovement;
    [SerializeField] private float _interactDistance;
    [SerializeField] private LayerMask _interactionLayer;

    private CharacterController _cc;
    private PlayerControl _playerControl;
    private float _cameraAngle;

    public override void OnNetworkSpawn()
    {
        CinemachineVirtualCamera cvm = _camTransform.gameObject.GetComponent<CinemachineVirtualCamera>();

        if (IsOwner)
        {
            cvm.Priority = 1;
        }
        else
        {
            cvm.Priority = 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _cc = GetComponent<CharacterController>();

        _playerControl = new PlayerControl();
        _playerControl.Enable();

        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 movementInput = _playerControl.Player.Move.ReadValue<Vector2>();
        Vector2 lookInput = _playerControl.Player.Look.ReadValue<Vector2>();
        
        if (IsClient && IsLocalPlayer)
        {
            _playerMovement.ProcessLocalPlayerMovement(movementInput, lookInput);
        } else
        {
            _playerMovement.ProcessSimulatedPlayerMovement();
        }


        if (IsLocalPlayer && _playerControl.Player.Interact.inProgress)
        {
            if (Physics.Raycast(_camTransform.position, _camTransform.forward, out RaycastHit hit, _interactDistance, _interactionLayer))
            {
                if (hit.collider.TryGetComponent<ButtonDoor>(out ButtonDoor buttonDoor))
                {
                    UseButtonServerRpc();
                }
            }
        }
    }

    [ServerRpc]
    private void UseButtonServerRpc()
    {
        if (Physics.Raycast(_camTransform.position, _camTransform.forward, out RaycastHit hit, _interactDistance, _interactionLayer))
        {
            if (hit.collider.TryGetComponent<ButtonDoor>(out ButtonDoor buttonDoor))
            {
                buttonDoor.Activate();
            }
        }
    }

    /*private void RotateCamera(Vector2 lookInput)
    {
        _cameraAngle = Vector3.SignedAngle(transform.forward, _camTransform.forward, _camTransform.right);
        float cameraRotationAmount = lookInput.y * _turnSpeed * Time.deltaTime;
        float newCameraAngle = _cameraAngle - cameraRotationAmount;
        if (newCameraAngle <= _minMaxRotationX.x && newCameraAngle >= _minMaxRotationX.y)
        {
            _camTransform.RotateAround(_camTransform.position, _camTransform.right, -lookInput.y * _turnSpeed * Time.deltaTime);
        }
    }*/
}
