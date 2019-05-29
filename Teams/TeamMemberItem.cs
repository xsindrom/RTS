using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teams
{
    public enum TeamRank : int
    {
        Member = 0,
        Leader = 1
    }

    [Serializable]
    public class TeamMemberItem : IComparable<TeamMemberItem>
    {
        [SerializeField]
        protected long gameId;
        [SerializeField]
        protected long teamId = -1;
        [SerializeField]
        protected string name;
        [SerializeField]
        protected TeamRank rank = 0;
        [SerializeField]
        protected int level;
        [SerializeField]
        protected int cups;
        [SerializeField]
        protected int donates;

        public long GameId
        {
            get { return gameId; }
            set { gameId = value; }
        }

        public long TeamId
        {
            get { return teamId; }
            set { teamId = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public TeamRank Rank
        {
            get { return rank; }
            set { rank = value; }
        }

        public int Level
        {
            get { return level; }
            set { level = value; }
        }

        public int Cups
        {
            get { return cups; }
            set { cups = value; }
        }

        public int Donates
        {
            get { return donates; }
            set { donates = value; }
        }

        public int CompareTo(TeamMemberItem member)
        {
            return cups == member.cups ? 0 :
                   (cups > member.cups ? 1 : -1);
        }
    }
}