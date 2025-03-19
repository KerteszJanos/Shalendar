<template>
<div class="dropdown">
    <div class="dropdown-selected tooltip" @click="toggleDropdown" :style="{ backgroundColor: selectedColor }" :title="selectedName">
        {{ selectedName || "Select a calendar" }}
    </div>
    <ul v-if="isOpen" class="dropdown-list">
        <li v-for="list in calendarLists" :key="list.id" @click="selectItem(list)" :style="{ backgroundColor: list.color }" class="tooltip" :title="list.name">
            {{ list.name }}
        </li>
    </ul>
</div>
</template>

  
<script>
import {
    ref,
    computed
} from "vue";

export default {
    props: {
        calendarLists: {
            type: Array,
            default: () => []
        },
        modelValue: [String, Number]
    },
    emits: ["update:modelValue"],
    setup(props, {
        emit
    }) {
        const isOpen = ref(false);
        const selectedItem = ref(null);

        const toggleDropdown = () => {
            isOpen.value = !isOpen.value;
        };

        const selectItem = (list) => {
            selectedItem.value = list;
            emit("update:modelValue", list.id);
            isOpen.value = false;
        };

        return {
            isOpen,
            selectedItem,
            toggleDropdown,
            selectItem,
            selectedName: computed(() => selectedItem.value ? selectedItem.value.name : ""),
            selectedColor: computed(() => selectedItem.value ? selectedItem.value.color : "#ffffff")
        };
    }
};
</script>

  
<style scoped>
.dropdown {
    position: relative;
    width: 100%;
}

.dropdown-selected {
    padding: 8px;
    border: 2px solid #0B6477;
    border-radius: 5px;
    font-size: 16px;
    cursor: pointer;
    text-align: center;
    overflow: hidden;
    text-overflow: ellipsis;
}

.dropdown-list {
    position: absolute;
    width: 100%;
    max-height: 150px;
    overflow-y: auto;
    list-style: none;
    margin: 0;
    padding: 0;
    border: 1px solid #0B6477;
    border-radius: 5px;
    background-color: white;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
    z-index: 1000;
}

.dropdown-list li {
    padding: 8px;
    cursor: pointer;
    text-align: center;
    overflow: hidden;
    text-overflow: ellipsis;
}

.dropdown-list li:hover {
    filter: brightness(85%);
}
</style>
