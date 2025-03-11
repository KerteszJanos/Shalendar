import * as signalR from "@microsoft/signalr";
import { API_BASE_URL } from "@/utils/config/config";

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/calendarHub`, {
        withCredentials: false
    })
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

let isStarting = false;

const startConnection = async () => {
    if (connection.state === signalR.HubConnectionState.Connected) {
        return;
    }

    if (isStarting) {
        return;
    }

    isStarting = true;
    try {
        await connection.start();
    } catch (err) {
        console.error("❌ SignalR Connection Error: ", err);
        setTimeout(startConnection, 5000);
    } finally {
        isStarting = false;
    }
};

const ensureConnected = async () => {
    while (connection.state === signalR.HubConnectionState.Connecting) {
        await new Promise((resolve) => setTimeout(resolve, 500));
    }
    if (connection.state === signalR.HubConnectionState.Disconnected) {
        await startConnection();
    }
};

ensureConnected();

export { connection, startConnection, ensureConnected };
