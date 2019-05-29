using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;
namespace Teams.Network
{
    [Serializable]
    public class TeamKickRequest : GameServerBaseRequest
    {
        public long game_id;
        public long team_id;
        public int rank;
    }

    [Serializable]
    public class TeamKickResponse: GameServerBaseResponse
    {
        public bool data;
    }
}