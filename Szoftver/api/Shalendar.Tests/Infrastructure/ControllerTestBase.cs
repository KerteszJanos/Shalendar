using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Shalendar.Contexts;
using Shalendar.Functions.Interfaces;
using Shalendar.Services.Interfaces;
using System;

namespace Shalendar.Tests.Infrastructure
{
	public abstract class ControllerTestBase
	{
		protected Mock<IJwtHelper> MockJwtHelper = null!;
		protected Mock<IGetCalendarIdHelper> MockGetCalendarIdHelper = null!;
		protected Mock<IGroupManagerService> MockGroupManager = null!;
		protected Mock<IHubContext<CalendarHub>> MockHubContext = null!;
		protected DefaultHttpContext HttpContext = null!;

		protected TController CreateController<TController>(
			ShalendarDbContext context,
			Func<ShalendarDbContext,
				 IJwtHelper,
				 IGetCalendarIdHelper,
				 IGroupManagerService,
				 IHubContext<CalendarHub>,
				 TController> factory
		) where TController : ControllerBase
		{
			MockJwtHelper = new Mock<IJwtHelper>();
			MockGetCalendarIdHelper = new Mock<IGetCalendarIdHelper>();
			MockGroupManager = new Mock<IGroupManagerService>();
			MockHubContext = new Mock<IHubContext<CalendarHub>>();
			HttpContext = new DefaultHttpContext();

			var controller = factory(
				context,
				MockJwtHelper.Object,
				MockGetCalendarIdHelper.Object,
				MockGroupManager.Object,
				MockHubContext.Object
			);

			controller.ControllerContext = new ControllerContext
			{
				HttpContext = HttpContext
			};

			return controller;
		}
	}
}
