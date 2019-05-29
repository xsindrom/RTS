using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teams.Network
{
    [Serializable]
    public class ChatMessageData
    {
        public long message_id;
        public string text;
        public string type;
        public long time;
        public long team_id;
        public long game_id;
    }
}