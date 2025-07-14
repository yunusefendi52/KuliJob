export function usePollingInterval() {
    const interval = useInterval(() => 1000)
    return interval
}