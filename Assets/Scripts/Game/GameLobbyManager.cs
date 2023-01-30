using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.Core.Data;
using GameFramework.Core.GameFramework.Manager;
using GameFramework.Events;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class GameLobbyManager : Singleton<GameLobbyManager>
    {
    
        private List<LobbyPlayerData> _lobbyPlayerDatas = new List<LobbyPlayerData>();
        private LobbyPlayerData _localLobbyPlayerData;
        private LobbyData _lobbyData;
        private int _maxNumberOfPlayers = 4;
        private bool _inGame;
        private bool _wasDisconnected;
        private string _previousRelayCode;

        public bool IsHost => _localLobbyPlayerData.Id == LobbyManager.Instance.GetHostId();
        
        private void OnEnable()
        {
            LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
        }
        
        private void OnDisable()
        {
            LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
        }
        
        public async Task<bool> HasActiveLobbies()
        {
            return await LobbyManager.Instance.HasActiveLobbies();
        }
    
        public string GetLobbyCode()
        {
            return LobbyManager.Instance.GetLobbyCode();
        }
        public async Task<bool> CreateLobby()
        {
            _localLobbyPlayerData = new LobbyPlayerData();
            _localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, "HostPlayer");
            _lobbyData = new LobbyData();
            _lobbyData.Initialize(0);
            
            bool succeeded = await LobbyManager.Instance.CreateLobby(_maxNumberOfPlayers, true, _localLobbyPlayerData.Serialize(), _lobbyData.Serialize());
            return succeeded;
        }
    
        public async Task<bool> JoinLobby(string code)
        {
            _localLobbyPlayerData = new LobbyPlayerData();
            _localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, "JoinPlayer");
            bool succeeded = await LobbyManager.Instance.JoinLobby(code, _localLobbyPlayerData.Serialize());
            return succeeded;
        }
        
        private async void OnLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.Instance.GetPlayersData();
            _lobbyPlayerDatas.Clear();

            int numberOfPlayerReady = 0;
            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new LobbyPlayerData();
                lobbyPlayerData.Initialize(data);

                if (lobbyPlayerData.IsReady)
                {
                    numberOfPlayerReady++;
                }

                if (lobbyPlayerData.Id == AuthenticationService.Instance.PlayerId)
                {
                    _localLobbyPlayerData = lobbyPlayerData;
                }
                
                _lobbyPlayerDatas.Add(lobbyPlayerData);
            }

            _lobbyData = new LobbyData();
            _lobbyData.Initialize(lobby.Data);
            
            Events.LobbyEvents.OnLobbyUpdated?.Invoke();

            if (numberOfPlayerReady == lobby.Players.Count)
            {
                Events.LobbyEvents.OnLobbyReady?.Invoke();
            }

            if (_lobbyData.RelayJoinCode != default && !_inGame)
            {
                if (_wasDisconnected)
                {
                    if (_lobbyData.RelayJoinCode != _previousRelayCode)
                    {
                        await JoinRelayServer(_lobbyData.RelayJoinCode);
                        SceneManager.LoadSceneAsync(_lobbyData.SceneName);
                    }
                }
                else
                {
                    await JoinRelayServer(_lobbyData.RelayJoinCode);
                    SceneManager.LoadSceneAsync(_lobbyData.SceneName);
                }
            }
        }

        public List<LobbyPlayerData> GetPlayers()
        {
            return _lobbyPlayerDatas;
        }

        public async Task<bool> SetPlayerReady()
        {
            _localLobbyPlayerData.IsReady = true;
            return await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize());
        }

        public int GetMapIndex()
        {
            return _lobbyData.MapIndex;
        }

        public async Task<bool> SetSelectedMap(int currentMapIndex, string sceneName)
        {
            _lobbyData.MapIndex = currentMapIndex;
            _lobbyData.SceneName = sceneName;
            return await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize());
        }

        public async Task StartGame()
        {
            string relayJoinCode = await RelayManager.Instance.CreateRelay(_maxNumberOfPlayers);
            _inGame = true;
            
            _lobbyData.RelayJoinCode = relayJoinCode;
            await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize());

            string allocationId = RelayManager.Instance.GetAllocationId();
            string connectionData = RelayManager.Instance.GetConnectionData();
            
            _localLobbyPlayerData.IsReady = false;
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, connectionData);

            
            SceneManager.LoadSceneAsync(_lobbyData.SceneName);
        }
        
        private async Task<bool> JoinRelayServer(string relayJoinCode)
        {
            _inGame = true;
            await RelayManager.Instance.JoinRelay(relayJoinCode);
            string allocationId = RelayManager.Instance.GetAllocationId();
            string connectionData = RelayManager.Instance.GetConnectionData();
            _localLobbyPlayerData.IsReady = false;
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, connectionData);
            return true;
        }

        public async Task<bool> RejoinGame()
        {
            return await LobbyManager.Instance.RejoinLobby();
        }

        public async Task<bool> LeaveAllLobby()
        {
            return await LobbyManager.Instance.LeaveAllLobby();
        }

        public async void GoBackToLobby(bool wasDisconnected)
        {
            _inGame = false;
            _wasDisconnected = wasDisconnected;

            if (_wasDisconnected)
            {
                _previousRelayCode = _lobbyData.RelayJoinCode;
            }

            _localLobbyPlayerData.IsReady = false;
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize());
            SceneManager.LoadSceneAsync("Lobby");
        }
    }
}

