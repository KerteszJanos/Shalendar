import api from "@/utils/config/axios-config";
import { setErrorMessage } from "@/utils/errorHandler";

export const updateTicketOrder = async (input, errorMessage) => {
    let ticketsArray = input.tickets ? input.tickets : input;

    if (!ticketsArray || ticketsArray.length === 0) return;

    // The index starts from 0, but newPosition is set to index + 1 to use 1-based indexing.
    const orderUpdates = ticketsArray.map((ticket, index) => ({
        ticketId: ticket.id,
        newPosition: index + 1
    }));

    try {
        await api.put("/api/Tickets/reorder", orderUpdates);

        ticketsArray.forEach((ticket, index) => {
            ticket.currentPosition = index + 1;
        });

        const sortedTickets = [...ticketsArray].sort((a, b) => a.currentPosition - b.currentPosition);

        // If `input` is an object with a `tickets` array, replace it with `sortedTickets`.
        // Otherwise, replace the entire array content without changing its reference.
        if (input.tickets) {
            input.tickets = sortedTickets;
        } else {
            input.splice(0, input.length, ...sortedTickets);
        }

        return sortedTickets;
    } catch (error) {
        if (error.response && error.response.status === 403) {
            setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
            console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
        } else {
            console.error("Error updating ticket order:", error);
            setErrorMessage(errorMessage, "Failed to update ticket order.");
        }
    }
};