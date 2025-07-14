<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui';

const columns: TableColumn<any>[] = [
    {
        header: 'Name',
        accessorKey: 'name',
    },
    {
        header: 'Schedule',
        id: 'schedule',
    },
    {
        header: 'Next Run',
        accessorKey: 'nextRun',
    },
    {
        header: 'Last Run',
        accessorKey: 'lastRun',
    },
]

const { data } = useFetch<any>('/api/kulijob/cron', {
    watch: [usePollingInterval()],
    query: {
    },
})
const crons = computed(() => (data.value?.data || []))
</script>

<template>
    <div class="break-words rounded-[var(--ui-radius)] mx-5 my-3">
        <UTable :data="crons" :columns="columns" class="flex-1">
            <!-- <template #id-cell="{ row }">
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
            </template>-->
            <template #schedule-cell="{ row }">
                <div class="flex flex-col *:self-start">
                    <div class="rounded-[var(--ui-radius)] bg-primary/10 text-primary border-primary border px-2">
                        <code>{{ row.original.cronExpression }}</code>
                    </div>
                </div>
            </template>
        </UTable>
    </div>
</template>