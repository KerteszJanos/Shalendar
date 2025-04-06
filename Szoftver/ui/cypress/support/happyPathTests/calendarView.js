export function calendarViewTests() {
    cy.url().should("include", "/dashboard");

  }
  
  //TODO tesztelni a léptetést valamint a copyCalendart