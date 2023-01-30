using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace GameFramework.Network.Movement
{
    public class NetworkMovementComponent : NetworkBehaviour
    {
        [SerializeField] private CharacterController _cc;

        [SerializeField] private float _speed;
        [SerializeField] private float _turnSpeed;

        [SerializeField] private Transform _camSocket;
        [SerializeField] private GameObject _vcam;

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private Color _color;

        private Transform _vcamTransform;

        private int _tick = 0;
        private float _tickRate = 1f / 60f;
        private float _tickDeltaTime = 0f;

        private const int BUFFER_SIZE = 1024;
        private InputState[] _inputStates = new InputState[BUFFER_SIZE];
        private TransformState[] _transformStates = new TransformState[BUFFER_SIZE];

        public NetworkVariable<TransformState> ServerTransformState = new NetworkVariable<TransformState>();
        public TransformState _previousTransformState;
        
        private int _lastProcessedTick = -0;

        private void OnEnable()
        {
            ServerTransformState.OnValueChanged += OnServerStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _vcamTransform = _vcam.transform;
        }

        private void OnServerStateChanged(TransformState previousvalue, TransformState serverState)
        {
            if(!IsLocalPlayer) return;

            if (_previousTransformState == null)
            {
                _previousTransformState = serverState;
            }

            TransformState calculatedState = _transformStates.First(localState => localState.Tick == serverState.Tick);
            if (calculatedState.Position != serverState.Position)
            {
                Debug.Log("Correcting client position");
                //Teleport the player to the server position
                TeleportPlayer(serverState);
                //Replay the inputs that happened after
                IEnumerable<InputState> inputs = _inputStates.Where(input => input.Tick > serverState.Tick);
                inputs = from input in inputs orderby input.Tick select input;
                
                foreach (InputState inputState in inputs)
                {
                    MovePlayer(inputState.MovementInput);
                    RotatePlayer(inputState.LookInput);

                    TransformState newTransformState = new TransformState()
                    {
                        Tick = inputState.Tick,
                        Position = transform.position,
                        Rotation = transform.rotation,
                        HasStartedMoving = true
                    };

                    for (int i = 0; i < _transformStates.Length; i++)
                    {
                        if (_transformStates[i].Tick == inputState.Tick)
                        {
                            _transformStates[i] = newTransformState;
                            break;
                        }
                    }
                }
            }
        }

        private void TeleportPlayer(TransformState state)
        {
            _cc.enabled = false;
            transform.position = state.Position;
            transform.rotation = state.Rotation;
            _cc.enabled = true;

            for (int i = 0; i < _transformStates.Length; i++)
            {
                if (_transformStates[i].Tick == state.Tick)
                {
                    _transformStates[i] = state;
                    break;
                }
            }
        }

        public void ProcessLocalPlayerMovement(Vector2 movementInput, Vector2 lookInput)
        {
            _tickDeltaTime += Time.deltaTime;
            if (_tickDeltaTime > _tickRate)
            {
                int bufferIndex = _tick % BUFFER_SIZE;

                if (!IsServer)
                {
                    MovePlayerServerRpc(_tick, movementInput, lookInput);
                    MovePlayer(movementInput);
                    RotatePlayer(lookInput);
                    SaveState(movementInput, lookInput, bufferIndex);
                }
                else
                {
                    MovePlayer(movementInput);
                    RotatePlayer(lookInput);

                    TransformState state = new TransformState()
                    {
                        Tick = _tick,
                        Position = transform.position,
                        Rotation = transform.rotation,
                        HasStartedMoving = true
                    };

                    SaveState(movementInput, lookInput, bufferIndex);

                    _previousTransformState = ServerTransformState.Value;
                    ServerTransformState.Value = state;
                }
                
                _tickDeltaTime -= _tickRate;
                _tick++;
            }
        }

        public void ProcessSimulatedPlayerMovement()
        {
            _tickDeltaTime += Time.deltaTime;
            if (_tickDeltaTime > _tickRate)
            {
                if (ServerTransformState.Value.HasStartedMoving)
                {
                    transform.position = ServerTransformState.Value.Position;
                    transform.rotation = ServerTransformState.Value.Rotation;
                }

                _tickDeltaTime -= _tickRate;
                _tick++;
            }
        }

        private void SaveState(Vector2 movementInput, Vector2 lookInput, int bufferIndex)
        {
            InputState inputState = new InputState()
            {
                Tick = _tick,
                MovementInput = movementInput,
                LookInput = lookInput
            };

            TransformState transformState = new TransformState()
            {
                Tick = _tick,
                Position = transform.position,
                Rotation = transform.rotation,
                HasStartedMoving = true
            };

            _inputStates[bufferIndex] = inputState;
            _transformStates[bufferIndex] = transformState;
        }

        private void MovePlayer(Vector2 movementInput)
        {
            Vector3 movement = movementInput.x * _vcamTransform.right + movementInput.y * _vcamTransform.forward;

            movement.y = 0;
            if (!_cc.isGrounded)
            {
                movement.y = -9.61f;
            }

            _cc.Move(movement * _speed * _tickRate);
        }


        private void RotatePlayer(Vector2 lookInput)
        {
            _vcamTransform.RotateAround(_vcamTransform.position, _vcamTransform.right, -lookInput.y * _turnSpeed * _tickRate);
            transform.RotateAround(transform.position, transform.up, lookInput.x * _turnSpeed * _tickRate);
        }

        [ServerRpc]
        private void MovePlayerServerRpc(int tick, Vector2 movementInput, Vector2 lookInput)
        {
            if (_lastProcessedTick + 1 != tick)
            {
                Debug.Log("I missed a tick");
                Debug.Log($"Received Tick {tick}");
            }

            _lastProcessedTick = tick;
            MovePlayer(movementInput);
            RotatePlayer(lookInput);

            TransformState state = new TransformState()
            {
                Tick = tick,
                Position = transform.position,
                Rotation = transform.rotation,
                HasStartedMoving = true
            };
            

            _previousTransformState = ServerTransformState.Value;
            ServerTransformState.Value = state;
        }

        private void OnDrawGizmos()
        {
            if (ServerTransformState.Value != null)
            {
                Gizmos.color = _color;
                Gizmos.DrawMesh(_meshFilter.mesh, ServerTransformState.Value.Position);
            }
        }
    }
}