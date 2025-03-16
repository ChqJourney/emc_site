<script lang="ts">
  import { modalStore } from './modalStore';

  
</script>

<div class="about-container">
  <h3>关于 EMC 预约系统</h3>
  
  <div class="content">
    <p class="version">版本：Page version v20250316</p>
    
    <div class="section">
      <h4>系统说明</h4>
      <p>EMC 预约系统是专门为 Intertek Zhejiang EMC实验室开发的预约管理系统。本系统用于查看和管理预约信息。</p>
    </div>

    <div class="section">
      <h4>功能特点</h4>
      <div class="features">
        <ul>
          <li>高度可视化界面</li>
          <li>支持按工位查看预约</li>
          <li>支持按日期查看预约</li>
          <li>预约信息详情展示</li>
          <li>支持数据源快速更换</li>
          <li style="color: orange;">支持预约创建、修改、删除</li>
          <li style="color: orange;">支持多时段预约创建</li>
          <li style="color: orange;">支持工位创建、修改、删除</li>
          <li style="color: orange;">支持工位复制式创建</li>
          <li style="color: orange;">支持工位拖曳排序</li>
          <li style="color: orange;">支持测试内容自定义</li>
          <li style="color: orange;">支持人员自定义</li>
          <li style="color: orange;">支持访问记录查询</li>
          <li style="color: orange;">支持测试工位智能适配</li>
          <li style="color: orange;">支持数据导出</li>
          <li style="color: orange;">支持数据图谱展示（待开发）</li>
        </ul>
        <div style="padding-left: 3rem;">
            <span style="width: 10px; height: 10px; display: inline-block; background-color: orange;"></span>
            <div style="display: inline-block; color: orange;">仅管理员端和工程师端含此功能</div>
      </div>
      </div>
    </div>

    <div class="section">
      <h4>技术支持</h4>
      <p>如有任何问题或建议，请联系系统管理员Leo Ye/Patrick Chen。</p>
    </div>
  </div>

  <footer class="copyright">
   
    <p style="display: flex; align-items: center; justify-content: center;"><svg style="margin-right: 5px; width: 12px;height:12px" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"  width="200" height="200"><path d="M596.1 423c-6.9-45.6-35-69.3-84.4-71h-1.8c-52.3 3.8-80.1 58.1-83.3 163.1v1c3.3 103.4 31.1 156.9 83.4 160.7h1.7c50.7-1.7 79-25.2 84.7-70.6 1-7.7 7.5-13.6 15.3-13.6h70.1c9.2 0 16.4 8.1 15.3 17.3-11.1 86.5-73.2 130.4-186.2 132-120.2-3.2-182-78.4-185.3-225.7v-1c4.9-150.3 66.5-227.9 184.8-232.9h1c111.4 1.7 173.2 47.2 185.5 136.6 1.3 9.2-5.9 17.4-15.3 17.4h-70.2c-7.7-0.1-14.2-5.7-15.3-13.3z"></path><path d="M512 65c60.4 0 118.9 11.8 174 35.1 53.2 22.5 101 54.7 142.1 95.8 41.1 41.1 73.3 88.9 95.8 142.1 23.3 55.1 35.1 113.6 35.1 174 0 60.4-11.8 118.9-35.1 174-22.5 53.2-54.7 101-95.8 142.1-41.1 41.1-88.9 73.3-142.1 95.8-55.1 23.3-113.6 35.1-174 35.1-60.4 0-118.9-11.8-174-35.1-53.2-22.5-101-54.7-142.1-95.8s-73.3-88.9-95.8-142.1C76.8 630.9 65 572.4 65 512c0-60.4 11.8-118.9 35.1-174 22.5-53.2 54.7-101 95.8-142.1 41.1-41.1 88.9-73.3 142.1-95.8C393.1 76.8 451.6 65 512 65m0-65C229.2 0 0 229.2 0 512s229.2 512 512 512 512-229.2 512-512S794.8 0 512 0z" ></path></svg> {new Date().getFullYear()} Intertek Zhejiang EMC Department</p>
    <p class="rights">All Rights Reserved</p>
  </footer>

</div>

<style>
  .about-container {
    color: #333;
    padding: 1rem 0rem;
    overflow: hidden;
    font-family: Arial, sans-serif;
    font-size: 14px;
    margin: 0 auto;
    position: relative;
    width: auto;
  }

  h3 {
    color: #fbc400;
    text-align: center;
    margin-bottom: 1rem;
    font-size: 1.2rem;
    font-weight: 600;
  }

  .content {
    margin-bottom: 0.6rem;
  }

  .version {
    color: #666;
    text-align: center;
    font-size: 0.9rem;
  }

  .section {
    margin-bottom: 0.4rem;
    background: #fff;
    padding: 0.2rem;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.05);
  }

  h4 {
    color: #444;
    margin-bottom: 0.8rem;
    font-size: 1.1rem;
    font-weight: 600;
  }

  .features {
    margin-bottom: 0.4rem;
    max-height: 150px;
    overflow-y: auto;
    padding-right: 10px;
  }

  .features::-webkit-scrollbar {
    width: 6px;
  }

  .features::-webkit-scrollbar-track {
    background: #f1f1f1;
    border-radius: 3px;
  }

  .features::-webkit-scrollbar-thumb {
    background: #fbc400;
    border-radius: 3px;
  }

  ul {
    list-style-type: none;
    margin-left: 0.5rem;
  }

  li {
    margin-bottom: 0.8rem;
    position: relative;
    padding-left: 1.5rem;
  }

  li:before {
    content: "•";
    color: #fbc400;
    font-weight: bold;
    position: absolute;
    left: 0;
  }

  .copyright {
    text-align: center;
    color: #666;
    font-size: 0.8rem;
    margin-top: 1rem;
  }

  .rights {
    color: #999;
    font-size: 0.75rem;
  }

  p {
    line-height: 1.6;
  }
  .close-button {
    position: absolute;
    top: 0;
    right: 0;
    margin: 0.5rem;
    padding: 0.5rem;
    border: none;
    background-color: transparent;
    cursor: pointer;
  }
  .close-button:hover {
    color: #bcb6b6;
  }
</style>