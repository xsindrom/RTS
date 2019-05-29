using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Teams.Network
{
    [Serializable]
    public class TeamData
    {
        public long team_id = -1;
        public string team_name;
        public string team_desc;
        public int type;
        public int min_level;
        public int badge;
        public int cups;
        public int members_count;
        public string join_requests;
    }
}