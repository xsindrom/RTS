using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Units;
using EventHandlerSystem;
using Network.PhotonNet;
using Photon.Pun;
using Photon.Realtime;
namespace Battle
{
    public interface IUnitHandler : IEventHandler
    {
        void OnUnitAdded(UnitMainAI unit);
        void OnUnitsAdded(UnitMainAI[] units);
        void OnUnitRemoved(UnitMainAI unit);
        void OnBuildingAdded(UnitMainAI building);
        void OnBuildingRemoved(UnitMainAI building);
    }

    public class UnitController : PhotonMonoSingleton<UnitController>, IPunObservable, IOnEventCallback
    {
        [SerializeField]
        protected List<UnitMainAI> units = new List<UnitMainAI>();
        public List<UnitMainAI> Units
        {
            get { return units; }
        }
        [SerializeField]
        protected List<UnitMainAI> buildings = new List<UnitMainAI>();
        public List<UnitMainAI> Buildings
        {
            get { return buildings; }
        }

        public void OnEvent(ExitGames.Client.Photon.EventData eventData)
        {
            OnBuildingCreatedCallbackInternal(eventData.Code, eventData.CustomData, eventData.Sender);
            OnUnitCreatedCallbackInternal(eventData.Code, eventData.CustomData, eventData.Sender);
            OnBuildingRemovedCallbackInternal(eventData.Code, eventData.CustomData, eventData.Sender);
            OnUnitRemovedCallbackInternal(eventData.Code, eventData.CustomData, eventData.Sender);
            OnBuildingsCreatedCallbackInternal(eventData.Code, eventData.CustomData, eventData.Sender);
            OnUnitsCreatedCallbackInternal(eventData.Code, eventData.CustomData, eventData.Sender);
        }


        public void CreateUnit(string id, int ownerId, int level, Vector3 position, Quaternion rotation)
        {
            var content = new object[] { id, ownerId, level, position, rotation };
            PhotonNetwork.RaiseEvent(PhotonEventMessages.ON_UNIT_CREATED, content, new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }, ExitGames.Client.Photon.SendOptions.SendReliable);
        }

