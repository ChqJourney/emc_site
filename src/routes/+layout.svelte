<script lang="ts">
    import { onMount } from "svelte";
    import { apiService } from "../biz/apiService";
    import { setGlobal } from "../biz/globalStore";
  import ErrorToast from "../components/ErrorToast.svelte";
  import Modal from "../components/Modal.svelte";
  import { hideModal, modalStore } from "../components/modalStore";
  import "../app.css";
  let { children,data } = $props();

</script>

{@render children?.()}
<Modal show={$modalStore.isShow} onNegative={()=>hideModal()} 
  item={{component:$modalStore.component,props:$modalStore.props}}/>
<ErrorToast/>
