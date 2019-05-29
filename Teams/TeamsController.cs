using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Network.GameServer;
using Teams.Network;
using Utilities;
using EventHandlerSystem;
using Network.PhotonNet;
namespace Teams
{
    public class TeamsController : MonoSingleton<TeamsController>, IGameHandler
    {
        public const char JOIN_REQUESTS_SEPARATOR = ',';

        public long GameId { get { return GameServer.Instance.GameId; } }
        public TeamServer Server { get; set; }
        public TeamItem CurrentTeam { get; set; }

        private TeamMemberItem currentMember;
        public TeamMemberItem CurrentMember
        {
            get
            {
                if (currentMember == null)
                    currentMember = new TeamMemberItem();

                currentMember.GameId = GameServer.Instance.GameId;
                currentMember.Name = GameController.Instance.StorageController.Data.UserData.Name.Value;
                currentMember.Level = GameController.Instance.StorageController.Data.UserData.Level.Value;
                currentMember.Cups = GameController.Instance.CurrencyController.GetCurrency(Currencies.CurrencyType.Cups);
                return currentMember;
            }
        }

        public List<TeamItem> CachedTeams { get; set; } = new List<TeamItem>();
        public List<TeamMemberItem> CachedTeamMembers { get; set; } = new List<TeamMemberItem>();
        public List<ChatMessageItem> CachedChatMessages { get; set; } = new List<ChatMessageItem>();

        public event Action<TeamItem> OnCurrentTeamReady;
        public event Action<List<TeamItem>> OnTeamsReady;
        public event Action<long, List<TeamMemberItem>> OnTeamMembersReady;
        public event Action<ChatMessageItem> OnChatMessageReceived;
        public event Action<int, int> OnCurrentTeamOnline;
        public event Action<long, List<ChatMessageItem>> OnChatMessagesReady;

        protected override async void Init()
        {
            base.Init();
            Server = new TeamServer();
            Server.Client.Connect();
            Server.Client.OnPacketReceived += OnTeamPacketReceived;
            Server.Client.OnReconnect += OnReconnect;
            var connected = await Server.Client.WaitForConnect();
            if (connected)
            {
                if (HasInstance)
                {
                    GetMyTeamMember(GameId);
                }
            }
        }
        #region ChatMessages
        public void SendChatMessage(string message)
        {
            Server.Client.Emit(TeamConstants.TEAM_MESSAGE_TEAM_EVENT_TYPE,
                                       currentMember.TeamId.ToString(),
                                       currentMember.GameId.ToString(),
                                       TeamMessageTypes.TEAM_MESSAGE_TYPE_TEXT,
                                       message);
        }

        public async void GetChatMessages(long teamId)
        {
            var response = await Server.GetChatMessagesAsync(teamId);
            if(response.status == Status.OK)
            {
                CachedChatMessages.Clear();
                for(int i = 0; i < response.data.Length; i++)
                {
                    var sMessage = response.data[i];
                    var chatMessageItem = new ChatMessageItem()
                    {
                        Id = sMessage.message_id,
                        TeamId = sMessage.team_id,
                        GameId = sMessage.game_id,
                        Text = sMessage.text,
                        Type = sMessage.type,
                        Time = sMessage.time
                    };
                    CachedChatMessages.Add(chatMessageItem);
                }
                EventHelper.SafeCall(OnChatMessagesReady, teamId, CachedChatMessages);
            }
        }

        protected virtual void OnTeamPacketReceived(TeamWebSocketPacket packet)
        {
            if (packet.EventType == TeamConstants.MESSAGE_TEAM_EVENT_TYPE)
            {
                var teamIdStr = packet.EventParams[0];
                var gameIdStr = packet.EventParams[1];
                var messageTypeStr = packet.EventParams[2];
                var messageStr = packet.EventParams[3];

                int.TryParse(teamIdStr, out int teamId);
                int.TryParse(gameIdStr, out int gameId);

                ChatMessageItem chatMessageItem = new ChatMessageItem()
                {
                    TeamId = teamId,
                    GameId = gameId,
                    Type = messageTypeStr,
                    Text = messageStr
                };
                EventHelper.SafeCall(OnChatMessageReceived, chatMessageItem);
            }
            if(packet.EventType == TeamConstants.ONLINE_TEAM_EVENT_TYPE)
            {
                var teamIdStr = packet.EventParams[0];
                var onlineStr = packet.EventParams[1];

                int.TryParse(teamIdStr, out int teamId);
                int.TryParse(onlineStr, out int online);

                EventHelper.SafeCall(OnCurrentTeamOnline, teamId, online);
            }

            if(packet.EventType == TeamConstants.MEMBER_REFRESH_EVENT_TYPE)
            {
                RefreshTeamMember();
            }
        }

