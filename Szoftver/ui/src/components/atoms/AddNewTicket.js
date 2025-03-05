import api from "@/utils/config/axios-config";
import { updateTicketOrder } from "@/components/atoms/updateTicketOrder";
import { validateNameField } from "@/components/atoms/ValidateModalInputFields";
import { setErrorMessage } from "@/utils/errorHandler";

export const addNewTicket = async (ticketData, selectedListId, calendarLists, showAddNewTicketModal, errorMessage) => {
    const nameValidationError = validateNameField(ticketData.name);
    if (nameValidationError) {
        setErrorMessage(errorMessage, nameValidationError);
        return;
    }
    
    try {
        const response = await api.post("/api/Tickets", ticketData);

        if (!Array.isArray(calendarLists.value) || calendarLists.value.length === 0) {
            showAddNewTicketModal.value = false;
            return;
        }
        const list = calendarLists.value.find(list => list.id === selectedListId.value);
        if (list) {
            list.tickets.push(response.data);
            await updateTicketOrder(list);
        }

        showAddNewTicketModal.value = false;
    } catch (error) {
        if (error.response && error.response.status === 403) {
            setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
            console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
        } else {
            console.error("Error adding ticket:", error);
            setErrorMessage(errorMessage, error.response?.data || "Failed to add ticket.");
        }
        return;
    }
};