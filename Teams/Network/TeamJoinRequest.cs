using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;
namespace Teams.Network
{
    [Serializable]
    public class TeamJoinRequest : GameServerBaseRequest
    {
        public long game_id;
        public long team_id;
    }

    [Serializable]
    public class TeamJoinResponse : GameServerBaseResponse
    {
        public bool data;
    }

}