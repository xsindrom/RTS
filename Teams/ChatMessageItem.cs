using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teams
{
    [Serializable]
    public class ChatMessageItem
    {
        [SerializeField]
        private long id;
        [SerializeField]
        private long teamId;
        [SerializeField]
        private long gameId;
        [SerializeField]
        private string type;
        [SerializeField]
        private string text;
        [SerializeField]
        private long time;

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public long TeamId
        {
            get { return teamId; }
            set { teamId = value; }
        }

        public long GameId
        {
            get { return gameId; }
            set { gameId = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public long Time
        {
            get { return time; }
            set { time = value; }
        }
    }
}