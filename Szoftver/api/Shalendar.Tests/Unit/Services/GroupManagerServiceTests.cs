using Shalendar.Services;
using System.Linq;
using Xunit;
using FluentAssertions;

namespace Shalendar.Tests.Unit.Services
{
    public class GroupManagerServiceTests
    {
        [Fact]
        public void AddConnection_ShouldAddConnectionToGroup()
        {
            var service = new GroupManagerService();

            service.AddConnection("groupA", "conn1");

            service.IsUserAloneInGroup("groupA").Should().BeTrue();
            service.GetGroupsForConnection("conn1").Should().ContainSingle().Which.Should().Be("groupA");
        }

        [Fact]
        public void AddConnection_ShouldAddMultipleConnectionsToSameGroup()
        {
            var service = new GroupManagerService();

            service.AddConnection("groupX", "conn1");
            service.AddConnection("groupX", "conn2");

            service.IsUserAloneInGroup("groupX").Should().BeFalse();
            service.GetGroupsForConnection("conn1").Should().Contain("groupX");
            service.GetGroupsForConnection("conn2").Should().Contain("groupX");
        }

        [Fact]
        public void RemoveConnection_ShouldRemoveConnectionFromGroup()
        {
            var service = new GroupManagerService();

            service.AddConnection("group1", "conn1");
            service.AddConnection("group1", "conn2");

            service.RemoveConnection("group1", "conn1");

            service.IsUserAloneInGroup("group1").Should().BeTrue();
            service.GetGroupsForConnection("conn1").Should().BeEmpty();
            service.GetGroupsForConnection("conn2").Should().Contain("group1");
        }

        [Fact]
        public void RemoveConnection_ShouldRemoveGroup_WhenLastConnectionRemoved()
        {
            var service = new GroupManagerService();

            service.AddConnection("soloGroup", "onlyConn");
            service.IsUserAloneInGroup("soloGroup").Should().BeTrue();

            service.RemoveConnection("soloGroup", "onlyConn");

            service.IsUserAloneInGroup("soloGroup").Should().BeFalse();
            service.GetGroupsForConnection("onlyConn").Should().BeEmpty();
        }

        [Fact]
        public void GetGroupsForConnection_ShouldReturnAllGroupsForConnection()
        {
            var service = new GroupManagerService();

            service.AddConnection("groupA", "conn1");
            service.AddConnection("groupB", "conn1");
            service.AddConnection("groupC", "conn2");

            var groups = service.GetGroupsForConnection("conn1");

            groups.Should().BeEquivalentTo(new[] { "groupA", "groupB" });
        }
    }
}
