import{k as Y,s as O,f as H,a as g,c as N,t as A,b as Q,d as V}from"../chunks/disclose-version.BtOGdF5q.js";import{j as Z,E as K,k as tt,D as u,ar as at,l as rt,u as et,as as st,at as ot,ac as it,y as nt,e as z,au as ft,T as ct,av as vt,W as I,X as U,Y as dt,I as M,J as j,L,M as S,N as E,K as lt,O as q}from"../chunks/utils.Dv3gV-s9.js";import{a as ut}from"../chunks/apiService.DgwLLXM9.js";import{b as mt,s as _t,m as $,a as B}from"../chunks/modalStore.dvlA8jbC.js";import{i as W}from"../chunks/if.DdkDn34N.js";import{i as pt}from"../chunks/lifecycle.CkqpZYRK.js";function D(a,t,...r){var e=a,s=u,o;Z(()=>{s!==(s=t())&&(o&&(at(o),o=null),o=tt(()=>s(e,...r)))},K),rt&&(e=et)}const ht=()=>performance.now(),m={tick:a=>requestAnimationFrame(a),now:()=>ht(),tasks:new Set};function J(a){m.tasks.forEach(t=>{t.c(a)||(m.tasks.delete(t),t.f())}),m.tasks.size!==0&&m.tick(J)}function wt(a){let t;return m.tasks.size===0&&m.tick(J),{promise:new Promise(r=>{m.tasks.add(t={c:a,f:r})}),abort(){m.tasks.delete(t)}}}function b(a,t){a.dispatchEvent(new CustomEvent(t))}function gt(a){if(a==="float")return"cssFloat";if(a==="offset")return"cssOffset";if(a.startsWith("--"))return a;const t=a.split("-");return t.length===1?t[0]:t[0]+t.slice(1).map(r=>r[0].toUpperCase()+r.slice(1)).join("")}function G(a){const t={},r=a.split(";");for(const e of r){const[s,o]=e.split(":");if(!s||o===void 0)break;const i=gt(s.trim());t[i]=o.trim()}return t}const yt=a=>a;function kt(a,t,r,e){var s=(a&vt)!==0,o="both",i,f=t.inert,n,v;function d(){var c=dt,p=z;I(null),U(null);try{return i??(i=r()(t,(e==null?void 0:e())??{},{direction:o}))}finally{I(c),U(p)}}var w={is_global:s,in(){t.inert=f,b(t,"introstart"),n=F(t,d(),v,1,()=>{b(t,"introend"),n==null||n.abort(),n=i=void 0})},out(c){t.inert=!0,b(t,"outrostart"),v=F(t,d(),n,0,()=>{b(t,"outroend"),c==null||c()})},stop:()=>{n==null||n.abort(),v==null||v.abort()}},_=z;if((_.transitions??(_.transitions=[])).push(w),Y){var h=s;if(!h){for(var l=_.parent;l&&l.f&K;)for(;(l=l.parent)&&!(l.f&st););h=!l||(l.f&ot)!==0}h&&it(()=>{nt(()=>w.in())})}}function F(a,t,r,e,s){var o=e===1;if(ft(t)){var i,f=!1;return ct(()=>{if(!f){var p=t({direction:o?"in":"out"});i=F(a,p,r,e,s)}}),{abort:()=>{f=!0,i==null||i.abort()},deactivate:()=>i.deactivate(),reset:()=>i.reset(),t:()=>i.t()}}if(r==null||r.deactivate(),!(t!=null&&t.duration))return s(),{abort:u,deactivate:u,reset:u,t:()=>e};const{delay:n=0,css:v,tick:d,easing:w=yt}=t;var _=[];if(o&&r===void 0&&(d&&d(0,1),v)){var h=G(v(0,1));_.push(h,h)}var l=()=>1-e,c=a.animate(_,{duration:n});return c.onfinish=()=>{var p=(r==null?void 0:r.t())??1-e;r==null||r.abort();var T=e-p,y=t.duration*Math.abs(T),P=[];if(y>0){if(v)for(var R=Math.ceil(y/16.666666666666668),C=0;C<=R;C+=1){var x=p+T*w(C/R),X=v(x,1-x);P.push(G(X))}l=()=>{var k=c.currentTime;return p+T*w(k/y)},d&&wt(()=>{if(c.playState!=="running")return!1;var k=l();return d(k,1-k),!0})}c=a.animate(P,{duration:y,fill:"forwards"}),c.onfinish=()=>{l=()=>e,d==null||d(e,1-e),s()}},{abort:()=>{c&&(c.cancel(),c.effect=null,c.onfinish=u)},deactivate:()=>{s=u},reset:()=>{e===0&&(d==null||d(1,0))},t:()=>l()}}const bt=!0,St=!1;function Et(){{const a=window.location.href,t=window.location.host,r=window.location.hostname,e=window.location.port;return{url:a,host:t,hostname:r,port:e}}}const It=Object.freeze(Object.defineProperty({__proto__:null,load:Et,prerender:bt,ssr:St},Symbol.toStringTag,{value:"Module"}));var Tt=A('<div class="toast-container svelte-10ukmgr"><div> </div></div>');function Ct(a,t){M(t,!1);const r=O(),e=()=>N(mt,"$errorStore",r);pt();var s=H(),o=j(s);W(o,()=>e().show,i=>{var f=Tt(),n=S(f),v=S(n,!0);E(n),E(f),lt(()=>{_t(n,`toast ${e().type??""} svelte-10ukmgr`),Q(v,e().message)}),g(i,f)}),g(a,s),L()}const $t=a=>a;function Ft(a,{delay:t=0,duration:r=400,easing:e=$t}={}){const s=+getComputedStyle(a).opacity;return{delay:t,duration:r,easing:e,css:o=>`opacity: ${o*s}`}}function Ot(a,t){a.target===a.currentTarget&&t.onClose()}var Nt=A('<div class="modal-overlay svelte-16tmf73"><div class="modal svelte-16tmf73"><!></div></div>');function At(a,t){M(t,!0);const r=O(),e=()=>N($,"$modalStore",r);var s=H(),o=j(s);W(o,()=>t.show,i=>{var f=Nt();f.__click=[Ot,t];var n=S(f),v=S(n);D(v,()=>e().component??u),E(n),E(f),kt(3,f,()=>Ft),g(i,f)}),g(a,s),L()}V(["click"]);var Mt=A("<!> <!> <!>",1);function Ut(a,t){M(t,!0);const r=O(),e=()=>N($,"$modalStore",r);t.data.port==="5000"?(B("run_mode","page"),ut.configure({baseUrl:"http://localhost:5000/api",username:"patri",machineName:"pwin"})):B("run_mode","desktop");var s=Mt(),o=j(s);D(o,()=>t.children??u);var i=q(o,2);At(i,{get show(){return e().show},onClose:()=>$.close()});var f=q(i,2);Ct(f,{}),g(a,s),L()}export{Ut as component,It as universal};
