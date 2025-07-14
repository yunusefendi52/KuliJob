<script setup lang="ts">
const renderKey = ref(0)
let intervalId: NodeJS.Timeout | undefined = undefined
onMounted(() => {
    intervalId = setInterval(() => {
        renderKey.value++
    }, 1000)
})
onUnmounted(() => {
    if (intervalId) {
        clearInterval(intervalId)
    }
})
</script>

<template>
    <div :key="renderKey">
        <slot></slot>
    </div>
</template>