using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
namespace Units
{
    public abstract class UnitSubAI : MonoBehaviourPunCallbacks
    {
        [SerializeField]
        protected UnitMainAI mainAI;
        [SerializeField]
        protected Unit source;

        public virtual void Init(Unit source, UnitMainAI mainAI)
        {
            this.source = source;
            this.mainAI = mainAI;
        }

        public virtual void OnCreate()
        {

        }

        public virtual void OnDeath()
        {

        }

        public virtual void OnLoseControll()
        {

        }

        public virtual void OnGetControll()
        {

        }
    }
}