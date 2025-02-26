// Tauri doesn't have a Node.js server to do proper SSR
// so we will use adapter-static to prerender the app (SSG)
// See: https://v2.tauri.app/start/frontend/sveltekit/ for more info
export const prerender = true;
export const ssr = false;

import { browser } from '$app/environment';

export function load() {
  if (browser) {
    const currentUrl = window.location.href;
    const currentHost = window.location.host; // 包含主机名和端口
    const currentHostname = window.location.hostname; // 仅主机名（IP或域名）
    const currentPort = window.location.port;
    
    return {
      url: currentUrl,
      host: currentHost,
      hostname: currentHostname,
      port: currentPort
    };
  }
  
  return {
    url: '',
    host: '',
    hostname: '',
    port: ''
  };
}
