using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;
namespace Teams.Network
{
    [Serializable]
    public class TeamUpdateMemberRequest : GameServerBaseRequest
    {
        public long game_id;
        public int level;
        public int cups;
        public long team_id;
        public string name;
    }

    [Serializable]
    public class TeamUpdateMemberResponse : GameServerBaseResponse
    {
        public bool data;
    }
}