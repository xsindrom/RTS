using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teams.UI;
namespace Teams
{
    [CreateAssetMenu(menuName ="Teams/TeamsSettings")]
    public class TeamsSettings : ScriptableObject
    {
        [SerializeField]
        private int teamMembersMaxCount;

        public int TeamMembersMaxCount
        {
            get { return teamMembersMaxCount; }
        }

    }
}