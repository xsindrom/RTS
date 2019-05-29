using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network.GameServer;

namespace Teams.Network
{
    [Serializable]
    public class TeamsRequest : GameServerBaseRequest
    {
    }

    [Serializable]
    public class TeamsResponse: GameServerBaseResponse
    {
        public TeamData[] data;
    }
}