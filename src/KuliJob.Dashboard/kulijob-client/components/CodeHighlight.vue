<script setup lang="ts">

const props = withDefaults(defineProps<{
    language?: 'json',
    code?: string | undefined,
}>(), {
    language: 'json',
})

function formatJson(data?: string | undefined) {
    if (!data) {
        return ''
    }
    return JSON.stringify(JSON.parse(data), null, 4)
}

const code = computed(() => {
    switch (props.language) {
        case 'json':
            return formatJson(props.code)
        default:
            return undefined
    }
})
</script>

<template>
    <ClientOnly>
        <div class="rounded">
            <highlightjs :language='props.language' :code="code" />
        </div>
    </ClientOnly>
</template>

<style>
@import 'highlight.js/styles/atom-one-dark.css';
/* @import 'highlight.js/styles/atom-one-light.css'; */
</style>