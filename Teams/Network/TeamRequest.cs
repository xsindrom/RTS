using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;

namespace Teams.Network
{
    [Serializable]
    public class TeamRequest : GameServerBaseRequest
    {
        public long team_id;
    }

    [Serializable]
    public class TeamResponse : GameServerBaseResponse
    {
        public TeamData data;
    }
}