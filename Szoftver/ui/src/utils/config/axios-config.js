import axios from "axios";
import { API_BASE_URL } from "@/utils/config/config";
import { useLogout } from "@/utils/LogoutHandler";

// Axios instance configured with base URL, auth token, and calendar ID headers for all API requests.
// Automatically handles 401 Unauthorized responses by logging the user out using the shared logout logic.

const api = axios.create({
    baseURL: API_BASE_URL
});

api.interceptors.request.use((config) => {
    const token = localStorage.getItem("token");
    if (token) {
        config.headers["Authorization"] = `Bearer ${token}`;
    }

    const calendarId = localStorage.getItem("calendarId");
    if (calendarId) {
        config.headers["X-Calendar-Id"] = calendarId;
    }

    return config;
}, (error) => Promise.reject(error));

api.interceptors.response.use(
    response => response,
    error => {
        if (error.response && error.response.status === 401) {
            const { logout } = useLogout();
            logout();
        }
        return Promise.reject(error);
    }
);

export default api;