        protected virtual void OnReconnect()
        {
            if (CurrentMember.TeamId != -1)
            {
                Server.Client.Emit(TeamConstants.TEAM_LOGIN_EVENT_TYPE, CurrentMember.TeamId.ToString());
            }
        }
        #endregion

        public async void CreateTeam(string teamName, string teamDescription, TeamType teamType, int minLevel, int badge)
        {
            var response = await Server.CreateTeamAsync(GameId, teamName, teamDescription, minLevel, teamType, badge);
            if (response.status == Status.OK)
            {
                CurrentTeam = new TeamItem()
                {
                    Id = response.data.team_id,
                    Name = response.data.team_name,
                    Description = response.data.team_desc,
                    Type = (TeamType)response.data.type,
                    MinLevel = response.data.min_level,
                    Badge = response.data.badge,
                    Cups = response.data.cups,
                    MembersCount = response.data.members_count
                };
                CurrentMember.TeamId = CurrentTeam.Id;
                CurrentMember.Rank = TeamRank.Leader;
                Server.Client.Emit(TeamConstants.TEAM_LOGIN_EVENT_TYPE, CurrentMember.TeamId.ToString());
                EventHelper.SafeCall(OnCurrentTeamReady, CurrentTeam);
            }
        }

        public async void UpdateTeam(string teamName, string teamDescription, TeamType teamType, int minLevel, int badge)
        {
            var response = await Server.UpdateTeamAsync(CurrentTeam.Id, teamName, teamDescription, minLevel, teamType, badge);
            if (response.status == Status.OK)
            {
                CurrentTeam.Name = teamName;
                CurrentTeam.Description = teamDescription;
                CurrentTeam.MinLevel = minLevel;
                CurrentTeam.Type = teamType;
                CurrentTeam.Badge = badge;
                EventHelper.SafeCall(OnCurrentTeamReady, CurrentTeam);
            }
        }

        public async void GetTeams()
        {
            var response = await Server.GetTeamsAsync();
            if (response.status == Status.OK)
            {
                CachedTeams.Clear();
                for (int i = 0; i < response.data.Length; i++)
                {
                    var sTeam = response.data[i];
                    var teamItem = new TeamItem()
                    {
                        Id = sTeam.team_id,
                        Name = sTeam.team_name,
                        Description = sTeam.team_desc,
                        Type = (TeamType)sTeam.type,
                        MinLevel = sTeam.min_level,
                        Badge = sTeam.badge,
                        Cups = sTeam.cups,
                        MembersCount = sTeam.members_count
                    };
                    var joinRequestStr = sTeam.join_requests.Split(JOIN_REQUESTS_SEPARATOR);
                    for (int j = 0; j < joinRequestStr.Length; j++)
                    {
                        if (long.TryParse(joinRequestStr[j], out long id))
                        {
                            teamItem.JoinRequests.Add(id);
                        }
                    }
                    CachedTeams.Add(teamItem);
                }
                EventHelper.SafeCall(OnTeamsReady, CachedTeams);
            }
        }

        public async void JoinTeam(TeamItem team)
        {
            var response = await Server.JoinTeamAsync(team.Id, CurrentMember.GameId);
            if (response.status == Status.OK)
            {
                var teamResponse = await Server.GetTeamAsync(team.Id);
                if (teamResponse.status == Status.OK)
                {
                    var data = teamResponse.data;
                    CurrentTeam = new TeamItem()
                    {
                        Id = data.team_id,
                        Name = data.team_name,
                        Description = data.team_desc,
                        Type = (TeamType)data.type,
                        MinLevel = data.min_level,
                        Badge = data.badge,
                        Cups = data.cups,
                        MembersCount = data.members_count
                    };
                    CurrentMember.TeamId = CurrentTeam.Id;
                    Server.Client.Emit(TeamConstants.TEAM_JOIN_EVENT_TYPE, CurrentMember.TeamId.ToString(), CurrentMember.GameId.ToString());
                    EventHelper.SafeCall(OnCurrentTeamReady, CurrentTeam);
                }
            }
        }

