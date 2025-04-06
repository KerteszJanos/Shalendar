import { registerUserIfNotExists } from "../support/happyPathTests/register";
import { loginUser } from "../support/happyPathTests/login";
import { calendarListsTests } from "../support/happyPathTests/calendarLists";
import { calendarViewTests } from "../support/happyPathTests/calendarView";
import { dayViewTests } from "../support/happyPathTests/dayView";
import { profileTests, deleteUserFromProfile } from "../support/happyPathTests/profile";
import { sharePermissionOnNewCalendarAndDeletesDefault, chooseSharedCalendar } from "../support/happyPathTests/calendars";

describe("Registration, Login, Profile Access and Deletion – Happy Path", () => {
  const userUsername = "CypressTestUser";
  const userEmail = "cypressuser@example.com";
  const userPassword = "Password123";

  const friendUsername = "CypressTestUserFriend";
  const friendEmail = "cypressuserFriend@example.com";
  const friendPassword = "Password123";

  it("registers OR logs in, navigates to profile and deletes the user", () => {
    cy.window().then((win) => {
      if (!win.confirm.isSinonProxy) {
        cy.stub(win, "confirm").as("confirmStub").returns(true);
      }
    });

    registerUserIfNotExists(userUsername, userEmail, userPassword);
    registerUserIfNotExists(friendUsername, friendEmail, friendPassword);
    
    loginUser(friendEmail, friendPassword);
    clickManageCalendars();
    sharePermissionOnNewCalendarAndDeletesDefault(userEmail);
    clickLogoutButton();

    loginUser(userEmail, userPassword);
    clickManageCalendars();
    chooseSharedCalendar(); //TODO: implementálni hogy átirányítson a dashboardra és mehessen tovább a cuccos

    calendarListsTests();

    clickFirstCalendarDay();
    dayViewTests();

    clickDashboardLogo();
    calendarViewTests();

    clickProfile();
    profileTests();

    loginUser(friendEmail, friendPassword);
    //TODO: itt ellenőrizheti hogy léteznek-e a naptárban a ticketek
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