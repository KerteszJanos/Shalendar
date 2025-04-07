export function sharePermissionOnNewCalendar(userEmail, testerEmail) {
    cy.url().should("include", "/calendars");
  
    createNewCalendar("Test Calendar");
    setPermissionOnCreatedCalendar(userEmail, "Test Calendar", "Owner");
    setPermissionOnCreatedCalendar(testerEmail, "Test Calendar", "Read");
  }
  
  export function chooseSharedCalendar(calendarName) {
    cy.get(".calendar-name").contains(calendarName).should("be.visible").then((calendar) => {
      cy.wrap(calendar)
        .parents(".calendar-box")
        .click();
    });
  }

  function createNewCalendar(calendarName) {
    cy.get(".add-calendar-button").click();
    cy.get(".modal-input").should("be.visible");
    cy.get(".modal-input").type(calendarName);
    cy.get(".modal-button").contains("Create").click();
    cy.get(".modal-overlay", { timeout: 10000 }).should('not.exist');
    cy.get(".calendar-container")
      .should("contain.text", calendarName)
      .and('be.visible');
  }
  
  function setPermissionOnCreatedCalendar(userEmail, calendarName, permissionLvl) {
    cy.contains(".calendar-name", calendarName)
      .should("be.visible")
      .parents(".calendar-box")
      .find(".calendar-actions button.permissions")
      .click();
    cy.get(".modal-content").should("be.visible");
    cy.get("input#emailInput").type(userEmail);
    cy.get(".modal-button").contains("Add").click();
    cy.contains(".permission-email", userEmail).should("exist");
    cy.contains(".permission-email", userEmail)
      .parents(".permission-item")
      .find(".permission-dropdown")
      .select(permissionLvl);
    cy.contains("button", "Close").click();
  }