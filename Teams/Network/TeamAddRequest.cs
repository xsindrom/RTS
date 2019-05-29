using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;
namespace Teams.Network
{
    [Serializable]
    public class TeamAddRequest : GameServerBaseRequest
    {
        public long game_id;
        public long team_id;
    }

    [Serializable]
    public class TeamAddResponse: GameServerBaseResponse
    {
        public bool data;
    }
}