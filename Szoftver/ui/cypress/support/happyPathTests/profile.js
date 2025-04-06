export function changePasswordInProfile() {
  cy.get("button.changePassword").click();
  cy.get(".modal-content").should("be.visible");
  cy.get("#oldPassword").type("Password123");
  cy.get("#newPassword").type("NewPassword123");
  cy.get("#confirmPassword").type("NewPassword123");
  cy.get("form").submit();
  cy.get(".success").should("contain.text", "Password changed successfully!");
  cy.get(".modal-content").should("not.exist");
}

export function deleteUserFromProfile() {
    cy.get("button.deleteAccount").click();
    cy.url().should("include", "/login");
    cy.window().its("localStorage.token").should("not.exist");
    cy.window().its("localStorage.user").should("not.exist");
  }
  