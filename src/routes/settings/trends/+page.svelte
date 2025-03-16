<script lang="ts">
  import { getGlobal } from "../../../biz/globalStore";
  import { showModal,hideModal } from "../../../components/modalStore";
  import { calendarPageInit } from "../../../biz/operation";
  import About from "../../../components/About.svelte";
  import { goto } from "$app/navigation";
  import { apiService } from "../../../biz/apiService";
  import { onMount } from 'svelte';
  
  // 导入 echarts 核心模块
  import * as echarts from 'echarts/core';
  import {
    TitleComponent,
    TooltipComponent,
    GridComponent,
    LegendComponent
  } from 'echarts/components';
  import { LineChart, BarChart } from 'echarts/charts';
  import { UniversalTransition } from 'echarts/features';
  import { CanvasRenderer } from 'echarts/renderers';
    import type { Reservation } from "../../../biz/types";

  // 注册必需的组件
  echarts.use([
    TitleComponent,
    TooltipComponent,
    GridComponent,
    LegendComponent,
    LineChart,
    BarChart,
    CanvasRenderer,
    UniversalTransition
  ]);

  // 图表通用配置
  const commonSeriesConfig = {
    connectNulls: true, // 连接空值点
    emphasis: {
      focus: 'series'
    }
  };

  interface AppointmentData {
    date: string;
    projectEngineer: string;
    testEngineer: string;
  }

  let monthlyData: number[] = new Array(12).fill(null);
  let dailyData: number[] = new Array(31).fill(null);
  let peMonthlyData: Record<string, number[]> = {};
  let teMonthlyData: Record<string, number[]> = {};
  let appointments: Reservation[] = [];
  let charts: echarts.EChartsType[] = [];
  let selectedMonth: number | null = new Date().getMonth(); // 默认选择当前月份，但可以为null
  let dailyDataByMonth: Record<number, number[]> = {};
  let selectedYear = new Date().getFullYear(); // 默认选择当前年份
  let monthlyDataByYear: Record<number, number[]> = {};
  let dailyDataByYearMonth: Record<string, number[]> = {};
  let peDailyData: Record<string, number[]> = {};
  let teDailyData: Record<string, number[]> = {};

  async function fetchAppointmentData() {
    try {
      const response = await apiService.Get(`/reservations/year/${selectedYear}`);
      appointments = response;
      processData(appointments);
      initCharts();
    } catch (error) {
      console.error('获取预约数据失败:', error);
    }
  }

  function processData(data: Reservation[]) {
    const currentDate = new Date();
    const currentYear = currentDate.getFullYear();
    const currentMonth = currentDate.getMonth();

    // 重置数据
    monthlyData = new Array(12).fill(null);
    dailyData = new Array(31).fill(null);
    peMonthlyData = {};
    teMonthlyData = {};
    peDailyData = {};
    teDailyData = {};
    monthlyDataByYear[selectedYear] = new Array(12).fill(null);

    // 初始化每月的日度数据
    for (let month = 0; month < 12; month++) {
      const yearMonthKey = `${selectedYear}-${month}`;
      // 只初始化当前月份及之前月份的数据
      if (selectedYear < currentYear || (selectedYear === currentYear && month <= currentMonth)) {
        monthlyDataByYear[selectedYear][month] = 0;
        dailyDataByYearMonth[yearMonthKey] = new Array(31).fill(0);
      } else {
        monthlyDataByYear[selectedYear][month] = null;
        dailyDataByYearMonth[yearMonthKey] = new Array(31).fill(null);
      }
    }

    // 处理每条预约数据
    data.forEach(appointment => {
      const date = new Date(appointment.reservation_date);
      const month = date.getMonth();
      const day = date.getDate() - 1;
      const yearMonthKey = `${selectedYear}-${month}`;

      // 只处理当前月份及之前的数据
      if (selectedYear < currentYear || (selectedYear === currentYear && month <= currentMonth)) {
        // 更新月度统计
        monthlyDataByYear[selectedYear][month]++;
        
        // 按月份更新日度统计
        if (dailyDataByYearMonth[yearMonthKey]) {
          dailyDataByYearMonth[yearMonthKey][day]++;
        }

        // 更新PE统计
        if (!peMonthlyData[appointment.project_engineer]) {
          peMonthlyData[appointment.project_engineer] = new Array(12).fill(null).map((_, i) => 
            selectedYear < currentYear || (selectedYear === currentYear && i <= currentMonth) ? 0 : null
          );
          peDailyData[appointment.project_engineer] = new Array(31).fill(0);
        }
        peMonthlyData[appointment.project_engineer][month]++;
        
        // 只在选择了对应月份时更新日度数据
        if (month === selectedMonth) {
          peDailyData[appointment.project_engineer][day]++;
        }

        // 更新TE统计
        if (!teMonthlyData[appointment.testing_engineer]) {
          teMonthlyData[appointment.testing_engineer] = new Array(12).fill(null).map((_, i) => 
            selectedYear < currentYear || (selectedYear === currentYear && i <= currentMonth) ? 0 : null
          );
          teDailyData[appointment.testing_engineer] = new Array(31).fill(0);
        }
        teMonthlyData[appointment.testing_engineer][month]++;
        
        // 只在选择了对应月份时更新日度数据
        if (month === selectedMonth) {
          teDailyData[appointment.testing_engineer][day]++;
        }
      }
    });
  }

  function initCharts() {
    const containers = document.querySelectorAll('.chart-container');
    charts.forEach(chart => chart?.dispose());
    charts = [];

    // 月度趋势图
    const monthlyChart = echarts.init(containers[0] as HTMLElement);
    monthlyChart.setOption({
      title: {
        text: `${selectedYear}年月度预约趋势`
      },
      tooltip: {
        trigger: 'axis'
      },
      xAxis: {
        type: 'category',
        data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月']
      },
      yAxis: {
        type: 'value'
      },
      series: [{
        data: monthlyDataByYear[selectedYear],
        type: 'line',
        smooth: true,
        connectNulls: true
      }]
    });
    charts.push(monthlyChart);

    // 日度分布图
    const dailyChart = echarts.init(containers[1] as HTMLElement);
    dailyChart.setOption({
      title: {
        text: `${selectedYear}年${selectedMonth !== null ? selectedMonth + 1 : '全年'}月日度预约分布`
      },
      tooltip: {
        trigger: 'axis'
      },
      xAxis: {
        type: 'category',
        data: Array.from({length: 31}, (_, i) => `${i + 1}日`)
      },
      yAxis: {
        type: 'value'
      },
      series: [{
        data: dailyDataByYearMonth[`${selectedYear}-${selectedMonth}`] || new Array(31).fill(0),
        type: 'bar'
      }]
    });
    charts.push(dailyChart);

    // PE月度统计图
    const peChart = echarts.init(containers[2] as HTMLElement);
    peChart.setOption({
      title: {
        text: `${selectedYear}年Project Engineer月度统计`
      },
      tooltip: {
        trigger: 'axis'
      },
      legend: {
        type: 'scroll',
        orient: 'vertical',
        right: 10,
        top: 20,
        bottom: 20,
      },
      xAxis: {
        type: 'category',
        data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月']
      },
      yAxis: {
        type: 'value',
        max:100
      },
      series: Object.entries(peMonthlyData).map(([name, data]) => ({
        name,
        type: 'line',
        data,
        ...commonSeriesConfig
      }))
    });
    charts.push(peChart);

    // TE月度统计图
    const teChart = echarts.init(containers[4] as HTMLElement);
    teChart.setOption({
      title: {
        text: `${selectedYear}年Test Engineer月度统计`
      },
      tooltip: {
        trigger: 'axis'
      },
      legend: {
        type: 'scroll',
        orient: 'vertical',
        right: 10,
        top: 20,
        bottom: 20,
      },
      xAxis: {
        type: 'category',
        data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月']
      },
      yAxis: {
        type: 'value'
      },
      series: Object.entries(teMonthlyData).map(([name, data]) => ({
        name,
        type: 'line',
        data,
        ...commonSeriesConfig
      }))
    });
    charts.push(teChart);

    // PE日度统计图
    const peDailyChart = echarts.init(containers[3] as HTMLElement);
    peDailyChart.setOption({
      title: {
        text: selectedMonth === null 
          ? `请选择月份查看Project Engineer日度统计` 
          : `${selectedYear}年${selectedMonth + 1}月Project Engineer日度统计`
      },
      tooltip: {
        trigger: 'axis'
      },
      legend: {
        type: 'scroll',
        orient: 'vertical',
        right: 10,
        top: 20,
        bottom: 20,
      },
      grid: {
        right: '15%'
      },
      xAxis: {
        type: 'category',
        data: Array.from({length: 31}, (_, i) => `${i + 1}日`)
      },
      yAxis: {
        type: 'value'
      },
      series: Object.entries(peDailyData).map(([name, data]) => ({
        name,
        type: 'bar',
        stack: 'total',
        data: selectedMonth !== null ? data : new Array(31).fill(0)
      }))
    });
    charts.push(peDailyChart);

    // TE日度统计图
    const teDailyChart = echarts.init(containers[5] as HTMLElement);
    teDailyChart.setOption({
      title: {
        text: selectedMonth === null 
          ? `请选择月份查看Test Engineer日度统计` 
          : `${selectedYear}年${selectedMonth + 1}月Test Engineer日度统计`
      },
      tooltip: {
        trigger: 'axis'
      },
      legend: {
        type: 'scroll',
        orient: 'vertical',
        right: 10,
        top: 20,
        bottom: 20,
      },
      grid: {
        right: '15%'
      },
      xAxis: {
        type: 'category',
        data: Array.from({length: 31}, (_, i) => `${i + 1}日`)
      },
      yAxis: {
        type: 'value'
      },
      series: Object.entries(teDailyData).map(([name, data]) => ({
        name,
        type: 'bar',
        stack: 'total',
        data: selectedMonth !== null ? data : new Array(31).fill(0)
      }))
    });
    charts.push(teDailyChart);
  }

  function updateMonthlyChart() {
    const monthlyChart = charts[0];
    if (monthlyChart) {
      monthlyChart.setOption({
        title: {
          text: `${selectedYear}年月度预约趋势`
        },
        series: [{
          data: monthlyDataByYear[selectedYear],
          type: 'line',
          smooth: true,
          connectNulls: true
        }]
      });
    }
  }

  function updateDailyChart() {
    const dailyChart = charts[1];
    if (dailyChart) {
      const yearMonthKey = `${selectedYear}-${selectedMonth}`;
      dailyChart.setOption({
        title: {
          text: `${selectedYear}年${selectedMonth !== null ? selectedMonth + 1 : '全年'}月日度预约分布`
        },
        series: [{
          data: dailyDataByYearMonth[yearMonthKey] || new Array(31).fill(0),
          type: 'bar'
        }]
      });
    }
  }

  function updatePEChart() {
    const peChart = charts[2];
    if (peChart) {
      peChart.setOption({
        title: {
          text: `${selectedYear}年Project Engineer月度统计`
        },
        series: Object.entries(peMonthlyData).map(([name, data]) => ({
          name,
          type: 'line',
          data,
          ...commonSeriesConfig
        }))
      });
    }
  }

  function updateTEChart() {
    const teChart = charts[3];
    if (teChart) {
      teChart.setOption({
        title: {
          text: `${selectedYear}年Test Engineer月度统计`
        },
        series: Object.entries(teMonthlyData).map(([name, data]) => ({
          name,
          type: 'line',
          data,
          ...commonSeriesConfig
        }))
      });
    }
  }

  function updatePEDailyChart() {
    const peDailyChart = charts[4];
    if (peDailyChart) {
      // 只在选择了月份时显示日度数据
      const yearMonthKey = `${selectedYear}-${selectedMonth}`;
      const dailyData = selectedMonth !== null 
        ? peDailyData 
        : Object.fromEntries(Object.keys(peDailyData).map(key => [key, new Array(31).fill(0)]));

      peDailyChart.setOption({
        title: {
          text: selectedMonth === null 
            ? `请选择月份查看Project Engineer日度统计` 
            : `${selectedYear}年${selectedMonth + 1}月Project Engineer日度统计`
        },
        series: Object.entries(dailyData).map(([name, data]) => ({
          name,
          type: 'bar',
          stack: 'total',
          data: selectedMonth !== null ? data : new Array(31).fill(0)
        }))
      });
    }
  }

  function updateTEDailyChart() {
    const teDailyChart = charts[5];
    if (teDailyChart) {
      // 只在选择了月份时显示日度数据
      const yearMonthKey = `${selectedYear}-${selectedMonth}`;
      const dailyData = selectedMonth !== null 
        ? teDailyData 
        : Object.fromEntries(Object.keys(teDailyData).map(key => [key, new Array(31).fill(0)]));

      teDailyChart.setOption({
        title: {
          text: selectedMonth === null 
            ? `请选择月份查看Test Engineer日度统计` 
            : `${selectedYear}年${selectedMonth + 1}月Test Engineer日度统计`
        },
        series: Object.entries(dailyData).map(([name, data]) => ({
          name,
          type: 'bar',
          stack: 'total',
          data: selectedMonth !== null ? data : new Array(31).fill(0)
        }))
      });
    }
  }

  function updateAllCharts() {
    updateMonthlyChart();
    updateDailyChart();
    updatePEChart();
    updateTEChart();
    updatePEDailyChart();
    updateTEDailyChart();
  }

  $: selectedYear !== undefined && fetchAppointmentData();
  $: selectedMonth !== undefined && updateAllCharts();

  onMount(() => {
    fetchAppointmentData();
    
    // 监听窗口大小变化，重新调整图表大小
    const resizeHandler = () => {
      charts.forEach(chart => chart?.resize());
    };
    window.addEventListener('resize', resizeHandler);

    return () => {
      window.removeEventListener('resize', resizeHandler);
      charts.forEach(chart => chart?.dispose());
    };
  });

  async function logout() {
    await apiService.logout();
    goto("/auth/login");
  }
