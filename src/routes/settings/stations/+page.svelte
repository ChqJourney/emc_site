<script lang="ts">
    import type { Station, StationDTO } from "../../../biz/types";
  import { showModal, hideModal } from "../../../components/modalStore";
    import SortableTable from "../../../components/SortableTable.svelte";
    import { message } from "@tauri-apps/plugin-dialog";
    import { load } from "@tauri-apps/plugin-store";
    import { getGlobal, setGlobal } from "../../../biz/globalStore";
    import { errorHandler } from "../../../biz/errorHandler";
    import type { AppError } from "../../../biz/errors";
    import { readTextFile, writeTextFile } from "@tauri-apps/plugin-fs";
    import { invoke } from "@tauri-apps/api/core";
    import { loadMergedStation, submitStation, deleteStation } from "../../../biz/operation";
    import { exportStations } from "../../../biz/exportSheets";
    
  let loadingIndicator=$state(0);
  async function loadStations(indicator:number) {
    let stations:Station[]=[];
    stations=await loadMergedStation(new Date().toISOString().split('T')[0]);
    return stations;
  }
async function handleStationSubmit(station:Station,isCreate:boolean){ 
  await submitStation(station,isCreate);
  loadingIndicator++;
}

async function handleCopyStation(station:Station,isCreate:boolean){
  console.log(isCreate)
  submitStation({...station},isCreate);
  loadingIndicator++;
}
  async function handleStationDelete(station: Station) {
   await deleteStation(station);
   loadingIndicator++;
  }
  async function handleExportStations() {
    try{
      const stations = await invoke<Station[]>("get_all_stations");
      await exportStations(stations);
      errorHandler.showInfo("导出成功");
    } catch (error) {
      errorHandler.handleError(error as AppError);
    }
  }
</script>
<div class="content">
  {#await loadStations(loadingIndicator)}
    <p>加载中...</p>
  {:then stations}
  <div class="header">
    <div class="title-group">

      <h4>工位列表</h4>
      {#if stations.length > 0}
      <div style="text-align: center; font-size: smaller;">{stations.length} items in total</div>
      {/if}
    </div>
      <div class="button-group">
      
      <button aria-label="导出数据" class="action-button export-button" onclick={handleExportStations}>
        <svg class="logo" viewBox="0 0 1309 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" width="200" height="200">
            <path d="M1177.713778 344.945778 740.408889 673.308444l0-230.968889c-418.360889 0-436.878222 253.667556-437.447111 288.199111 0-16.782222 0-82.460444 0-93.44 0-145.009778 84.849778-389.518222 437.447111-389.518222L740.408889 28.444444 1177.713778 344.945778zM355.697778 534.186667c0 0 87.808-140.544 396.458667-140.544 22.584889 0 46.392889 0 46.392889 0l0 170.410667 289.507556-219.107556L798.549333 137.671111l0 164.096c0 0-23.864889 0-46.392889 0C421.063111 301.767111 355.697778 534.186667 355.697778 534.186667zM302.961778 730.538667c0 2.474667 0 3.953778 0 3.953778S302.904889 733.041778 302.961778 730.538667zM186.311111 175.559111l0 706.019556c0 32.540444 26.112 58.823111 58.311111 58.823111l728.974222 0c32.199111 0 58.311111-26.311111 58.311111-58.823111l0-176.526222 58.339556 0 0 176.526222c0 64.967111-52.224 117.674667-116.650667 117.674667L244.650667 999.253333c-64.426667 0-116.650667-52.707556-116.650667-117.674667L128 175.559111c0-64.995556 52.224-117.674667 116.650667-117.674667l262.428444 0 0 58.823111L244.650667 116.707556C212.423111 116.707556 186.311111 143.047111 186.311111 175.559111z"></path>
          </svg>
          <span>导出数据</span>
        </button>
            <button aria-label="添加工位" class="action-button add-button" onclick={()=>showModal(StationForm,{
          label:'添加工位',
          submitHandler:handleStationSubmit,
          onNegative:()=>hideModal()
        })}>
          <svg viewBox="0 0 1024 1024" width="20" height="20">
            <path d="M832 448H576V192a64 64 0 0 0-128 0v256H192a64 64 0 0 0 0 128h256v256a64 64 0 1 0 128 0V576h256a64 64 0 1 0 0-128z"/>
          </svg>
          <span>添加工位</span>
        </button>
      </div>
    </div>
      <SortableTable
        data={stations}
        columns={[
          { key: 'name', label: '名称', sortable: true,maxWidth:'120px'  },
          { key: 'short_name', label: '简称', sortable: true,maxWidth:'100px' },
          { key: 'description', label: '描述' },
          { key: 'photo_path', label: '图片路径',maxWidth:'150px' },
          { key: 'status', label: '状态', sortable: true,maxWidth:'150px',formatter: async (status) => {
            switch (status) {
              case 'in_service': return Promise.resolve('正常');
              case 'maintenance': return Promise.resolve('维护');
              case 'out_of_service': return Promise.resolve('停用');
              case 'calibration':return Promise.resolve('计量');
              default: return Promise.resolve('正常');
            }}
          }
        ]}
        actions={[
          {
            label: '编辑工位',
            class: 'edit',
            handler: (station: Station) =>showModal(StationForm, {
              label: '编辑',
              submitHandler: handleStationSubmit,
              onNegative: () => hideModal(),
              station: {...station}
            })
          },
          {
            label: '拷贝工位',
            class:'insert',
            handler: (station: Station) => {
              showModal(StationForm, {
                label: `Copy ${station.short_name}`,
                submitHandler: handleCopyStation,
                onNegative: () => hideModal(),
                station:{...station,id:0}
              });
            }
          },
          {
            label: '删除',
            class: 'delete',
            handler: (station: Station) => handleStationDelete(station)
          }
        ]}
        draggable={true}
        onRowReorder={async(orders:{id:number,seq:number}[])=>{
          const remote_source=getGlobal("remote_source");
          const settingsStr=await readTextFile(`${remote_source}\\settings.json`);
          const settings=JSON.parse(settingsStr);
          const newSettings={...settings,station_orders:orders};
          console.log(newSettings)
          await writeTextFile(`${remote_source}\\settings.json`,JSON.stringify(newSettings));
          const store=await load("settings.json");
          await store.set("station_orders",orders);
          setGlobal("station_orders",orders);
          
        }}
      />
    {/await}
  </div>

  <style>
    .logo{
    fill: #4a90e2;
    width: 20px;
    height: 20px;
  } 
  /**
   * 限制content的高度
   * 100vh - 200px是header和footer的高度
   * padding-bottom: 20px是为了让最后一行数据可以被看见
   */
    .content {
    max-height: calc(100vh - 200px);
    overflow: hidden;
    padding-bottom: 20px;
  }
  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }
  .button-group {
    display: flex;
    gap: 12px;
    align-items: center;
  }
  .title-group {
    display: flex;
    align-items: center;
    gap: 16px;
  }
  .action-button {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 16px;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    transition: all 0.2s ease;
    fill: #4a90e2;
  }

  .action-button:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }
  </style>