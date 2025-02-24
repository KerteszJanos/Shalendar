import api from "@/utils/config/axios-config";

export const sendBackToCalendarList = async (ticketId) => {
    if (!ticketId) {
        console.error('Hibás ticketId:', ticketId);
        return;
    }

    try {
        await api.put(`/api/Tickets/move-to-calendar/${ticketId}`);
    } catch (error) {
        console.error('Hiba a ticket visszaküldése során:', error);
    }
};