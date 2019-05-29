using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teams
{
    public enum TeamType : int { Opened, Closed }

    [Serializable]
    public class TeamItem
    {
        [SerializeField]
        protected long id = -1;
        [SerializeField]
        protected string name;
        [SerializeField]
        protected string description;
        [SerializeField]
        protected TeamType type;
        [SerializeField]
        protected int minLevel;
        [SerializeField]
        protected int badge;
        [SerializeField]
        protected int cups;
        [SerializeField]
        protected int membersCount;
        [SerializeField]
        protected List<long> joinRequests = new List<long>();

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public TeamType Type
        {
            get { return type; }
            set { type = value; }
        }

        public int MinLevel
        {
            get { return minLevel; }
            set { minLevel = value; }
        }

        public int Badge
        {
            get { return badge; }
            set { badge = value; }
        }

        public int Cups
        {
            get { return cups; }
            set { cups = value; }
        }

        public int MembersCount
        {
            get { return membersCount; }
            set { membersCount = value; }
        }

        public List<long> JoinRequests
        {
            get { return joinRequests; }
            set { joinRequests = value; }
        }
    }
}