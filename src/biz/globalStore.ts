import { writable } from 'svelte/store';

// Create a writable store to hold our global state
const globalStore = writable<Record<string, any>>({});

// Function to set a value in the global store
export function setGlobal(key: string, value: any) {
    globalStore.update(store => ({
        ...store,
        [key]: value
    }));
}

// Function to get a value from the global store
export function getGlobal(key: string) {
    let value: any;
    globalStore.subscribe(store => {
        value = store[key];
    });
    return value;
}

// Export the store itself for direct subscription
export { globalStore };