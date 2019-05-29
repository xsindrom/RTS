using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace Units.PureECS
{
    [Serializable]
    public struct PhotonSyncTransform : IComponentData
    {
        public float3 networkPosition;
        public quaternion networkRotation;
        public float3 direction;
        public double infoTime;
    }

    public class PhotonSyncTransformProxy : ComponentDataProxy<PhotonSyncTransform>, IPunObservable, IInRoomCallbacks
    {
        private float m_Distance;
        private float m_Angle;

        private UnitMainAI m_UnitMainAI;
        private PhotonView m_PhotonView;
        private Vector3 m_Direction;
        private Vector3 m_NetworkPosition;
        private Vector3 m_StoredPosition;

        private Quaternion m_NetworkRotation;

        public bool m_SynchronizePosition = true;
        public bool m_SynchronizeRotation = true;

        public void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            m_PhotonView = GetComponent<PhotonView>();
            m_UnitMainAI = GetComponent<UnitMainAI>();
            m_StoredPosition = transform.position;
            m_NetworkPosition = Vector3.zero;
            m_NetworkRotation = Quaternion.identity;

            
            PhotonNetwork.AddCallbackTarget(this);
        }
        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            if (!m_PhotonView.IsMine && m_UnitMainAI.enabled)
            {
                var entity = GetComponent<GameObjectEntity>();
                entity.EntityManager.AddComponentData(entity.Entity, new Position() { Value = transform.position });
                entity.EntityManager.AddComponentData(entity.Entity, new Rotation() { Value = transform.rotation });
                entity.EntityManager.AddComponentData(entity.Entity, new CopyTransformToGameObject());

                Value = new PhotonSyncTransform()
                {
                    networkPosition = transform.position,
                    networkRotation = transform.rotation,
                    direction = Vector3.zero,
                    infoTime = PhotonNetwork.Time
                };
            }
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            if (m_PhotonView.IsMine && m_UnitMainAI.enabled)
            {
                var entity = GetComponent<GameObjectEntity>();
                entity.EntityManager.RemoveComponent(entity.Entity, typeof(Position));
                entity.EntityManager.RemoveComponent(entity.Entity, typeof(Rotation));
                entity.EntityManager.RemoveComponent(entity.Entity, typeof(CopyTransformToGameObject));
                entity.enabled = false;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                if (this.m_SynchronizePosition)
                {
                    this.m_Direction = transform.position - this.m_StoredPosition;
                    this.m_StoredPosition = transform.position;

                    stream.SendNext(transform.position);
                    stream.SendNext(this.m_Direction);
                }

                if (this.m_SynchronizeRotation)
                {
                    stream.SendNext(transform.rotation);
                }
            }
            else
            {
                if (this.m_SynchronizePosition)
                {
                    this.m_NetworkPosition = (Vector3)stream.ReceiveNext();
                    this.m_Direction = (Vector3)stream.ReceiveNext();
                }

                if (this.m_SynchronizeRotation)
                {
                    this.m_NetworkRotation = (Quaternion)stream.ReceiveNext();
                }

                Value = new PhotonSyncTransform()
                {
                    networkPosition = m_NetworkPosition,
                    networkRotation = m_NetworkRotation,
                    direction = m_Direction,
                    infoTime = info.SentServerTime
                };
            }
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
        }

        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
        }
    }
}