export async function withMinDelay<T>(
    promise: Promise<T>,
    delayMs: number = 300
): Promise<T> {
    const [result] = await Promise.all([
        promise,
        new Promise(resolve => setTimeout(resolve, delayMs))
    ]);
    return result;
}
