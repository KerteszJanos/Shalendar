import api from "@/utils/config/axios-config";

export const updateTicketOrder = async (input) => {
    // Ha input egy lista, akkor a tickets mezőből dolgozunk
    let ticketsArray = input.tickets ? input.tickets : input;

    if (!ticketsArray || ticketsArray.length === 0) return;

    // Újraszámoljuk a pozíciókat
    const orderUpdates = ticketsArray.map((ticket, index) => ({
        ticketId: ticket.id,
        newPosition: index + 1
    }));

    try {
        await api.put("/api/Tickets/reorder", orderUpdates);
        // Vue reaktivitás fenntartása érdekében új tömbbel felülírjuk az adatokat
        if (input.tickets) {
            input.tickets = [...ticketsArray].sort((a, b) => a.currentPosition - b.currentPosition);
        } else {
            input.length = 0;
            input.push(...ticketsArray.sort((a, b) => a.currentPosition - b.currentPosition));
        }
    } catch (error) {
        console.error("Error updating ticket order:", error);
        throw new Error("Failed to update ticket positions.");
    }
};
