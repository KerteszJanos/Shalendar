export function calendarListsTests() {
    cy.url().should("include", "/Dashboard");
  
    const listName = `Cypress List`;
    const updatedListName = `Renamed List`;
    const ticketBaseName = `Cypress Ticket`;
    const updatedTicketName = `Updated Ticket Name`;
  
    createCalendarList(listName);
    editCalendarList(listName, updatedListName);

    createTicket(updatedListName, ticketBaseName);
    toggleTicketCompletion(ticketBaseName);
    editTicket(ticketBaseName, updatedTicketName, 3);
    deleteTicket(updatedTicketName);

    deleteCalendarList(updatedListName);
    
    createCalendarList(listName);
  }
  
  
  export function createCalendarList(name) {
    const color = "#ff8800";
    cy.get(".header button.add-button").click();
    cy.get("input#list-name").type(name);
    cy.get("input[type='color']").invoke("val", color).trigger("input");
    cy.contains("button", "Add").click();
    cy.get(".list-title").should("contain", name);
  }
  
  function editCalendarList(originalName, newName) {
    const newColor = "#007bff";
    cy.contains(".list-item", originalName).within(() => {
      cy.get(".edit-list-button").click();
    });
    cy.get("input#edit-list-name").clear().type(newName);
    cy.get("input[type='color']").first().invoke("val", newColor).trigger("input");
    cy.contains("button", "Save").click();
    cy.get(".list-title").should("contain", newName);
  }
  
  function deleteCalendarList(name) {
    cy.contains(".list-item", name).within(() => {
      cy.get(".edit-list-button").click();
    });
    cy.get("button.delete-list-button").click();
    cy.contains(".list-title", name).should("not.exist");
  }
  
  export function createTicket(listName, ticketName) {
    const description = "This is a Cypress-created ticket.";
    const priority = 5;
  
    cy.contains(".list-item", listName).within(() => {
      cy.get("button.add-ticket-button").click();
    });
  
    cy.get("input#ticket-name").type(ticketName);
    cy.get("textarea#ticket-description").type(description);
    cy.get("input#ticket-priority").type(priority);
    cy.contains("button", "Add").click();
  
    cy.contains(".ticket-name", ticketName).should("exist");
  }
  
  function editTicket(originalName, newName, priority) {
    const newDescription = "This ticket was edited by Cypress.";
  
    cy.contains(".ticket-item", originalName).click();
    cy.get("input#edit-ticket-name").clear().type(newName);
    cy.get("textarea#edit-ticket-description").clear().type(newDescription);
    cy.get("input#edit-ticket-priority").clear().type(priority);
    cy.contains("button", "Save").click();
  
    cy.contains(".ticket-name", newName).should("exist");
  }

  function toggleTicketCompletion(ticketName) {
    cy.contains(".ticket-item", ticketName)
      .find(".ticket-checkbox")
      .as("checkbox");
  
    cy.get("@checkbox").should("exist").click();
    cy.wait(500);
  
    cy.get("@checkbox").click();
  }
  
  function deleteTicket(name) {
    cy.contains(".ticket-item", name).within(() => {
      cy.get(".delete-icon").click();
    });
    cy.contains(".ticket-name", name).should("not.exist");
  }

  export function copyTicket(name) {
    cy.contains(".ticket-item", name).within(() => {
      cy.get(".copy-icon").click();
    });
    cy.get(".modal-content").should("be.visible");
    cy.get(".selected-option").click();
    cy.get(".options-list").should("be.visible");
    cy.get(".options-list li").first().click();
    cy.get("button").contains("Copy").click();
  }