import api from "@/utils/config/axios-config";

export const updateTicketOrder = async (input) => {
    let ticketsArray = input.tickets ? input.tickets : input;

    if (!ticketsArray || ticketsArray.length === 0) return;

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

        if (input.tickets) {
            input.tickets = sortedTickets;
        } else {
            input.splice(0, input.length, ...sortedTickets);
        }

        return sortedTickets;
    } catch (error) {
        console.error("Error updating ticket order:", error);
        throw new Error("Failed to update ticket positions.");
    }
};
