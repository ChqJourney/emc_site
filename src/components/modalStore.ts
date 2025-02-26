import type { SvelteComponent } from 'svelte';
import { writable } from 'svelte/store';

function createModalStore() {
  const { subscribe, set, update } = writable<{ show: boolean; component: any; props: any }>({
    show: false,
    component: null,
    props: {}
  });

  return {
    subscribe,
    open: (component: any, props:any = {}) => {
      set({ show: true, component, props });
    },
    close: () => {
      set({ show: false, component: null, props: {} });
    }
  };
}

export const modalStore = createModalStore();