</script>

<div class="trends-container">
  <div class="bg-white p-1 lg:p-4 mb-4 rounded-lg shadow">
    <div class="flex gap-4 items-center">
      <select 
        bind:value={selectedYear}
        class="block w-40 px-3 py-2 bg-white border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500"
      >
        {#each Array.from({length: 5}, (_, i) => new Date().getFullYear() - 2 + i) as year}
          <option value={year}>{year}年</option>
        {/each}
      </select>
      <select 
        bind:value={selectedMonth}
        class="block w-40 px-3 py-2 bg-white border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500"
      >
        <option value={null}>全年</option>
        {#each Array.from({length: 12}, (_, i) => i) as month}
          <option value={month}>{month + 1}月</option>
        {/each}
      </select>
    </div>
  </div>
  
  <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 p-1 lg:p-4">
    <div class="bg-white p-1 lg:p-4 rounded-lg shadow">
      <div class="chart-container" style="width: 100%; height: 400px;"></div>
    </div>
    <div class="bg-white p-1 lg:p-4 rounded-lg shadow">
      <div class="chart-container" style="width: 100%; height: 400px;"></div>
    </div>
    <div class="bg-white p-1 lg:p-4 rounded-lg shadow">
      <div class="chart-container" style="width: 100%; height: 400px;"></div>
    </div>
    <div class="bg-white p-1 lg:p-4 rounded-lg shadow">
      <div class="chart-container" style="width: 100%; height: 400px;"></div>
    </div>
    <div class="bg-white p-1 lg:p-4 rounded-lg shadow">
      <div class="chart-container" style="width: 100%; height: 400px;"></div>
    </div>
    <div class="bg-white p-1 lg:p-4 rounded-lg shadow">
      <div class="chart-container" style="width: 100%; height: 400px;"></div>
    </div>
  </div>
</div>

<style>
  .trends-container {
    width: 100%;
    height: 100vh;
    background-color: #f5f5f5;
    overflow-y: auto;
    padding: 0.5rem;
    padding-bottom: 6rem;
  }
</style>
