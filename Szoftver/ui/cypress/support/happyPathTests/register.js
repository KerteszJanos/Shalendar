export function registerUserIfNotExists(username, email, password) {
    cy.visit("/register");
  
    cy.get("input#username").type(username);
    cy.get("input#email").type(email);
    cy.get("input#password").type(password);
    cy.get("input#passwordAgain").type(password);
    cy.get("button[type=submit]").click();
  
    cy.get("body").then(($body) => {
      if ($body.find(".error").length > 0) {
        cy.get(".error").then(($error) => {
          const text = $error.text();
          if (text.includes("already exists") || text.includes("létezik")) {
            cy.log("User already exists. Skipping registration.");
            cy.visit("/login");
          } else {
            cy.url().should("include", "/login");
          }
        });
      } else {
        cy.url().should("include", "/login");
      }
    });
  }
  