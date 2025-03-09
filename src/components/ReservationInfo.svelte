<script lang="ts">
    import { apiService } from '../biz/apiService';
    import { errorHandler } from '../biz/errorHandler';
    import type { AppError } from '../biz/errors';
  
    let {reservation}=$props();


  console.log(reservation);
    let showDetails = $state(false);
    
    const formatTimeslot = (timeslot: string): string => {
      switch (timeslot) {
          case 'T1':
              return '9:30-12:00';
          case 'T2':
              return '13:00-15:00';
          case 'T3':
              return '15:00-17:30';
          case 'T4':
              return '18:00-20:30';
          case 'T5':
              return '20:30-23:59';
          default:
              return '未知时间段';
      }
    }
    const formatTime = (date: Date) => {
      
      return date.toString().substring(0, 16);
    }
    const getStationName=async(id:number)=>{
        try{
          const station=await apiService.Get(`/stations/${id}`);
          return station.name
        }catch(e){
            errorHandler.handleError(e as AppError);
        }
    }
</script>

<div class="reservation-info">
  <div class="header">
    <h4>预约详情</h4>
    <button class="detail-btn" onclick={() => showDetails = !showDetails}>
      {showDetails ? '收起' : '展开'}详情
    </button>
  </div>
  
  <div class="info-grid">
    <div class="info-item">
      <span class="label">预约日期：</span>
      <span class="value">{reservation.reservation_date}</span>
    </div>
    <div class="info-item">
      <span class="label">工位：</span>
      {#await getStationName(reservation.station_id)}
        <span class="value">加载中...</span>
      {:then stationName}
        <span class="value">{stationName}</span>
      {:catch error}
        <span class="value">加载失败:{error.message}</span>
      {/await}
    </div>
    
    
    <div class="info-item">
      <span class="label">时间段：</span>
      <span class="value">{formatTimeslot(reservation.time_slot)}</span>
    </div>
    <div class="info-item">
      <span class="label">项目工程师：</span>
      <span class="value">{reservation.project_engineer}</span>
    </div>
    <div class="info-item">
      <span class="label">测试工程师：</span>
      <span class="value">{reservation.testing_engineer}</span>
    </div>
    <div class="info-item">
      <span class="label">项目号：</span>
      <span class="value">{reservation.job_no}</span>
    </div>
    <div class="info-item">
      <span class="label">测试内容：</span>
      <span class="value">{reservation.tests}</span>
    </div>
    <div class="info-item">
      <span class="label">客户名称：</span>
      <span class="value">{reservation.client_name}</span>
    </div>
    {#if showDetails}
    <div class="info-item">
      <span class="label">产品名称：</span>
      <span class="value">{reservation.product_name??""}</span>
    </div>
    <div class="info-item purpose">
      <span class="label">用途描述：</span>
      <span class="value">{reservation.purpose_description??""}</span>
    </div>
    <div class="info-item">
      <span class="label">联系人：</span>
      <span class="value">{reservation.contact_name??""}</span>
    </div>
    
    <div class="info-item">
      <span class="label">联系电话：</span>
      <span class="value">{reservation.contact_phone??""}</span>
    </div>
    <div class="info-item">
      <span class="label">负责销售：</span>
      <span class="value">{reservation.sales??""}</span>
    </div>
    <div class="info-item">
      <span class="label">创建：</span>
      <span class="value">{reservation.reservate_by??""}</span>
    </div>
    <div class="info-item">
      <span class="label">创建时间：</span>
      <span class="value">{formatTime(reservation.created_on)}</span>
    </div>
    <div class="info-item">
      <span class="label">修改时间：</span>
      <span class="value">{formatTime(reservation.updated_on)}</span>
    </div>
    
    {/if}
  </div>
</div>

<style>
  .reservation-info {
    padding: 2.5rem 1.5rem 2.5rem 1.5rem;
    color: #333;
    position: relative;
    font-family: Arial, sans-serif;
    font-size: 14px;
  }

  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
    padding-bottom: 0.5rem;
    border-bottom: 2px solid #fbc400;
  }

  h4 {
    margin: 0;
    color: #fbc400;
    font-size: 1.2rem;
    font-weight: 600;
  }

  .detail-btn {
    background: transparent;
    color: #666;
    border: 1px solid #fbc400;
    padding: 0.3rem 0.8rem;
    border-radius: 4px;
    font-size: 0.9rem;
    cursor: pointer;
    transition: all 0.2s ease;
  }

  .detail-btn:hover {
    background: #fbc400;
    color: white;
  }

  .info-grid {
    display: grid;
    gap: 0.8rem;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  }

  .info-item {
    display: flex;
    align-items: flex-start;
    padding: 0.5rem 0.8rem;
    border-radius: 4px;
    background: #f8f9fa;
    transition: all 0.2s ease;
  }

  .info-item:hover {
    background: #f1f3f5;
  }

  .label {
    color: #666;
    font-size: 0.9rem;
    min-width: 90px;
    padding-right: 0.8rem;
  }

  .value {
    color: #333;
    font-size: 0.9rem;
    word-break: break-word;
  }

  .value.status {
    padding: 0.2rem 0.6rem;
    border-radius: 3px;
    font-size: 0.85rem;
  }

  .status.normal {
    color: #2b8a3e;
    background: #d3f9d8;
  }

  .status.cancelled {
    color: #c92a2a;
    background: #ffe3e3;
  }

  .status.locked {
    color: #e67700;
    background: #fff3bf;
  }

  .purpose {
    grid-column: 1 / -1;
  }

  .purpose .value {
    white-space: pre-wrap;
    line-height: 1.4;
  }

  @media (max-width: 640px) {
    .info-grid {
      grid-template-columns: 1fr;
    }
  }
</style>