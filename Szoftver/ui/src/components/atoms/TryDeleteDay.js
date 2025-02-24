import api from "@/utils/config/axios-config";

export const tryDeleteDay = async (calendarId, date, ticketCount) => {
    if (!calendarId || !date) {
        console.error("Hibás calendarId vagy dátum:", calendarId, date);
        return;
    }
    
    if (ticketCount > 0) {
        return;
    }

    try {
        await api.delete(`/api/Days/${calendarId}/${date}`);
    } catch (error) {
        console.error("Hiba a nap törlése során:", error);
    }
};