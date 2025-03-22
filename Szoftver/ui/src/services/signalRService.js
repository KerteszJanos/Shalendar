import * as signalR from "@microsoft/signalr";
import { API_BASE_URL } from "@/utils/config/config";

// Provides a SignalR connection to the calendarHub for real-time communication with automatic reconnect support.

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/calendarHub`, {
        withCredentials: false
    })
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

let isStarting = false;

// Attempts to start the connection if it's not already connected or in the process of connecting.
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
        console.error("SignalR Connection Error: ", err);
        setTimeout(startConnection, 5000);
    } finally {
        isStarting = false;
    }
};

// Ensures the SignalR connection is established before proceeding, reconnecting if needed.
const ensureConnected = async () => {
    while (connection.state === signalR.HubConnectionState.Connecting) {
        await new Promise((resolve) => setTimeout(resolve, 500));
    }
    if (connection.state === signalR.HubConnectionState.Disconnected) {
        await startConnection();
    }
};

ensureConnected();

// Exports the shared SignalR connection instance and helper functions for other components.
export { connection, startConnection, ensureConnected };