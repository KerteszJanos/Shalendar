import api from "@/utils/config/axios-config";
import { setErrorMessage } from "@/utils/errorHandler";

export const sendBackToCalendarList = async (ticketId, errorMessage) => {
    if (!ticketId) {
        console.error('Hibás ticketId:', ticketId);
        return;
    }

    try {
        await api.put(`/api/Tickets/move-to-calendar/${ticketId}`);
    } catch (error) {
        if (error.response && error.response.status === 403) {
            setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
            console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
        } else {
            console.error('Hiba a ticket visszaküldése során:', error);
            setErrorMessage(errorMessage, "Failed to send ticket back to calendar.");
        }
    }
};