        public async void GetMyTeam()
        {
            var teamResponse = await Server.GetTeamAsync(CurrentMember.TeamId);
            if (teamResponse.status == Status.OK)
            {
                var data = teamResponse.data;
                CurrentTeam = new TeamItem()
                {
                    Id = data.team_id,
                    Name = data.team_name,
                    Description = data.team_desc,
                    Type = (TeamType)data.type,
                    MinLevel = data.min_level,
                    Badge = data.badge,
                    Cups = data.cups,
                    MembersCount = data.members_count
                };
                CurrentMember.TeamId = CurrentTeam.Id;
                EventHelper.SafeCall(OnCurrentTeamReady, CurrentTeam);
            }
        }

        public async void GetMyTeamMember(long gameId)
        {
            var response = await Server.GetTeamMemberAsync(gameId);
            if (response.status == Status.OK && response.data == null)
            {
                await Server.UpdateTeamMemberAsync(GameId, CurrentMember.Level, CurrentMember.Cups, CurrentMember.TeamId);
                if (CurrentMember.TeamId != -1)
                {
                    GetMyTeam();
                    Server.Client.Emit(TeamConstants.TEAM_LOGIN_EVENT_TYPE, CurrentMember.TeamId.ToString(), CurrentMember.GameId.ToString());
                }
            }
            else
            {
                CurrentMember.TeamId = response.data.team_id;
                CurrentMember.Donates = response.data.donates;
                CurrentMember.Rank = (TeamRank)response.data.rank;
                if (CurrentMember.TeamId != -1)
                {
                    GetMyTeam();
                    Server.Client.Emit(TeamConstants.TEAM_LOGIN_EVENT_TYPE, CurrentMember.TeamId.ToString(), CurrentMember.GameId.ToString());
                }
            }
        }

        public async void RefreshTeamMember()
        {
            var response = await Server.GetTeamMemberAsync(GameId);
            if(response.status == Status.OK)
            {
                CurrentMember.TeamId = response.data.team_id;
                CurrentMember.Donates = response.data.donates;
                CurrentMember.Rank = (TeamRank)response.data.rank;
                if (CurrentMember.TeamId != -1)
                {
                    GetMyTeam();
                }
            }
        }

        public async void GetTeamMembers(long teamId)
        {
            var response = await Server.GetTeamMembersAsync(teamId);
            if(response.status == Status.OK)
            {
                CachedTeamMembers.Clear();
                var data = response.data;
                for(int i = 0; i < data.Length; i++)
                {
                    var sMember = data[i];
                    var teamMember = new TeamMemberItem()
                    {
                        GameId = sMember.game_id,
                        TeamId = sMember.team_id,
                        Name = sMember.name,
                        Cups = sMember.cups,
                        Level = sMember.level,
                        Donates = sMember.donates,
                        Rank = (TeamRank)sMember.rank
                    };
                    CachedTeamMembers.Add(teamMember);
                }
                EventHelper.SafeCall(OnTeamMembersReady, teamId, CachedTeamMembers);
            }
        }

        public void GetTeamOnline(long teamId)
        {
            Server.Client.Emit(TeamConstants.TEAM_ONLINE_EVENT_TYPE, teamId.ToString());
        }

        public async void UpdateTeamMember()
        {
            var response = await Server.UpdateTeamMemberAsync(GameId, CurrentMember.Level, CurrentMember.Cups, CurrentMember.TeamId);
            if (response.status == Status.OK)
            {

            }
        }

        public async void KickTeamMember(long gameId, long teamId, TeamRank rank)
        {
            var response = await Server.KickTeamMemberAsync(gameId,teamId,(int)rank);
            if (response.status == Status.OK)
            {
                Server.Client.Emit(TeamConstants.TEAM_LEAVE_EVENT_TYPE, CurrentMember.TeamId.ToString(), gameId.ToString());
                if (gameId == CurrentMember.GameId)
                {
                    CurrentMember.TeamId = -1;
                    CurrentTeam = null;
                    EventHelper.SafeCall(OnCurrentTeamReady, CurrentTeam);
                }
            }
        }
        protected override void OnDestroy()
        {
            if (Server != null && Server.Client != null)
            {
                Server.Client.OnReconnect -= OnReconnect;
                Server.Client.Close();
            }
            base.OnDestroy();
        }

        void IGameHandler.OnGameStarted()
        {
        }

        void IGameHandler.OnGameFinished()
        {
            UpdateTeamMember();
        }

        void IGameHandler.OnDisconnect()
        {
        }

        void IGameHandler.OnReconnect()
        {
        }

        void IGameHandler.OnLeaveRoom()
        {
        }
    }
}