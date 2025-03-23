using Microsoft.AspNetCore.SignalR;
using Moq;
using Shalendar.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Shalendar.Tests.Unit.Hubs
{
    public class CalendarHubTests
    {
        private CalendarHub CreateHub(out Mock<IGroupManagerService> mockGroupManager,
                                      out Mock<IGroupManager> mockGroups,
                                      string connectionId = "test-connection")
        {
            mockGroupManager = new Mock<IGroupManagerService>();
            mockGroups = new Mock<IGroupManager>();

            var hub = new CalendarHub(mockGroupManager.Object)
            {
                Context = Mock.Of<HubCallerContext>(c => c.ConnectionId == connectionId),
                Groups = mockGroups.Object
            };

            return hub;
        }

        [Fact]
        public async Task JoinGroup_ShouldAddToSignalRGroup_AndTrackConnection()
        {
            var hub = CreateHub(out var mockGroupManager, out var mockGroups);
            var groupName = "calendar-42";

            await hub.JoinGroup(groupName);

            mockGroups.Verify(g => g.AddToGroupAsync("test-connection", groupName, default), Times.Once);
            mockGroupManager.Verify(gm => gm.AddConnection(groupName, "test-connection"), Times.Once);
        }

        [Fact]
        public async Task LeaveGroup_ShouldRemoveFromSignalRGroup_AndUntrackConnection()
        {
            var hub = CreateHub(out var mockGroupManager, out var mockGroups);
            var groupName = "calendar-99";

            await hub.LeaveGroup(groupName);

            mockGroups.Verify(g => g.RemoveFromGroupAsync("test-connection", groupName, default), Times.Once);
            mockGroupManager.Verify(gm => gm.RemoveConnection(groupName, "test-connection"), Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ShouldRemoveConnectionFromAllGroups()
        {
            var hub = CreateHub(out var mockGroupManager, out var mockGroups);
            var connectionId = "test-connection";

            mockGroupManager.Setup(gm => gm.GetGroupsForConnection(connectionId))
                .Returns(new[] { "groupA", "groupB" });

            await hub.OnDisconnectedAsync(null);

            mockGroupManager.Verify(gm => gm.RemoveConnection("groupA", connectionId), Times.Once);
            mockGroupManager.Verify(gm => gm.RemoveConnection("groupB", connectionId), Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_ShouldCallBaseMethod()
        {
            var hub = CreateHub(out var _, out var _);
            var result = hub.OnConnectedAsync();

            await result;
        }
    }
}
