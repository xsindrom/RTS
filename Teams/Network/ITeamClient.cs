using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teams.Network
{
    public interface ITeamClient
    {
        void Connect();
        string ReadMessage();
        void Emit(string eventType, params string[] eventParams);
        void Close();
    }
}