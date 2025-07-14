<script setup lang="ts">
const isDark = useDark()
const toggleDark = useToggle(isDark)
</script>

<template>
    <UButton :icon="isDark ? 'i-lucide-moon' : 'i-lucide-sun'" class="self-start" size="xl" variant="ghost" text @click="() => {
        toggleDark()
    }" />
</template>