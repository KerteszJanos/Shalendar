export function loginUser(email, password) {
    cy.get("input[type=email]").type(email);
    cy.get("input[type=password]").type(password);
    cy.get("button[type=submit]").click();
  
    cy.url().should("include", "/dashboard");
    cy.window().its("localStorage.token").should("exist");
    cy.window().its("localStorage.user").should("exist");
  }
  