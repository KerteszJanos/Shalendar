export function dayViewTests() {
    const ticketBaseName = `Cypress Ticket`;
    const updatedTicketName = `Updated Ticket Name`;

    createTicket(ticketBaseName + " TodoList", null, null, "This is a Cypress-created ticket.", 9);
    createTicket(ticketBaseName + " ScheduledList", getCurrentTimeString(0), getCurrentTimeString(30), "This is a Cypress-created ticket.", 1);
    toggleTicketCompletion(ticketBaseName + " TodoList");
    toggleTicketCompletion(ticketBaseName + " ScheduledList");
    editTicket(ticketBaseName + " TodoList", updatedTicketName + " TodoList", 3);
    editTicket(ticketBaseName + " ScheduledList", updatedTicketName + " ScheduledList", 5);
    deleteTicket(updatedTicketName + " TodoList");
    deleteTicket(updatedTicketName + " ScheduledList");
    stepDay("previous");

    for (let i = 0; i < 4; i++) {
        createTicket(ticketBaseName + " TodoList " + (i + 1));
        createTicket(ticketBaseName + " ScheduledList " + (i + 1), getCurrentTimeString(0), getCurrentTimeString(30));
        stepDay("next");
    }
}

function createTicket(ticketName, startTime = null, endTime = null, description = null, priority = null) {
    cy.get("button.add-ticket-btn").click();
    cy.get("input#ticket-name").type(ticketName);

    // Ha a description nem null vagy undefined, akkor beállítjuk
    if (description != null) {
        cy.get("textarea#ticket-description").type(description);
    }

    // Ha a priority nem null vagy undefined, akkor beállítjuk
    if (priority != null) {
        cy.get("input#ticket-priority").type(priority);
    }

    cy.get(".dropdown-selected").click();
    const randomIndex = Math.floor(Math.random() * 2);
    cy.get(".dropdown-list li").eq(randomIndex).click();

    if (startTime && endTime) {
        cy.get("input#ticket-start-time").type(startTime);
        cy.get("input#ticket-end-time").type(endTime);
    }

    cy.contains("button", "Add").click();

    if (startTime && endTime) {
        cy.get(".dayPanel").within(() => {
            cy.get(".ticket-name").contains(ticketName).should("exist");
        });
    } else {
        cy.get(".todoList").within(() => {
            cy.get(".ticket-name").contains(ticketName).should("exist");
        });
    }
}

function editTicket(ticketBaseName, updatedTicketName, priority) {
    cy.get(".ticket-item").each(($ticket) => {
        const ticketText = $ticket.find(".ticket-name").text();
        
        if (ticketText.includes(ticketBaseName)) {
            cy.wrap($ticket).click();  // Click the ticket to open the edit modal
            cy.get("input#edit-ticket-name").clear().type(updatedTicketName);  // Update the ticket name
            cy.get("textarea#edit-ticket-description").clear().type("Updated description for " + updatedTicketName);  // Update the description
            cy.get("input#edit-ticket-priority").clear().type(priority);  // Update the priority
            cy.contains("button", "Save").click();  // Save the changes

            cy.contains(".ticket-name", updatedTicketName).should("exist");  // Verify the updated ticket name
        }
    });
}

function toggleTicketCompletion(ticketName) {
    cy.get(".time-container .ticket-item").each(($ticket) => {
        const ticketText = $ticket.find(".ticket-name").text();
        
        if (ticketText.includes(ticketName)) {
            cy.wrap($ticket).find(".ticket-checkbox").click();
            cy.wait(500);
            cy.wrap($ticket).find(".ticket-checkbox").click();
        }
    });

    cy.get(".todo-list .ticket-item").each(($ticket) => {
        const ticketText = $ticket.find(".ticket-name").text();
        
        if (ticketText.includes(ticketName)) {
            cy.wrap($ticket).find(".ticket-checkbox").click();
            cy.wait(500);
            cy.wrap($ticket).find(".ticket-checkbox").click();
        }
    });
}

function deleteTicket(ticketName) {
    cy.get(".ticket-item").each(($ticket) => {
        const ticketText = $ticket.find(".ticket-name").text();
        
        if (ticketText.includes(ticketName)) {
            cy.wrap($ticket).find(".ticket-actions .delete-icon").click();  
        }
    });
}

function stepDay(direction) {
    cy.url().then((url) => {
        const initialDate = url.split('/').pop();
        const stepButton = direction === 'previous' ? '.nav-btn.left' : '.nav-btn.right';
        const expectedDate = new Date(initialDate);
        direction === 'previous' ? expectedDate.setDate(expectedDate.getDate() - 1) : expectedDate.setDate(expectedDate.getDate() + 1);
        const formattedExpectedDate = `${expectedDate.getFullYear()}-${String(expectedDate.getMonth() + 1).padStart(2, '0')}-${String(expectedDate.getDate()).padStart(2, '0')}`;

        cy.get(stepButton).click();
        cy.url().should('include', formattedExpectedDate);
    });
}

function getCurrentTimeString(offsetMinutes = 0) {
    const currentTime = new Date();
    currentTime.setMinutes(currentTime.getMinutes() + offsetMinutes);
    const hours = currentTime.getHours().toString().padStart(2, '0');
    const minutes = currentTime.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes}`;
}
