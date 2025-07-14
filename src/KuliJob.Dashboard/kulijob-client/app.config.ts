export default defineAppConfig({
    toaster: {
        position: 'bottom-right' as const,
        expand: true,
        duration: 5000
    },
    theme: {
        radius: 0,
        blackAsPrimary: false
    },
    ui: {
        colors: {
            primary: 'green',
            neutral: 'zinc',
        },
        table: {
            slots: {
                th: 'text-default text-base whitespace-normal',
                td: 'text-default text-base whitespace-normal',
                separator: 'bg-(--ui-border-muted)',
                tr: 'border-b-[var(--ui-border-muted)]'
            },
        },
    },
})