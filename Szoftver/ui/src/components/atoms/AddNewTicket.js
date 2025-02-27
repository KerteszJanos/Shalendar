import api from "@/utils/config/axios-config";
import { updateTicketOrder } from "@/components/atoms/updateTicketOrder";

export const addNewTicket = async (ticketData, selectedListId, calendarLists, showAddNewTicketModal, errorMessage) => {
    if (!ticketData.name.trim()) {
        errorMessage.value = "Ticket name is required.";
        return;
    }
    try {
        const response = await api.post("/api/Tickets", ticketData);
        const list = calendarLists.value.find(list => list.id === selectedListId.value);
        if (list) {
            list.tickets.push(response.data);
            await updateTicketOrder(list);
        }
        showAddNewTicketModal.value = false;
    } catch (error) {
        console.error("Error adding ticket:", error);
        errorMessage.value = error.response?.data || "Failed to add ticket.";
    }
};
