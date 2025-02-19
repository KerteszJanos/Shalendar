<template>
  <div class="lists-container">
    <div class="header">
      <h2>To be scheduled</h2>
      <button class="add-button" @click="openModal">+</button>
    </div>
    <p v-if="loading">Betöltés...</p>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <div class="lists-content" v-if="calendarLists.length > 0">
      <div 
        v-for="list in calendarLists" 
        :key="list.id" 
        class="list-item" 
        :style="{ backgroundColor: list.color || '#CCCCCC' }"
      >
        <p class="list-title">{{ list.name }}</p>
      </div>
    </div>
    <p v-else>Nincs ütemezett lista.</p>

    <!-- MODAL -->
    <Modal 
      :show="showModal" 
      title="Új lista hozzáadása"
      confirmText="Hozzáadás"
      @close="showModal = false"
      @confirm="addCalendarList"
    >
      <div class="modal-content">
        <input v-model="newList.name" placeholder="Lista neve" />

        <div class="color-picker">
          <input v-model="newList.color" type="color" />
          <div class="color-preview" :style="{ backgroundColor: newList.color }"></div>
        </div>
      </div>
    </Modal>
  </div>
</template>

<script>
import { ref, onMounted } from "vue";
import api from "@/utils/config/axios-config";
import Modal from "@/components/Modal.vue";

export default {
  components: { Modal },
  setup() {
    const calendarLists = ref([]);
    const loading = ref(true);
    const errorMessage = ref("");
    const showModal = ref(false);
    const newList = ref({ name: "", color: "#CCCCCC" });

    const fetchCalendarLists = async () => {
      try {
        const user = JSON.parse(localStorage.getItem("user"));
        if (!user || !user.defaultCalendarId) {
          throw new Error("Nincs alapértelmezett naptár beállítva.");
        }

        const calendarId = user.defaultCalendarId;
        const response = await api.get(`/api/CalendarLists/calendar/${calendarId}`);
        calendarLists.value = response.data;
      } catch (error) {
        console.error("Error loading calendar lists:", error);
        errorMessage.value = error.response?.data || "Nem sikerült betölteni a listákat.";
      } finally {
        loading.value = false;
      }
    };

    const openModal = () => {
      newList.value = { name: "", color: "#CCCCCC" };
      showModal.value = true;
    };

    const addCalendarList = async () => {
      try {
        const user = JSON.parse(localStorage.getItem("user"));
        if (!user || !user.defaultCalendarId) {
          throw new Error("Nincs alapértelmezett naptár beállítva.");
        }

        const calendarId = user.defaultCalendarId;
        const response = await api.post("/api/CalendarLists", {
          name: newList.value.name,
          color: newList.value.color,
          calendarId
        });

        calendarLists.value.push(response.data);
        showModal.value = false;
      } catch (error) {
        console.error("Error adding list:", error);
        errorMessage.value = "Nem sikerült hozzáadni a listát.";
      }
    };

    onMounted(fetchCalendarLists);

    return {
      calendarLists,
      loading,
      errorMessage,
      showModal,
      newList,
      openModal,
      addCalendarList
    };
  }
};
</script>

<style scoped>
.lists-container {
  width: 30vw;
  height: 90vh;
  background: #c8e6c9;
  padding: 15px;
  border-radius: 10px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.add-button {
  background: #4caf50;
  color: white;
  border: none;
  padding: 5px 10px;
  font-size: 18px;
  border-radius: 50%;
  cursor: pointer;
}

.lists-content {
  display: flex;
  flex-direction: row;
  gap: 10px;
  overflow-x: auto;
  white-space: nowrap;
  padding-bottom: 10px;
}

.list-item {
  padding: 10px;
  border-radius: 5px;
  box-shadow: 2px 2px 5px rgba(0, 0, 0, 0.1);
  min-width: 200px;
  flex-shrink: 0;
  text-align: center;
}

.list-title {
  font-weight: bold;
  color: #ffffff;
}

.modal-content {
  display: flex;
  flex-direction: column;
  gap: 10px;
  align-items: center;
}

.color-picker {
  display: flex;
  align-items: center;
  gap: 10px;
}

.color-preview {
  width: 30px;
  height: 30px;
  border-radius: 5px;
  border: 1px solid #ccc;
}

.error {
  color: red;
}
</style>
