export function profileTests() {
  deleteUserFromProfile();
}

export function deleteUserFromProfile() {
    cy.get("button.deleteAccount").click();
    cy.url().should("include", "/login");
    cy.window().its("localStorage.token").should("not.exist");
    cy.window().its("localStorage.user").should("not.exist");
  }
  