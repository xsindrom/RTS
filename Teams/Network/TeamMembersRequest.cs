using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;

namespace Teams.Network
{
    [Serializable]
    public class TeamMembersRequest : GameServerBaseRequest
    {
        public long team_id;
    }

    [Serializable]
    public class TeamMembersResponse : GameServerBaseResponse
    {
        public TeamMemberData[] data;
    }
}