using System;
using System.Threading.Tasks;
using UnityEngine;
using Network.GameServer;

namespace Teams.Network
{
    public class TeamServer
    {
        public TeamWebSocketClient Client { get; set; }

        public TeamServer()
        {
            var settings = ResourceManager.GetGeneralSettings();
            Client = new TeamWebSocketClient(settings.SocketUrl.address, settings.SocketUrl.port);
        }
        #region Async
        public Task<TeamMemberResponse> GetTeamMemberAsync(long gameId)
        {
            var teamMemberRequest = new TeamMemberRequest()
            {
                path = TeamConstants.GET_TEAM_MEMBER_BY_ID_PATH,
                game_id = gameId
            };
            return GameServer.Instance.Api.PostAsync<TeamMemberRequest, TeamMemberResponse>(teamMemberRequest);
        }

        public Task<TeamMembersResponse> GetTeamMembersAsync(long teamId)
        {
            var teamMembersRequest = new TeamMembersRequest()
            {
                path = TeamConstants.GET_TEAM_MEMBERS_PATH,
                team_id = teamId
            };
            return GameServer.Instance.Api.PostAsync<TeamMembersRequest, TeamMembersResponse>(teamMembersRequest);
        }

        public Task<TeamResponse> GetTeamAsync(long teamId)
        {
            var teamRequest = new TeamRequest()
            {
                path = TeamConstants.GET_TEAM_PATH,
                team_id = teamId
            };
            return GameServer.Instance.Api.PostAsync<TeamRequest,TeamResponse>(teamRequest);
        }

        public Task<TeamsResponse> GetTeamsAsync()
        {
            var teamsRequest = new TeamsRequest()
            {
                path = TeamConstants.GET_TEAMS_PATH
            };
            return GameServer.Instance.Api.PostAsync<TeamsRequest, TeamsResponse>(teamsRequest);
        }

        public Task<TeamCreateResponse> CreateTeamAsync(long gameId, string teamName, string description, int minLevel, TeamType type, int badge)
        {
            var createTeamRequest = new TeamCreateRequest()
            {
                path = TeamConstants.CREATE_TEAM_PATH,
                game_id = gameId,
                team_name = teamName,
                team_desc = description,
                min_level = minLevel,
                type = (int)type,
                badge = badge
            };
            return GameServer.Instance.Api.PostAsync<TeamCreateRequest, TeamCreateResponse>(createTeamRequest);
        }

        public Task<TeamUpdateResponse> UpdateTeamAsync(long teamId, string teamName, string description, int minLevel, TeamType type, int badge)
        {
            var updateTeamRequest = new TeamUpdateRequest()
            {
                path = TeamConstants.UPDATE_TEAM_PATH,
                team_id = teamId,
                team_name = teamName,
                team_desc = description,
                min_level = minLevel,
                type = (int)type,
                badge = badge
            };
            return GameServer.Instance.Api.PostAsync<TeamUpdateRequest, TeamUpdateResponse>(updateTeamRequest);
        }

        public Task<TeamJoinResponse> JoinTeamAsync(long teamId, long gameId)
        {
            var joinTeamRequest = new TeamJoinRequest()
            {
                path = TeamConstants.JOIN_TEAM_PATH,
                team_id = teamId,
                game_id = gameId,
            };
            return GameServer.Instance.Api.PostAsync<TeamJoinRequest,TeamJoinResponse>(joinTeamRequest);
        }

        public Task<TeamUpdateMemberResponse> UpdateTeamMemberAsync(long gameId, int level, int cups, long team_id)
        {
            var updateTeamMemberRequest = new TeamUpdateMemberRequest()
            {
                path = TeamConstants.UPDATE_TEAM_MEMBER_PATH,
                game_id = gameId,
                level = level,
                cups = cups,
                team_id = team_id
            };
            return GameServer.Instance.Api.PostAsync<TeamUpdateMemberRequest, TeamUpdateMemberResponse>(updateTeamMemberRequest);
        }

        public Task<TeamAddResponse> AddTeamMemberAsync(long teamId, long gameId)
        {
            var addTeamMemberRequest = new TeamAddRequest()
            {
                path = TeamConstants.ADD_TEAM_MEMBER_PATH,
                team_id = teamId,
                game_id = gameId
            };
            return GameServer.Instance.Api.PostAsync<TeamAddRequest,TeamAddResponse>(addTeamMemberRequest);
        }

        public Task<TeamKickResponse> KickTeamMemberAsync(long gameId, long teamId, int rank)
        {
            var kickTeamRequest = new TeamKickRequest()
            {
                path = TeamConstants.KICK_TEAM_MEMBER_PATH,
                game_id = gameId,
                team_id = teamId,
                rank = rank
            };
            return GameServer.Instance.Api.PostAsync<TeamKickRequest,TeamKickResponse>(kickTeamRequest);
        }

        public Task<ChatMessageResponse> GetChatMessagesAsync(long teamId)
        {
            var chatMessagesRequest = new ChatMessagesRequest()
            {
                path = TeamConstants.GET_CHAT_MESSAGES_PATH,
                team_id = teamId
            };
            return GameServer.Instance.Api.PostAsync<ChatMessagesRequest,ChatMessageResponse>(chatMessagesRequest);
        }

        #endregion
    }
}