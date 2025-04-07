import { registerUserIfNotExists } from "../support/happyPathTests/register";
import { loginUser } from "../support/happyPathTests/login";
import { calendarListsTests, createCalendarList, createTicket, copyTicket } from "../support/happyPathTests/calendarLists";
import { calendarViewTests, NavigateMonth } from "../support/happyPathTests/calendarView";
import { dayViewTests } from "../support/happyPathTests/dayView";
import { changePasswordInProfile, deleteUserFromProfile } from "../support/happyPathTests/profile";
import { sharePermissionOnNewCalendar, chooseSharedCalendar } from "../support/happyPathTests/calendars";

describe("Registration, Login, Profile Access and Deletion – Happy Path", () => {
  const userUsername = "CypressTestUser";
  const userEmail = "cypressuser@example.com";
  const userPassword = "Password123";

  const friendUsername = "CypressTestUserFriend";
  const friendEmail = "cypressuserFriend@example.com";
  const friendPassword = "Password123";

  const testerUsername = "CypressTestTester";
  const testerEmail = "CypressTestTester@example.com";
  const testerPassword = "Password123";

  it("registers OR logs in, navigates to profile and deletes the user", () => {
    cy.window().then((win) => {
      if (!win.confirm.isSinonProxy) {
        cy.stub(win, "confirm").as("confirmStub").returns(true);
      }
    });

    registerUserIfNotExists(userUsername, userEmail, userPassword);
    registerUserIfNotExists(friendUsername, friendEmail, friendPassword);
    registerUserIfNotExists(testerUsername, testerEmail, testerPassword);
    
    loginUser(friendEmail, friendPassword);
    clickManageCalendars();
    sharePermissionOnNewCalendar(userEmail, testerEmail);
    clickLogoutButton();

    loginUser(userEmail, userPassword);
    clickManageCalendars();
    chooseSharedCalendar("Test Calendar");

    calendarListsTests();

    clickFirstCalendarDay();
    dayViewTests();

    clickDashboardLogo();
    calendarViewTests();

    createCalendarList("CopyList");
    createTicket("CopyList", "CopyTicket");
    copyTicket("CopyTicket");
    clickManageCalendars();
    chooseSharedCalendar("CypressTestUser's Default Calendar");
    TicketANdCalendarCopyTest();

    clickProfile();
    changePasswordInProfile();
    clickLogoutButton();
    loginUser(userEmail, "NewPassword123");

    clickProfile();
    deleteUserFromProfile();

    loginUser(friendEmail, friendPassword);

    clickManageCalendars();
    chooseSharedCalendar("Test Calendar");
    NavigateMonth("prev");
    NavigateMonth("next");
    NavigateMonth("next");
    NavigateMonth("prev");

    clickProfile();
    deleteUserFromProfile();
  });
});

function clickFirstCalendarDay() {
  cy.get(".calendar-day", { timeout: 10000 })
    .not(".header")
    .first()
    .should("be.visible")
    .click({ force: true });
}

function clickDashboardLogo() {
  cy.get(".dashboard-logo")
    .click({ force: true });
}

function clickProfile()
{
  cy.get(".profile-button").click();
  cy.url().should("include", "/profile");
  cy.contains("Username:").should("exist");
  cy.get("p.email").should("exist");
}

function clickManageCalendars() {
  cy.get("button.add-button[title='View and manage all your calendars']")
    .click({ force: true });
}

function clickLogoutButton() {
  cy.get(".logout-button").click();
}

function TicketANdCalendarCopyTest()
{
  NavigateMonth("prev");
  NavigateMonth("next");
  NavigateMonth("next");
  NavigateMonth("prev");

  cy.get('.list-item').contains('CopyList').should('exist');
  cy.get('.ticket-item').contains('CopyTicket').should('exist'); 
  cy.get('.ticket').should('have.attr', 'title', 'Cypress Ticket ScheduledList 2');
}