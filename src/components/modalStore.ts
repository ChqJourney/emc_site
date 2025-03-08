import { writable } from 'svelte/store';
import type { Component } from 'svelte';

type ModalState = {
    isShow: boolean;
    component: Component | null;
    props?: Record<string, unknown>;
}
export const modalStore=writable<ModalState>({
    isShow: false,
    component: null,
    props: {}
});

export const showModal=(component: Component, props: Record<string, unknown> = {}) => {
    modalStore.update(state => ({
        ...state,
        isShow: true,
        component,
        props
    }));
}
export const hideModal=() => {
    modalStore.update(state => ({
       ...state,
        isShow: false,
        component: null,
        props: {}
    }));
}