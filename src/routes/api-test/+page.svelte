<script lang="ts">
  import { onMount } from 'svelte';
  import type { Sevent } from '../../biz/types';
  
  let sevents: Sevent[] = [];
  let rawResponse: string = '';
  let error: string = '';
  let stationId: number = 1;
  
  // 直接用fetch测试
  async function testFetch() {
    try {
      error = '';
      rawResponse = '';
      
      const response = await fetch(`http://localhost:5000/api/sevents/station/${stationId}`);
      
      if (!response.ok) {
        error = `HTTP error: ${response.status} ${response.statusText}`;
        return;
      }
      
      const text = await response.text();
      rawResponse = text;
      
      if (!text || text.trim() === '') {
        sevents = [];
        return;
      }
      
      try {
        const data = JSON.parse(text);
        console.log("解析后的数据:", data);
        
        if (Array.isArray(data)) {
          sevents = data;
        } else {
          sevents = [];
          error = "API未返回数组格式";
        }
      } catch (e) {
        error = `解析JSON失败: ${e.message}`;
      }
    } catch (e) {
      error = `网络请求错误: ${e.message}`;
    }
  }
  
  onMount(() => {
    testFetch();
  });
</script>

<div class="container">
  <h1>API测试页面</h1>
  
  <div class="form-group">
    <label for="station-id">站点ID：</label>
    <input type="number" id="station-id" bind:value={stationId} min="1" />
    <button on:click={testFetch}>测试</button>
  </div>
  
  {#if error}
    <div class="error">
      <h3>错误信息</h3>
      <pre>{error}</pre>
    </div>
  {/if}
  
  <div class="response">
    <h3>原始响应</h3>
    <pre>{rawResponse}</pre>
  </div>
  
  <div class="result">
    <h3>解析结果</h3>
    {#if sevents.length > 0}
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>名称</th>
            <th>开始日期</th>
            <th>结束日期</th>
            <th>站点ID</th>
            <th>创建时间</th>
            <th>更新时间</th>
          </tr>
        </thead>
        <tbody>
          {#each sevents as event}
            <tr>
              <td>{event.id}</td>
              <td>{event.name}</td>
              <td>{event.from_date}</td>
              <td>{event.to_date}</td>
              <td>{event.station_id}</td>
              <td>{event.created_on}</td>
              <td>{event.updated_on}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    {:else}
      <p>没有找到事件数据</p>
    {/if}
  </div>
</div>

<style>
  .container {
    padding: 20px;
    max-width: 1200px;
    margin: 0 auto;
  }
  
  .form-group {
    margin-bottom: 20px;
  }
  
  input, button {
    padding: 8px;
    margin-right: 10px;
  }
  
  .error {
    background-color: #ffeeee;
    padding: 10px;
    border-radius: 4px;
    margin-bottom: 20px;
  }
  
  .response, .result {
    background-color: #f5f5f5;
    padding: 10px;
    border-radius: 4px;
    margin-bottom: 20px;
  }
  
  pre {
    white-space: pre-wrap;
    word-break: break-all;
    background-color: #eee;
    padding: 10px;
    border-radius: 4px;
    max-height: 300px;
    overflow: auto;
  }
  
  table {
    width: 100%;
    border-collapse: collapse;
  }
  
  th, td {
    padding: 8px;
    border: 1px solid #ddd;
    text-align: left;
  }
  
  th {
    background-color: #f2f2f2;
  }
</style>
