import api from "@/utils/config/axios-config";
import { emitter } from "@/utils/eventBus";

// Deletes a day if it has no tickets and emits an event on success.
export const tryDeleteDay = async (calendarId, date, ticketCount) => {
    if (!calendarId || !date) {
        console.error("Invalid calendarId or date:", calendarId, date);
        return;
    }
    
    if (ticketCount > 0) {
        return;
    }

    try {
        await api.delete(`/api/days/${calendarId}/${date}`);
        emitter.emit("successfulDayDelete");
    } catch (error) {
        console.error("Error deleting the day:", error);
    }
};
