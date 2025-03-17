export const getPriorityColor = (priority) => {
    if (priority === 9) return "#2E7D32";
    if (priority === 8) return "#4CAF50";
    if (priority === 7) return "#66BB6A";
    if (priority === 6) return "#FFEB3B";
    if (priority === 5) return "#FFC107";
    if (priority === 4) return "#FF9800";
    if (priority === 3) return "#FF5722";
    if (priority === 2) return "#F44336";
    if (priority === 1) return "#B71C1C";
    return "#9E9E9E";
};