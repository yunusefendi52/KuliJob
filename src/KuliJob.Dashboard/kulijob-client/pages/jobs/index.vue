<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'

const status = ref([{
    id: 'all',
    name: 'All',
}, {
    id: 'created',
    name: 'Created',
}, {
    id: 'retry',
    name: 'Retry',
}, {
    id: 'active',
    name: 'Active',
}, {
    id: 'completed',
    name: 'Completed',
}, {
    id: 'cancelled',
    name: 'Cancelled',
}, {
    id: 'failed',
    name: 'Failed',
}])

const urlSearch = useUrlSearchParams('history', {
    writeMode: 'replace',
})
onMounted(() => {
    if (!urlSearch.status) {
        urlSearch.status = 'all'
    }
})
const page = computed({
    get: () => {
        try {
            return Number.parseInt(urlSearch.page.toString())
        } catch {
            return 1
        }
    },
    set: (v) => urlSearch.page = `${v}`,
})
const urlSearchStatus = computed(() => urlSearch.status)
watch(urlSearchStatus, () => {
    page.value = 1
})
const selectedStatus = computed(() => status.value.find(e => e.id === urlSearchStatus.value))
const { data } = useFetch<any>('/api/kulijob/jobs', {
    watch: [usePollingInterval()],
    query: {
        page: page,
        jobState: computed(() => selectedStatus.value && selectedStatus.value.id !== 'all' ? status.value.indexOf(selectedStatus.value) - 1 : undefined),
    },
})

function getJobTime(data: any) {
    return data.stateCreatedAt
}

const columns: TableColumn<any>[] = [
    {
        header: 'Job',
        id: 'job',
    },
    {
        id: 'queue',
        header: 'Queue',
    },
    {
        id: 'status',
        header: 'Status',
    },
    // {
    //     id: 'duration',
    //     header: 'Duration',
    // },
    {
        id: 'time',
    },
    {
        id: 'action'
    }
]
</script>

<template>
    <div class="flex flex-col px-5 pb-5 pt-2">
        <div class="flex flex-row">
            <div v-for="item in status" :key="item.id" @click="() => {
                urlSearch.status = item.id
            }">
                <div class="cursor-pointer px-5 py-3 border-b border-b-transparent hover:border-b-[var(--p-content-border-color)] transition-all"
                    :class="{
                        '!border-b-[var(--p-primary-color)] !text-[var(--p-primary-color)] font-medium': selectedStatus?.id === item.id,
                    }">
                    <span>{{ item.name }}</span>
                </div>
            </div>
        </div>
        <div class="flex-1 min-w-0">
            <div class="my-5" v-if="!(data?.data || []).length">
                <span class="text-xl">{{ page > 1 ? `No more jobs` : `You haven't schedule a job` }}</span>
            </div>
            <div v-else class="border border-muted break-words rounded-[var(--ui-radius)] my-5">
                <UTable :data="data?.data || []" :columns="columns" class="flex-1" :column-sizing="{

                }">
                    <template #job-cell="{ row }">
                        <div class="flex flex-col items-start">
                            <NuxtLink class="cursor-pointer" :to="{
                                name: 'jobs-detail-jobId',
                                params: {
                                    jobId: row.original.id,
                                },
                            }">
                                <span class="font-medium text-sm hover:underline">{{ row.original.id }}</span>
                            </NuxtLink>
                            <NuxtLink class="cursor-pointer" :to="{
                                name: 'jobs-detail-jobId',
                                params: {
                                    jobId: row.original.id,
                                },
                            }">
                                <span class="text-primary-500 font-medium text-xl hover:underline">{{
                                    row.original.jobName
                                    }}</span>
                            </NuxtLink>
                            <div v-if="row.original.stateMessage" class="flex flex-col gap-1">
                                <code
                                    class="line-clamp-2"><span class="text-error">Error: </span>{{ row.original.stateMessage }}</code>
                            </div>
                        </div>
                    </template>
                    <template #status-cell="{ row }">
                        <JobStateBadge :job-state="row.original.jobState" />
                    </template>
                    <!-- <template #duration-cell="{ row }">
                        <span>{{ `${(row.original.startedOn && row.original.completedOn) ? `${new
                            Date(row.original.completedOn) - new Date(slotProps.data.startedOn)} ms` : 'n/a'}`
                        }}</span>
                    </template> -->
                    <template #queue-cell="{ row }">
                        <UBadge color="info" variant="subtle" :label="row.original.queue"></UBadge>
                    </template>
                    <template #time-cell="{ row }">
                        <div>
                            <UTooltip :text="getJobTime(row.original)">
                                <Refresher>
                                    <span>{{ formatRelativeTime(getJobTime(row.original)) }}</span>
                                </Refresher>
                            </UTooltip>
                        </div>
                    </template>
                </UTable>
            </div>
            <div class="flex justify-center">
                <UPagination :total="Number.MAX_SAFE_INTEGER" :items-per-page="25" v-model:page="page" :siblingCount="0"
                    :ui="{
                        first: 'hidden',
                        last: 'hidden',
                    }" active-variant="outline" />
            </div>
        </div>
    </div>
</template>