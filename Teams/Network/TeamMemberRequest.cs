using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;

namespace Teams.Network
{
    [Serializable]
    public class TeamMemberRequest : GameServerBaseRequest
    {
        public long game_id;
    }

    [Serializable]
    public class TeamMemberResponse : GameServerBaseResponse
    {
        public TeamMemberData data;
    }
}