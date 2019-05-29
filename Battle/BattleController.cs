using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using EventHandlerSystem;
using Units;
using Network.PhotonNet;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace Battle
{
    public class BattleController : PhotonMonoSingleton<BattleController>, IPunObservable, IUnitHandler
    {
        protected BattleSettings battleSettings;
        public BattleSettings BattleSettings
        {
            get { return battleSettings; }
        }

        public ChValueInt Time = new ChValueInt();
        public ChValueInt MaxGameTime = new ChValueInt();
        public ChValueBool IsFinish = new ChValueBool();
        public ChValueInt CurrentPhase = new ChValueInt();
        public ChValueInt CurrentEnergyMultipler = new ChValueInt() { Value = 1 };

        public bool LocalWinner { get; set; }
        public bool IsDraw { get; set; }

        public Timer.TimerData timerUpdater;
        public Timer.TimerData energyUpdater;

        public DictionaryStats syncStats = new DictionaryStats();
        public event Action<int> OnSyncStatReady;

        protected override void Init()
        {
            base.Init();
            AssetBundlesLoader.SubscribeOrGetOnBundlesLoaded(this, OnBundleLoaded, Constants.SETTINGS_BUNDLE);
        }

        protected void OnBundleLoaded(string[] bundles)
        {
            battleSettings = AssetBundlesLoader.Instance.GetAsset<BattleSettings>(Constants.SETTINGS_BUNDLE, Constants.ASSET_BATTLE_SETTINGS);
        }

        protected override void OnDestroy()
        {
            if (Timer.HasInstance)
            {
                Timer.Instance.RemoveTimer(timerUpdater);
                Timer.Instance.RemoveTimer(energyUpdater);
            }
            EventManager.Remove(this);
            AssetBundlesLoader.UnSubscribeOnBundlesLoaded(this);
            base.OnDestroy();
        }

        public void ClearStats()
        {
            IsFinish = new ChValueBool();
            Time = new ChValueInt();
            MaxGameTime = new ChValueInt();
            CurrentPhase = new ChValueInt();
            CurrentEnergyMultipler = new ChValueInt() { Value = 1 };
            LocalWinner = false;
            IsDraw = false;
            syncStats.Clear();
        }

        public void OnGameStarted()
        {
            ClearStats();

            for (int i = 0; i < Player.Players.Count; i++)
            {
                var player = Player.Players[i];
                if (!syncStats.ContainsKey(player.photonView.ViewID))
                {
                    syncStats.Add(player.photonView.ViewID, new PlayerStats());
                    EventHelper.SafeCall(OnSyncStatReady, player.photonView.ViewID);
                }
            }

            if (PhotonNetwork.IsMasterClient)
            {
                energyUpdater = new Timer.TimerData(0.1f, UpdateEnergy);
                timerUpdater = new Timer.TimerData(1.0f, UpdateTime);
                
                Timer.Instance.AddTimer(timerUpdater);
                Timer.Instance.AddTimer(energyUpdater);

                for (int i = 0; i < Player.Players.Count; i++)
                {
                    var player = Player.Players[i];
                    syncStats[player.photonView.ViewID].Energy.Value = battleSettings.StartEnergy;
                    syncStats[player.photonView.ViewID].EnergyFloat.Value = player.Stats.Energy.Value;
                    syncStats[player.photonView.ViewID].Lives.Value = battleSettings.StartLives;
                    player.UnitSpawnController.SpawnAll();
                }
                SetUserStats();
            }
            Time.OnValueChanged += OnChangePhase;
            IsFinish.OnValueChanged += FinishGame;
            battleSettings.phases[CurrentPhase.Value].StartPhase();
            EventManager.Call<IGameHandler>(x => x.OnGameStarted());

            if (photonView.IsMine)
            {
                PhotonLobbyManager.Instance.ChangeIsStarted(true);
            }
        }

        public void FinishGame(bool isFinish)
        {
            if (!photonView.IsMine)
                return;

            PhotonLobbyManager.Instance.ChangeIsFinish(true);
            photonView.RPC("RPCFinishGame", RpcTarget.AllBufferedViaServer, isFinish);
        }
        [PunRPC]
        protected void RPCFinishGame(bool isFinish)
        {
            if (isFinish)
            {
                EventManager.Call<IGameHandler>(x => x.OnGameFinished());
            }
        }

        public void SetUserStats()
        {
            photonView.RPC("RPCSetUserStats", RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void RPCSetUserStats()
        {
            var name = GameController.Instance.StorageController.Data.UserData.Name.Value;
            var cups = GameController.Instance.CurrencyController.GetCurrency(Currencies.CurrencyType.Cups);
            var playerId = Player.LocalPlayer.photonView.ViewID;

            if (photonView.IsMine)
            {
                RPCSetUserStatsToMaster(playerId, name, cups);
            }
            else
            {
                photonView.RPC("RPCSetUserStatsToMaster", RpcTarget.MasterClient, playerId, name, cups);
            }
        }

        [PunRPC]
        public void RPCSetUserStatsToMaster(int playerId, string name, int cups)
        {
            syncStats[playerId].Cups.Value = cups;
            syncStats[playerId].Name.Value = name;
        }

        public void OnReconnect()
        {
            ClearStats();
            
            for (int i = 0; i < Player.Players.Count; i++)
            {
                var player = Player.Players[i];
                if (!syncStats.ContainsKey(player.photonView.ViewID))
                {
                    syncStats.Add(player.photonView.ViewID, new PlayerStats());
                    EventHelper.SafeCall(OnSyncStatReady, player.photonView.ViewID);
                }
            }
            Time.OnValueChanged += OnChangePhase;
            IsFinish.OnValueChanged += FinishGame;
            EventManager.Call<IGameHandler>(x => x.OnReconnect());
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            if (photonView.IsMine)
            {
                energyUpdater = new Timer.TimerData(0.1f, UpdateEnergy);
                timerUpdater = new Timer.TimerData(1.0f, UpdateTime);
                Timer.Instance.AddTimer(timerUpdater);
                Timer.Instance.AddTimer(energyUpdater);
            }
        }

        public override void OnDisconnected(DisconnectCause disconnectCause)
        {
            ClearStats();

            Timer.Instance.RemoveTimer(energyUpdater);
            Timer.Instance.RemoveTimer(timerUpdater);

            if (SceneLoader.Instance.CurrentScene.StartsWith(SceneConstants.BATTLE_SCENE))
            {
                EventManager.Call<IGameHandler>(x => x.OnDisconnect());
                SceneLoader.Instance.LoadSceneAsync(SceneConstants.MAIN_SCENE);
            }
            else
            {
                EventManager.Call<IGameHandler>(x => x.OnLeaveRoom());
            }
        }

        public void UpdateTime()
        {
            Time.Value++;
        }

        public void OnChangePhase(int time)
        {
            if (CurrentPhase.Value >= battleSettings.phases.Count)
            {
                IsFinish.Value = true;
                Timer.Instance.RemoveTimer(timerUpdater);
                Timer.Instance.RemoveTimer(energyUpdater);
                return;
            }

            if (battleSettings.phases[CurrentPhase.Value].IsCompleted(time))
            {
                battleSettings.phases[CurrentPhase.Value].FinishPhase();

                CurrentPhase.Value++;

                while (CurrentPhase.Value < battleSettings.phases.Count && !battleSettings.phases[CurrentPhase.Value].IsAvailablePhase())
                {
                    CurrentPhase.Value++;
                }

                if (CurrentPhase.Value < battleSettings.phases.Count)
                {
                    battleSettings.phases[CurrentPhase.Value].StartPhase();
                }
            }
        }

        public void UpdateEnergy()
        {
            foreach (var statsPair in syncStats)
            {
                var stats = statsPair.Value;
                if (stats.EnergyFloat.Value < battleSettings.MaxEnergy)
                {
                    stats.EnergyFloat.Value += energyUpdater.WaitTime * CurrentEnergyMultipler.Value;
                    var roundedEnergy = Mathf.RoundToInt(stats.EnergyFloat.Value * 10);
                    if (roundedEnergy % 10 == 0)
                    {
                        stats.Energy.Value = roundedEnergy / 10;
                    }
                }
            }
        }

        public void RemoveLives(int ownerId, int count)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                var player = Player.Players.Find(x => x.OwnerId == ownerId);
                if (player && !IsFinish.Value)
                {
                    var stats = syncStats[player.photonView.ViewID];
                    stats.Lives.Value = count > stats.Lives.Value ? 0 : stats.Lives.Value - count;
                    if (stats.Lives.Value == 0)
                    {
                        IsFinish.Value = true;
                    }
                }
            }
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey("players"))
            {
                var playersProp = (string)propertiesThatChanged["players"];
                var playerData = PlayerInfo.FromJson(playersProp);
                var readyPlayers = playerData.Players.FindAll(x => x.r);

                if(readyPlayers.Count == PhotonLobbyManager.Instance.roomSize)
                {
                    if (PhotonLobbyManager.Instance.IsAfterReconnect)
                    {
                        OnReconnect();
                        PhotonLobbyManager.Instance.IsAfterReconnect = false;
                    }
                    else
                    {
                        bool isStarted = (bool)PhotonNetwork.CurrentRoom.CustomProperties["isStarted"];
                        if (!isStarted)
                        {
                            OnGameStarted();
                        }
                    }
                }
            }
        }

        public void OnBuildingAdded(UnitMainAI building)
        {

        }

        public void OnBuildingRemoved(UnitMainAI building)
        {
        }
     
        public void OnUnitAdded(UnitMainAI unit)
        {
            var player = Player.Players.Find(x => x.OwnerId == unit.OwnerId);
            if (player && !IsFinish.Value && syncStats.ContainsKey(player.photonView.ViewID))
            {
                var stats = syncStats[player.photonView.ViewID];
                stats.Energy.Value -= unit.Source.EnergyCost;
                stats.EnergyFloat.Value -= unit.Source.EnergyCost;
            }
        }

        public void OnUnitsAdded(UnitMainAI[] units)
        {
            if (units == null || units.Length == 0)
                return;

            var unit = units[0];
            OnUnitAdded(unit);
        }

        public void OnUnitRemoved(UnitMainAI unit)
        {
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(Time.Value);
                stream.SendNext(MaxGameTime.Value);
                stream.SendNext(CurrentPhase.Value);
                stream.SendNext(CurrentEnergyMultipler.Value);
                foreach (var statsPair in syncStats)
                {
                    var stats = statsPair.Value;
                    stream.SendNext(stats.Name.Value);
                    stream.SendNext(stats.Cups.Value);
                    stream.SendNext(stats.Energy.Value);
                    stream.SendNext(stats.EnergyFloat.Value);
                    stream.SendNext(stats.Lives.Value);
                }
                stream.SendNext(IsFinish.Value);
            }
            else
            {
                Time.Value = (int)stream.ReceiveNext();
                MaxGameTime.Value = (int)stream.ReceiveNext();
                CurrentPhase.Value = (int)stream.ReceiveNext();
                CurrentEnergyMultipler.Value = (int)stream.ReceiveNext();
                foreach (var statsPair in syncStats)
                {
                    var stats = statsPair.Value;
                    stats.Name.Value = (string)stream.ReceiveNext();
                    stats.Cups.Value = (int)stream.ReceiveNext();
                    stats.Energy.Value = (int)stream.ReceiveNext();
                    stats.EnergyFloat.Value = (float)stream.ReceiveNext();
                    stats.Lives.Value = (int)stream.ReceiveNext();
                }
                var isFinish = stream.ReceiveNext();
                if (syncStats.Count == Player.Players.Count)
                    IsFinish.Value = (bool)isFinish;
            }
        }

        private void OnGUI()
        {
            if (PhotonLobbyManager.Instance.enableGUI)
            {
                GUI.Label(new Rect(0, 100, 100, 100), "BattleController: " + (photonView.IsMine ? "Mine" : "NotMine"));
            }
        }
    }
}