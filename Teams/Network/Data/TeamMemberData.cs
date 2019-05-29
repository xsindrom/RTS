using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teams.Network
{
    [Serializable]
    public class TeamMemberData
    {
        public long game_id = -1;
        public long team_id = -1;
        public string name;
        public int rank;
        public int level;
        public int cups;
        public int donates;
    }
}