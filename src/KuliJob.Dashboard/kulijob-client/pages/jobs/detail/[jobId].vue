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
const jobDuration = computed(() => {
    const successIndex = jobStates.value.findIndex((e: any) => e.jobState === 3)
    if (successIndex !== 0) {
        return undefined
    }
    const failedState = jobStates.value[successIndex + 1]
    const successState = jobStates.value[successIndex]
    const durationms = new Date(successState.createdAt).getTime() - new Date(failedState.createdAt).getTime()
    return durationms
})
const jobData = computed(() => {
    const jobDataStr = data.value?.jobData
    if (!jobDataStr) {
        return undefined
    }
    return JSON.parse(jobDataStr)
})
const codeLanguage = computed(() => jobData.value?.k_type ? 'table_view' : 'json')

const dataParams = computed(() => {
    if (codeLanguage.value === 'json') {
        return undefined
    }

    const methodExprCall = jobData.value?.k_args?.find(() => true) || []
    const methodArgs = methodExprCall.value.arguments
    const argumentsName = methodExprCall.value.argumentsName || []

    return methodArgs.map((e: any, index: number) => {
        return {
            name: argumentsName[index],
            value: e.value,
        }
    })
})
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
        <USeparator class="my-5" />
        <div class="flex flex-col gap-1">
            <span class="text-xl font-medium mb-2">Data Parameters</span>
            <template v-if="codeLanguage === 'json'">
                <CodeHighlight :code="data?.jobData" />
            </template>
            <template v-else>
                <div>
                    <UTable :data="dataParams" class="flex-1" />
                </div>
            </template>
        </div>
        <USeparator class="my-5" />
        <div class="flex flex-col gap-1">
            <span class="text-xl font-medium mb-2">States</span>
            <div class="flex flex-col gap-3">
                <div v-for="item in jobStates" :key="item.id"
                    class="rounded-[var(--ui-radius)] border border-accented flex flex-col" :class="{
                        'border-red-500': item.jobState === 5,
                        'border-green-500': item.jobState === 3,
                    }">
                    <div class="flex flex-row gap-1 px-4 p-3 items-center" :class="{
                        'bg-red-500/10 text-red-600': item.jobState === 5,
                        'bg-green-500/10 text-green-600': item.jobState === 3,
                    }">
                        <div>
                            <span class="font-bold text-lg">{{ jobStateToName(item.jobState) }}</span>
                        </div>
                        <div class="flex-1"></div>
                        <UTooltip :text="item.createdAt">
                            <Refresher>
                                <span>{{ formatRelativeTime(item.createdAt) }}</span>
                            </Refresher>
                        </UTooltip>
                    </div>
                    <div v-if="item.message || item.jobState === 3">
                        <div class="border-b border-accented" :class="{
                            'border-red-500': item.jobState === 5,
                            'border-green-500': item.jobState === 3,
                        }">
                        </div>
                        <div class="px-4 p-4">
                            <code v-if="item.jobState === 5">
                        <span>{{ item.message }}</span>
                    </code>
                            <div v-if="item.jobState === 3">
                                <div class="flex flex-row gap-2">
                                    <div class="lg:w-[120px] text-end font-semibold">
                                        <span>Duration:</span>
                                    </div>
                                    <div class="flex-1">
                                        <span>{{ jobDuration }}ms</span>
                                    </div>
                                </div>
                            </div>
                            <span v-else>{{ item.message }}</span>
                        </div>
                    </div>
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