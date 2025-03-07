import api from "@/utils/config/axios-config";
import { setErrorMessage } from "@/utils/errorHandler";

export async function toggleTicketCompletion(ticketId, newState, errorMessage)
{
    try {
        await api.put(`/api/Tickets/updateTicketCompleted?ticketId=${ticketId}&isCompleted=${newState}`);
    } catch (error) {
        if (error.response && error.response.status === 403) {
            setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
            console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
        } else {
            console.error("Failed to update ticket status:", error);
            setErrorMessage(errorMessage, "Failed to update ticket status.");
        }
    }
};
