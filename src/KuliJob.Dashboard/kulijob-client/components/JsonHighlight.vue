<script setup lang="ts">

const props = defineProps<{
    code?: string | undefined,
}>()

function formatJson(data?: string | undefined) {
    if (!data) {
        return ''
    }
    return JSON.stringify(JSON.parse(data), null, 4)
}
</script>

<template>
    <ClientOnly>
        <div class="rounded">
            <highlightjs language='json' :code="formatJson(props.code)" />
        </div>
    </ClientOnly>
</template>

<style>
@import 'highlight.js/styles/atom-one-dark.css';
/* @import 'highlight.js/styles/atom-one-light.css'; */
</style>