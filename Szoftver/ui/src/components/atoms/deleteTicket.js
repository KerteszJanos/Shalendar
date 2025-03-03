import api from "@/utils/config/axios-config";
import { updateTicketOrder } from "@/components/atoms/updateTicketOrder";

export const deleteTicket = async (ticketId, listOrTickets, errorMessage) => {
    try {
        await api.delete(`/api/tickets/${ticketId}`);

        if (listOrTickets.tickets) {
            listOrTickets.tickets = listOrTickets.tickets.filter(ticket => ticket.id !== ticketId);
            if (listOrTickets.tickets.length > 0) {
                await updateTicketOrder(listOrTickets);
            }
        }

        else if (Array.isArray(listOrTickets)) {
            listOrTickets.splice(
                listOrTickets.findIndex(ticket => ticket.id === ticketId), 1
            );
            if (listOrTickets.length > 0) {
                await updateTicketOrder(listOrTickets);
            }
        }
    } catch (error) {
        if (error.response && error.response.status === 403) {
            errorMessage.value = `Access denied: ${error.response.data?.message || "You do not have permission."}`;
            console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
        } else {
            console.error("Error deleting ticket:", error);
            errorMessage.value = "Failed to delete ticket.";
        }
    }
};
