using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Teams.Network
{
    public class TeamConstants
    {
        public const string GET_TEAM_MEMBER_BY_ID_PATH = "team.member_by_id";
        public const string GET_TEAM_MEMBERS_PATH = "team.members";
        public const string GET_TEAMS_PATH = "team.teams";
        public const string GET_TEAM_PATH = "team.team";
        public const string GET_CHAT_MESSAGES_PATH = "chat.messages";
        public const string JOIN_TEAM_PATH = "team.join";
        public const string CREATE_TEAM_PATH = "team.create";
        public const string UPDATE_TEAM_PATH = "team.update";
        public const string UPDATE_TEAM_MEMBER_PATH = "team.update_member";
        public const string ADD_TEAM_MEMBER_PATH = "team.add";
        public const string KICK_TEAM_MEMBER_PATH = "team.kick";

        public const string MESSAGE_TEAM_EVENT_TYPE = "message";
        public const string ONLINE_TEAM_EVENT_TYPE = "online";

        public const string TEAM_MESSAGE_TEAM_EVENT_TYPE = "team_message";
        public const string TEAM_LOGIN_EVENT_TYPE = "team_login";
        public const string TEAM_JOIN_EVENT_TYPE = "team_join";
        public const string TEAM_LEAVE_EVENT_TYPE = "team_leave";
        public const string TEAM_ONLINE_EVENT_TYPE = "team_online";

        public const string MEMBER_REFRESH_EVENT_TYPE = "member_refresh";
    }

    public class TeamMessageTypes
    {
        public const string TEAM_MESSAGE_TYPE_TEXT = "text";
        public const string TEAM_MESSAGE_TYPE_SYSTEM_JOIN = "system_join";
        public const string TEAM_MESSAGE_TYPE_SYSTEM_LEAVE = "system_leave";
    }
}