import axios from "axios";
import { useRouter } from "vue-router";

export function useLogout() {
    const router = useRouter();

    const logout = () => {
        localStorage.removeItem("user");
        localStorage.removeItem("token");
        localStorage.removeItem("calendarId");

        delete axios.defaults.headers.common["Authorization"];

        router.push("/login");

        window.dispatchEvent(new Event("storage"));
    };

    return { logout };
}
