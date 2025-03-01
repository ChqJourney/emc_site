// Tauri doesn't have a Node.js server to do proper SSR
// so we will use adapter-static to prerender the app (SSG)
// See: https://v2.tauri.app/start/frontend/sveltekit/ for more info
export const prerender = true;
export const ssr = false;

import { browser } from '$app/environment';
import { apiService } from '../biz/apiService';

export function load() {
  if (browser) {
    const currentUrl = window.location.href;
    const currentHost = window.location.host; // 包含主机名和端口
    const currentHostname = window.location.hostname; // 仅主机名（IP或域名）
    const currentPort = window.location.port;
    console.log(`Current URL: ${currentUrl}`);
    console.log(`Current Host: ${currentHost}`);
    console.log(`Current Hostname: ${currentHostname}`);
    console.log(`Current Port: ${currentPort}`);
    // 在这里配置apiService
    apiService.configure({
      baseUrl: currentPort==="1420"
      ?"http://localhost:5000/api":`http://${currentHost}/api`,
      username: "patri",
      machineName: "pwin"
    });
    
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
