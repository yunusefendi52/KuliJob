<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui';

const columns: TableColumn<any>[] = [
    {
        header: 'Server Name',
        id: 'id',
    },
    {
        header: 'Worker',
        accessorKey: 'Worker',
    },
    {
        header: 'Queues',
        id: 'queues',
    },
]

const { data } = useFetch<any>('/api/kulijob/servers', {
    watch: [usePollingInterval()],
    query: {
    },
})
const servers = computed(() => (data.value?.data || []).map((e: any) => {
    const p = JSON.parse(e.data)
    return {
        ...p,
        ...e,
    }
}))
</script>

<template>
    <div class="break-words rounded-[var(--ui-radius)] mx-5 my-3">
        <UTable :data="servers" :columns="columns" class="flex-1">
            <template #id-cell="{ row }">
                <div class="flex flex-col">
                    <span class="text-lg font-medium">{{ row.original.id }}</span>
                    <UTooltip :text="row.original.lastHeartbeat">
                        <Refresher>
                            <span class="text-sm">
                                Last Heartbeat: <b>{{ formatRelativeTime(row.original.lastHeartbeat) }}</b>
                            </span>
                        </Refresher>
                    </UTooltip>
                    <UTooltip :text="row.original.lastHeartbeat">
                        <Refresher>
                            <span class="text-sm">
                                Started At: <b>{{ formatRelativeTime(row.original.StartedAt) }}</b>
                            </span>
                        </Refresher>
                    </UTooltip>
                </div>
            </template>
            <template #queues-cell="{ row }">
                <div class="flex flex-col">
                    <div v-for="item in row.original.Queues" :key="item">
                        <UButton :label="item" variant="ghost" />
                    </div>
                </div>
            </template>
        </UTable>
    </div>
</template>