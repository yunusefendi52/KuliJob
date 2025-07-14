<script setup lang="ts">
defineProps<{
    jobState: number,
}>()
</script>

<template>
    <UBadge :color="jobState === 3 ? 'success' : jobState === 5 ? 'error' : jobState === 2 ? 'secondary' : 'neutral'"
        variant="subtle" :label="jobStateToName(jobState)"></UBadge>
</template>