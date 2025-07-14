<script setup lang="ts">
import type { BreadcrumbItem } from "@nuxt/ui";
import type { MenuItem } from "primevue/menuitem";
import { ref } from "vue";
import { validate as uuidValidate } from 'uuid'

const items = ref([
    // {
    //     label: 'KuliJob',
    //     command(event) {
    //         navigateTo('/')
    //     },
    // },
    {
        label: 'Jobs',
        icon: 'i-lucide-layout-list',
        navigateTo: '/jobs',
    },
    {
        label: 'Cron',
        icon: 'i-lucide-calendar',
        navigateTo: '/cron',
    },
    {
        label: 'Servers',
        icon: 'i-lucide-server',
        navigateTo: '/servers',
    },
] satisfies MenuItem[]);

const route = useRoute()

const breadcrumbs = computed(() => ['Home', ...route.path.split('/').filter(e => e ? true : false)].map(e => {
    if (e === 'Home') {
        return {
            label: e,
            to: '/',
        }
    }
    return {
        label: uuidValidate(e) ? e : `${e[0].toUpperCase()}${e.substring(1)}`,
    }
}) satisfies BreadcrumbItem[])
</script>

<template>
    <div class="flex flex-row h-full">
        <div class="flex flex-col w-[250px] bg-muted/80 border-r border-r-[var(--ui-border-muted)]">
            <div class="mx-3 px-3 h-[60px] flex items-center">
                <label class="text-xl font-semibold cursor-pointer">KuliJob</label>
            </div>
            <div class="flex-1 flex flex-col gap-2">
                <template v-for="item in items" :key="item.label">
                    <NuxtLink :to="item.navigateTo">
                        <div class="mx-3 px-3 rounded py-2 flex flex-row items-center gap-3 cursor-pointer hover:bg-accented/60 border border-transparent hover:border-accented"
                            :class="{
                                'bg-accented/60 !border-accented': route.path.startsWith(item.navigateTo),
                            }">
                            <!-- <Button :icon="item.icon" :label="item.label" severity="secondary" @click="() => {
                        item.command(undefined)
                    }" /> -->
                            <UIcon :name="item.icon" class="size-5" />
                            <span>{{ item.label }}</span>
                        </div>
                    </NuxtLink>
                </template>
            </div>
            <div class="flex flex-col gap-3 p-3">
                <ThemeToggleButton />
            </div>
        </div>
        <div class="flex-1 min-w-0 overflow-auto">
            <div class="h-[55px] flex items-center p-5 sticky z-10 top-0 border-b border-b-muted backdrop-blur-3xl">
                <!-- <span class="text-xl font-semibold">{{ route.path }}</span> -->
                <UBreadcrumb :items="breadcrumbs" :ui="{
                    linkLabel: 'text-lg'
                }"></UBreadcrumb>
            </div>
            <slot></slot>
        </div>
    </div>
</template>