        public void CreateUnits(string id, int ownerId, int level, int spawnCount, Vector3[] positions, Quaternion rotations)
        {
            var content = new object[] { id, ownerId,  level, spawnCount, positions, rotations };
            PhotonNetwork.RaiseEvent(PhotonEventMessages.ON_UNITS_CREATED, content, new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }, ExitGames.Client.Photon.SendOptions.SendReliable);
        }

        public void CreateBuilding(string id, int ownerId, int level, Vector3 position, Quaternion rotation)
        {
            var content = new object[] { id, ownerId, level, position, rotation };
            PhotonNetwork.RaiseEvent(PhotonEventMessages.ON_BUILDING_CREATED, content, new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }, ExitGames.Client.Photon.SendOptions.SendReliable);
        }

        public void CreateBuildings(string[] ids, int ownerId, int level, Vector3[] positions, Quaternion[] rotations)
        {
            var content = new object[] { ids, ownerId, level, positions, rotations };
            PhotonNetwork.RaiseEvent(PhotonEventMessages.ON_BUILDINGS_CREATED, content, new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }, ExitGames.Client.Photon.SendOptions.SendReliable);
        }
    
        protected void OnUnitCreatedCallbackInternal(byte eventCode, object content, int senderId)
        {
            if (eventCode == PhotonEventMessages.ON_UNIT_CREATED)
            {
                var unitContent = (object[])content;
                var unitId = (string)unitContent[0];
                var unitOwnerId = (int)unitContent[1];
                var unitLevel = (int)unitContent[2];
                var unitPosition = (Vector3)unitContent[3];
                var unitRotation = (Quaternion)unitContent[4];


                var unit = GameController.Instance.UnitDatabase.Units.Find(x => x.Id == unitId);
                if (!unit)
                    return;

                if (unit.SpawnCount == 1)
                {
                    var unitViewId = PhotonNetwork.AllocateViewID();
                    photonView.RPC("RpcCreateUnit", RpcTarget.AllBufferedViaServer, unitId, unitOwnerId,unitLevel, unitViewId, unitPosition, unitRotation);
                }
                else
                {
                    var unitViewIds = new int[unit.SpawnCount];
                    var positions = new Vector3[unit.SpawnCount];

                    for(int i =0; i < unitViewIds.Length; i++)
                    {
                        unitViewIds[i] = PhotonNetwork.AllocateViewID();
                        positions[i] = unitViewIds.Length > 1 ? unitPosition + Random.insideUnitSphere : unitPosition;
                    }
                    photonView.RPC("RpcCreateUnits", RpcTarget.AllBufferedViaServer, unitId, unitOwnerId,unitLevel, unitViewIds, positions, unitRotation);
                }
            }
        }

        protected void OnUnitsCreatedCallbackInternal(byte eventCode, object content, int senderId)
        {
            if (eventCode == PhotonEventMessages.ON_UNITS_CREATED)
            {
                var unitsContent = (object[])content;

                var unitId = (string)unitsContent[0];
                var unitsOwnerId = (int)unitsContent[1];
                var unitLevel = (int)unitsContent[2];
                var unitSpawnCount = (int)unitsContent[3];
                var positions = (Vector3[])unitsContent[4];
                var rotation = (Quaternion)unitsContent[5];

                var unitsViewIds = new int[unitSpawnCount];
                for (int i = 0; i < unitsViewIds.Length; i++)
                {
                    unitsViewIds[i] = PhotonNetwork.AllocateViewID();
                }
                photonView.RPC("RpcCreateUnits", RpcTarget.AllBufferedViaServer, unitId, unitsOwnerId,unitLevel, unitsViewIds, positions, rotation);
            }
        }

        [PunRPC]
        public void RpcCreateUnit(string id, int ownerId,int level, int viewId, Vector3 position, Quaternion rotation)
        {
            var unit = GameController.Instance.UnitDatabase.Units.Find(x => x.Id == id);
            if (unit != null)
            {
                var cloned = Instantiate(unit.UnitAI, position, rotation);
                cloned.Init(unit, level);
                cloned.photonView.ViewID = viewId;
                cloned.OwnerId = ownerId;
                cloned.OnCreate();
                units.Add(cloned);
                EventManager.Call<IUnitHandler>(x => x.OnUnitAdded(cloned));
            }
        }

        [PunRPC]
        public void RpcCreateUnits(string id, int ownerId,int level, int[] viewIds, Vector3[] positions, Quaternion rotation)
        {
            var unit = GameController.Instance.UnitDatabase.Units.Find(x => x.Id == id);
            if (unit != null)
            {
                var addedUnits = new UnitMainAI[viewIds.Length];
                for(int i = 0; i < viewIds.Length; i++)
                {
                    var viewId = viewIds[i];
                    var position = positions[i];

                    var cloned = Instantiate(unit.UnitAI, position, rotation);
                    cloned.Init(unit,level);
                    cloned.photonView.ViewID = viewId;
                    cloned.OwnerId = ownerId;
                    cloned.OnCreate();
                    units.Add(cloned);
                    addedUnits[i] = cloned;
                }

                EventManager.Call<IUnitHandler>(x => x.OnUnitsAdded(addedUnits));
            }
        }

        public void RemoveUnit(UnitMainAI unit)
        {
            PhotonNetwork.RaiseEvent(PhotonEventMessages.ON_UNIT_REMOVE, unit.photonView.ViewID, new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }, ExitGames.Client.Photon.SendOptions.SendReliable);
        }

        protected void OnUnitRemovedCallbackInternal(byte eventCode, object content, int sendedrId)
        {
            if (eventCode == PhotonEventMessages.ON_UNIT_REMOVE)
            {
                var unitViewId = (int)content;
                photonView.RPC("RpcRemoveUnit", RpcTarget.AllBufferedViaServer, unitViewId);
            }
        }

        [PunRPC]
        public void RpcRemoveUnit(int viewId)
        {
            var unitView = PhotonView.Find(viewId);
            if (unitView)
            {
                var unit = unitView.GetComponent<UnitMainAI>();
                units.Remove(unit);
                EventManager.Call<IUnitHandler>(x => x.OnUnitRemoved(unit));
                Destroy(unit.gameObject);
            }
        }
       
        protected void OnBuildingsCreatedCallbackInternal(byte eventCode, object content, int senderId)
        {
            if (eventCode == PhotonEventMessages.ON_BUILDINGS_CREATED)
            {
                var buildingContent = (object[])content;
                var buildingIds = (string[])buildingContent[0];
                var buildingOwnerIds = (int)buildingContent[1];
                var buildingLevel = (int)buildingContent[2];
                var buildingPositions = (Vector3[])buildingContent[3];
                var buildingRotations = (Quaternion[])buildingContent[4];

                var buildingViewIds = new int[buildingIds.Length];
                for(int i = 0; i < buildingIds.Length; i++)
                {
                    buildingViewIds[i] = PhotonNetwork.AllocateViewID();
                }
                photonView.RPC("RpcCreateBuildings", RpcTarget.AllBufferedViaServer, buildingIds, buildingOwnerIds,buildingLevel, buildingViewIds, buildingPositions, buildingRotations);
            }
        }

        protected void OnBuildingCreatedCallbackInternal(byte eventCode, object content, int senderId)
        {
            if (eventCode == PhotonEventMessages.ON_BUILDING_CREATED)
            {
                var buildingContent = (object[])content;
                var buildingId = (string)buildingContent[0];
                var buildingOwnerId = (int)buildingContent[1];
                var buildingLevel = (int)buildingContent[2];
                var buildingPosition = (Vector3)buildingContent[3];
                var buildingRotation = (Quaternion)buildingContent[4];
                var buildingViewId = PhotonNetwork.AllocateViewID();
                photonView.RPC("RpcCreateBuilding", RpcTarget.AllBufferedViaServer, buildingId, buildingOwnerId, buildingLevel, buildingViewId, buildingPosition, buildingRotation);
            }
        }

        [PunRPC]
        public void RpcCreateBuildings(string[] ids, int ownerId,int level, int[] viewIds, Vector3[] positions, Quaternion[] rotations)
        {
            for(int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var viewId = viewIds[i];
                var position = positions[i];
                var rotation = rotations[i];

                var building = GameController.Instance.UnitDatabase.Units.Find(x => x.Id == id);
                if (building != null)
                {
                    var cloned = Instantiate(building.UnitAI, position, rotation);
                    cloned.Init(building, level);
                    cloned.photonView.ViewID = viewId;
                    cloned.OwnerId = ownerId;
                    cloned.OnCreate();
                    buildings.Add(cloned);
                    EventManager.Call<IUnitHandler>(x => x.OnBuildingAdded(cloned));
                }
            }
        }

        [PunRPC]
        public void RpcCreateBuilding(string id, int ownerId,int level, int viewId, Vector3 position, Quaternion rotation)
        {
            var building = GameController.Instance.UnitDatabase.Units.Find(x => x.Id == id);
            if (building != null)
            {
                var cloned = Instantiate(building.UnitAI, position, rotation);
                cloned.Init(building, level);
                cloned.photonView.ViewID = viewId;
                cloned.OwnerId = ownerId;
                cloned.OnCreate();
                buildings.Add(cloned);
                EventManager.Call<IUnitHandler>(x => x.OnBuildingAdded(cloned));
            }
        }

        public void RemoveBuilding(UnitMainAI building)
        {
            PhotonNetwork.RaiseEvent(PhotonEventMessages.ON_BUILDING_REMOVE, building.photonView.ViewID, new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }, ExitGames.Client.Photon.SendOptions.SendReliable);
        }

        protected void OnBuildingRemovedCallbackInternal(byte eventCode, object content, int senderId)
        {
            if (eventCode == PhotonEventMessages.ON_BUILDING_REMOVE)
            {
                var buildingViewId = (int)content;
                photonView.RPC("RpcRemoveBuilding", RpcTarget.AllBufferedViaServer, buildingViewId);
            }
        }

        [PunRPC]
        public void RpcRemoveBuilding(int viewId)
        {
            var buildingView = PhotonView.Find(viewId);
            if (buildingView)
            {
                var building = buildingView.GetComponent<UnitMainAI>();
                buildings.Remove(building);
                EventManager.Call<IUnitHandler>(x => x.OnBuildingRemoved(building));
                Destroy(building.gameObject);
            }
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            if (photonView.IsMine)
            {
                for (int i = 0; i < buildings.Count; i++)
                {
                    var building = buildings[i];
                    building.photonView.TransferOwnership(newMasterClient);
                }

                for (int i = 0; i < units.Count; i++)
                {
                    var unit = units[i];
                    unit.photonView.TransferOwnership(newMasterClient);
                }
            }
        }

        public override void OnDisconnected(DisconnectCause disconnectCause)
        {
            units.Clear();
            buildings.Clear();
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                int[] unitViews = new int[units.Count];
                int[] buildingViews = new int[buildings.Count];
                for (int i = 0; i < units.Count; i++)
                {
                    unitViews[i] = units[i].photonView.ViewID;
                }
                for (int i = 0; i < buildings.Count; i++)
                {
                    buildingViews[i] = buildings[i].photonView.ViewID;
                }
                stream.SendNext(unitViews);
                stream.SendNext(buildingViews);
            }
            else
            {
                units.Clear();
                buildings.Clear();

                var unitObj = stream.ReceiveNext();
                var buildingObj = stream.ReceiveNext();

                int[] unitViews = unitObj as int[];
                int[] buildingViews = buildingObj as int[];

                for (int i = 0; i < unitViews.Length; i++)
                {
                    var unitView = PhotonView.Find(unitViews[i]);
                    if (unitView)
                    {
                        var unit = unitView.GetComponent<UnitMainAI>();
                        units.Add(unit);
                    }
                }

                for (int i = 0; i < buildingViews.Length; i++)
                {
                    var buildingView = PhotonView.Find(buildingViews[i]);
                    if (buildingView)
                    {
                        var building = buildingView.GetComponent<UnitMainAI>();
                        buildings.Add(building);
                    }
                }
            }
        }
    }
}