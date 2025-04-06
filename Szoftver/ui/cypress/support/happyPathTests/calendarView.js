export function calendarViewTests() {
    cy.url().should("include", "/dashboard");
    cy.wait(500);
    NavigateMonth("prev");
    NavigateMonth("next");
    NavigateMonth("next");
    NavigateMonth("prev");
    testCopyCalendar();
  }
  
 export function NavigateMonth(direction) {
  if (direction === "prev") {
    cy.get("button[title='Go to previous month']").click();
  } else if (direction === "next") {
    cy.get("button[title='Go to next month']").click();
  } else {
    throw new Error("Invalid direction. Use 'prev' or 'next'.");
  }
  cy.wait(500);
  cy.get(".calendar-grid").should("be.visible");
}

export function testCopyCalendar() {
    cy.get(".copy-icon").click();
    cy.get(".modal-content").should("be.visible");
    cy.get(".selected-option").click();
    cy.get(".options-list").should("be.visible");
    cy.get(".options-list li").first().click();
    cy.get("button").contains("Copy").click();
  }
  
  