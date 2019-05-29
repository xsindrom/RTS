using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Photon;
using Photon.Pun;
using Battle;
using EventHandlerSystem;
using Photon.Realtime;
using Network.PhotonNet;

namespace Units
{
    public class UnitMainAI :  MonoBehaviourPunCallbacks, IPunObservable, IUnitHandler, IGameHandler
    {
        public const float MIN_DISTANCE = 1000f;

        public int OwnerId;
        [SerializeField]
        protected Transform infoAttachPosition;
        [SerializeField]
        protected Transform highlightAttachPosition;
        [SerializeField]
        protected Animator animator;
        [SerializeField]
        protected Unit source;
        [SerializeField]
        protected int level;
        [SerializeField]
        protected bool wasCreated;
        [SerializeField]
        protected List<UnitSubAI> subAIs = new List<UnitSubAI>();
        protected List<UnitMainAI> enemiesUnits = new List<UnitMainAI>();
        protected List<UnitMainAI> friendsUnits = new List<UnitMainAI>();

        public Transform InfoAttachPosition
        {
            get { return infoAttachPosition; }
        }
        public Transform HighlightAttachPosition
        {
            get { return highlightAttachPosition; }
        }
        public Animator Animator
        {
            get { return animator; }
        }
        public bool WasCreated
        {
            get { return wasCreated; }
        }
        public Unit Source
        {
            get { return source; }
            set { source = value; }
        }
        public int Level
        {
            get { return level; }
            set { level = value; }
        }
        public List<UnitMainAI> EnemiesUnits
        {
            get { return enemiesUnits; }
        }
        public List<UnitMainAI> FriendsUnits
        {
            get { return friendsUnits; }
        }
        
        public ChValueFloat Haste = new ChValueFloat() { Value = 1 };
        public ChValueBool IsActivated = new ChValueBool();
        private Timer.TimerData spawnTimer;

        public virtual void Init(Unit source, int level)
        {
            this.source = source;
            this.level = level;

            var unitAnimatorResources = ResourceManager.GetResource<UnitAnimatorResources>("UnitAnimatorResources");
            var resource = unitAnimatorResources.Resources.Find(x => x.id == Source.Id);
            if (resource != null)
            {
                animator.runtimeAnimatorController = resource.animatorController;
            }

            subAIs.Clear();
            subAIs.AddRange(GetComponents<UnitSubAI>());
            for (int i = 0; i < subAIs.Count; i++)
            {
                var subAI = subAIs[i];
                subAI.Init(source, this);
            }
            EventManager.Add(this);
        }

        protected void FillUnits(List<UnitMainAI> inUnits)
        {
            for(int i = 0; i < inUnits.Count; i++)
            {
                var inUnit = inUnits[i];
                if(inUnit.OwnerId != OwnerId)
                {
                    enemiesUnits.Add(inUnit);
                }
                else
                {
                    friendsUnits.Add(inUnit);
                }
            }
        }

        public virtual void OnCreate()
        {
            if (photonView.IsMine)
            {
                spawnTimer = new Timer.TimerData(source.SpawnTime, Activate);
               
                Timer.Instance.AddTimer(spawnTimer);
                wasCreated = true;
            }
        }

        private void Activate()
        {
            photonView.RPC("RpcActivate", RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void RpcActivate()
        {
            FillUnits(UnitController.Instance.Units);
            FillUnits(UnitController.Instance.Buildings);

            Timer.Instance.RemoveTimer(spawnTimer);
            for(int i = 0; i < subAIs.Count; i++)
            {
                var subAI = subAIs[i];
                subAI.OnCreate();
            }
            IsActivated.Value = true;
        }

        protected virtual void OnDestroy()
        {
            Timer.Instance.RemoveTimer(spawnTimer);
            EventManager.Remove(this);
        }
       
        public T GetAI<T>() where T: UnitSubAI
        {
            var subAI = subAIs.Find(x => x is T);
            return subAI as T;
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(wasCreated);
            }
            else
            {
                wasCreated = (bool)stream.ReceiveNext();
            }
        }

        protected virtual void Update()
        {
        }

        public virtual void OnUnitAdded(UnitMainAI unit)
        {
            if (unit == this)
                return;

            if (unit.OwnerId == OwnerId)
            {
                friendsUnits.Add(unit);
            }
            else
            {
                enemiesUnits.Add(unit);
            }
        }

        public virtual void OnUnitsAdded(UnitMainAI[] units)
        {
            if (units == null)
                return;

            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                OnUnitAdded(unit);
            }
        }

        public virtual void OnUnitRemoved(UnitMainAI unit)
        {
            if (unit == this)
                return;

            if (unit.OwnerId == OwnerId)
            {
                friendsUnits.Remove(unit);
            }
            else
            {
                enemiesUnits.Remove(unit);
            }
        }

        public virtual void OnBuildingAdded(UnitMainAI building)
        {
            OnUnitAdded(building);
        }

        public virtual void OnBuildingRemoved(UnitMainAI building)
        {
            OnUnitRemoved(building);
        }

        public virtual void OnSubAILoseControll(UnitSubAI caller)
        {
            for(int i = 0; i < subAIs.Count; i++)
            {
                if(subAIs[i] != caller)
                {
                    subAIs[i].OnLoseControll();
                }
            }
        }

        public virtual void OnSubAIGetControll(UnitSubAI caller)
        {
            for (int i = 0; i < subAIs.Count; i++)
            {
                if (subAIs[i] != caller)
                {
                    subAIs[i].OnGetControll();
                }
            }
        }

        public virtual void Death()
        {
            for(int i = 0; i < subAIs.Count; i++)
            {
                subAIs[i].OnDeath();
            }
        }

        protected virtual void Start()
        {
            photonView.TransferOwnership(PhotonNetwork.MasterClient);
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            photonView.TransferOwnership(newMasterClient);
        }

        public void OnGameStarted()
        {
        }

        public void OnGameFinished()
        {
            enabled = false;
            for (int i = 0; i < subAIs.Count; i++)
            {
                subAIs[i].enabled = false;
            }
        }

        public void OnDisconnect()
        {
        }

        public void OnReconnect()
        {
        }

        public void OnLeaveRoom()
        {
        }
    }
}