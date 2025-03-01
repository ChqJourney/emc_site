<script lang="ts">
    import { apiService } from "../biz/apiService";
    import { setGlobal } from "../biz/globalStore";
  import ErrorToast from "../components/ErrorToast.svelte";
  import Modal from "../components/Modal.svelte";
  import { modalStore } from "../components/modalStore";
  let { children,data } = $props();
  if(data.port==="5000"){
    setGlobal("run_mode","page");
    apiService.configure({
      baseUrl:"http://localhost:5000/api",
      username:"patri",
      machineName:"pwin"
    })
  }else{
    setGlobal("run_mode","desktop");
  }
</script>

{@render children?.()}
<Modal show={$modalStore.show} onClose={() => modalStore.close()}></Modal>
<ErrorToast/>
