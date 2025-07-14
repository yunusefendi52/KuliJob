export function formatRelativeTime(date: string, baseDate = Date.now(), locale = undefined, options = { numeric: 'auto', style: 'long' }) {
    const rtf = new Intl.RelativeTimeFormat(locale, options);

    const d1 = new Date(baseDate).getTime();
    const d2 = new Date(date).getTime();

    if (isNaN(d1) || isNaN(d2)) return '';

    const diffInSeconds = Math.floor((d2 - d1) / 1000);

    const units = [
        { unit: 'year', seconds: 60 * 60 * 24 * 365 },
        { unit: 'month', seconds: 60 * 60 * 24 * 30 },
        { unit: 'week', seconds: 60 * 60 * 24 * 7 },
        { unit: 'day', seconds: 60 * 60 * 24 },
        { unit: 'hour', seconds: 60 * 60 },
        { unit: 'minute', seconds: 60 },
        { unit: 'second', seconds: 1 },
    ];

    for (const { unit, seconds } of units) {
        const delta = diffInSeconds / seconds;
        if (Math.abs(delta) >= 1 || unit === 'second') {
            return rtf.format(Math.round(delta), unit);
        }
    }
}

export function jobStateToName(jobState?: number) {
    switch (jobState) {
        case 0:
            return 'Created'
        case 1:
            return 'Retry'
        case 2:
            return 'Active'
        case 3:
            return 'Completed'
        case 4:
            return 'Cancelled'
        case 5:
            return 'Failed'
        default:
            return 'Unknown'
    }
}