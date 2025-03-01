<script lang="ts">
    import { onMount } from "svelte";
    import { apiService } from "../biz/apiService";
    import { setGlobal } from "../biz/globalStore";
  import ErrorToast from "../components/ErrorToast.svelte";
  import Modal from "../components/Modal.svelte";
  import { modalStore } from "../components/modalStore";
  let { children,data } = $props();

</script>

{@render children?.()}
<Modal show={$modalStore.show} onClose={() => modalStore.close()}></Modal>
<ErrorToast/>
