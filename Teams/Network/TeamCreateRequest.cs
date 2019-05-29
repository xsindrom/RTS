using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;
namespace Teams.Network
{
    [Serializable]
    public class TeamCreateRequest : GameServerBaseRequest
    {
        public long game_id;
        public string team_name;
        public string team_desc;
        public int type;
        public int min_level;
        public int badge;
    }

    [Serializable]
    public class TeamCreateResponse : GameServerBaseResponse
    {
        public TeamData data;
    }
}