<script setup lang="ts">
import Refresher from '~/components/Refresher.vue';

const { params: { jobId } } = useRoute()
const { data: result } = useFetch<any>('/kulijob/api/kulijob/job', {
    query: {
        jobId,
    },
    watch: [usePollingInterval()],
})

const data = computed(() => result.value?.data)
const jobStates = computed(() => result.value?.jobStates || [])

</script>

<template>
    <div class="flex flex-col p-5">
        <!-- <div>
            <span class="font-light">Job Details</span>
        </div> -->
        <div class="flex flex-col gap-1">
            <span class="">{{ data?.id }}</span>
            <span class="text-3xl font-medium">{{ data?.jobName }}</span>
        </div>
        <USeparator class="my-5" />
        <div class="grid grid-cols-3 sm:grid-cols-6 gap-5">
            <div class="flex flex-col gap-2">
                <span>State</span>
                <div class="self-start">
                    <JobStateBadge :job-state="data?.jobState" />
                </div>
            </div>
            <div class="flex flex-col gap-2">
                <span>Retry</span>
                <span class="font-medium">{{ data?.retryCount }}/{{ data?.retryMaxCount }}</span>
            </div>
            <div class="flex flex-col gap-2">
                <span>Priority</span>
                <span class="font-medium">{{ data?.priority }}</span>
            </div>
            <div class="flex flex-col gap-2">
                <span>Queue</span>
                <span class="font-medium">{{ data?.queue }}</span>
            </div>
            <div class="flex flex-col gap-2">
                <span>Server</span>
                <span class="font-medium">{{ data?.serverName }}</span>
            </div>
            <div class="flex flex-col gap-2">
                <span>Created</span>
                <UTooltip :text="data?.createdOn">
                    <Refresher>
                        <span class="font-medium">{{ formatRelativeTime(data?.createdOn) }}</span>
                    </Refresher>
                </UTooltip>
            </div>
        </div>
        <template v-if="data?.stateMessage">
            <USeparator class="my-5" />
            <div class="flex flex-col gap-1">
                <span class="text-xl font-medium mb-2">Message</span>
                <code>{{ data?.stateMessage }}</code>
            </div>
        </template>
        <USeparator class="my-5" />
        <div class="flex flex-col gap-1">
            <span class="text-xl font-medium mb-2">Data Parameters</span>
            <JsonHighlight :code="data?.jobData" />
        </div>
        <USeparator class="my-5" />
        <div class="flex flex-col gap-1">
            <span class="text-xl font-medium mb-2">States</span>
            <div class="flex flex-col gap-3">
                <div v-for="item in jobStates" :key="item.id"
                    class="rounded-[var(--ui-radius)] border border-default px-5 py-3 flex flex-col gap-1">
                    <span class="font-bold text-lg">{{ jobStateToName(item.jobState) }}</span>
                    <code v-if="item.message"><span>{{ item.message }}</span></code>
                    <UTooltip :text="item.createdAt">
                        <Refresher>
                            <span>{{ formatRelativeTime(item.createdAt) }}</span>
                        </Refresher>
                    </UTooltip>
                </div>
            </div>
        </div>
        <!-- <div class="flex flex-col gap-1 mt-5">
            <span class="text-xl font-medium mb-2">State</span>
            <div class="flex flex-col gap-3">
                <div class="rounded border border-[var(--p-content-border-color)] flex flex-col" v-if="data?.startedOn">
                    <div class="px-5 py-3 flex flex-row gap-2">
                        <div class="flex-1">
                            <span class="font-bold text-lg">Processing</span>
                        </div>
                    </div>
                </div>
                <div class="rounded border border-[var(--p-content-border-color)] flex flex-row gap-2 px-5 py-3 items-center"
                    v-if="data?.createdOn">
                    <div class="flex-1">
                        <span class="font-bold text-lg">Created</span>
                    </div>
                    <div v-tooltip.bottom="data?.createdOn">
                        <Refresher>
                            <span class="text-[var(--p-text-muted-color)]">{{ formatRelativeTime(data?.createdOn)
                                }}</span>
                        </Refresher>
                    </div>
                </div>
            </div>
        </div> -->
    </div>
</template>